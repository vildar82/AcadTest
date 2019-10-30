using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AcadLib.Errors;
using AcadLib.RTree.SpatialIndex;
using AcadTest.AutoGenerateSPP.Elements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;
using UnitsNet.Extensions.NumberToArea;
using Section = AcadTest.AutoGenerateSPP.Elements.Section;

namespace AcadTest.AutoGenerateSPP
{
    /// <summary>
    /// Фильтр объектов для СПП - Стены, Оси, Тексты, Окна
    /// </summary>
    public class SppParser
    {
        private readonly List<(Type entType, string layerMatch, Type sppType)> types = new List<(Type, string, Type)>
        {
            (typeof(Line), "Оси", typeof(AxisSpp)),
            (typeof(Line), "WALL", typeof(WallSpp)),
            (typeof(Hatch), "A-AREA-PATT", typeof(ApartHatch)),
            (typeof(Hatch), "A-WALL-PATT", typeof(WallHatch)),
            (typeof(MText), "ANNO-TEXT", typeof(TextSpp)),
            (typeof(MText), "ANNO-TEXT", typeof(TextSpp)),
            (typeof(DBText), "ANNO-TEXT", typeof(TextSpp)),
            (typeof(DBText), "AREA-IDEN", typeof(TextSpp)),
            (typeof(MText), "AREA-IDEN", typeof(TextSpp)),
            (typeof(BlockReference), "A-GLAZ", typeof(WindowSpp)),
        };

        private RTree<ISppElement> tree;
        private int unknownSection;

        public List<ISppElement> Elements { get; private set; }
        public List<Section> Sections { get; private set; }

        public void CreateBlocks()
        {
            foreach (var section in Sections)
            {
                section.CreateBlock(this);
            }
        }

        public void CreateTree()
        {
            tree = new RTree<ISppElement>();
            foreach (var elem in Elements)
            {
                tree.Add(elem.Rectangle, elem);
            }
        }

        public void DefineSections()
        {
            Sections = GroupSectionsByHatch();
            // Удаление маленьких секций (это виды торцов)
            var smallSecs = new List<Section>();
            foreach (var section in Sections)
            {
                section.RectangleOffset = section.ApartHatches.Select(s => s.Rectangle).ToList().Union().Offset(1000);
                if (section.RectangleOffset.GetArea().SquareMillimeters().SquareMeters < 400)
                {
                    smallSecs.Add(section);
                }
                else
                {
                    // элементы секции
                    var intersects = tree.Intersects(section.RectangleOffset);
                    section.Elements = new HashSet<ISppElement>(intersects);
                    // определение марки секции
                    DefineSectionMark(section);
                }
            }
            smallSecs.ForEach(s => Sections.Remove(s));
        }

        public void Filtering(List<ObjectId> ids)
        {
            Elements = new List<ISppElement>();
            foreach (var ent in ids.GetObjects<Entity>())
            {
                var (_, _, sppType) = types.FirstOrDefault(t => IsTypeSpp(ent, t));
                if (sppType == null) continue;
                ISppElement elem;
                if (sppType == typeof(TextSpp))
                {
                    var text = GetEntText(ent);
                    var TextSppType = TextSpp.GetSppType(text.text);
                    if (TextSppType == TextSppType.None) continue;
                    elem = new TextSpp(ent, TextSppType, text.text, text.height);
                }
                else
                {
                    elem = (ISppElement)Activator.CreateInstance(sppType, ent);
                }
                Elements.Add(elem);
            }
        }

        [NotNull]
        public List<ISppElement> Intersects(Rectangle rec)
        {
            return tree.Intersects(rec);
        }

        [NotNull]
        public List<ISppElement> Nearest(Point3d pt, double tolerance)
        {
            return tree.Nearest(new Point(pt.X, pt.Y,0), tolerance);
        }

        private static (string text, double height) GetEntText(Entity ent)
        {
            switch (ent)
            {
                case DBText dT: return (dT.TextString.Trim(), dT.Height);
                case MText mt: return (NetLib.StringExt.ClearString(mt.Text), mt.TextHeight);
            }
            return default;
        }

        private static bool IsTypeSpp([NotNull] Entity ent, (Type entType, string layerMatch, Type sppType) template)
        {
            return ent.GetType() == template.entType &&
                   Regex.IsMatch(ent.Layer, template.layerMatch, RegexOptions.IgnoreCase);
        }

        private void DefineSectionMark([NotNull] Section section)
        {
            var textMark = section.Elements.OfType<TextSpp>()
                .FirstOrDefault(w => w.TextType == TextSppType.SectionMark);
            if (textMark != null)
            {
                section.Elements.Remove(textMark);
                section.Mark = textMark.Text;
            }
            else
            {
                textMark = tree.Intersects(section.RectangleOffset.Offset(3000)).OfType<TextSpp>()
                    .FirstOrDefault(f => f.TextType == TextSppType.SectionMark);
                if (textMark == null)
                {
                    Inspector.AddError("Не определена марка секции", section.RectangleOffset.GetExtents(), Matrix3d.Identity);
                    section.Mark = $"Undefined_{unknownSection++}";
                }
                else
                {
                    section.Mark = textMark.Text;
                }
            }
        }

        [NotNull]
        private List<Section> GroupSectionsByHatch()
        {
            var sections = new List<Section>();
            var apartsH = Elements.OfType<ApartHatch>().ToList();
            // Объединение штриховок с допуском 1метр
            while (apartsH.Any())
            {
                var section = new Section();
                sections.Add(section);
                var apart = apartsH[0];
                var rec = apart.Rectangle.Offset(1000);
                var countAparts = -1;
                var hatches = new List<ApartHatch>();
                while (countAparts < hatches.Count)
                {
                    countAparts = hatches.Count;
                    hatches = tree.Intersects(rec).OfType<ApartHatch>().ToList();
                    if (hatches.Any())
                    {
                        rec = hatches.Select(s => s.Rectangle).ToList().Union().Offset(1000);
                    }
                    else
                    {
                        break;
                    }
                }
                hatches.ForEach(i => apartsH.Remove(i));
                section.ApartHatches = new HashSet<ApartHatch>(hatches);
            }
            return sections;
        }
    }
}
