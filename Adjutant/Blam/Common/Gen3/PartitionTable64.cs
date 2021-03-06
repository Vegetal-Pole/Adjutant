using Adjutant.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Common.Gen3
{
    public class PartitionTable64 : IPartitionTable, IWriteable
    {
        private readonly IPartitionLayout[] partitions;

        public PartitionTable64(ICacheFile cache, EndianReader reader)
        {
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            partitions = new IPartitionLayout[6];

            for (int i = 0; i < partitions.Length; i++)
                partitions[i] = reader.ReadObject<PartitionLayout64>();
        }

        public void Write(EndianWriter writer, double? version)
        {
            foreach (var partition in partitions)
                writer.WriteObject(partition);
        }

        #region IPartitionTable
        public IPartitionLayout this[int index] => partitions[index];

        public int Count => partitions.Length;

        public IEnumerator<IPartitionLayout> GetEnumerator() => ((IReadOnlyList<IPartitionLayout>)partitions).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => partitions.GetEnumerator();
        #endregion
    }

    [FixedSize(16)]
    public class PartitionLayout64 : IPartitionLayout
    {
        [Offset(0)]
        public ulong Address { get; set; }

        [Offset(8)]
        public ulong Size { get; set; }
    }
}
