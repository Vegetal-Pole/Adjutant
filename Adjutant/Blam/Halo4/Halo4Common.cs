﻿using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Adjutant.Blam.Halo4
{
    [FixedSize(28)]
    public struct VertexBufferInfo
    {
        [Offset(0)]
        public int VertexCount { get; set; }

        [Offset(8)]
        public int DataLength { get; set; }
    }

    [FixedSize(28)]
    public struct IndexBufferInfo
    {
        [Offset(0)]
        public IndexFormat IndexFormat { get; set; }

        [Offset(8)]
        public int DataLength { get; set; }
    }

    internal static class Halo4Common
    {
        public static IEnumerable<GeometryMaterial> GetMaterials(IList<ShaderBlock> shaders)
        {
            for (int i = 0; i < shaders.Count; i++)
            {
                var tag = shaders[i].MaterialReference.Tag;
                if (tag == null)
                {
                    yield return null;
                    continue;
                }

                var material = new GeometryMaterial
                {
                    Name = Utils.GetFileName(tag.FullPath)
                };

                var shader = tag?.ReadMetadata<material>();
                if (shader == null)
                {
                    yield return material;
                    continue;
                }

                var subMaterials = new List<ISubmaterial>();
                var map = shader.ShaderProperties.FirstOrDefault()?.ShaderMaps.FirstOrDefault();
                var bitmTag = map?.BitmapReference.Tag;
                if (bitmTag == null)
                    continue;

                var tile = map.TilingIndex == byte.MaxValue
                    ? (RealVector4D?)null
                    : shader.ShaderProperties[0].TilingData[map.TilingIndex];

                subMaterials.Add(new SubMaterial
                {
                    Usage = MaterialUsage.Diffuse,
                    Bitmap = bitmTag.ReadMetadata<bitmap>(),
                    Tiling = new RealVector2D(tile?.X ?? 1, tile?.Y ?? 1)
                });

                if (subMaterials.Count == 0)
                {
                    yield return material;
                    continue;
                }

                material.Submaterials = subMaterials;

                yield return material;
            }
        }

        public static IEnumerable<GeometryMesh> GetMeshes(ICacheFile cache, ResourceIdentifier resourcePointer, IList<SectionBlock> sections, Func<SectionBlock, short?> boundsIndex)
        {
            VertexBufferInfo[] vertexBufferInfo;
            IndexBufferInfo[] indexBufferInfo;

            var resourceGestalt = cache.TagIndex.GetGlobalTag("zone").ReadMetadata<cache_file_resource_gestalt>();
            var entry = resourceGestalt.ResourceEntries[resourcePointer.ResourceIndex];
            using (var cacheReader = cache.CreateReader(cache.DefaultAddressTranslator))
            using (var reader = cacheReader.CreateVirtualReader(resourceGestalt.FixupDataPointer.Address))
            {
                var fixupOffset = entry.FixupOffsets.FirstOrDefault();
                reader.Seek(fixupOffset + (entry.FixupSize - 24), SeekOrigin.Begin);
                var vertexBufferCount = reader.ReadInt32();
                reader.Seek(8, SeekOrigin.Current);
                var indexBufferCount = reader.ReadInt32();

                reader.Seek(fixupOffset, SeekOrigin.Begin);
                vertexBufferInfo = reader.ReadEnumerable<VertexBufferInfo>(vertexBufferCount).ToArray();
                reader.Seek(12 * vertexBufferCount, SeekOrigin.Current); //12 byte struct here for each vertex buffer
                indexBufferInfo = reader.ReadEnumerable<IndexBufferInfo>(indexBufferCount).ToArray();
                //12 byte struct here for each index buffer
                //4x 12 byte structs here
            }

            using (var ms = new MemoryStream(resourcePointer.ReadData()))
            using (var reader = new EndianReader(ms, ByteOrder.BigEndian))
            {
                var doc = new XmlDocument();
                doc.LoadXml(Adjutant.Properties.Resources.Halo4VertexBuffer);

                var lookup = doc.FirstChild.ChildNodes.Cast<XmlNode>()
                    .ToDictionary(n => Convert.ToInt32(n.Attributes["type"].Value, 16));

                foreach (var section in sections)
                {
                    if (section.VertexBufferIndex < 0 || section.IndexBufferIndex < 0)
                    {
                        yield return new GeometryMesh();
                        continue;
                    }

                    var node = lookup[section.VertexFormat];
                    var vInfo = vertexBufferInfo[section.VertexBufferIndex];
                    var iInfo = indexBufferInfo[section.IndexBufferIndex];

                    Func<XmlNode, string, bool> hasUsage = (n, u) =>
                    {
                        return n.ChildNodes.Cast<XmlNode>().Any(c => c.Attributes["usage"]?.Value == u);
                    };

                    var skinType = VertexWeights.None;
                    if (hasUsage(node, "blendindices"))
                        skinType = hasUsage(node, "blendweight") ? VertexWeights.Skinned : VertexWeights.Rigid;
                    else if (section.NodeIndex < byte.MaxValue)
                        skinType = VertexWeights.Rigid;

                    var mesh = new GeometryMesh
                    {
                        IndexFormat = iInfo.IndexFormat,
                        Vertices = new IVertex[vInfo.VertexCount],
                        VertexWeights = skinType,
                        NodeIndex = section.NodeIndex == byte.MaxValue ? (byte?)null : section.NodeIndex,
                        BoundsIndex = boundsIndex(section)
                    };

                    mesh.Submeshes.AddRange(
                        section.Submeshes.Select(s => new GeometrySubmesh
                        {
                            MaterialIndex = s.ShaderIndex,
                            IndexStart = s.IndexStart,
                            IndexLength = s.IndexLength
                        })
                    );

                    var address = entry.ResourceFixups[section.VertexBufferIndex].Offset & 0x0FFFFFFF;
                    reader.Seek(address, SeekOrigin.Begin);
                    for (int i = 0; i < vInfo.VertexCount; i++)
                    {
                        var vert = new XmlVertex(reader, node);
                        mesh.Vertices[i] = vert;
                    }

                    var totalIndices = section.Submeshes.Sum(s => s.IndexLength);
                    address = entry.ResourceFixups[vertexBufferInfo.Length * 2 + section.IndexBufferIndex].Offset & 0x0FFFFFFF;
                    reader.Seek(address, SeekOrigin.Begin);
                    if (vInfo.VertexCount > ushort.MaxValue)
                        mesh.Indicies = reader.ReadEnumerable<int>(totalIndices).ToArray();
                    else mesh.Indicies = reader.ReadEnumerable<ushort>(totalIndices).Select(i => (int)i).ToArray();

                    yield return mesh;
                }
            }
        }
    }
}