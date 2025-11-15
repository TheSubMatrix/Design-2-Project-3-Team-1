float3 RGBToHSL(float3 rgb)
{
    float maxC = max(max(rgb.r, rgb.g), rgb.b);
    float minC = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maxC - minC;
    
    float l = (maxC + minC) * 0.5;
    float s = delta / (1.0 - abs(2.0 * l - 1.0) + 1e-10);
    
    float3 hueVec = (rgb.gbr - rgb.brg) / (delta + 1e-10);
    float3 hueTest = float3(
        step(maxC, rgb.r + 1e-10) * step(rgb.r, maxC + 1e-10),
        step(maxC, rgb.g + 1e-10) * step(rgb.g, maxC + 1e-10),
        step(maxC, rgb.b + 1e-10) * step(rgb.b, maxC + 1e-10)
    );
    
    float h = dot(hueTest, hueVec + float3(0.0, 2.0, 4.0));
    h = frac(h / 6.0 + 1.0);
    
    s = lerp(0.0, s, step(1e-10, delta));
    
    return float3(h, s, l);
}

float3 HSLToRGB(float3 hsl)
{
    float h = hsl.x * 6.0;
    float s = hsl.y;
    float l = hsl.z;
    
    float c = (1.0 - abs(2.0 * l - 1.0)) * s;
    float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
    float m = l - c * 0.5;
    
    float3 rgb1 = float3(c, x, 0.0);
    float3 rgb2 = float3(x, c, 0.0);
    float3 rgb3 = float3(0.0, c, x);
    float3 rgb4 = float3(0.0, x, c);
    float3 rgb5 = float3(x, 0.0, c);
    float3 rgb6 = float3(c, 0.0, x);
    
    float3 rgb = rgb1 * step(h, 1.0) +
                 rgb2 * step(1.0, h) * step(h, 2.0) +
                 rgb3 * step(2.0, h) * step(h, 3.0) +
                 rgb4 * step(3.0, h) * step(h, 4.0) +
                 rgb5 * step(4.0, h) * step(h, 5.0) +
                 rgb6 * step(5.0, h);
    
    return rgb + m;
}

float Luminance(float3 rgb)
{
    return dot(rgb, float3(0.2126, 0.7152, 0.0722));
}

float3 RGBToHSV(float3 rgb)
{
    float maxC = max(max(rgb.r, rgb.g), rgb.b);
    float minC = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maxC - minC;
    
    float v = maxC;
    float s = delta / (maxC + 1e-10);
    
    float3 hueVec = (rgb.gbr - rgb.brg) / (delta + 1e-10);
    float3 hueTest = float3(
        step(maxC, rgb.r + 1e-10) * step(rgb.r, maxC + 1e-10),
        step(maxC, rgb.g + 1e-10) * step(rgb.g, maxC + 1e-10),
        step(maxC, rgb.b + 1e-10) * step(rgb.b, maxC + 1e-10)
    );
    
    float h = dot(hueTest, hueVec + float3(0.0, 2.0, 4.0));
    h = frac(h / 6.0 + 1.0);
    
    s = lerp(0.0, s, step(1e-10, delta));
    
    return float3(h, s, v);
}

float3 HSVToRGB(float3 hsv)
{
    float h = hsv.x * 6.0;
    float s = hsv.y;
    float v = hsv.z;
    
    float c = v * s;
    float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
    float m = v - c;
    
    float3 rgb1 = float3(c, x, 0.0);
    float3 rgb2 = float3(x, c, 0.0);
    float3 rgb3 = float3(0.0, c, x);
    float3 rgb4 = float3(0.0, x, c);
    float3 rgb5 = float3(x, 0.0, c);
    float3 rgb6 = float3(c, 0.0, x);
    
    float3 rgb = rgb1 * step(h, 1.0) +
                 rgb2 * step(1.0, h) * step(h, 2.0) +
                 rgb3 * step(2.0, h) * step(h, 3.0) +
                 rgb4 * step(3.0, h) * step(h, 4.0) +
                 rgb5 * step(4.0, h) * step(h, 5.0) +
                 rgb6 * step(5.0, h);
    
    return rgb + m;
}

float3 HueShift(float3 rgb, float hueShift)
{
    float3 hsl = RGBToHSL(rgb);
    hsl.x = frac(hsl.x + hueShift);
    return HSLToRGB(hsl);
}

float3 AdjustSaturation(float3 rgb, float satMult)
{
    float lum = Luminance(rgb);
    return lerp(float3(lum, lum, lum), rgb, satMult);
}

float3 AdjustContrast(float3 rgb, float contrast)
{
    return (rgb - 0.5) * contrast + 0.5;
}

float3 LinearToSRGB(float3 linearColor)
{
    float3 low = linearColor * 12.92;
    float3 high = 1.055 * pow(max(linearColor, 0.0), 1.0 / 2.4) - 0.055;
    float3 useHigh = step(0.0031308, linearColor);
    return low * (1.0 - useHigh) + high * useHigh;
}

float3 SRGBToLinear(float3 srgbColor)
{
    float3 low = srgbColor / 12.92;
    float3 high = pow(max((srgbColor + 0.055) / 1.055, 0.0), 2.4);
    float3 useHigh = step(0.04045, srgbColor);
    return low * (1.0 - useHigh) + high * useHigh;
}

// ===== Wrappers for use in the Unity shader graph =====

void RGBToHSL_float(float3 rgb, out float3 hsl)
{
    hsl = RGBToHSL(rgb);
}

void HSLToRGB_float(float3 hsl, out float3 rgb)
{
    rgb = HSLToRGB(hsl);
}

void RGBToHSL_half(half3 rgb, out half3 hsl)
{
    hsl = RGBToHSL(rgb);
}

void HSLToRGB_half(half3 hsl, out half3 rgb)
{
    rgb = HSLToRGB(hsl);
}

void Luminance_float(float3 rgb, out float luminance)
{
    luminance = Luminance(rgb);
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

void LinearToSRGB_float(float3 linearColor, out float3 srgb)
{
    srgb = LinearToSRGB(linearColor);
}

void SRGBToLinear_float(float3 srgb, out float3 linearColor)
{
    linearColor = SRGBToLinear(srgb);
}