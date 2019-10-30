// Khisyametdinovvt Хисяметдинов Вильдар Тямильевич
// 2018 01 29 10:05

using System;
using System.Collections.Generic;
using System.Linq;
using AcadLib;
using AcadLib.Comparers;
using AcadLib.Geometry;
using AcadLib.Layers;
using AcadLib.RTree.SpatialIndex;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP.Elements
{
    public class Section : IDisposable
    {
        private const double scale = 0.001;
        private readonly Settings settings = AutoGenerateSppService.Settings;
        private BlockTableRecord block;
        private int index;
        private SppParser parser;
        private Polyline poly;
        private Transaction t;
        private Matrix3d transform;
        public HashSet<ApartHatch> ApartHatches { get; set; } = new HashSet<ApartHatch>();
        public string BlockName { get; private set; }
        public HashSet<ISppElement> Elements { get; set; }
        public Point3d LocationInModel { get; private set; }
        public string Mark { private get; set; }
        public Rectangle RectangleOffset { get; set; }

        public void Dispose()
        {
            poly?.Dispose();
            block?.Dispose();
        }

        /// <summary>
        ///     Добавление внутренних стен
        /// </summary>
        private void AddIWalls()
        {
            var wallsH = Elements.OfType<WallHatch>().ToList();
            using (var disposeLines = new DisposableSet<Line>())
            {
                var lines = wallsH.Select(s => GetExtentLine(s.Ext, false)).Where(w => w.Length > 2500).ToList();
                disposeLines.AddRange(lines);
                var lineEqComparer = new LineEqualityComparer(new Tolerance(0.1, 0.1), 450, 450);
                while (true)
                {
                    var linesCountBefore = lines.Count;
                    lines = lines.GroupBy(g => g, lineEqComparer).Select(s => s.ToList().GetUnionLine()).ToList();
                    disposeLines.AddRange(lines);
                    if (lines.Count == linesCountBefore)
                        break;
                }
                foreach (var line in lines)
                {
                    line.TransformBy(transform);
                    if (IsInnerWall(line))
                    {
                        line.LayerId = settings.LayerApart.LayerId;
                        block.AppendEntity(line);
                        t.AddNewlyCreatedDBObject(line, true);
                    }
                }
            }
        }

        private void AddLine(Point3d pt1, Point3d pt2, ObjectId layerId)
        {
            using (var line = new Line(pt1, pt2) {LayerId = layerId})
            {
                line.TransformBy(transform);
                block.AppendEntity(line);
                t.AddNewlyCreatedDBObject(line, true);
            }
        }

        /// <summary>
        ///     Внешние стены
        /// </summary>
        private void AddOuterWallContour()
        {
            // Внешние оси
            var axis = Elements.OfType<AxisSpp>().ToList();
            var horAxis = axis.Where(w => w.Hor).OrderBy(o => o.Score).ToList();
            var verAxis = axis.Where(w => !w.Hor).OrderBy(o => o.Score).ToList();

            // Последовательный поиск точек контура внешних стен по часовой стрелке от minHorAxis/minVerAxis
            var pt = GetOuterWallPointVer(verAxis.First(), horAxis);

            // Частный случай - для прямоугольной секции
            var minHorAxis = horAxis.First();
            var maxHorAxis = horAxis.Last();
            var minVerAxis = verAxis.First();
            var maxVerAxis = verAxis.Last();
            AddPolyRectangle(minVerAxis.Score, maxVerAxis.Score, minHorAxis.Score, maxHorAxis.Score,
                settings.OffsetOuterWallFromAxis, settings.LayerGNS);
        }

        private static Point3d GetOuterWallPointVer(AxisSpp verAxis, List<AxisSpp> horAxes)
        {
            foreach (var horAxis in horAxes)
            {
                var pt = new Point3d(verAxis.Score, horAxis.Score,0);
                return pt;
                //var nears = parser.Nearest(pt, 500).OfType<>();
            }
            return Point3d.Origin;
        }

        private void AddPolyRectangle(double minX, double maxX, double minY, double maxY, double offset,
            [NotNull] LayerInfo layer)
        {
            poly = new Polyline();
            poly.AddVertexAt(0, new Point2d(minX - offset, minY - offset), 0, 0, 0);
            poly.AddVertexAt(1, new Point2d(minX - offset, maxY + offset), 0, 0, 0);
            poly.AddVertexAt(2, new Point2d(maxX + offset, maxY + offset), 0, 0, 0);
            poly.AddVertexAt(3, new Point2d(maxX + offset, minY - offset), 0, 0, 0);
            poly.Closed = true;
            poly.LayerId = layer.LayerId;
            poly.TransformBy(transform);
            block.AppendEntity(poly);
            t.AddNewlyCreatedDBObject(poly, true);
        }

        private void AddText(string text, Point3d pt, double height)
        {
            using (var dbText = new DBText
            {
                TextString = text,
                Justify = AttachmentPoint.MiddleCenter,
                AlignmentPoint = pt,
                Height = height,
                LayerId = settings.LayerApartText.LayerId
            })
            {
                block.AppendEntity(dbText);
                t.AddNewlyCreatedDBObject(dbText, true);
            }
        }

        /// <summary>
        ///     Добавление подписей
        /// </summary>
        private void AddTexts()
        {
            var texts = Elements.OfType<TextSpp>().ToList();
            foreach (var text in texts)
                AddText(text.Text, text.Center.TransformBy(transform), text.Height * scale);
        }

        private void AddWindow([NotNull] WindowSpp window)
        {
            using (var line = GetExtentLine(window.Ext, true))
            {
                line.TransformBy(transform);
                line.LayerId = settings.LayerWindow.LayerId;
                if (IsKitchenWindow(window))
                    line.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                block.AppendEntity(line);
                t.AddNewlyCreatedDBObject(line, true);
            }
        }

        /// <summary>
        ///     Добавление окон
        /// </summary>
        private void AddWindows()
        {
            var windows = Elements.OfType<WindowSpp>().ToList();
            foreach (var window in windows)
                AddWindow(window);
        }

        public void CreateBlock(SppParser sppParser)
        {
            parser = sppParser;
            var db = AcadHelper.Doc.Database;
            t = db.TransactionManager.TopTransaction;
            LocationInModel = GetLocationInModel();
            transform = Matrix3d.Scaling(scale, Point3d.Origin)
                .PostMultiplyBy(Matrix3d.Displacement(Point3d.Origin - LocationInModel));
            block = new BlockTableRecord();
            SetBlockName();
            var bt = (BlockTable) db.BlockTableId.GetObject(OpenMode.ForWrite);
            bt.Add(block);
            t.AddNewlyCreatedDBObject(block, true);
            AddOuterWallContour();
            AddText(Mark, poly.GeometricExtents.Center(), 2);
            AddWindows();
            AddTexts();
            AddIWalls();
        }

        [NotNull]
        public static Line GetExtentLine(Extents3d ext, bool isWindow)
        {
            var diag = ext.MaxPoint - ext.MinPoint;
            Vector3d vec;
            if (ext.GetLength() > ext.GetHeight())
                vec = diag.OrthoProjectTo(Vector3d.XAxis) * 0.5;
            else
                vec = diag.OrthoProjectTo(Vector3d.YAxis) * 0.5;
            var startPt = ext.MinPoint + vec;
            var endPt = ext.MaxPoint - vec;
            if (isWindow)
            {
                var dir = (endPt - startPt).GetNormal();
                return new Line(startPt + dir * 25, endPt - dir * 25);
            }
            return new Line(startPt, endPt);
        }

        private Point3d GetLocationInModel()
        {
            var axeses = Elements.OfType<AxisSpp>().ToList();
            var minY = axeses.Where(x => x.Hor).Min(x => x.Score);
            var minX = axeses.Where(x => !x.Hor).Min(x => x.Score);
            return new Point3d(minX, minY, 0);
        }

        private bool IsInnerWall([NotNull] Line line)
        {
            // Стартовая и конечная точка линии не должна лежать на полилинии контура внешних стен
            return !(poly.IsPointOnPolyline(line.StartPoint, 0.5) && poly.IsPointOnPolyline(line.EndPoint, 0.5));
        }

        private bool IsKitchenWindow([NotNull] WindowSpp win)
        {
            var ext = win.Ext;
            var center = ext.Center();
            var vec = ext.GetLength() > ext.GetHeight()
                ? (ext.MaxPoint - ext.MinPoint).OrthoProjectTo(Vector3d.YAxis).GetNormal()
                : (ext.MaxPoint - ext.MinPoint).OrthoProjectTo(Vector3d.XAxis).GetNormal();
            ext.AddPoint(ext.MinPoint - vec * 500);
            ext.AddPoint(ext.MaxPoint + vec * 500);
            var vecPerp = vec.GetPerpendicularVector();
            ext.AddPoint(center + vecPerp * 4500);
            ext.AddPoint(center - vecPerp * 4500);
            var rec = new Rectangle(ext);
            var textKitchen = parser.Intersects(rec).OfType<TextSpp>().FirstOrDefault(v => v.TextType == TextSppType.Kitchen);
            return textKitchen != null;
        }

        private void SetBlockName()
        {
            BlockName = $"СПП_{Mark}";
            while (true)
            {
                try
                {
                    block.Name = BlockName;
                    break;
                }
                catch
                {
                    BlockName += index++;
                }
            }
        }
    }
}