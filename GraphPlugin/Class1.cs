using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;

namespace GraphPlugin
{
    public class Commands
    {
        [CommandMethod("CREATE_GRAPH_NODE")]
        public void CreateGraphNode()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var pointResult = ed.GetPoint(
                "\nУкажите точку для размещения узла графа: "
            );

            if (pointResult.Status != PromptStatus.OK)
                return;

            Point3d position = pointResult.Value;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var blockTable =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);

                var modelSpace =
                    (BlockTableRecord)tr.GetObject(
                        blockTable[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);

                var circle = new Circle(
                    position,
                    Vector3d.ZAxis,
                    10.0);

                modelSpace.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                tr.Commit();
            }
        }
    }
}