using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeadTracker
{
    public struct PixelStretch
    {
        public readonly short startX;
        public readonly short endX;
        public readonly short y;

        public PixelStretch(int sX, int eX, int yy)
        {
            this.startX = (short)sX;
            this.endX = (short)eX;
            this.y = (short)yy;
        }
    }
}
