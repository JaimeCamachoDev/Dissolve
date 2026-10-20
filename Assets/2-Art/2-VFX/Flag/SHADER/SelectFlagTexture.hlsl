void SelectFlagTexture_float(float option, UnityTexture2D atlas1, UnityTexture2D atlas2, UnityTexture2D atlas3, float2 UV, out float4 selectedAtlas)
{
    switch (option)
    {
        case 1:
            selectedAtlas = SAMPLE_TEXTURE2D(atlas1, atlas1.samplerstate, UV);
            break;
        case 2:
            selectedAtlas = SAMPLE_TEXTURE2D(atlas2, atlas2.samplerstate, UV);
            break;
        case 3:
            selectedAtlas = SAMPLE_TEXTURE2D(atlas3, atlas3.samplerstate, UV);
            break;
        default:
            selectedAtlas = SAMPLE_TEXTURE2D(atlas1, atlas1.samplerstate, UV);
            break;
    }
    
}
//void SelectFlagTexture_float(float option, UnityTexture2D atlas1, float2 UV, out float4 selectedAtlas)
//{
//    selectedAtlas = SAMPLE_TEXTURE2D(atlas1, atlas1.samplerstate, UV);    
//}
