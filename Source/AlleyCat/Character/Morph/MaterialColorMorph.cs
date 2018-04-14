using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Godot;
using JetBrains.Annotations;

namespace AlleyCat.Character.Morph
{
    public class MaterialColorMorph : Morph<Color, MaterialColorMorphDefinition>
    {
        [NotNull]
        public MeshInstance Mesh { get; }

        public IEnumerable<SpatialMaterial> Materials =>
            SurfaceIndexes.Select(Mesh.GetSurfaceMaterial).OfType<SpatialMaterial>();

        public IEnumerable<int> SurfaceIndexes { get; }

        private readonly IDisposable _disposable;

        public MaterialColorMorph([NotNull] MeshInstance mesh,
            IEnumerable<int> indexes,
            [NotNull] MaterialColorMorphDefinition definition) : base(definition)
        {
            Ensure.Any.IsNotNull(mesh, nameof(mesh));
            Ensure.Any.IsNotNull(indexes, nameof(indexes));

            Mesh = mesh;
            SurfaceIndexes = indexes.ToList();

            _disposable = OnChange.Subscribe(v => Materials.ToList().ForEach(m => m.AlbedoColor = v));
        }

        protected override void Dispose(bool disposing)
        {
            _disposable?.Dispose();
           
            base.Dispose(disposing);
        }
    }
}
