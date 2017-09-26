﻿using ImageInfo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCL;

namespace HeadTracker
{
    class ColorClusterCreator
    {
        public readonly List<ColorCluster> clusters;
        private float MaxColorDistanceForMatch = 1.5f;

        private readonly byte[] colors;
        private readonly sbyte[] colorsAsLab;
        private readonly int ImageWidth;
        private readonly int ImageHeight;
        private readonly byte[] RGBPixels;
        private readonly sbyte[] LabPixels;
        private readonly ushort[] ClusterMap;
        private readonly Bitmap clusterImage;
        private readonly EasyCL gpuAccel;
        
        private const int TOP_DISTANCE_INDEX = 0;
        private const int LEFT_DISTANCE_INDEX = 1;
        private const int RIGHT_DISTANCE_INDEX = 2;
        private const int BOTTOM_DISTANCE_INDEX = 3;

        public ColorClusterCreator(int width, int height)
        {
            const int colorSplits = 14;
            int colorCount = (int)Math.Pow(colorSplits + 1, 3);
            this.colors = new byte[colorCount * 3];
            this.colorsAsLab = new sbyte[colors.Length];

            this.ImageWidth = width;
            this.ImageHeight = height;
            this.RGBPixels = new byte[ImageWidth * ImageHeight * 3];
            this.LabPixels = new sbyte[ImageWidth * ImageHeight * 3];
            this.ClusterMap = new ushort[ImageWidth * ImageHeight];
            clusterImage = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format24bppRgb);

            this.gpuAccel = new EasyCL();
            this.gpuAccel.Accelerator = AcceleratorDevice.GPU;
            this.gpuAccel.LoadKernel(OpenClKernels.Kernel);

            int index = 0;
            for (int x = 0; x <= colorSplits; x++)
            {
                for (int y = 0; y <= colorSplits; y++)
                {
                    for (int z = 0; z <= colorSplits; z++)
                    {
                        colors[index + 0] = (byte)Math.Min((256f / colorSplits) * x, 255);
                        colors[index + 1] = (byte)Math.Min((256f / colorSplits) * y, 255);
                        colors[index + 2] = (byte)Math.Min((256f / colorSplits) * z, 255);
                        index += 3;
                    }
                }
            }


            gpuAccel.Invoke("RGBToLab", 0, colorsAsLab.Length / 3, colors, colorsAsLab, 255f);
        }

        public void UpdateClusters(Bitmap image)
        {
            if (image.Width != ImageWidth ||
                image.Height != ImageHeight)
            {
                throw new ArgumentException("Image width and height doesn't correspond with the expected width and height.");
            }

            ToLabPixels(image);
            CreateClusterMap();
        }

