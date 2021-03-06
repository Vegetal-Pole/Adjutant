using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo1
{
    public class BSPAddressTranslator : IAddressTranslator
    {
        private readonly StructureBspBlock data;

        public int Magic => data.Magic - data.MetadataAddress;

        public int TagAddress => data.MetadataAddress;

        public BSPAddressTranslator(CacheFile cache, int id)
        {
            var bspData = cache.TagIndex.GetGlobalTag("scnr").ReadMetadata<scenario>().StructureBSPs.SingleOrDefault(i => (i.BSPReference.TagId) == id);
            if (bspData == null)
                throw new InvalidOperationException();

            data = bspData;
        }

        public long GetAddress(long pointer)
        {
            return (int)pointer - Magic;
        }

        public long GetPointer(long address)
        {
            return (int)address + Magic;
        }
    }
}
