#ifndef COLOR_UTILS_HLSL
#define COLOR_UTILS_HLSL

float GetLuminance(float3 rgb)
{
    return dot(saturate(rgb), float3(0.2126, 0.7152, 0.0722));
}

float3 RGBToHSV(float3 rgb)
{
    float maxC = max(max(rgb.r, rgb.g), rgb.b);
    float minC = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maxC - minC;
    
    float v = maxC;
    float s = (maxC > 1e-10) ? (delta / maxC) : 0.0;
    
    float h = 0.0;
    if (delta > 1e-10)
    {
        float3 hueVec = (rgb.gbr - rgb.brg) / delta;
        float3 hueTest = float3(
            step(maxC - 1e-10, rgb.r) * step(rgb.r, maxC + 1e-10),
            step(maxC - 1e-10, rgb.g) * step(rgb.g, maxC + 1e-10),
            step(maxC - 1e-10, rgb.b) * step(rgb.b, maxC + 1e-10)
        );
        
        h = dot(hueTest, hueVec + float3(0.0, 2.0, 4.0));
        h = frac(h / 6.0 + 1.0);
    }
    
    return float3(h, s, v);
}

float3 HSVToRGB(float3 hsv)
{
    float hue = frac(hsv.x) * 6.0;
    float sat = saturate(hsv.y);
    float val = saturate(hsv.z);
    
    float c = val * sat;
    float x = c * (1.0 - abs(fmod(hue, 2.0) - 1.0));
    float m = val - c;
    
    float3 rgb1 = float3(c, x, 0.0);
    float3 rgb2 = float3(x, c, 0.0);
    float3 rgb3 = float3(0.0, c, x);
    float3 rgb4 = float3(0.0, x, c);
    float3 rgb5 = float3(x, 0.0, c);
    float3 rgb6 = float3(c, 0.0, x);
    
    float3 rgb = rgb1 * step(hue, 1.0) +
                 rgb2 * step(1.0, hue) * step(hue, 2.0) +
                 rgb3 * step(2.0, hue) * step(hue, 3.0) +
                 rgb4 * step(3.0, hue) * step(hue, 4.0) +
                 rgb5 * step(4.0, hue) * step(hue, 5.0) +
                 rgb6 * step(5.0, hue);
    
    return saturate(rgb + m);
}

float3 HueShift(float3 rgb, float hueShift)
{
    float3 hsv = RGBToHSV(rgb);
    hsv.x = frac(hsv.x + hueShift);
    return HSVToRGB(hsv);
}

float3 AdjustSaturation(float3 rgb, float satMult)
{
    float lum = GetLuminance(rgb);
    satMult = max(0.0, satMult);
    return lerp(float3(lum, lum, lum), rgb, satMult);
}

float3 AdjustContrast(float3 rgb, float contrast)
{
    contrast = max(0.0, contrast);
    return saturate((rgb - 0.5) * contrast + 0.5);
}

float LerpHue(float hue1, float hue2, float alpha)
{
    float difference = frac(hue2 - hue1 + 0.5) - 0.5;
    return frac(hue1 + difference * alpha);
}

float3 LerpHSV(float3 hsv1, float3 hsv2, float alpha)
{
    return float3(
        LerpHue(hsv1.x, hsv2.x, alpha),
        lerp(hsv1.y, hsv2.y, alpha),
        lerp(hsv1.z, hsv2.z, alpha)
    );
}

// Unity Shader Graph wrappers
void GetLuminance_float(float3 rgb, out float luminance)
{
    luminance = GetLuminance(rgb);
}

void RGBToHSV_float(float3 rgb, out float3 hsv)
{
    hsv = RGBToHSV(rgb);
}

void HSVToRGB_float(float3 hsv, out float3 rgb)
{
    rgb = HSVToRGB(hsv);
}

void HueShift_float(float3 rgb, float shift, out float3 result)
{
    result = HueShift(rgb, shift);
}

void AdjustSaturation_float(float3 rgb, float saturation, out float3 result)
{
    result = AdjustSaturation(rgb, saturation);
}

void AdjustContrast_float(float3 rgb, float contrast, out float3 result)
{
    result = AdjustContrast(rgb, contrast);
}

void LerpHSV_float(float3 hsv1, float3 hsv2, float alpha, out float3 result)
{
    result = LerpHSV(hsv1, hsv2, alpha);
}

#endif // COLOR_UTILS_HLSL