        private void ToLabPixels(Bitmap image)
        {
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(image);
            if (pixelInfo.GetBytesForColor(RGBAColor.RGB) != 1)
            {
                throw new Exception("Pixeltype is not supported.");
            }
            Rectangle imageSize = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData originalBitmapData = image.LockBits(imageSize, ImageLockMode.ReadOnly, image.PixelFormat);

            unsafe
            {
                byte* rowPtr = (byte*)originalBitmapData.Scan0;
                int index = 0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* pixelPtr = rowPtr;

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        RGBPixels[index + 0] = pixelPtr[(int)RGBAColor.Red];
                        RGBPixels[index + 1] = pixelPtr[(int)RGBAColor.Green];
                        RGBPixels[index + 2] = pixelPtr[(int)RGBAColor.Blue];

                        index += 3;
                        pixelPtr += 3;
                    }

                    rowPtr += originalBitmapData.Stride;
                }
            }
            image.UnlockBits(originalBitmapData);

            gpuAccel.Invoke("RGBToLab", 0, LabPixels.Length / 3, RGBPixels, LabPixels, 255f);
            //for (int i = 0; i < LabPixels.Length / 3; i++)
            //{
            //    RGBToLab(RGBPixels, LabPixels, 255f, i);
            //}
            //for (int i = 0; i < (ImageWidth - 2) * (ImageHeight - 2); i++)
            //{
            //    CalcLabDistances(LabPixels, LabDistances, ImageWidth, ImageHeight, i);
            //}
        }



        void RGBToLab(byte[] rgbPixels, float[] labPixels, float maxColorNumber, int index)
        {
            index = index * 3;

            float red = rgbPixels[index + 0];
            float green = rgbPixels[index + 1];
            float blue = rgbPixels[index + 2];


            //First convert from RGB to XYZ
            float sR = red / maxColorNumber;
            float sG = green / maxColorNumber;
            float sB = blue / maxColorNumber;

            float rLinear = (sR > 0.04045f) ? (float)Math.Pow((sR + 0.055) / 1.055, 2.4) : sR / 12.92f;
            float gLinear = (sG > 0.04045f) ? (float)Math.Pow((sG + 0.055) / 1.055, 2.4) : sG / 12.92f;
            float bLinear = (sB > 0.04045f) ? (float)Math.Pow((sB + 0.055) / 1.055, 2.4) : sB / 12.92f;

            float X = rLinear * 0.4124f + gLinear * 0.3576f + bLinear * 0.1805f;
            float Y = rLinear * 0.2126f + gLinear * 0.7152f + bLinear * 0.0722f;
            float Z = rLinear * 0.0193f + gLinear * 0.1192f + bLinear * 0.9505f;


            //Then convert from XYZ to Lab
            const float xRef = 0.95047f;
            const float yRef = 1.00f;
            const float zRef = 1.08883f;

            float xReffed = X / xRef;
            float yReffed = Y / yRef;
            float zReffed = Z / zRef;

            float xF = (xReffed > 0.008856f) ? (float)Math.Pow(xReffed, 1 / 3.0f) : (7.787f * xReffed) + (4 / 29.0f);
            float yF = (yReffed > 0.008856f) ? (float)Math.Pow(yReffed, 1 / 3.0f) : (7.787f * yReffed) + (4 / 29.0f);
            float zF = (zReffed > 0.008856f) ? (float)Math.Pow(zReffed, 1 / 3.0f) : (7.787f * zReffed) + (4 / 29.0f);

            float L = 116 * yF - 16;
            float a = 500 * (xF - yF);
            float b = 200 * (yF - zF);

            int iL = (int)L;
            int ia = (int)a;
            int ib = (int)b;


            labPixels[index + 0] = (sbyte)Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
            labPixels[index + 1] = (sbyte)Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
            labPixels[index + 2] = (sbyte)Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
        }



        float DistanceCIE94(float L1, float a1, float b1, float L2, float a2, float b2)
        {
            float C1 = (float)Math.Sqrt((a1 * a1) + (b1 * b1));
            float C2 = (float)Math.Sqrt((a2 * a2) + (b2 * b2));
            float DeltaCab = C1 - C2;

            float DeltaL = L1 - L2;
            float Deltaa = a1 - a2;
            float Deltab = b1 - b2;

            float DeltaHab = (float)Math.Sqrt((Deltaa * Deltaa) + (Deltab * Deltab) - (DeltaCab * DeltaCab));

            const float kL = 1;
            const float kC = 1;
            const float kH = 1;
            const float K1 = 0.045f;
            const float K2 = 0.015f;

            float SL = 1;
            float SC = 1 + K1 * C1;
            float SH = 1 + K2 * C1;

            float LRes = DeltaL / (kL * SL);
            float CRes = DeltaCab / (kC * SC);
            float HRes = DeltaHab / (kH * SH);

            float f = (float)Math.Sqrt((LRes * LRes) + (CRes * CRes) + (HRes * HRes));
            return f;
        }


        void GetClusterMap(sbyte[] colors, int colorCount, sbyte[] labPixels, ushort[] clusterMap, int index)
        {
            float L = labPixels[index * 3 + 0];
            float a = labPixels[index * 3 + 1];
            float b = labPixels[index * 3 + 2];

            int bestColorIndex = 0;
            float bestDistance = 10000;
            for (int i = 0; i < colorCount; i++)
            {
                float cL = colors[i * 3 + 0];
                float ca = colors[i * 3 + 1];
                float cb = colors[i * 3 + 2];

                float distance = DistanceCIE94(L, a, b, cL, ca, cb);

                float notNanDistance = (float.IsNaN(distance)) ? 100000 : distance;
                float notInfDistance = (float.IsPositiveInfinity(distance) || float.IsNegativeInfinity(distance)) ? 100000 : notNanDistance;

                bestColorIndex = (notInfDistance < bestDistance) ? i : bestColorIndex;
                bestDistance = (notInfDistance < bestDistance) ? notInfDistance : bestDistance;
            }

            clusterMap[index] = (ushort)bestColorIndex;
        }



        private void CreateClusterMap()
        {
            //GetClusterMap(global char* colors, int colorCount, global char* labPixels, global ushort* clusterMap)
            gpuAccel.Invoke("GetClusterMap", 0, ClusterMap.Length, colorsAsLab, colorsAsLab.Length / 3, LabPixels, ClusterMap);
            //for (int i = 0; i < ClusterMap.Length; i++)
            //{
            //    GetClusterMap(colorsAsLab, colorsAsLab.Length / 3, LabPixels, ClusterMap, i);
            //}
        }

        public Bitmap BitmapFromClusterMap()
        {
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(clusterImage);
            Rectangle imageSize = new Rectangle(0, 0, clusterImage.Width, clusterImage.Height);
            BitmapData originalBitmapData = clusterImage.LockBits(imageSize, ImageLockMode.WriteOnly, clusterImage.PixelFormat);

            unsafe
            {
                byte* rowPtr = (byte*)originalBitmapData.Scan0;
                int clusterIndex = 0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* pixelPtr = rowPtr;

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        int clusterNumber = ClusterMap[clusterIndex];

                        pixelPtr[0] = colors[clusterNumber * 3 + 0];
                        pixelPtr[1] = colors[clusterNumber * 3 + 1];
                        pixelPtr[2] = colors[clusterNumber * 3 + 2];

                        clusterIndex++;
                        pixelPtr += 3;
                    }

                    rowPtr += originalBitmapData.Stride;
                }
            }
            clusterImage.UnlockBits(originalBitmapData);

            return clusterImage;
        }

        public List<ColorCluster> GetClustersSortedByMostRed()
        {
            LabPixel redPixel = new RGBPixel(255, 0, 0).ToLabPixel();
            return clusters.OrderBy(x => redPixel.DistanceCIE94IgnoreIllumination(x.ClusterColor.ToLabPixel())).ToList();
        }
        
        public List<ColorCluster> GetClustersSortedByMostGreen()
        {
            return clusters.OrderByDescending(x => x.ClusterColor.green - (x.ClusterColor.red + x.ClusterColor.blue) / 2).ToList();
        }

        public List<ColorCluster> GetClustersSortedByMostBlue()
        {
            return clusters.OrderByDescending(x => x.ClusterColor.blue - (x.ClusterColor.green + x.ClusterColor.red) / 2).ToList();
        }

        public List<ColorCluster> GetClustersSortedByMostBlack()
        {
            return clusters.OrderBy(x => x.ClusterColor.red + x.ClusterColor.green + x.ClusterColor.blue).ToList();
        }

        public void SetColorDistance(float distance)
        {
            MaxColorDistanceForMatch = distance;
        }
    }
}