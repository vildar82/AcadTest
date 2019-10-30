using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Стены
    /// </summary>
    public class WallSpp : BaseSppElemnt
    {
        public Point3d EndPt { get; }
        public double Length { get; }
        public Point3d StartPt { get; }
        public bool IsOuterWall { get; set; }

        public WallSpp([NotNull] Entity ent) : base(ent)
        {
            var line = (Line)ent;
            IsOuterWall = line.Layer.StartsWith("A");
            Length = line.Length;
            StartPt = line.StartPoint;
            EndPt = line.EndPoint;
        }
    }
}