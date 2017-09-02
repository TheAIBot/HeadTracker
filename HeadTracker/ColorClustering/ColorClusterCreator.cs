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
        public readonly List<ColorCluster> clusters = new List<ColorCluster>();

        public ColorClusterCreator(Bitmap image)
        {
            CreateClusters(image);
            clusters.RemoveAll(x => x.ClusterSize <= 10);
        }

        private void CreateClusters(Bitmap image)
        {
            PixelTypeInfo pixelInfo = PixelInfo.GetPixelTypeInfo(image);
            int pixelSize = pixelInfo.pixelSize;
            int rgbSize = pixelInfo.rgbSize;
            Rectangle imageSize = new Rectangle(0, 0, image.Width, image.Height);

            BitmapData originalBitmapData = image.LockBits(imageSize, ImageLockMode.ReadOnly, image.PixelFormat);

            ColorCluster[] previousRowClusters = new ColorCluster[image.Width];
            ColorCluster[] currentRowClusters = new ColorCluster[image.Width];

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

                        int firstPixelRed = PixelInfo.GetPixelColorAsInt(firstPixelStretchPtr, RGBAColor.Red, pixelInfo);
                        int firstPixelGreen = PixelInfo.GetPixelColorAsInt(firstPixelStretchPtr, RGBAColor.Green, pixelInfo);
                        int firstPixelBlue = PixelInfo.GetPixelColorAsInt(firstPixelStretchPtr, RGBAColor.Blue, pixelInfo);
                        
                        PixelColor prevPixelColor = new PixelColor(firstPixelRed, firstPixelGreen, firstPixelBlue);

                        Boolean isStretchClosed = false;

                        for (int z = x; z < originalBitmapData.Width; z++)
                        {
                            byte* pixelPtr = originalRowPtr + ((z) * pixelSize);

                            int pixelRed = PixelInfo.GetPixelColorAsInt(pixelPtr, RGBAColor.Red, pixelInfo);
                            int pixelGreen = PixelInfo.GetPixelColorAsInt(pixelPtr, RGBAColor.Green, pixelInfo);
                            int pixelBlue = PixelInfo.GetPixelColorAsInt(pixelPtr, RGBAColor.Blue, pixelInfo);

                            PixelColor currentPixelColor = new PixelColor(pixelRed, pixelGreen, pixelBlue);

                            currentRowPixels[z] = currentPixelColor;

                            const int MAX_PIXEL_DIFFERENCE = 8;

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
                            //z - 1 because this pixel isn't part of this cluster.
                            PixelStretch stretch = new PixelStretch(x, z - 1, y);
                            currentRowClusters[z - 1].AddPixels(stretch);
                            isStretchClosed = true;

                            //set x to this pixels position and break, so a new pixel stretch can
                            //be created with this pixel as the starting pixel.
                            x += (z - x);
                            break;

                            createCluster:
                            ColorCluster newCluster = new ColorCluster();
                            currentRowClusters[z] = newCluster;
                            clusters.Add(newCluster);
                            goto end;

                            usePreviousAboveCluster:
                            currentRowClusters[z] = previousRowClusters[z];
                            goto end;

                            usePreviousPixelCluster:
                            currentRowClusters[z] = currentRowClusters[z - 1];
                            goto end;

                            combineClusters:
                            ColorCluster combinedCluster = currentRowClusters[z - 1].CombineWith(previousRowClusters[z]);
                            ColorCluster clusterAToRemove = currentRowClusters[z - 1];
                            ColorCluster clusterBToRemove = previousRowClusters[z];
                            for (int q = 0; q < currentRowClusters.Length; q++)
                            {
                                if (currentRowClusters[q] == clusterAToRemove)
                                {
                                    currentRowClusters[q] = combinedCluster;
                                }
                                else if (currentRowClusters[q] == clusterBToRemove)
                                {
                                    currentRowClusters[q] = combinedCluster;
                                }
                            }
                            for (int q = 0; q < previousRowClusters.Length; q++)
                            {
                                if (previousRowClusters[q] == clusterBToRemove)
                                {
                                    previousRowClusters[q] = combinedCluster;
                                }
                            }
                            clusters.Remove(clusterAToRemove);
                            clusters.Remove(clusterBToRemove);

                            clusters.Add(combinedCluster);
                            currentRowClusters[z] = combinedCluster;
                            goto end;

                            end:
                            prevPixelColor = currentPixelColor;
                        }

                        if (!isStretchClosed)
                        {
                            PixelStretch stretch = new PixelStretch(x, originalBitmapData.Width - 1, y);
                            currentRowClusters[originalBitmapData.Width - 1].AddPixels(stretch);
                            break;
                        }
                    }

                    ColorCluster[] tempCluster = previousRowClusters;
                    previousRowClusters = currentRowClusters;
                    currentRowClusters = tempCluster;

                    PixelColor[] tempPixel = previousRowPixels;
                    previousRowPixels = currentRowPixels;
                    currentRowPixels = tempPixel;
                }
            }
            image.UnlockBits(originalBitmapData);
        }
    }
}
