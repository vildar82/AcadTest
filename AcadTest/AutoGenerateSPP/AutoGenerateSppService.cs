using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Linq;
using AcadLib.Blocks;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;

namespace AcadTest.AutoGenerateSPP
{
    public static class AutoGenerateSppService
    {
        private static Database db;
        private static Document doc;
        private static Editor ed;
        public static Settings Settings { get; set; }

        public static void Generate()
        {
            doc = AcadHelper.Doc;
            db = doc.Database;
            ed = doc.Editor;
            var ids = ed.Select("Выбор:");
            using (var t = db.TransactionManager.StartTransaction())
            {
                Settings = new Settings(db);
                // Перебор примитивов
                var parser = new SppParser();
                parser.Filtering(ids);
                // дерево элементов
                parser.CreateTree();
                // Группировка секций
                parser.DefineSections();
                // Создание блоков секций
                parser.CreateBlocks();

                // Тестовая вставка секций
                var ms = (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForWrite);
                foreach (var section in parser.Sections)
                {
                    BlockInsert.InsertBlockRef(section.BlockName, section.LocationInModel, ms, t, 1000);
                }
                t.Commit();
            }
        }
    }
}