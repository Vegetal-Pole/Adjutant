using Adjutant.Blam.Common;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo3
{
    public enum PageType
    {
        Auto,
        Primary,
        Secondary
    }

    public struct ResourceIdentifier
    {
        private const string shared_map = "shared.map";

        private readonly ICacheFile cache;
        private readonly int identifier; //actually two shorts

        public ResourceIdentifier(int identifier, ICacheFile cache)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            this.cache = cache;
            this.identifier = identifier;
        }

        public ResourceIdentifier(DependencyReader reader, ICacheFile cache)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            this.cache = cache;
            identifier = reader.ReadInt32();
        }

        public int Value => identifier;

        public int ResourceIndex => identifier & ushort.MaxValue;

        public byte[] ReadData(PageType mode) => ReadData(mode, int.MaxValue);

        public byte[] ReadData(PageType mode, int maxLength)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            if (cache.CacheType <= CacheType.Halo3Beta)
                return ReadDataHalo3Beta(mode, maxLength);

            var resourceGestalt = cache.TagIndex.GetGlobalTag("zone").ReadMetadata<cache_file_resource_gestalt>();
            var resourceLayoutTable = cache.TagIndex.GetGlobalTag("play").ReadMetadata<cache_file_resource_layout_table>();
            var entry = resourceGestalt.ResourceEntries[ResourceIndex];

            if (entry.SegmentIndex < 0)
                throw new InvalidOperationException("Data not found");

            var segment = resourceLayoutTable.Segments[entry.SegmentIndex];
            var useSecondary = mode == PageType.Secondary || (mode == PageType.Auto && segment.SecondaryPageIndex >= 0);

            var pageIndex = useSecondary ? segment.SecondaryPageIndex : segment.PrimaryPageIndex;
            var segmentOffset = useSecondary ? segment.SecondaryPageOffset : segment.PrimaryPageOffset;

            if (pageIndex < 0 || segmentOffset < 0)
                throw new InvalidOperationException("Data not found");

            var page = resourceLayoutTable.Pages[pageIndex];
            if (mode == PageType.Auto && (page.DataOffset < 0 || page.CompressedSize == 0))
            {
                pageIndex = segment.PrimaryPageIndex;
                segmentOffset = segment.PrimaryPageOffset;
                page = resourceLayoutTable.Pages[pageIndex];
            }

            var targetFile = cache.FileName;
            if (page.CacheIndex >= 0)
            {
                var directory = Directory.GetParent(cache.FileName).FullName;
                var mapName = Utils.GetFileName(resourceLayoutTable.SharedCaches[page.CacheIndex].FileName);
                targetFile = Path.Combine(directory, mapName);
            }

            using (var fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            using (var reader = new EndianReader(fs, cache.ByteOrder))
            {
                int dataTableAddress;
                switch (cache.CacheType)
                {
                    case CacheType.MccHalo3:
                    case CacheType.MccHalo3U4:
                    case CacheType.MccHalo3ODST:
                        if (page.CacheIndex >= 0)
                            dataTableAddress = 12288; //header size
                        else
                        {
                            reader.Seek(1208, SeekOrigin.Begin);
                            dataTableAddress = reader.ReadInt32();
                        }
                        break;
                    default:
                        reader.Seek(1136, SeekOrigin.Begin); //xbox
                        dataTableAddress = reader.ReadInt32();
                        break;
                }

                reader.Seek(dataTableAddress + page.DataOffset, SeekOrigin.Begin);
                return ContentFactory.GetResourceData(reader, cache.Metadata.ResourceCodec, maxLength, segmentOffset, page.CompressedSize, page.DecompressedSize);
            }
        }

        private byte[] ReadDataHalo3Beta(PageType mode, int maxLength)
        {
            var resourceGestalt = cache.TagIndex.GetGlobalTag("zone").ReadMetadata<cache_file_resource_gestalt>();
            var directory = Directory.GetParent(cache.FileName).FullName;
            var entry = resourceGestalt.ResourceEntries[ResourceIndex];

            var useSecondary = mode == PageType.Secondary || (mode == PageType.Auto && entry.SecondaryOffset > 0);

            var address = useSecondary ? entry.SecondaryOffset : entry.PrimaryOffset;
            var size = useSecondary ? entry.SecondarySize : entry.PrimarySize;

            var targetFile = entry.CacheIndex == -1 ? cache.FileName : Path.Combine(directory, shared_map);

            using (var fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            using (var reader = new EndianReader(fs))
            {
                reader.Seek(address, SeekOrigin.Begin);
                return ContentFactory.GetResourceData(reader, cache.Metadata.ResourceCodec, maxLength, 0, size, size);
            }
        }

        public byte[] ReadSoundData()
        {
            var directory = Directory.GetParent(cache.FileName).FullName;
            var resourceGestalt = cache.TagIndex.GetGlobalTag("zone").ReadMetadata<cache_file_resource_gestalt>();
            var resourceLayoutTable = cache.TagIndex.GetGlobalTag("play").ReadMetadata<cache_file_resource_layout_table>();
            var entry = resourceGestalt.ResourceEntries[ResourceIndex];

            if (entry.SegmentIndex < 0)
                throw new InvalidOperationException("Data not found");

            var segment = resourceLayoutTable.Segments[entry.SegmentIndex];
            var size1 = resourceLayoutTable.SizeGroups[segment.PrimarySizeIndex];
            var size2 = resourceLayoutTable.SizeGroups[segment.SecondarySizeIndex];
            var page1 = resourceLayoutTable.Pages[segment.PrimaryPageIndex];
            var page2 = resourceLayoutTable.Pages[segment.SecondaryPageIndex];

            if (page1.CompressedSize != page1.DecompressedSize || page2.CompressedSize != page2.DecompressedSize)
                throw new NotSupportedException("Compressed sound data");

            if (size2.Sizes.Count > 1)
                throw new NotSupportedException("Segmented sound data");

            var output = new byte[size1.TotalSize + size2.TotalSize];
            if (page1.CompressedSize > 0 && size1.TotalSize > 0)
            {
                var temp = ReadSoundData(directory, resourceLayoutTable, page1, size1.TotalSize);
                Array.Copy(temp, segment.PrimaryPageOffset, output, 0, size1.TotalSize);
            }

            if (page2.CompressedSize > 0 && size2.TotalSize > 0)
            {
                var temp = ReadSoundData(directory, resourceLayoutTable, page2, size2.TotalSize);
                Array.Copy(temp, segment.SecondaryPageOffset, output, size1.TotalSize, size2.TotalSize);
            }

            return output;
        }

        private byte[] ReadSoundData(string directory, cache_file_resource_layout_table resourceLayoutTable, PageBlock page, int size)
        {
            var targetFile = cache.FileName;
            if (page.CacheIndex >= 0)
            {
                var mapName = Utils.GetFileName(resourceLayoutTable.SharedCaches[page.CacheIndex].FileName);
                targetFile = Path.Combine(directory, mapName);
            }

            using (var fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            using (var reader = new EndianReader(fs, cache.ByteOrder))
            {
                reader.Seek(1136, SeekOrigin.Begin);
                var dataTableAddress = reader.ReadInt32();

                reader.Seek(dataTableAddress + page.DataOffset, SeekOrigin.Begin);
                return reader.ReadBytes(Math.Max(page.CompressedSize, size));
            }
        }

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

        #region Equality Operators

        public static bool operator ==(ResourceIdentifier value1, ResourceIdentifier value2)
        {
            return value1.identifier == value2.identifier;
        }

        public static bool operator !=(ResourceIdentifier value1, ResourceIdentifier value2)
        {
            return !(value1 == value2);
        }

        public static bool Equals(ResourceIdentifier value1, ResourceIdentifier value2)
        {
            return value1.identifier.Equals(value2.identifier);
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is ResourceIdentifier))
                return false;

            return ResourceIdentifier.Equals(this, (ResourceIdentifier)obj);
        }

        public bool Equals(ResourceIdentifier value)
        {
            return ResourceIdentifier.Equals(this, value);
        }

        public override int GetHashCode()
        {
            return identifier.GetHashCode();
        }

        #endregion
    }
}
