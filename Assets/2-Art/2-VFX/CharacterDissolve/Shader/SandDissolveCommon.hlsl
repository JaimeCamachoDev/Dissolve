#ifndef VZ_SAND_DISSOLVE_COMMON_INCLUDED
#define VZ_SAND_DISSOLVE_COMMON_INCLUDED

// Disolve de arena por altura en espacio mundo.
// Lo visible es lo que queda por encima del borde (_DissolveHeight): al aparecer el borde baja
// (las cabezas se definen primero) y al desaparecer sube (los pies se deshacen primero).
// VFX_Sand_CharacterDissolve evalua el mismo SD_EdgeDistance para emitir la arena en ese borde.

#include "SandDissolveNoise.hlsl"

// Distancia (m) por encima del borde. < 0 -> ya es arena (se descarta).
float SandDissolveDistance(float3 positionWS, float2 uv)
{
    // Grano fino en UV: estable sobre la piel/tela aunque el personaje se mueva
    float g = SD_Hash31(float3(floor(uv * _DissolveGrainScale), 3.0));
    return SD_EdgeDistance(positionWS, _DissolveHeight, _DissolveDirection, _DissolveNoiseScale, _DissolveNoiseAmount)
         + (g - 0.5) * _DissolveGrainAmount;
}

void SandDissolveClip(float3 positionWS, float2 uv)
{
    clip(SandDissolveDistance(positionWS, uv));
}

#endif
