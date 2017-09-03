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
        public int ClusterSize = 0;
        public Point CenterPoint;

        public ColorCluster()
        {
            PixelStretches.Add(new List<PixelStretch>());
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

        public void AddPixels(PixelStretch pixelStretch)
        {
            PixelStretches[0].Add(pixelStretch);

            //If the pixel stretch is 1 long then startX == endX, but
            //there is still 1 pixel inside the stretch so add 1 to show that.
            ClusterSize += (pixelStretch.endX - pixelStretch.startX) + 1;
        }

        public void AddCluster(ColorCluster cluster)
        {
            PixelStretches.AddRange(cluster.PixelStretches);
            ClusterSize += cluster.ClusterSize;
        }

        public ColorCluster CombineWith(ColorCluster cluster)
        {
            ColorCluster combinedCluster = new ColorCluster();
            combinedCluster.AddCluster(this);
            combinedCluster.AddCluster(cluster);

            return combinedCluster;
        }
    }
}
