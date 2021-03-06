using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo2
{
    [FixedSize(16)]
    public class ResourceInfoBlock
    {
        [Offset(4)]
        public short Type0 { get; set; }

        [Offset(6)]
        public short Type1 { get; set; }

        [Offset(8)]
        public int Size { get; set; }

        [Offset(12)]
        public int Offset { get; set; }
    }

    internal static class Halo2Common
    {
        public static IEnumerable<GeometryMaterial> GetMaterials(IEnumerable<ShaderBlock> shaderBlocks)
        {
            foreach (var shaderRef in shaderBlocks.Select(b => b.ShaderReference))
            {
                var shader = shaderRef.Tag?.ReadMetadata<shader>();
                if (shader == null)
                {
                    yield return null;
                    continue;
                }

                var bitmTag = shader.ShaderMaps[0].DiffuseBitmapReference.Tag;
                if (bitmTag == null)
                {
                    yield return null;
                    continue;
                }

                yield return new GeometryMaterial
                {
                    Name = Utils.GetFileName(shaderRef.Tag.FullPath),
                    Submaterials = new List<ISubmaterial>
                    {
                        new SubMaterial
                        {
                            Usage = MaterialUsage.Diffuse,
                            Bitmap = bitmTag.ReadMetadata<bitmap>(),
                            Tiling = new RealVector2D(1, 1)
                        }
                    }
                };
            }
        }
    }
}
