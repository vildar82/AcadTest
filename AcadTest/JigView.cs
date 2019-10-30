using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AcadTest
{
    public class JigView : DrawJig
    {
        private Point3d pt;
        private Window1 view;

        public void Start(Document doc)
        {
            view = new Window1();
            Application.ShowModelessWindow(view);
            doc.Editor.Drag(this);
        }

        /// <inheritdoc />
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var res = prompts.AcquirePoint();
            if ((res.Value - pt).Length < 1)
                return SamplerStatus.NoChange;
            pt = res.Value;
            return SamplerStatus.OK;
        }

        /// <inheritdoc />
        protected override bool WorldDraw(WorldDraw draw)
        {
            return draw.Geometry.Circle(pt, view.Value ? 100 : 500, Vector3d.ZAxis);
        }
    }
}
