using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Spatial
{
    public class RealBounds3D
    {
        public RealBounds XBounds { get; set; }
        public RealBounds YBounds { get; set; }
        public RealBounds ZBounds { get; set; }

        public float Length
        {
            get
            {
                return (float)Math.Sqrt(
                    Math.Pow(XBounds.Length, 2) +
                    Math.Pow(YBounds.Length, 2) +
                    Math.Pow(ZBounds.Length, 2));
            }
        }
    }
}
