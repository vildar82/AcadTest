using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Visual;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcadTest
{
    public class DimVisual : VisualTransient
    {
        private readonly Dimension dim;

        public DimVisual(Dimension dim)
        {
            this.dim = dim;
        }

        public override List<Entity> CreateVisual()
        {
            return new List<Entity> { dim };
        }
    }
}
