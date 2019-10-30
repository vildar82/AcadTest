using System;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JetBrains.Annotations;
using NetLib;

namespace AcadTest.AutoGenerateSPP.Elements
{
    /// <summary>
    /// Подписи нужные
    /// </summary>
    public class TextSpp : BaseSppElemnt
    {
        public Point3d Center { get; }
        public double Height { get; }
        public string Text { get; }
        public TextSppType TextType { get; }

        public TextSpp([NotNull] Entity ent, TextSppType textSppType, string text, double height) : base(ent)
        {
            TextType = textSppType;
            Text = textSppType == TextSppType.Kitchen ? "Кухня" : text;
            Height = height;
            Center = Ext.Center();
        }

        public static TextSppType GetSppType(string text)
        {
            if (text.IsNullOrEmpty()) return TextSppType.None;
            if (text.IndexOf("Кухня", StringComparison.OrdinalIgnoreCase) != -1 &&
                text.IndexOf("ниша", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return TextSppType.Kitchen;
            }
            if (Regex.IsMatch(text, @"^\d[кk]$", RegexOptions.IgnoreCase))
            {
                return TextSppType.ApartType;
            }
            if (Regex.IsMatch(text, @"^\S+(-\d+)+$"))
            {
                return TextSppType.SectionMark;
            }
            return TextSppType.None;
        }
    }
}