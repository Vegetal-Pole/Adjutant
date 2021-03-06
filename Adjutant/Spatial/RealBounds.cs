using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Spatial
{
    [FixedSize(8)]
    [StructLayout(LayoutKind.Sequential)]
    public struct RealBounds : IRealBounds
    {
        private float min, max;

        [Offset(0)]
        public float Min
        {
            get { return min; }
            set { min = value; }
        }

        [Offset(4)]
        public float Max
        {
            get { return max; }
            set { max = value; }
        }

        public RealBounds(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float Length => max - min;

        public float Midpoint => (min + max) / 2;

        public override string ToString() => Utils.CurrentCulture($"[{Min:F6}, {Max:F6}]");

        #region Equality Operators

        public static bool operator ==(RealBounds value1, RealBounds value2)
        {
            return value1.min == value2.min && value1.max == value2.max;
        }

        public static bool operator !=(RealBounds value1, RealBounds value2)
        {
            return !(value1 == value2);
        }

        public static bool Equals(RealBounds value1, RealBounds value2)
        {
            return value1.min.Equals(value2.min) && value1.max.Equals(value2.max);
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is RealBounds))
                return false;

            return RealBounds.Equals(this, (RealBounds)obj);
        }

        public bool Equals(RealBounds value)
        {
            return RealBounds.Equals(this, value);
        }

        public override int GetHashCode()
        {
            return min.GetHashCode() ^ max.GetHashCode();
        }

        #endregion
    }
}
