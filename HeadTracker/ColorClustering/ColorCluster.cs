using ImageInfo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public class ColorCluster
    {
        public readonly List<List<PixelStretch>> PixelStretches = new List<List<PixelStretch>>();
        private readonly List<HashSet<ColorCluster>> SurroundingClusters = new List<HashSet<ColorCluster>>();
        public long Red = 0;
        public long Green = 0;
        public long Blue = 0;
        public int ClusterSize = 0;
        public Point CenterPoint;
        public ColorCluster partOf;

        public ColorCluster()
        {
            PixelStretches.Add(new List<PixelStretch>());
            SurroundingClusters.Add(new HashSet<ColorCluster>());
        }

        public void Init()
        {
            /*
            Point center = new Point(0, 0);
            foreach (ColorPoint cPoint in ColorPoints)
            {
                center.X += cPoint.x;
                center.Y += cPoint.y;
            }

            center.X /= ClusterSize;
            center.Y /= ClusterSize;

            CenterPoint = center;
            */    
        }

        public void AddPixels(PixelStretch pixelStretch, int red, int green, int blue)
        {
            PixelStretches[0].Add(pixelStretch);

            //If the pixel stretch is 1 long then startX == endX, but
            //there is still 1 pixel inside the stretch so add 1 to show that.
            ClusterSize += (pixelStretch.endX - pixelStretch.startX) + 1;

            //add colors to the clusters color so the cluster can have an average color
            this.Red += red;
            this.Green += green;
            this.Blue += blue;
        }

        public void AddCluster(ColorCluster cluster)
        {
            PixelStretches.AddRange(cluster.PixelStretches);
            ClusterSize += cluster.ClusterSize;
            SurroundingClusters.AddRange(cluster.SurroundingClusters);

            this.Red += cluster.Red;
            this.Green += cluster.Green;
            this.Blue += cluster.Blue;

            cluster.partOf = this;
        }

        public PixelColor GetColorOfCluster()
        {
            return new PixelColor(((int)(Red / ClusterSize)),
                                  ((int)(Green / ClusterSize)),
                                  ((int)(Blue / ClusterSize)));
        }

        public ColorCluster GetBestMatchingSurroundingCluster(HashSet<ColorCluster> removedClusters)
        {
            PixelColor colorToMatch = GetColorOfCluster();

            ColorCluster bestCluster = null;
            int bestDistance = int.MaxValue;

            foreach (HashSet<ColorCluster> surClusters in SurroundingClusters)
            {
                foreach (ColorCluster subCluster in surClusters)
                {
                    ColorCluster cluster = subCluster.GetSuperCluster();

                    if (/*removedClusters.Contains(cluster) || */cluster == this)
                    {
                        continue;
                    }

                    int distance = colorToMatch.Distance(cluster.GetColorOfCluster());
                    if (distance < bestDistance || bestCluster == null)
                    {
                        bestCluster = cluster;
                        bestDistance = distance;
                    }
                }
            }

            return bestCluster;
        }

        public ColorCluster GetSuperCluster()
        {
            if (partOf != null)
            {
                return partOf.GetSuperCluster();
            }
            else
            {
                return this;
            }
        }

        public void AddSurroundingCluster(ColorCluster cluster)
        {
            SurroundingClusters[0].Add(cluster);
        }
    }
}
