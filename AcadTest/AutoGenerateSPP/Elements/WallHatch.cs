using Autodesk.AutoCAD.DatabaseServices;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Штриховки стен
    /// </summary>
    public class WallHatch : BaseSppElemnt
    {
        public Hatch Hatch { get; }

        public WallHatch([NotNull] Entity ent) : base(ent)
        {
            Hatch = (Hatch)ent;
        }
    }
}