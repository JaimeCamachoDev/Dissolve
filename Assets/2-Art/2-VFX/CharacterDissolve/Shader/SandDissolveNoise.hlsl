#ifndef VZ_SAND_DISSOLVE_NOISE_INCLUDED
#define VZ_SAND_DISSOLVE_NOISE_INCLUDED

// Ruido compartido por el shader "VZ/Character Sand Dissolve" y VFX_Sand_CharacterDissolve:
// asi la arena del VFX nace exactamente en el borde irregular que recorta el material.

float SD_Hash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float SD_ValueNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = SD_Hash31(i);
    float n100 = SD_Hash31(i + float3(1, 0, 0));
    float n010 = SD_Hash31(i + float3(0, 1, 0));
    float n110 = SD_Hash31(i + float3(1, 1, 0));
    float n001 = SD_Hash31(i + float3(0, 0, 1));
    float n101 = SD_Hash31(i + float3(1, 0, 1));
    float n011 = SD_Hash31(i + float3(0, 1, 1));
    float n111 = SD_Hash31(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
}

// Distancia (m) por encima del borde sin el grano UV: (y - altura) + ruido de forma en [-amount, amount]
float SD_EdgeDistance(float3 positionWS, float height, float direction, float noiseScale, float noiseAmount)
{
    float3 p = positionWS * noiseScale;
    float n = SD_ValueNoise(p) * 0.65 + SD_ValueNoise(p * 2.7 + 17.0) * 0.35;
    return (positionWS.y - height) * direction + (n - 0.5) * 2.0 * noiseAmount;
}

#endif
