using System;
using AcadLib.RTree.SpatialIndex;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcadTest.AutoGenerateSPP.Elements
{
    public interface ISppElement : IEquatable<ISppElement>
    {
        ObjectId EntId { get; }
        Rectangle Rectangle { get; }
    }
}