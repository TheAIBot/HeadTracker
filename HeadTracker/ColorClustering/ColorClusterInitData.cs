using ImageInfo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public class ColorClusterInitData
    {
        public readonly List<PixelStretch> PixelStretches = new List<PixelStretch>();
        private readonly HashSet<ColorClusterInitData> SurroundingClusters = new HashSet<ColorClusterInitData>();
        private readonly List<ColorClusterInitData> SuperClusterOverTheseClusters = new List<ColorClusterInitData>();
        public int SurroundingClustersCount = 0;
        public long Red = 0;
        public long Green = 0;
        public long Blue = 0;
        public int ClusterSize = 0;
        public ColorClusterInitData partOf;

        public ColorCluster CreateColorCluster()
        {
            foreach (ColorClusterInitData cluster in SuperClusterOverTheseClusters)
            {
                cluster.FullyMergeClusters(PixelStretches);
            }

            long totalX = 0;
            long totalY = 0;

            foreach (PixelStretch stretch in PixelStretches)
            {
                totalX += (stretch.startX + stretch.endX) / 2;
                totalY += stretch.y;
            }

            Point centerPoint = new Point((int)(totalX / PixelStretches.Count), (int)(totalY / PixelStretches.Count));

            return new ColorCluster(PixelStretches, GetColorOfCluster(), ClusterSize, centerPoint);
        }

        private void FullyMergeClusters(List<PixelStretch> superClusterPixelStretches)
        {
            superClusterPixelStretches.AddRange(PixelStretches);

            foreach (ColorClusterInitData cluster in SuperClusterOverTheseClusters)
            {
                cluster.FullyMergeClusters(superClusterPixelStretches);
            }
        }

        public void AddPixels(PixelStretch pixelStretch, int red, int green, int blue)
        {
            PixelStretches.Add(pixelStretch);

            //If the pixel stretch is 1 long then startX == endX, but
            //there is still 1 pixel inside the stretch so add 1 to show that.
            ClusterSize += (pixelStretch.endX - pixelStretch.startX) + 1;

            //add colors to the clusters color so the cluster can have an average color
            this.Red += red;
            this.Green += green;
            this.Blue += blue;
        }

        public void AddCluster(ColorClusterInitData cluster)
        {
            SuperClusterOverTheseClusters.Add(cluster);
            SurroundingClustersCount += cluster.SurroundingClustersCount;
            ClusterSize += cluster.ClusterSize;

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

        public ColorClusterInitData GetBestMatchingSurroundingCluster()
        {
            PixelColor colorToMatch = GetColorOfCluster();

            ColorClusterInitData bestCluster = null;
            float bestDistance = 10000;

            foreach (ColorClusterInitData subCluster in GetSurroundingClusters())
            {
                ColorClusterInitData cluster = subCluster.GetSuperCluster();

                if (cluster == this)
                {
                    continue;
                }

                float distance = colorToMatch.Distance(cluster.GetColorOfCluster());
                if (distance < bestDistance || bestCluster == null)
                {
                    bestCluster = cluster;
                    bestDistance = distance;
                }
            }

            return bestCluster;
        }

        public ColorClusterInitData[] GetSurroundingClusters()
        {
            int newI;
            return GetSurroundingClusters(0, new ColorClusterInitData[SurroundingClustersCount], out newI);
        }
        public ColorClusterInitData[] GetSurroundingClusters(int i, ColorClusterInitData[] surClusters, out int newI)
        {
            foreach (ColorClusterInitData cluster in SurroundingClusters)
            {
                surClusters[i] = cluster;
                i++;
            }

            foreach (ColorClusterInitData cluster in SuperClusterOverTheseClusters)
            {
                int newnewI = 0;
                cluster.GetSurroundingClusters(i, surClusters, out newnewI);
                i = newnewI;
            }

            newI = i;
            return surClusters;
        }

        public List<List<PixelStretch>> GetPixelStretches()
        {
            List<List<PixelStretch>> allPixelStretches = new List<List<PixelStretch>>();
            allPixelStretches.Add(PixelStretches);

            foreach (ColorClusterInitData cluster in SuperClusterOverTheseClusters)
            {
                cluster.GetPixelStretches(allPixelStretches);
            }

            return allPixelStretches;
        }
        public List<List<PixelStretch>> GetPixelStretches(List<List<PixelStretch>> allPixelStretches)
        {
            allPixelStretches.Add(PixelStretches);

            foreach (ColorClusterInitData cluster in SuperClusterOverTheseClusters)
            {
                cluster.GetPixelStretches(allPixelStretches);
            }

            return allPixelStretches;
        }

        public ColorClusterInitData GetSuperCluster()
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

        public void AddSurroundingCluster(ColorClusterInitData cluster)
        {
            if (SurroundingClusters.Add(cluster))
            {
                SurroundingClustersCount++;
            }            
        }
    }
}
