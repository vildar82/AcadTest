using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Окна
    /// </summary>
    public class WindowSpp : BaseSppElemnt
    {
        public WindowSpp([NotNull] Entity ent) : base(ent)
        {
        }
    }
}