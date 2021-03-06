using Adjutant.Geometry;
using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo2
{
    public class Vertex : IVertex
    {
        public UInt16N4 Position { get; set; }

        public UInt16N2 TexCoords { get; set; }

        public HenDN3 Normal { get; set; }

        public RealVector4D BlendIndices { get; set; }

        public RealVector4D BlendWeight { get; set; }

        #region IVertex

        IReadOnlyList<IXMVector> IVertex.Position => new IXMVector[] { Position };

        IReadOnlyList<IXMVector> IVertex.TexCoords => new IXMVector[] { TexCoords };

        IReadOnlyList<IXMVector> IVertex.Normal => new IXMVector[] { Normal };

        IReadOnlyList<IXMVector> IVertex.Binormal => new IXMVector[0];

        IReadOnlyList<IXMVector> IVertex.Tangent => new IXMVector[0];

        IReadOnlyList<IXMVector> IVertex.BlendIndices => new IXMVector[] { BlendIndices };

        IReadOnlyList<IXMVector> IVertex.BlendWeight => new IXMVector[] { BlendWeight };

        IReadOnlyList<IXMVector> IVertex.Color => new IXMVector[0];

        #endregion
    }

    public class WorldVertex : IVertex
    {
        public RealVector3D Position { get; set; }

        public RealVector2D TexCoords { get; set; }

        public HenDN3 Normal { get; set; }

        #region IVertex

        IReadOnlyList<IXMVector> IVertex.Position => new IXMVector[] { Position };

        IReadOnlyList<IXMVector> IVertex.TexCoords => new IXMVector[] { TexCoords };

        IReadOnlyList<IXMVector> IVertex.Normal => new IXMVector[] { Normal };

        IReadOnlyList<IXMVector> IVertex.Binormal => new IXMVector[0];

        IReadOnlyList<IXMVector> IVertex.Tangent => new IXMVector[0];

        IReadOnlyList<IXMVector> IVertex.BlendIndices => new IXMVector[0];// { new RealVector2D(NodeIndex1, NodeIndex2) };

        IReadOnlyList<IXMVector> IVertex.BlendWeight => new IXMVector[0];// { NodeWeights };

        IReadOnlyList<IXMVector> IVertex.Color => new IXMVector[0];

        #endregion
    }
}
