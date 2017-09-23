using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public static class OpenClKernels
    {
        private static readonly string RGBToLab = @"

kernel void RGBToLab(global int* rgbPixels, global float* labPixels, float maxColorNumber)
{
    int index = get_global_id(0) * 3;

    float red   = convert_float_rtp(rgbPixels[index + 0]);
    float green = convert_float_rtp(rgbPixels[index + 1]);
    float blue  = convert_float_rtp(rgbPixels[index + 2]);


    //First convert from RGB to XYZ
    float sR = red   / maxColorNumber;
    float sG = green / maxColorNumber;
    float sB = blue  / maxColorNumber;

    float rLinear = (sR > 0.04045) ? pow((sR + 0.055) / 1.055, 2.4) : sR / 12.92;
    float gLinear = (sG > 0.04045) ? pow((sG + 0.055) / 1.055, 2.4) : sG / 12.92;
    float bLinear = (sB > 0.04045) ? pow((sB + 0.055) / 1.055, 2.4) : sB / 12.92;

    float X = rLinear * 0.4124 + gLinear * 0.3576 + bLinear * 0.1805;
    float Y = rLinear * 0.2126 + gLinear * 0.7152 + bLinear * 0.0722;
    float Z = rLinear * 0.0193 + gLinear * 0.1192 + bLinear * 0.9505;


    //Then convert from XYZ to Lab
    const float xRef = 0.95047;
    const float yRef = 1.00;
    const float zRef = 1.08883;

    float xReffed = X / xRef;
    float yReffed = Y / yRef;
    float zReffed = Z / zRef;

    float xF = (xReffed > 0.008856) ? pow(xReffed, 1 / 3.0f) : (7.787 * xReffed) + (4 / 29.0);
    float yF = (yReffed > 0.008856) ? pow(yReffed, 1 / 3.0f) : (7.787 * yReffed) + (4 / 29.0);
    float zF = (zReffed > 0.008856) ? pow(zReffed, 1 / 3.0f) : (7.787 * zReffed) + (4 / 29.0);

    float L = 116 * yF - 16;
    float a = 500 * (xF - yF);
    float b = 200 * (yF - zF);


    labPixels[index + 0] = L;
    labPixels[index + 1] = a;
    labPixels[index + 2] = b;
}";

        private static readonly string LabDistance = @"
kernel void RGBToLab(global int* rgbPixels, global float* labPixels, global float maxColorNumber)
{
    int index = get_global_id(0) * 3;

    float red   = convert_float_rtp(rgbPixels[index + 0]);
    float green = convert_float_rtp(rgbPixels[index + 1]);
    float blue  = convert_float_rtp(rgbPixels[index + 2]);


    //First convert from RGB to XYZ
    float sR = red   / maxColorNumber;
    float sG = green / maxColorNumber;
    float sB = blue  / maxColorNumber;

    float rLinear = (sR > 0.04045) ? pow((sR + 0.055) / 1.055, 2.4) : sR / 12.92;
    float gLinear = (sG > 0.04045) ? pow((sG + 0.055) / 1.055, 2.4) : sG / 12.92;
    float bLinear = (sB > 0.04045) ? pow((sB + 0.055) / 1.055, 2.4) : sB / 12.92;

    float X = rLinear * 0.4124 + gLinear * 0.3576 + bLinear * 0.1805;
    float Y = rLinear * 0.2126 + gLinear * 0.7152 + bLinear * 0.0722;
    float Z = rLinear * 0.0193 + gLinear * 0.1192 + bLinear * 0.9505;


    //Then convert from XYZ to Lab
    const float xRef = 0.95047;
    const float yRef = 1.00;
    const float zRef = 1.08883;

    float xReffed = X / xRef;
    float yReffed = Y / yRef;
    float zReffed = Z / zRef;

    float xF = (xReffed > 0.008856) ? pow(xReffed, 1 / 3.0) : (7.787 * xReffed) + (4 / 29.0);
    float yF = (yReffed > 0.008856) ? pow(yReffed, 1 / 3.0) : (7.787 * yReffed) + (4 / 29.0);
    float zF = (zReffed > 0.008856) ? pow(zReffed, 1 / 3.0) : (7.787 * zReffed) + (4 / 29.0);

    float L = 116 * yF - 16;
    float a = 500 * (xF - yF);
    float b = 200 * (yF - zF);


    labPixels[index + 0] = L;
    labPixels[index + 1] = a;
    labPixels[index + 2] = b;
}";

        public static readonly string Kernel = RGBToLab;
    }
}
