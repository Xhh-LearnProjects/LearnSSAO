#ifndef SCREEN_SPACE_OCCLUSION_BLUR_INCLUDED
#define SCREEN_SPACE_OCCLUSION_BLUR_INCLUDED



float4 FragBlurH(Varyings input) : SV_Target
{
    return 1;
    // return FragBlur(input, half2(1, 0));

}
float4 FragBlurV(Varyings input) : SV_Target
{
    return 1;
    // return FragBlur(input, half2(0, 1));

}


#endif // SCREEN_SPACE_OCCLUSION_BLUR_INCLUDED
