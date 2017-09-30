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
        private readonly int ImageWidth;
        private readonly int ImageHeight;
        private readonly byte[] RGBPixels;
        private readonly sbyte[] LabPixels;
        private readonly byte[] LabDistances;
        private readonly int[] ClusterMap;
        private readonly Bitmap clusterImage;
        private readonly EasyCL gpuAccel;
        
        private const int TOP_DISTANCE_INDEX = 0;
        private const int LEFT_DISTANCE_INDEX = 1;
        private const int RIGHT_DISTANCE_INDEX = 2;
        private const int BOTTOM_DISTANCE_INDEX = 3;

        public ColorClusterCreator(int width, int height)
        {
            List<byte> lColors = new List<byte>();
            foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                System.Drawing.Color color = System.Drawing.Color.FromKnownColor((KnownColor)colorValue);
                lColors.Add(color.R);
                lColors.Add(color.G);
                lColors.Add(color.B);
            }
            this.colors = lColors.ToArray();

            this.ImageWidth = width;
            this.ImageHeight = height;
            this.RGBPixels = new byte[ImageWidth * ImageHeight * 3];
            this.LabPixels = new sbyte[ImageWidth * ImageHeight * 3];
            this.LabDistances = new byte[ImageWidth * ImageHeight * 4];
            this.ClusterMap = new int[ImageWidth * ImageHeight];
            clusterImage = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format24bppRgb);

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
            gpuAccel.Invoke("LabDistances", 0, (ImageWidth - 2) * (ImageHeight - 2), LabPixels, LabDistances, ImageWidth, ImageHeight, MaxColorDistanceForMatch);
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

                if (LabDistances[currentPixelIndex * 4 + LEFT_DISTANCE_INDEX] > 0)
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

                if (LabDistances[currentPixelIndex * 4 + TOP_DISTANCE_INDEX] > 0)
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

                    int isSimilarToTopPixel  = LabDistances[currentPixelIndex * 4 + TOP_DISTANCE_INDEX] * 1;
                    int isSimilarToLeftPixel = LabDistances[currentPixelIndex * 4 + LEFT_DISTANCE_INDEX] * 2;
                    int matchingPixels = isSimilarToTopPixel + isSimilarToLeftPixel;

                    int topClusterIndex = ClusterMap[topPixelIndex];
                    int leftClusterIndex = ClusterMap[leftPixelIndex];

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
                            ClusterMap[currentPixelIndex] = topClusterIndex;
                            break;
                        //Only left pixel match.
                        //Use cluster from left pixel.
                        case 2:
                            ClusterMap[currentPixelIndex] = leftClusterIndex;
                            break;
                        //Both pixels match.
                        //Merge top cluster into left cluster and then use left cluster.
                        case 3:
                            int minClusterIndex = Math.Min(topClusterIndex, leftClusterIndex);
                            clusterIndexes[topClusterIndex] = clusterIndexes[minClusterIndex];
                            clusterIndexes[leftClusterIndex] = clusterIndexes[minClusterIndex];
                            ClusterMap[currentPixelIndex] = minClusterIndex;
                            break;
                        default:
                            break;
                    }
                }
            }
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
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(clusterImage);
            Rectangle imageSize = new Rectangle(0, 0, clusterImage.Width, clusterImage.Height);
            BitmapData originalBitmapData = clusterImage.LockBits(imageSize, ImageLockMode.WriteOnly, clusterImage.PixelFormat);

            unsafe
            {
                byte* rowPtr = (byte*)originalBitmapData.Scan0;
                int pixelIndex = 0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* pixelPtr = rowPtr;

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        int clusterNumber = ClusterMap[pixelIndex];

                        int colorIndex = (clusterNumber % (colors.Length / 3)) * 3;
                        pixelPtr[0] = colors[colorIndex + 0];
                        pixelPtr[1] = colors[colorIndex + 1];
                        pixelPtr[2] = colors[colorIndex + 2];

                        //byte d1 = LabDistances[pixelIndex * 4 + 0];
                        //byte d2 = LabDistances[pixelIndex * 4 + 1];
                        ////byte d3 = LabDistances[pixelIndex * 4 + 2];
                        ////byte d4 = LabDistances[pixelIndex * 4 + 3];
                        //byte sum = (byte)((d1 + d2/* + d3 + d4*/) * 10);
                        //byte value = Math.Min(sum, byte.MaxValue);

                        //pixelPtr[0] = value;
                        //pixelPtr[1] = value;
                        //pixelPtr[2] = value;

                        pixelIndex++;
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