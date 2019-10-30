using Autodesk.Civil.ApplicationServices;

namespace AcadTest
{
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.PlottingServices;
    using Autodesk.AutoCAD.Runtime;


    /// <summary>
    /// http://adndevblog.typepad.com/autocad/2012/05/how-to-use-the-autodeskautocadpublishingpublisherpublishdsd-api-in-net.html
    /// </summary>
    public class PlotDirToPdf
    {
        private string dir;
        private string filePdfOutputName;

        [CommandMethod("TestPlotPdf", CommandFlags.Modal)]
        public void Plot()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            dir = Path.GetDirectoryName(doc.Name);
            filePdfOutputName = Path.GetFileNameWithoutExtension(doc.Name);
            using (var dsdCol = new DsdEntryCollection())
            {
                using (var t = doc.TransactionManager.StartTransaction())
                {
                    var layouts = GetLayouts(doc.Database);
                    foreach (var layout in layouts)
                    {
                        var dsdEntry = new DsdEntry
                        {
                            Layout = layout.LayoutName,
                            DwgName = doc.Name,
                            NpsSourceDwg = doc.Name,
                            Title = layout.LayoutName
                        };
                        dsdCol.Add(dsdEntry);
                    }

                    t.Commit();
                }

                PublisherDSD(dsdCol);
            }
        }

        private void PublisherDSD(DsdEntryCollection collection)
        {
            var dsdFile = Path.Combine(dir, filePdfOutputName + ".dsd");
            var destFile = Path.Combine(dir, filePdfOutputName + ".pdf");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            using (var dsd = new DsdData())
            {
                dsd.SetDsdEntryCollection(collection);
                dsd.SheetType = SheetType.MultiPdf;
                dsd.IsSheetSet = true;
                dsd.NoOfCopies = 1;
                dsd.IsHomogeneous = false;
                dsd.DestinationName = destFile;
                dsd.SheetSetName = "PublisherSet";
                dsd.PromptForDwfName = false;
                PostProcessDSD(dsd, dsdFile);
            }

            using (var progressDlg = new PlotProgressDialog(false, collection.Count, true))
            {
                progressDlg.IsVisible = true;
                var publisher = Application.Publisher;
                PlotConfigManager.SetCurrentConfig("clk-PDF.pc3");
                publisher.PublishDsd(dsdFile, progressDlg);
                progressDlg.Destroy();
            }
        }

        private void PostProcessDSD(DsdData dsd, string destFile)
        {
            var tmpFile = Path.Combine(dir, "temp.dsd");
            dsd.WriteDsd(tmpFile);
            using (var reader = new StreamReader(tmpFile, Encoding.Default))
            using (var writer = new StreamWriter(destFile, false, Encoding.Default))
            {
                var fileDwg = string.Empty;
                while (!reader.EndOfStream)
                {
                    var str = reader.ReadLine();
                    if (str == null)
                        continue;
                    string newStr;
                    if (str.StartsWith("Has3DDWF="))
                    {
                        newStr = "Has3DDWF=0";
                    }
                    else if (str.StartsWith("DWG=", StringComparison.OrdinalIgnoreCase))
                    {
                        fileDwg = str.Substring(4);
                        newStr = str;
                    }
                    else if (str.StartsWith("OriginalSheetPath="))
                    {
                        newStr = "OriginalSheetPath=" + fileDwg;
                    }
                    else if (str.StartsWith("Type="))
                    {
                        newStr = "Type=6";
                    }
                    else if (str.StartsWith("PromptForDwfName="))
                    {
                        newStr = "PromptForDwfName=FALSE";
                    }
                    else
                    {
                        newStr = str;
                    }

                    writer.WriteLine(newStr);
                }
            }

            File.Delete(tmpFile);
        }

        private static List<Layout> GetLayouts(Database db)
        {
            var layouts = new List<Layout>();
            var dictLayout = db.LayoutDictionaryId.GetObject(OpenMode.ForRead) as DBDictionary;
            if (dictLayout == null) return layouts;
            foreach (var entry in dictLayout)
            {
                if (entry.Key == "Model") continue;
                var layout = entry.Value.GetObject(OpenMode.ForRead) as Layout;
                if (layout != null)
                {
                    layouts.Add(layout);
                }
            }

            return layouts;
        }
    }
}
