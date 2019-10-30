using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Autodesk.Windows.Themes;
using DynamicData;
using NetLib;
using NetLib.WPF;
using ReactiveUI;

namespace AcadTest.ColorThemes
{
    public class TestColorsVM : BaseViewModel
    {
        public TestColorsVM()
        {
            var theme = ThemeManager.PaletteSettings.CurrentTheme;
            var srcColors = new SourceList<ThemeColor>();
            foreach (var p in theme.GetType().GetProperties().OrderBy(o=>o.Name))
            {
                var themeColor = new ThemeColor();
                themeColor.Name = p.Name;

                if (p.PropertyType == typeof(Color))
                {
                    themeColor.Color = new SolidColorBrush((Color) p.GetValue(theme));
                }
                else if (p.PropertyType == typeof(Brush))
                {
                    themeColor.Color = (Brush) p.GetValue(theme);
                }
                
                srcColors.Add(themeColor);
            }

            srcColors.Connect()
                .AutoRefreshOnObservable(c => this.WhenAnyValue(v => v.Filter))
                .Filter(FilterColors)
                .ObserveOnDispatcher()
                .Bind(out var data)
                .Subscribe();

            Colors = data;
        }
        
        public ReadOnlyObservableCollection<ThemeColor> Colors { get; set; }

        public string Filter { get; set; }

        private bool FilterColors(ThemeColor color)
        {
            if (Filter.IsNullOrEmpty())
                return true;
            return Regex.IsMatch(color.Name, Filter, RegexOptions.IgnoreCase);
        }
    }

    public class ThemeColor
    {
        public string Name { get; set; }
        public Brush Color { get; set; }
    }
}