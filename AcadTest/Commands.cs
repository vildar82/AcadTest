namespace AcadTest
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using AcadLib;
    using AcadLib.Jigs;
    using AcadLib.Reactive;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.Civil.DatabaseServices;
    using ColorThemes;
    using FlaUI.Core.Definitions;
    using FlaUI.Core.Input;
    using FlaUI.UIA3;
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;

    public class Commands
    {
        [CommandMethod("Test", CommandFlags.Modal)]
        public void Test()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var selRes = doc.Editor.GetEntity("Выбор колодца");
            if (selRes.Status != PromptStatus.OK) return;

            using var t = doc.TransactionManager.StartTransaction();
            var s = selRes.ObjectId.GetObject(OpenMode.ForWrite, false, true) as Structure;
            if (s == null)
            {
                Log("Это не колодец");
                return;
            }

            var partData = s.PartData;

            foreach (var f in partData.GetAllDataFields().OrderBy(o => o.Name))
            {
                Log(
                    $"{f.Name}={f.Value}, {f.Description}, Context={f.Context}, Units={f.Units}, IsReadOnly={f.IsReadOnly}");
            }

            var field = partData.GetDataFieldBy("Mark");
            if (field == null)
            {
                Log("Не найден параметр марки");
                return;
            }

            var newValue = field.ValueList[3];
            field.Value = newValue;
            s.PartData = partData;
            t.Commit();
        }

        private static void Log(string msg)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage($"{msg}\n");
        }

        private AlignmentLine GetLineAfter(AlignmentLine line, Alignment align)
        {
            var after = line.EntityAfter;
            var ent = align.Entities.EntityAtId(after);
            switch (ent)
            {
                case AlignmentLine afterLine:
                    return afterLine;
                case AlignmentArc arc:
                    return align.Entities.EntityAtId(arc.EntityAfter) as AlignmentLine;
            }

            return null;
        }

        [CommandMethod("TestPlotConfigs", CommandFlags.Modal)]
        public void TestPlotConfigs()
        {
            var doc = AcadHelper.Doc;
            using (doc.TransactionManager.StartTransaction())
            {
                var plSetDict = doc.Database.PlotSettingsDictionaryId.GetObject<DBDictionary>();
                foreach (var item in plSetDict)
                {
                    Debug.WriteLine(item.Key);
                    var obj = item.Value.GetObject<DBObject>();
                }
            }
        }

        [CommandMethod("TestPolylineJig", CommandFlags.Modal)]
        public void TestPolylineJig()
        {
            var doc = AcadHelper.Doc;
            var ed = doc.Editor;
            var plJig = new PolylineJig();
            var res = plJig.DrawPolyline(ed);
        }

        [CommandMethod("TestModified", CommandFlags.Modal)]
        public void TestModified()
        {
            var doc = AcadHelper.Doc;
            doc.Database.Events().ObjectModified.Subscribe(s =>
            {
                Debug.WriteLine($"ObjectModified {s.EventArgs.DBObject}, {s.EventArgs.DBObject.Id.ObjectClass.Name}");
            });
        }

        [CommandMethod(nameof(TestColors), CommandFlags.Modal)]
        public void TestColors()
        {
            var testColors = new TestColors(new TestColorsVM());
            Application.ShowModelessWindow(testColors);
        }

        [CommandMethod(nameof(TestHatchLoopTypes), CommandFlags.Modal)]
        public void TestHatchLoopTypes()
        {
            var doc = AcadHelper.Doc;
            using (var t = doc.TransactionManager.StartTransaction())
            {
                var h = doc.Editor.SelectEntity<Hatch>("Выбор штриховки").GetObject<Hatch>();
                for (var i = 0; i < h.NumberOfLoops; i++)
                {
                    var loop = h.GetLoopAt(i);
                    $"loop {i}: LoopType={loop.LoopType}, IsPolyline={loop.IsPolyline}".WriteToCommandLine();
                }
                t.Commit();
            }
        }

        [CommandMethod(nameof(TestShortcut), CommandFlags.Modal)]
        public void TestShortcut()
        {
            Console.WriteLine("Нажми любую кнопку для старта:");
            Console.ReadKey();
            var processID = Process.GetCurrentProcess().Id;
            var app = FlaUI.Core.Application.Attach(processID);
            using (var auto = new UIA3Automation())
            {
                var mainWin = app.GetMainWindow(auto);
                Console.WriteLine($"Главное окно - {mainWin.Name}");
                var elem = mainWin.FindFirstByXPath("/Pane[@Name='Область инструментов']/Pane[@Name='Навигатор']");
                Console.WriteLine($"{elem.Name}");
                var gl = elem.FindFirst(TreeScope.Children, elem.ConditionFactory.ByName("Навигатор:Главное представление"));
                Console.WriteLine($"{gl.Name}");
                var pane = gl.FindFirstByXPath("/Pane/Pane");
                var tree = pane.FindFirstChild(gl.ConditionFactory.ByControlType(ControlType.Tree)).AsTree();
                var tiShorts = tree.Items.FirstOrDefault(i => i.Name.StartsWith("Быстрые ссылки"));
                Console.WriteLine($"{tiShorts.Name}");
                tiShorts.Expand();
                var tiNets = tiShorts.Items.FirstOrDefault(i => i.Name.StartsWith("Трубопроводные сети"));
                Console.WriteLine($"{tiNets.Name}");
                tiNets.Expand();
                foreach (var item in tiNets.Items)
                {
                    Console.WriteLine($"Вставка быстрой ссылки {item.Name}");
                    item.Select();
                    item.RightClick();
                    Mouse.MoveBy(50, 5);
                    Mouse.Click(MouseButton.Left);
                    while (true)
                    {
                        var winLink = mainWin.ModalWindows.FirstOrDefault(w => w.Name == "Создать ссылку трубопроводной сети");
                        if (winLink == null)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        var bOk = winLink.FindFirstByXPath("/Button[@Name='OK']").AsButton();
                        bOk.Click();
                        app.WaitWhileBusy();
                        Thread.Sleep(3000);
                        break;
                    }
                }

                Console.ReadKey();

                //uiJob.Container.Automate(mainWin);
            }
        }
    }
}
