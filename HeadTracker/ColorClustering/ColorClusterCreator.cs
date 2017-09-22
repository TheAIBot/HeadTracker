using ImageInfo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    class ColorClusterCreator
    {
        public readonly List<ColorCluster> clusters;
        public readonly Bitmap clusterBitmap;

        public ColorClusterCreator(Bitmap image)
        {
            LabPixel[] labPixels = ToLabPixels(image);
            int[] clusterMap = CreateClusterMap(labPixels, image.Width, image.Height);
            clusterBitmap = BitmapFromClusterMap(clusterMap, image.Width, image.Height);
        }

        private LabPixel[] ToLabPixels(Bitmap image)
        {
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(image);
            Rectangle imageSize = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData originalBitmapData = image.LockBits(imageSize, ImageLockMode.ReadOnly, image.PixelFormat);

            LabPixel[] labPixels = new LabPixel[image.Width * image.Height];

            unsafe
            {
                byte* originalPtr = (byte*)originalBitmapData.Scan0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* originalRowPtr = originalPtr + (y * originalBitmapData.Stride);

                    for (int x = 0; x < originalBitmapData.Width; x++)
                    {
                        byte* pixelPtr = originalRowPtr + (x * pixelInfo.pixelSize);
                        LabPixel labPixel = PixelInfo.GetPixelColorAsPixelColor(pixelPtr, pixelInfo).ToLabPixel();
                        labPixels[y * image.Width + x] = labPixel;
                    }
                }
            }
            image.UnlockBits(originalBitmapData);

            return labPixels;
        }

        private int[] CreateClusterMap(LabPixel[] labPixels, int width, int height)
        {
            List<int> clusterIndexes = new List<int>();
            int[] clusterMap = new int[width * height];
            int clusterCount = 0;
            const double maxColorDifference = 1.7;

            //The top left most pixel is part of cluster 0
            //as 0 is the default value of an int it's not nessesary
            //to set the pixels cluster in the clustermap. instead
            //just add the cluster.
            clusterIndexes.Add(0);
            clusterCount++;

            for (int x = 1; x < width; x++)
            {
                int currentPixelIndex = x;
                int leftPixelIndex = x - 1;

                LabPixel currentPixel = labPixels[currentPixelIndex];
                LabPixel leftPixel = labPixels[leftPixelIndex];

                if (currentPixel.DistanceCIE94(leftPixel) < maxColorDifference)
                {
                    clusterMap[currentPixelIndex] = clusterMap[leftPixelIndex];
                }
                else
                {
                    clusterIndexes.Add(clusterCount);
                    clusterMap[currentPixelIndex] = clusterCount;
                    clusterCount++;
                }
            }

            for (int y = 1; y < height; y++)
            {
                int currentPixelIndex = y * width;
                int topPixelIndex = (y - 1) * width;

                LabPixel currentPixel = labPixels[currentPixelIndex];
                LabPixel topPixel = labPixels[topPixelIndex];

                if (currentPixel.DistanceCIE94(topPixel) < maxColorDifference)
                {
                    clusterMap[currentPixelIndex] = clusterMap[topPixelIndex];
                }
                else
                {
                    clusterIndexes.Add(clusterCount);
                    clusterMap[currentPixelIndex] = clusterCount;
                    clusterCount++;
                }
            }

            //Now in the next step it's possible to merge clusters
            //That is done by making the clusterIndex point at the index of the 
            //cluster it was merged into. But this 
            //const uint signBit = 0x80000000;

            for (int y = 1; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    int currentPixelIndex = y * width + x;
                    int topPixelIndex = currentPixelIndex - width;
                    int leftPixelIndex = currentPixelIndex - 1;

                    LabPixel currentPixel = labPixels[currentPixelIndex];
                    LabPixel topPixel = labPixels[topPixelIndex];
                    LabPixel leftPixel = labPixels[leftPixelIndex];

                    int isSimilarToTopPixel = (currentPixel.DistanceCIE94(topPixel) < maxColorDifference) ? 1 : 0;
                    int isSimilarToLeftPixel = (currentPixel.DistanceCIE94(leftPixel) < maxColorDifference) ? 2 : 0;
                    int matchingPixels = isSimilarToTopPixel + isSimilarToLeftPixel;

                    switch (matchingPixels)
                    {
                        //No pixel match.
                        //Create new cluster.
                        case 0:
                            clusterIndexes.Add(clusterCount);
                            clusterMap[currentPixelIndex] = clusterCount;
                            clusterCount++;
                            break;
                        //Only top pixel match.
                        //Use cluster from top pixel.
                        case 1:
                            clusterMap[currentPixelIndex] = clusterMap[topPixelIndex];
                            break;
                        //Only left pixel match.
                        //Use cluster from left pixel.
                        case 2:
                            clusterMap[currentPixelIndex] = clusterMap[leftPixelIndex];
                            break;
                        //Both pixels match.
                        //Merge top cluster into left cluster and then use left cluster.
                        case 3:
                            int minClusterIndex = Math.Min(clusterMap[topPixelIndex], clusterMap[leftPixelIndex]);
                            clusterIndexes[clusterMap[topPixelIndex]] = minClusterIndex;
                            clusterIndexes[clusterMap[leftPixelIndex]] = minClusterIndex;
                            clusterMap[currentPixelIndex] = minClusterIndex;
                            //clusterIndexes[clusterMap[leftPixelIndex]] = clusterMap[topPixelIndex];
                            //clusterMap[currentPixelIndex] = clusterMap[topPixelIndex];
                            break;
                        default:
                            break;
                    }
                }
            }

            int uniqueClustersCount = 0;
            for (int i = 0; i < clusterIndexes.Count; i++)
            {
                if (i == clusterIndexes[i])
                {
                    uniqueClustersCount++;
                }
            }

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

            //Now go through all clusters and replace merged clusters with the merged cluster number
            for (int i = 0; i < clusterMap.Length; i++)
            {
                clusterMap[i] = clusterIndexes[clusterMap[i]];
            }

            return clusterMap;
        }

        public Bitmap BitmapFromClusterMap(int[] clusterMap, int width, int height)
        {
            Bitmap withClusters = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            foreach (var colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                colors.Add(System.Drawing.Color.FromKnownColor((KnownColor)colorValue));
            }

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
                        int clusterNumber = clusterMap[y * width + x];

                        System.Drawing.Color color = colors[clusterNumber % colors.Count];
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
