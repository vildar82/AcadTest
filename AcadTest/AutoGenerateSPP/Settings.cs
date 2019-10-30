using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Layers;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using JetBrains.Annotations;

namespace AcadTest.AutoGenerateSPP
{
    public class Settings
    {
        public LayerInfo LayerApart { get; }
        public LayerInfo LayerApartText { get; }
        public LayerInfo LayerAxis { get; }
        public LayerInfo LayerGNS { get; }
        public LayerInfo LayerWindow { get; }
        public ObjectId TextStyleId { get; }
        public double OffsetOuterWallFromAxis { get; set; } = 300;

        public Settings(Database db)
        {
            TextStyleId = db.GetTextStylePIK();
            var white = Color.FromColorIndex(ColorMethod.ByAci, 7);
            var con = SymbolUtilityServices.LinetypeContinuousName;
            LayerGNS = GetLayerInfo("121_Зд_Секции_ГНС", white, LineWeight.LineWeight009, con);
            LayerAxis = GetLayerInfo("121_Зд_Секции_Оси", white, LineWeight.LineWeight050, con);
            LayerWindow = GetLayerInfo("121_Зд_Окна", Color.FromColorIndex(ColorMethod.ByAci, 2), LineWeight.LineWeight030,
                con, false);
            LayerApartText = GetLayerInfo("121_Зд_Квартирография_Подписи", white, LineWeight.LineWeight015, con, false);
            LayerApart = GetLayerInfo("121_Зд_Квартирография", white, LineWeight.LineWeight015, con, false);
        }

        [NotNull]
        private static LayerInfo GetLayerInfo(string name, Color color, LineWeight lw, string lt, bool isPlottable = true)
        {
            var layInfo = new LayerInfo(name)
            {
                Color = color,
                LineWeight = lw,
                LineType = lt,
                IsPlotable = isPlottable
            };
            layInfo.CheckLayerState();
            return layInfo;
        }
    }
}