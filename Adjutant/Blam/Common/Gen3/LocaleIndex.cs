﻿using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Adjutant.Blam.Common.Gen3
{
    public class LocaleIndex : ILocaleIndex
    {
        private readonly Dictionary<int, LocaleTable> languages;

        public LocaleIndex(IGen3CacheFile cache, int offset, int size, int count)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            languages = new Dictionary<int, LocaleTable>();

            var globalsTag = cache.TagIndex.GetGlobalTag("matg");
            using (var reader = cache.CreateReader(cache.DefaultAddressTranslator))
            {
                for (int i = 0; i < count; i++)
                {
                    reader.Seek(globalsTag.MetaPointer.Address + offset + i * size, SeekOrigin.Begin);
                    var definition = reader.ReadObject<LanguageDefinition>();
                    languages.Add(i, new LocaleTable(cache, definition));
                }
            }
        }

        public IEnumerable<ILocaleTable> Languages => languages.Values;

        public LocaleTable this[Language lang] => languages.ValueOrDefault((int)lang);

        public string this[Language lang, StringId key] => languages.ValueOrDefault((int)lang)?[key];

        ILocaleTable ILocaleIndex.this[Language lang] => this[lang];
    }

    public class LocaleTable : ILocaleTable, IEnumerable<KeyValuePair<StringId, string>>
    {
        private readonly IGen3CacheFile cache;
        private readonly LanguageDefinition definition;
        private readonly Dictionary<int, string> values;

        private bool isInitialised = false;

        public int Count => values.Count;

        public string this[StringId key]
        {
            get
            {
                if (!isInitialised)
                    ReadItems();

                return values.ContainsKey(key.Id) ? values[key.Id] : null;
            }
        }

        public LocaleTable(IGen3CacheFile cache, LanguageDefinition definition)
        {
            this.cache = cache;
            this.definition = definition;
            values = new Dictionary<int, string>(definition.StringCount);
        }

        private void ReadItems()
        {
            string key;
            switch (cache.CacheType)
            {
                case CacheType.HaloReachBeta:
                    key = HaloReach.CacheFile.BetaKey;
                    break;
                case CacheType.HaloReachRetail:
                    key = HaloReach.CacheFile.LocalesKey;
                    break;
                case CacheType.Halo4Beta:
                case CacheType.Halo4Retail:
                    key = Halo4.CacheFile.LocalesKey;
                    break;

                default:
                    key = null;
                    break;
            }

            using (var reader = cache.CreateReader(cache.DefaultAddressTranslator))
            {
                var localeSectionOffset = cache.Header.SectionOffsetTable?[3] ?? 0;
                var addr = definition.IndicesOffset + localeSectionOffset;
                reader.Seek(addr, SeekOrigin.Begin);
                var entries = reader.ReadEnumerable<LocaleEntry>(definition.StringCount).ToList();

                addr = definition.StringsOffset + localeSectionOffset;
                reader.Seek(addr, SeekOrigin.Begin);

                Stream ms = null;
                EndianReader tempReader;

                if (!string.IsNullOrEmpty(key))
                {
                    var decrypted = reader.ReadAesBytes(definition.StringsSize, key);
                    ms = new MemoryStream(decrypted);
                    tempReader = new EndianReader(ms);
                }
                else tempReader = reader.CreateVirtualReader();

                for (int i = 0; i < definition.StringCount; i++)
                {
                    if (entries[i].Offset < 0)
                        continue;

                    tempReader.Seek(entries[i].Offset, SeekOrigin.Begin);
                    values.Add(entries[i].StringId.Id, tempReader.ReadNullTerminatedString());
                }

                ms?.Dispose();
                tempReader.Dispose();
            }

            isInitialised = true;
        }

        #region IEnumerable
        public IEnumerator<KeyValuePair<StringId, string>> GetEnumerator()
        {
            if (!isInitialised)
                ReadItems();

            return ((IReadOnlyList<KeyValuePair<StringId, string>>)values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (!isInitialised)
                ReadItems();

            return ((IReadOnlyList<KeyValuePair<StringId, string>>)values).GetEnumerator();
        }
        #endregion

        #region ILocaleTable
        string ILocaleTable.this[StringId key] => this[key];

        int ILocaleTable.StringCount
        {
            get { return definition.StringCount; }
            set { definition.StringCount = value; }
        }

        int ILocaleTable.StringsSize
        {
            get { return definition.StringsSize; }
            set { definition.StringsSize = value; }
        }

        int ILocaleTable.IndicesOffset
        {
            get { return definition.IndicesOffset; }
            set { definition.IndicesOffset = value; }
        }

        int ILocaleTable.StringsOffset
        {
            get { return definition.StringsOffset; }
            set { definition.StringsOffset = value; }
        }
        #endregion

        [FixedSize(8)]
        public class LocaleEntry //must be public for dynamic reader to instanciate
        {
            [Offset(0)]
            public StringId StringId { get; set; }

            [Offset(4)]
            public int Offset { get; set; }
        }
    }
}