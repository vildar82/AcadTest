using System;
using AcadLib.RTree.SpatialIndex;
using Autodesk.AutoCAD.DatabaseServices;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    public abstract class BaseSppElemnt : ISppElement
    {
        public ObjectId EntId { get; }
        public Extents3d Ext { get; }
        public Rectangle Rectangle { get; }

        protected BaseSppElemnt([NotNull] Entity ent)
        {
            EntId = ent.Id;
            Ext = ent.GeometricExtents;
            Rectangle = new Rectangle(Ext);
        }

        public bool Equals(ISppElement other)
        {
            return other != null && EntId.Equals(other.EntId);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ISppElement)obj);
        }

        public override int GetHashCode()
        {
            return EntId.GetHashCode();
        }
    }
}