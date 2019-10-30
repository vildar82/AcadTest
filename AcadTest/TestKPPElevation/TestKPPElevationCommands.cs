using AcadLib;
using AcadLib.Extensions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JetBrains.Annotations;
using NetLib;
using System;
using System.Linq;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AcadTest
{
    public static class TestKPPElevationCommands
    {
        private static bool UseBedit;
        private static Editor ed;

        [CommandMethod("StartTest")]
        public static void StartTest()
        {
            var doc = AcadHelper.Doc;
            ed = doc.Editor;
            UseBedit = false;
            BeginDoubleClick += Application_BeginDoubleClick;
            ed.PointMonitor += Ed_PointMonitor;
            DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
        }

        private static void DocumentManager_DocumentLockModeChanged(object sender, [NotNull] DocumentLockModeChangedEventArgs e)
        {
            if (UseBedit) return;
            if (e.GlobalCommandName == "BEDIT")
            {
                e.Veto();
            }
        }

        private static void Ed_PointMonitor(object sender, [NotNull] PointMonitorEventArgs e)
        {
            e.AppendToolTipText(DateTime.Now.ToString());
        }

        private static void Application_BeginDoubleClick(object sender, [NotNull] BeginDoubleClickEventArgs e)
        {
            UseBedit = true;
            var doc = AcadHelper.Doc;
            var sel = ed.SelectAtPickBox(e.Location);
            if (sel.Status == PromptStatus.OK)
            {
                var selIds = sel.Value.GetObjectIds();
                if (selIds.All(a => a.ObjectClass != RXObject.GetClass(typeof(BlockReference))))
                {
                    return;
                }
                var db = doc.Database;
                using (doc.LockDocument())
                using (var t = db.TransactionManager.StartTransaction())
                {
                    foreach (var blRef in selIds.GetObjects<BlockReference>())
                    {
                        var dynBtr = blRef.DynamicBlockTableRecord.GetObject<BlockTableRecord>() ?? throw new InvalidOperationException();
                        if (dynBtr.Name.StartsWith("КПП"))
                        {
                            UseBedit = false;
                            var btr = blRef.BlockTableRecord.GetObject<BlockTableRecord>();
                            var section = GetSection(btr, e.Location.TransformBy(blRef.BlockTransform.Inverse()));
                            if (section == null)
                            {
                                ed.WriteMessage("\nНе определена секция в блоке КПП по двойному клику.");
                                return;
                            }
                            IndexerElevation(section);
                            t.Commit();
                            doc.Editor.Regen();
                            return;
                        }
                    }
                }
            }
        }

        private static void IndexerElevation(BlockReference section)
        {
            var atrElev = section.EnumerateAttributes().FirstOrDefault(a => a.Tag.EqualsIgnoreCase("Этажность"));
            if (atrElev == null)
            {
                ed.WriteMessage("\nНе найден атрибут этажности в блоке секции.");
                return;
            }
            var elevVM = new ElevationViewModel(int.Parse(atrElev.Text));
            var elevView = new ElevationView(elevVM);
            if (elevView.ShowDialog() == true)
            {
                var atr = atrElev.IdAtr.GetObject<AttributeReference>(OpenMode.ForWrite);
                atr.TextString = elevVM.Elevation.ToString();
            }
        }

        [CanBeNull]
        private static BlockReference GetSection(BlockTableRecord btr, Point3d pt)
        {
            return btr.GetObjects<BlockReference>().FirstOrDefault(b =>
                b.Visible &&
                b.GeometricExtents.IsPointInBounds(pt) &&
                b.GetEffectiveName().StartsWith("ГП_СПП_2018"));
        }
    }

    public static class EditorSelectionExtension
    {
        public static PromptSelectionResult SelectAtPickBox([NotNull] this Editor ed, Point3d pt)
        {
            var points = new Point3dCollection
            {
                GetPoint(pt, -1, -1),
                GetPoint(pt, -1, 1),
                GetPoint(pt, 1, 1),
                GetPoint(pt, 1, -1),
            };
            return ed.SelectCrossingPolygon(points);
        }

        private static Point3d GetPoint(Point3d pt, int dx, int dy)
        {
            return new Point3d(pt.X + dx, pt.Y + dy, 0);
        }
    }
}