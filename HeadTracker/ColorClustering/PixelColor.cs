using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public struct PixelColor
    {
        public readonly int red;
        public readonly int green;
        public readonly int blue;

        public PixelColor(int r, int g, int b)
        {
            this.red = r;
            this.green = g;
            this.blue = b;
        }

        public int Distance(PixelColor pColor)
        {
            int redDistance = red - pColor.red;
            int greenDistance = green - pColor.green;
            int blueDistance = blue - pColor.blue;

            return (int)Math.Sqrt((redDistance * redDistance) +
                                  (greenDistance * greenDistance) +
                                  (blueDistance * blueDistance));
        }
    }
}
