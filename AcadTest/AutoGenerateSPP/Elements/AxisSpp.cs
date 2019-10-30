using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Оси
    /// </summary>
    public class AxisSpp : BaseSppElemnt
    {
        private readonly Tolerance tolerance = new Tolerance(0.1, 0.1);

        public bool Hor { get; }
        public Line Line { get; }
        public double Score { get; }

        public AxisSpp([NotNull] Entity ent) : base(ent)
        {
            Line = (Line)ent;
            var dir = Line.EndPoint - Line.StartPoint;
            Hor = dir.IsParallelTo(Vector3d.XAxis, tolerance);
            Score = Hor ? Line.StartPoint.Y : Line.StartPoint.X;
        }
    }
}