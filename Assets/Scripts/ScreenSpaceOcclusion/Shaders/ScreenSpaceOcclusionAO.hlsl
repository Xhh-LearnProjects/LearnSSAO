#ifndef SCREEN_SPACE_OCCLUSION_AO_INCLUDED
#define SCREEN_SPACE_OCCLUSION_AO_INCLUDED



float4 FragAO(Varyings input) : SV_Target
{
    float2 uv = input.texcoord;
    
    return float4(uv, 0, 1);
}


#endif // SCREEN_SPACE_OCCLUSION_AO_INCLUDED
