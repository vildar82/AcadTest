using Autodesk.AutoCAD.DatabaseServices;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Штриховки квартир
    /// </summary>
    public class ApartHatch : BaseSppElemnt
    {
        public ApartHatch([NotNull] Entity ent) : base(ent)
        {
        }
    }
}