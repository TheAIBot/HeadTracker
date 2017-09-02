using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public struct ColorPoint
    {
        public readonly ushort x;
        public readonly ushort y;
        public readonly ushort colorDistance;

        public ColorPoint(int xx, int yy, int cDistance)
        {
            this.x = (ushort)xx;
            this.y = (ushort)yy;
            this.colorDistance = (ushort)cDistance;
        }

        public int GetHash()
        {
            return this.x * short.MaxValue + this.y;
        }
    }
}
