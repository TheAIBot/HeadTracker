using ImageInfo;
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
        public float MaxColorDistanceForMatch = 1.5f;

        private readonly System.Drawing.Color[] colors;
        private readonly int ImageWidth;
        private readonly int ImageHeight;
        private readonly byte[] RGBPixels;
        private readonly float[] LabPixels;
        private readonly float[] LabDistances;
        private readonly int[] ClusterMap;
        private readonly EasyCL gpuAccel;
        
        private const int TOP_DISTANCE_INDEX = 0;
        private const int LEFT_DISTANCE_INDEX = 1;
        private const int RIGHT_DISTANCE_INDEX = 2;
        private const int BOTTOM_DISTANCE_INDEX = 3;

        public ColorClusterCreator(int width, int height)
        {
            List<System.Drawing.Color> lColors = new List<System.Drawing.Color>();
            foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                lColors.Add(System.Drawing.Color.FromKnownColor((KnownColor)colorValue));
            }
            this.colors = lColors.ToArray();

            this.ImageWidth = width;
            this.ImageHeight = height;
            this.RGBPixels = new byte[ImageWidth * ImageHeight * 3];
            this.LabPixels = new float[ImageWidth * ImageHeight * 3];
            this.LabDistances = new float[ImageWidth * ImageHeight * 4];
            this.ClusterMap = new int[ImageWidth * ImageHeight];

            this.gpuAccel = new EasyCL();
            this.gpuAccel.Accelerator = AcceleratorDevice.GPU;
            this.gpuAccel.LoadKernel(OpenClKernels.Kernel);
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
                byte* originalPtr = (byte*)originalBitmapData.Scan0;
                int index = 0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* originalRowPtr = originalPtr + (y * originalBitmapData.Stride);

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        byte* pixelPtr = originalRowPtr + (x * pixelInfo.pixelSize);
                        //RGBPixel pixel = PixelInfo.GetPixelColorAsPixelColor(pixelPtr, pixelInfo);
                        //int red = pixelPtr[(int)RGBAColor.Red];
                        //int green = pixelPtr[(int)RGBAColor.Green];
                        //int blue = pixelPtr[(int)RGBAColor.Blue];

                        RGBPixels[index + 0] = pixelPtr[(int)RGBAColor.Red];
                        RGBPixels[index + 1] = pixelPtr[(int)RGBAColor.Green];
                        RGBPixels[index + 2] = pixelPtr[(int)RGBAColor.Blue];

                        index += 3;
                    }
                }
            }
            image.UnlockBits(originalBitmapData);

            gpuAccel.Invoke("RGBToLab", 0, LabPixels.Length / 3, RGBPixels, LabPixels, 255f);
            gpuAccel.Invoke("LabDistances", 0, (ImageWidth - 2) * (ImageHeight - 2), LabPixels, LabDistances, ImageWidth, ImageHeight);
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


            labPixels[index + 0] = Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
            labPixels[index + 1] = Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
            labPixels[index + 2] = Math.Min(Math.Max(iL, sbyte.MinValue), sbyte.MaxValue);
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

        void CalcLabDistances(float[] labPixels, float[] labDistances, int bigWidth, int bigHeight, int index)
        {
            //int index = get_global_id(0);

            int smallWidth = bigWidth - 2;
            int smallHeight = bigHeight - 2;

            int x = (index % smallWidth) + 1;
            int y = (index / smallWidth) + 1;

            int labDistancesIndex = (y * bigWidth + x) * 4;

            int centerIndex = (y * bigWidth + x) * 3;
            float centerL = labPixels[centerIndex + 0];
            float centera = labPixels[centerIndex + 1];
            float centerb = labPixels[centerIndex + 2];

            int topIndex = centerIndex - bigWidth * 3;
            float topL = labPixels[topIndex + 0];
            float topa = labPixels[topIndex + 1];
            float topb = labPixels[topIndex + 2];
            labDistances[labDistancesIndex + 0] = DistanceCIE94(centerL, centera, centerb, topL, topa, topb);

            int leftIndex = centerIndex - 1 * 3;
            float leftL = labPixels[leftIndex + 0];
            float lefta = labPixels[leftIndex + 1];
            float leftb = labPixels[leftIndex + 2];
            labDistances[labDistancesIndex + 1] = DistanceCIE94(centerL, centera, centerb, leftL, lefta, leftb);

            int rightIndex = centerIndex + 1 * 3;
            float rightL = labPixels[rightIndex + 0];
            float righta = labPixels[rightIndex + 1];
            float rightb = labPixels[rightIndex + 2];
            if (index == 6719)
            {

            }
            labDistances[labDistancesIndex + 2] = DistanceCIE94(centerL, centera, centerb, rightL, righta, rightb);

            int bottomIndex = centerIndex + bigWidth * 3;
            float bottomL = labPixels[bottomIndex + 0];
            float bottoma = labPixels[bottomIndex + 1];
            float bottomb = labPixels[bottomIndex + 2];
            labDistances[labDistancesIndex + 3] = DistanceCIE94(centerL, centera, centerb, bottomL, bottoma, bottomb);

            if (float.IsNaN(labDistances[index * 4 + 0]))
            {

            }
            if (float.IsNaN(labDistances[index * 4 + 1]))
            {

            }
            if (float.IsNaN(labDistances[index * 4 + 2]))
            {

            }
            if (float.IsNaN(labDistances[index * 4 + 3]))
            {

            }
        }





        private void CreateClusterMap()
        {
            List<int> clusterIndexes = new List<int>();
            int clusterCount = 0;

            //The top left most pixel is part of cluster 0
            //as 0 is the default value of an int it's not nessesary
            //to set the pixels cluster in the clustermap. instead
            //just add the cluster.
            clusterIndexes.Add(0);
            clusterCount++;

            InitClusterMapClusters(clusterIndexes, ref clusterCount);
            FinishClusterMap(clusterIndexes, ref clusterCount);

            RemoveSmallClusters();
            MergeSimilarClusters();

            FlattenClusterIndexes(clusterIndexes);

            //Now go through all clusters and replace merged clusters with the merged cluster number
            for (int i = 0; i < ClusterMap.Length; i++)
            {
                ClusterMap[i] = clusterIndexes[ClusterMap[i]];
            }
        }

        private void InitClusterMapClusters(List<int> clusterIndexes, ref int clusterCount)
        {
            for (int x = 1; x < ImageWidth - 1; x++)
            {
                int currentPixelIndex = x;
                int leftPixelIndex = x - 1;

                if (LabDistances[currentPixelIndex * 4 + LEFT_DISTANCE_INDEX] < MaxColorDistanceForMatch)
                {
                    ClusterMap[currentPixelIndex] = ClusterMap[leftPixelIndex];
                }
                else
                {
                    clusterIndexes.Add(clusterCount);
                    ClusterMap[currentPixelIndex] = clusterCount;
                    clusterCount++;
                }
            }

            for (int y = 1; y < ImageHeight - 1; y++)
            {
                int currentPixelIndex = y * ImageWidth;
                int topPixelIndex = (y - 1) * ImageWidth;

                if (LabDistances[currentPixelIndex * 4 + TOP_DISTANCE_INDEX] < MaxColorDistanceForMatch)
                {
                    ClusterMap[currentPixelIndex] = ClusterMap[topPixelIndex];
                }
                else
                {
                    clusterIndexes.Add(clusterCount);
                    ClusterMap[currentPixelIndex] = clusterCount;
                    clusterCount++;
                }
            }

        }

        private void FinishClusterMap(List<int> clusterIndexes, ref int clusterCount)
        {
            for (int y = 1; y < ImageHeight - 1; y++)
            {
                for (int x = 1; x < ImageWidth - 1; x++)
                {
                    int currentPixelIndex = y * ImageWidth + x;
                    int topPixelIndex = currentPixelIndex - ImageWidth;
                    int leftPixelIndex = currentPixelIndex - 1;

                    int isSimilarToTopPixel = (LabDistances[currentPixelIndex * 4 + TOP_DISTANCE_INDEX] < MaxColorDistanceForMatch) ? 1 : 0;
                    int isSimilarToLeftPixel = (LabDistances[currentPixelIndex * 4 + LEFT_DISTANCE_INDEX] < MaxColorDistanceForMatch) ? 2 : 0;
                    int matchingPixels = isSimilarToTopPixel + isSimilarToLeftPixel;

                    switch (matchingPixels)
                    {
                        //No pixel match.
                        //Create new cluster.
                        case 0:
                            clusterIndexes.Add(clusterCount);
                            ClusterMap[currentPixelIndex] = clusterCount;
                            clusterCount++;
                            break;
                        //Only top pixel match.
                        //Use cluster from top pixel.
                        case 1:
                            ClusterMap[currentPixelIndex] = ClusterMap[topPixelIndex];
                            break;
                        //Only left pixel match.
                        //Use cluster from left pixel.
                        case 2:
                            ClusterMap[currentPixelIndex] = ClusterMap[leftPixelIndex];
                            break;
                        //Both pixels match.
                        //Merge top cluster into left cluster and then use left cluster.
                        case 3:
                            int minClusterIndex = Math.Min(ClusterMap[topPixelIndex], ClusterMap[leftPixelIndex]);
                            clusterIndexes[ClusterMap[topPixelIndex]] = minClusterIndex;
                            clusterIndexes[ClusterMap[leftPixelIndex]] = minClusterIndex;
                            ClusterMap[currentPixelIndex] = minClusterIndex;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void RemoveSmallClusters()
        {

        }

        private void MergeSimilarClusters()
        {

        }

        private void FlattenClusterIndexes(List<int> clusterIndexes)
        {
            for (int i = 0; i < clusterIndexes.Count; i++)
            {
                int index = i;
                int clusterIndex = clusterIndexes[index];
                //Only superclusters won't have the sign bit set
                while (clusterIndex != index)
                {
                    index = clusterIndex;
                    clusterIndex = clusterIndexes[index];
                }

                clusterIndexes[i] = clusterIndex;
            }
        }

        public Bitmap BitmapFromClusterMap()
        {
            Bitmap withClusters = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format24bppRgb);

            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(withClusters);
            Rectangle imageSize = new Rectangle(0, 0, withClusters.Width, withClusters.Height);
            BitmapData originalBitmapData = withClusters.LockBits(imageSize, ImageLockMode.WriteOnly, withClusters.PixelFormat);

            unsafe
            {
                byte* originalPtr = (byte*)originalBitmapData.Scan0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* originalRowPtr = originalPtr + (y * originalBitmapData.Stride);

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        byte* pixelPtr = originalRowPtr + (x * pixelInfo.pixelSize);
                        int clusterNumber = ClusterMap[y * ImageWidth + x];

                        System.Drawing.Color color = colors[clusterNumber % colors.Length];
                        pixelPtr[0] = color.R;
                        pixelPtr[1] = color.G;
                        pixelPtr[2] = color.B;
                    }
                }
            }
            withClusters.UnlockBits(originalBitmapData);

            return withClusters;
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
    }
}