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
        public readonly PixelColor ClusterColor;
        public readonly int ClusterSize = 0;
        public readonly Point CenterPoint;

        public ColorCluster(List<PixelStretch> stretches, PixelColor color, int size, Point center)
        {
            this.PixelStretches = stretches;
            this.ClusterColor = color;
            this.ClusterSize = size;
            this.CenterPoint = center;
        }
    }
}
