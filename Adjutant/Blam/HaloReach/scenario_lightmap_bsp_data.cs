using Adjutant.Blam.Common;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.HaloReach
{
    public class scenario_lightmap_bsp_data
    {
        [Offset(112, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(124, MinVersion = (int)CacheType.HaloReachRetail)]
        public BlockCollection<SectionBlock> Sections { get; set; }

        [Offset(244, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(268, MinVersion = (int)CacheType.HaloReachRetail)]
        public ResourceIdentifier ResourcePointer { get; set; }
    }
}
