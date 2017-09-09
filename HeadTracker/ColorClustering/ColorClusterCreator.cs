﻿using ImageInfo;
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

        public ColorClusterCreator(Bitmap image)
        {
            List<ColorClusterInitData> createdClusters = CreateClusters(image);
            createdClusters = createdClusters.OrderByDescending(x => x.ClusterSize).ToList();

            //need to merge the smaller clusters into bigger clusters 
            const int requiredPixels = 400;

            for (int i = createdClusters.Count - 1; i >= 0; i--)
            {
                ColorClusterInitData cluster = createdClusters[i];

                if (cluster.ClusterSize >= requiredPixels)
                {
                    continue;
                }

                ColorClusterInitData bestMatch = cluster.GetBestMatchingSurroundingCluster();
                if (bestMatch == null)
                {
                    continue;
                }
                bestMatch.AddCluster(cluster);
            }

            createdClusters.RemoveAll(x => x.ClusterSize < requiredPixels);
            clusters = createdClusters.Select(x => x.CreateColorCluster()).ToList();
        }

        private List<ColorClusterInitData> CreateClusters(Bitmap image)
        {
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(image);
            int pixelSize = pixelInfo.pixelSize;
            int rgbSize = pixelInfo.rgbSize;
            Rectangle imageSize = new Rectangle(0, 0, image.Width, image.Height);

            BitmapData originalBitmapData = image.LockBits(imageSize, ImageLockMode.ReadOnly, image.PixelFormat);

            Dictionary<ColorClusterInitData, bool> createdClusters = new Dictionary<ColorClusterInitData, bool>();

            ColorClusterInitData[] previousRowClusters = new ColorClusterInitData[image.Width];
            ColorClusterInitData[] currentRowClusters = new ColorClusterInitData[image.Width];

            PixelColor[] previousRowPixels = new PixelColor[image.Width];
            PixelColor[] currentRowPixels = new PixelColor[image.Width];

            unsafe
            {
                byte* originalPtr = (byte*)originalBitmapData.Scan0;

                for (int y = 0; y < originalBitmapData.Height; y++)
                {
                    byte* originalRowPtr = originalPtr + (y * originalBitmapData.Stride);

                    for (int x = 0; x < originalBitmapData.Width; /*do nothing here as it's handled elsewhere*/)
                    {
                        
                        byte* firstPixelStretchPtr = originalRowPtr + (x * pixelSize);
                        PixelColor prevPixelColor = PixelInfo.GetPixelColorAsPixelColor(firstPixelStretchPtr, pixelInfo);

                        int stretchRed = 0;
                        int stretchGreen = 0;
                        int stretchBlue = 0;

                        Boolean isStretchClosed = false;

                        for (int z = x; z < originalBitmapData.Width; z++)
                        {
                            byte* pixelPtr = originalRowPtr + (z * pixelSize);
                            PixelColor currentPixelColor = PixelInfo.GetPixelColorAsPixelColor(pixelPtr, pixelInfo);

                            stretchRed += currentPixelColor.red;
                            stretchGreen += currentPixelColor.green;
                            stretchBlue += currentPixelColor.blue;

                            currentRowPixels[z] = currentPixelColor;

                            const float MAX_PIXEL_DIFFERENCE = 1;

                            //This giant nested if is a clusterfuck but i currently don't
                            //know how to make it better.
                            if (currentPixelColor.Distance(prevPixelColor) < MAX_PIXEL_DIFFERENCE)
                            {
                                //if this is the first pixel in this cluster.
                                if (x == z)
                                {
                                    if (previousRowClusters[z] == null)
                                    {
                                        goto createCluster;
                                    }
                                    else
                                    {
                                        if (previousRowPixels[z].Distance(currentPixelColor) < MAX_PIXEL_DIFFERENCE)
                                        {
                                            goto usePreviousAboveCluster;
                                        }
                                        else
                                        {
                                            goto createCluster;
                                        }
                                    }
                                }
                                else
                                {
                                    if (previousRowClusters[z] == null)
                                    {
                                        goto usePreviousPixelCluster;
                                    }
                                    else
                                    {
                                        if (previousRowPixels[z].Distance(currentPixelColor) < MAX_PIXEL_DIFFERENCE)
                                        {
                                            if (previousRowClusters[z] == currentRowClusters[z - 1])
                                            {
                                                goto usePreviousAboveCluster;
                                            }
                                            else
                                            {
                                                goto combineClusters;
                                            }
                                        }
                                        else
                                        {
                                            goto usePreviousPixelCluster;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                goto endStretch;
                            }

                            endStretch:
                            //this pixel isn't part of the stretch so remove its color value
                            stretchRed -= currentPixelColor.red;
                            stretchGreen -= currentPixelColor.green;
                            stretchBlue -= currentPixelColor.blue;

                            //z - 1 because this pixel isn't part of this cluster.
                            PixelStretch stretch = new PixelStretch(x, z - 1, y);
                            currentRowClusters[z - 1].AddPixels(stretch, stretchRed, stretchGreen, stretchBlue);
                            isStretchClosed = true;

                            //set x to this pixels position and break, so a new pixel stretch can
                            //be created with this pixel as the starting pixel.
                            x += (z - x);
                            break;

                            createCluster:
                            ColorClusterInitData newCluster = new ColorClusterInitData();
                            currentRowClusters[z] = newCluster;
                            createdClusters.Add(newCluster, true);
                            goto end;

                            usePreviousAboveCluster:
                            currentRowClusters[z] = previousRowClusters[z];
                            goto end;

                            usePreviousPixelCluster:
                            currentRowClusters[z] = currentRowClusters[z - 1];
                            goto end;

                            combineClusters:
                            currentRowClusters[z - 1].AddCluster(previousRowClusters[z]);
                            ColorClusterInitData clusterBToRemove = previousRowClusters[z];
                            for (int q = 0; q < currentRowClusters.Length; q++)
                            {
                                if (currentRowClusters[q] == clusterBToRemove)
                                {
                                    currentRowClusters[q] = currentRowClusters[z - 1];
                                }
                            }
                            for (int q = 0; q < previousRowClusters.Length; q++)
                            {
                                if (previousRowClusters[q] == clusterBToRemove)
                                {
                                    previousRowClusters[q] = currentRowClusters[z - 1];
                                }
                            }
                            createdClusters[clusterBToRemove] = false;

                            currentRowClusters[z] = currentRowClusters[z - 1];
                            goto end;

                            end:
                            //now add surrounding clusters to the current cluster and those surrounding it

                            //if first pixel in stretch and there is a pixel to the left
                            if (z == x && z > 0)
                            {
                                //add left cluster to right surrounding clusters
                                currentRowClusters[z].AddSurroundingCluster(currentRowClusters[z - 1]);
                                //add right cluster to left surrounding clusters
                                currentRowClusters[z - 1].AddSurroundingCluster(currentRowClusters[z]);
                            }
                            //if there is a pixel above
                            if (y > 0)
                            {
                                //add top cluster to bottom surrounding clusters
                                currentRowClusters[z].AddSurroundingCluster(previousRowClusters[z]);
                                //add bottom cluster to top surrounding clusters
                                previousRowClusters[z].AddSurroundingCluster(currentRowClusters[z]);
                            }

                            prevPixelColor = currentPixelColor;
                        }

                        if (!isStretchClosed)
                        {
                            PixelStretch stretch = new PixelStretch(x, originalBitmapData.Width - 1, y);
                            currentRowClusters[originalBitmapData.Width - 1].AddPixels(stretch, stretchRed, stretchGreen, stretchBlue);
                            break;
                        }
                    }

                    ColorClusterInitData[] tempCluster = previousRowClusters;
                    previousRowClusters = currentRowClusters;
                    currentRowClusters = tempCluster;

                    PixelColor[] tempPixel = previousRowPixels;
                    previousRowPixels = currentRowPixels;
                    currentRowPixels = tempPixel;
                }
            }
            image.UnlockBits(originalBitmapData);

            return createdClusters.Where(x => x.Value).Select(x => x.Key).ToList();
        }

        public List<ColorCluster> GetClustersSortedByMostRed()
        {
            return clusters.OrderByDescending(x => x.ClusterColor.red - (x.ClusterColor.green + x.ClusterColor.blue) / 2).ToList();
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
