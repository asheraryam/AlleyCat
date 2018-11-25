using System.Linq;
using AlleyCat.Common;
using Godot;
using Godot.Collections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace AlleyCat.Character.Morph
{
    public class MaterialColorMorphDefinitionFactory : ColorMorphDefinitionFactory<MaterialColorMorphDefinition>
    {
        [Export]
        public string Mesh { get; set; }

        [Export]
        public Array<string> Materials { get; set; }

        protected override Validation<string, MaterialColorMorphDefinition> CreateService(
            string key, string displayName, ILoggerFactory loggerFactory)
        {
            return
                from mesh in Mesh.TrimToOption()
                    .ToValidation("Missing the target mesh's name.")
                from materials in Optional(Materials).Filter(Enumerable.Any)
                    .ToValidation("Missing the target material list.")
                select new MaterialColorMorphDefinition(
                    key, displayName, mesh, materials, Default, UseAlpha, loggerFactory);
        }
    }
}