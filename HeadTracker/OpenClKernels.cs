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
kernel void RGBToLab(global char* rgbPixels, global float* labPixels, float maxColorNumber)
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

        private static readonly string LabDistances = @"
float DistanceCIE94(float L1, float a1, float b1, float L2, float a2, float b2)
{
    float C1 = sqrt((a1 * a1) + (b1 * b1));
    float C2 = sqrt((a2 * a2) + (b2 * b2));
    float DeltaCab = C1 - C2;

    float DeltaL = L1 - L2;
    float Deltaa = a1 - a2;
    float Deltab = b1 - b2;

    float DeltaHab = sqrt((Deltaa * Deltaa) + (Deltab * Deltab) - (DeltaCab * DeltaCab));

    const float kL = 1;
    const float kC = 1;
    const float kH = 1;
    const float K1 = 0.045;
    const float K2 = 0.015;

    float SL = 1;
    float SC = 1 + K1 * C1;
    float SH = 1 + K2 * C1;

    float LRes = DeltaL / (kL * SL);
    float CRes = DeltaCab / (kC * SC);
    float HRes = DeltaHab / (kH * SH);

    return sqrt((LRes * LRes) + (CRes * CRes) + (HRes * HRes));
}

kernel void LabDistances(global float* labPixels, global float* labDistances, int bigWidth, int bigHeight)
{
    int index = get_global_id(0);
    
    int smallWidth = bigWidth - 2;
    int smallHeight = bigHeight - 2;

    int x = (index % smallWidth)  + 1;
    int y = (index / smallWidth) + 1;

    int labDistancesIndex = (y * bigWidth + x) * 4;

    int centerIndex = (y * bigWidth + x) * 3;
    float centerL = labPixels[centerIndex + 0];
    float centera = labPixels[centerIndex + 1];
    float centerb = labPixels[centerIndex + 2];

    float topL = labPixels[centerIndex - bigWidth * 3 + 0];
    float topa = labPixels[centerIndex - bigWidth * 3 + 1];
    float topb = labPixels[centerIndex - bigWidth * 3 + 2];
    labDistances[labDistancesIndex + 0] = DistanceCIE94(centerL, centera, centerb, topL, topa, topb);

    float leftL = labPixels[centerIndex - 1 * 3 + 0];
    float lefta = labPixels[centerIndex - 1 * 3 + 1];
    float leftb = labPixels[centerIndex - 1 * 3 + 2];
    labDistances[labDistancesIndex + 1] = DistanceCIE94(centerL, centera, centerb, leftL, lefta, leftb);

    float rightL = labPixels[centerIndex + 1 * 3 + 0];
    float righta = labPixels[centerIndex + 1 * 3 + 1];
    float rightb = labPixels[centerIndex + 1 * 3 + 2];
    labDistances[labDistancesIndex + 2] = DistanceCIE94(centerL, centera, centerb, rightL, righta, rightb);

    float bottomL = labPixels[centerIndex + bigWidth * 3 + 0];
    float bottoma = labPixels[centerIndex + bigWidth * 3 + 1];
    float bottomb = labPixels[centerIndex + bigWidth * 3 + 2];
    labDistances[labDistancesIndex + 3] = DistanceCIE94(centerL, centera, centerb, bottomL, bottoma, bottomb);
}";

        public static readonly string Kernel = RGBToLab + LabDistances;
    }
}
