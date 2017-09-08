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
        public readonly List<PixelStretch> PixelStretches = new List<PixelStretch>();
        private readonly HashSet<ColorCluster> SurroundingClusters = new HashSet<ColorCluster>();
        private readonly List<ColorCluster> SuperClusterOverTheseClusters = new List<ColorCluster>();
        public int SurroundingClustersCount = 0;
        public long Red = 0;
        public long Green = 0;
        public long Blue = 0;
        public int ClusterSize = 0;
        public Point CenterPoint;
        public ColorCluster partOf;

        public ColorCluster()
        {
        }

        public void Init()
        {  
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

        public void AddCluster(ColorCluster cluster)
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

        public ColorCluster GetBestMatchingSurroundingCluster()
        {
            PixelColor colorToMatch = GetColorOfCluster();

            ColorCluster bestCluster = null;
            int bestDistance = int.MaxValue;

            foreach (ColorCluster subCluster in GetSurroundingClusters())
            {
                ColorCluster cluster = subCluster.GetSuperCluster();

                if (cluster == this)
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

            return bestCluster;
        }

        public ColorCluster[] GetSurroundingClusters()
        {
            int newI;
            return GetSurroundingClusters(0, new ColorCluster[SurroundingClustersCount], out newI);
        }
        public ColorCluster[] GetSurroundingClusters(int i, ColorCluster[] surClusters, out int newI)
        {
            foreach (ColorCluster cluster in SurroundingClusters)
            {
                surClusters[i] = cluster;
                i++;
            }

            foreach (ColorCluster cluster in SuperClusterOverTheseClusters)
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

            foreach (ColorCluster cluster in SuperClusterOverTheseClusters)
            {
                cluster.GetPixelStretches(allPixelStretches);
            }

            return allPixelStretches;
        }
        public List<List<PixelStretch>> GetPixelStretches(List<List<PixelStretch>> allPixelStretches)
        {
            allPixelStretches.Add(PixelStretches);

            foreach (ColorCluster cluster in SuperClusterOverTheseClusters)
            {
                cluster.GetPixelStretches(allPixelStretches);
            }

            return allPixelStretches;
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
            if (SurroundingClusters.Add(cluster))
            {
                SurroundingClustersCount++;
            }            
        }
    }
}
