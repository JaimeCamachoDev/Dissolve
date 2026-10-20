#ifndef VZ_SAND_DISSOLVE_PASSES_INCLUDED
#define VZ_SAND_DISSOLVE_PASSES_INCLUDED

// Passes del Lit de URP con el clip del disolve de arena.
// SAND_DISSOLVE_FORWARD / SAND_DISSOLVE_SHADOW / SAND_DISSOLVE_DEPTH / SAND_DISSOLVE_DEPTHNORMALS seleccionan el pass.

#if defined(SAND_DISSOLVE_FORWARD)
// ---------------------------------------------------------------------------------------------
// ForwardLit: se reutiliza el vertex de URP y se reescribe el fragment para teñir de arena y
// encender el borde antes de iluminar.
#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#define LitPassFragment LitPassFragment_URP
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#undef LitPassFragment
#include "SandDissolveCommon.hlsl"

void SandDissolveForwardFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float d = SandDissolveDistance(input.positionWS, input.uv);
    clip(d);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    // Cerca del borde la superficie ya es arena: se tiñe, pierde brillo y el filo brilla dorado.
    // _DissolveColorOffset > 0 (solve): una franja de ese alto por encima del borde es "estatua de arena"
    // (forma completa, sin color) y el color real vuelve por detras.
    half sandT = 1.0h - saturate((d - _DissolveColorOffset) / max(_DissolveSandWidth, 1e-4));
    sandT *= sandT;
    half grain = SD_Hash31(float3(floor(input.uv * _DissolveGrainScale * 0.5), 7.0));
    half3 sandAlbedo = _DissolveSandColor.rgb * lerp(0.78h, 1.15h, grain);
    surfaceData.albedo = lerp(surfaceData.albedo, sandAlbedo, sandT * _DissolveSandColor.a);
    surfaceData.smoothness *= 1.0h - sandT;
    surfaceData.metallic *= 1.0h - sandT;
    half edgeT = 1.0h - saturate(d / max(_DissolveEdgeWidth, 1e-4));
    surfaceData.emission += _DissolveEdgeColor.rgb * (edgeT * edgeT * edgeT);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
#endif // SAND_DISSOLVE_FORWARD

#if defined(SAND_DISSOLVE_SHADOW) || defined(SAND_DISSOLVE_DEPTH) || defined(SAND_DISSOLVE_DEPTHNORMALS)
// ---------------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(SAND_DISSOLVE_SHADOW)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#endif
#if defined(LOD_FADE_CROSSFADE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "SandDissolveCommon.hlsl"

#if defined(SAND_DISSOLVE_SHADOW)
float3 _LightDirection;
float3 _LightPosition;
#endif

struct SDAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SDVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3  normalWS   : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

SDVaryings SandDissolveDepthVertex(SDAttributes input)
{
    SDVaryings output = (SDVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = positionWS;
    output.normalWS = half3(normalWS);

#if defined(SAND_DISSOLVE_SHADOW)
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(positionCS);
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif
    return output;
}

half4 SandDissolveDepthFragment(SDVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SandDissolveClip(input.positionWS, input.uv);

#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif

#if defined(SAND_DISSOLVE_DEPTHNORMALS)
    // Normal de la geometria (sin normal map): solo alimenta SSAO y similares
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
        return half4(PackFloat2To888(remappedOctNormalWS), 0.0);
    #else
        return half4(normalWS, 0.0);
    #endif
#elif defined(SAND_DISSOLVE_DEPTH)
    return input.positionCS.z;
#else
    return 0;
#endif
}
#endif

#endif
