using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo1
{
    public class TagAddressTranslator : IAddressTranslator
    {
        private readonly CacheFile cache;

        private int Magic => cache.TagIndex.Magic - (cache.Header.IndexAddress + cache.TagIndex.HeaderSize);

        public TagAddressTranslator(CacheFile cache)
        {
            this.cache = cache;
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
