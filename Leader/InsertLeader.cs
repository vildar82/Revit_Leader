using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI.Events;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Reflection;

namespace Leader
{
    [Transaction(TransactionMode.Manual)]
    public class InsertTextLeader : IExternalCommand, IExternalApplication
    {        
        FormAnno form;

        public Result OnStartup(UIControlledApplication application)
        {            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {            
            return Result.Succeeded;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = Result.Succeeded;
            try
            {
                var app = new InsertTextLeader();
                app.Show();                
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            catch (Exception ex)
            {
                message = ex.Message;
                result = Result.Failed;
            }
            return result;
        }

        private void Show()
        {
            var exEvent = ExternalEvent.Create(new ExternalEventHandler());
            form = new FormAnno(exEvent);
            form.Show();
        }        

        /// <summary>
        /// Проверка текущего вида - 3d и вид спецификации не поддерживается
        /// </summary>
        private static void CheckView(View view)
        {            
            if (view.ViewType == ViewType.ThreeD ||
            view.ViewType == ViewType.Schedule)
                throw new Exception($"Текущий вид не поддерживается '{view.Name}'");
        }

        /// <summary>
        /// Поиск семейства выноски в чертеже
        /// </summary>
        /// <returns></returns>
        private static AnnotationSymbolType FindAnnoSymboltype(Document doc, string typeName)
        {
            // Семейство ATP_Текст
            string annoName = "ATP_Текст % Текст_ISO-2,5_0,9-I";
            // Загрузка семейства с переопределением
            Family family= FamilyLoadHelper.LoadFromCurrentDllLocation(doc, annoName);
            if (family == null)
            {
                family = new FilteredElementCollector(doc).OfClass(typeof(Family)).FirstOrDefault(w => w.Name == annoName) as Family;                
            }
            var annoType = family.GetFamilySymbolIds().Select(s=> doc.GetElement(s)).FirstOrDefault(f=>f.Name == typeName) as AnnotationSymbolType;
            if (annoType == null)
            {
                throw new Exception($"Не найден тип '{typeName}' у семейства '{annoName}'");                               
            }
            return annoType;
        }

        /// <summary>
        /// Вставка выноски
        /// </summary>        
        public static void Insert(UIApplication uiApp)
        {
            try
            {
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                CheckView(uiDoc.ActiveView);
                var annoType = FindAnnoSymboltype(doc, "Без стрелки");

                // Запрос точек ввода
                var pt1 = uiDoc.Selection.PickPoint("Первая точка");
                var pt2 = uiDoc.Selection.PickPoint("Вторая точка");
                var dir = GetAnnoDirection(pt1, pt2);

                AnnotationSymbol annoText;
                using (var t = new Transaction(doc, "Создание выноски"))
                {
                    t.Start();

                    // Вставка семейства выноски в документ
                    annoText = doc.Create.NewFamilyInstance(pt2, annoType, uiDoc.ActiveView) as AnnotationSymbol;
                    annoText.LookupParameter("Пояснение сверху").Set(FormAnno.Text1);
                    annoText.LookupParameter("Пояснение снизу").Set(FormAnno.Text2);

                    // Определение ширины выноски по тексту
                    annoText.LookupParameter("Длина").Set(0.01); // Минимальная длина линии 
                    doc.Regenerate();
                    var bound = annoText.get_BoundingBox(uiDoc.ActiveView);
                    var length = (bound.Max.X - bound.Min.X) * 1.1; // 1.1 - для отступа линии от текста

                    // Сдвиг на половыну выноски в сторону
                    var moveLocation = new XYZ(length * 0.52 * dir, 0, 0); // 0.52 - отступ точки присоединения выноски
                    annoText.Location.Move(moveLocation);
                    annoText.LookupParameter("Длина").Set(length / doc.ActiveView.Scale);

                    // Добавление линии выноски
                    annoText.addLeader();
                    var leader = annoText.Leaders.get_Item(0);
                    leader.Elbow = pt2;
                    leader.End = pt1;

                    t.Commit();
                }

                PluginStatistic.Writer.WriteRunToDB("Revit", "R1.09_Выноска", "",
                  FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion, doc.Title, false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Выноска", ex.Message);
            }
        }

        /// <summary>
        /// Напраление построения выноски от первой и второй точек (1 - вправо, -1 влево)
        /// </summary>        
        private static int GetAnnoDirection(XYZ pt1, XYZ pt2)
        {
            var angle = (pt2 - pt1).AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ);
            return angle > 1.57 && angle < 4.71 ? -1 : 1;
        }
    }


    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TestIdling : IExternalCommand
    {
        ExternalCommandData commandData;
        XYZ pt1;
        XYZ lastPt;
        DetailCurve line;
        private int callCounter;
        private System.Drawing.Point lastCursor;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.commandData = commandData;
            pt1 = commandData.Application.ActiveUIDocument.Selection.PickPoint("Первая точка");
            commandData.Application.Idling += Application_Idling;
            //var pt2 = commandData.Application.ActiveUIDocument.Selection.PickPoint("Вторая точка");

            //commandData.Application.Idling -= Application_Idling;

            return Result.Succeeded;
        }

        private void Application_Idling(object sender, IdlingEventArgs e)
        {
            //var app = sender as Application;
            //var uiApp = new UIApplication(app);
            var uiApp = sender as UIApplication;
            var uiDoc = uiApp.ActiveUIDocument;
            var view = uiDoc.ActiveView;
            var doc = uiDoc.Document;

            e.SetRaiseWithoutDelay();

            if (Math.Abs(lastCursor.X - System.Windows.Forms.Cursor.Position.X) >= 1)
            {
                var curPt = GetCurrentCursorPosition(uiDoc);
                using (var t = new Transaction(doc, "TestIdling"))
                {
                    t.Start();

                    var l = Line.CreateBound(pt1, curPt);
                    if (line == null)
                    {
                        line = doc.Create.NewDetailCurve(view, l);
                    }
                    else
                    {
                        line.GeometryCurve = l;
                    }
                    t.Commit();
                }
            }
            lastCursor = System.Windows.Forms.Cursor.Position;
        }

        private XYZ GetCurrentCursorPosition(UIDocument uiDoc)
        {
            UIView uiview = GetActiveUiView(uiDoc);
            Rectangle rect = uiview.GetWindowRectangle();
            System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
            double dx = (double)(p.X - rect.Left) / (rect.Right - rect.Left);
            double dy = (double)(p.Y - rect.Bottom) / (rect.Top - rect.Bottom);
            IList<XYZ> corners = uiview.GetZoomCorners();
            XYZ a = corners[0];
            XYZ b = corners[1];
            XYZ v = b - a;

            XYZ q = a
              + dx * v.X * XYZ.BasisX
              + dy * v.Y * XYZ.BasisY;
            return q;
        }

        public void app_Idling(Object sender, IdlingEventArgs arg)
        {
            callCounter++;

            //do the short time taking task here.
            Trace.WriteLine("Idling session called " + callCounter.ToString());

            //When Revit users don't move the mouse            
            if (Math.Abs(lastCursor.X - System.Windows.Forms.Cursor.Position.X) <= 1)
            {
                //move the cursor left and right with small distance:
                //1 pixel. so it looks like it is stable
                //this way can trigger the Idling event repeatedly
                if (callCounter % 2 == 0)
                {
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(
                      System.Windows.Forms.Cursor.Position.X + 10, System.Windows.Forms.Cursor.Position.Y);
                }
                else if (callCounter % 2 == 1)
                {
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(
                      System.Windows.Forms.Cursor.Position.X - 10, System.Windows.Forms.Cursor.Position.Y);
                }
            }
            lastCursor = System.Windows.Forms.Cursor.Position;
        }

        /// <summary>
        /// Return currently active UIView or null.
        /// </summary>
        static UIView GetActiveUiView(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            IList<UIView> uiviews = uidoc.GetOpenUIViews();
            UIView uiview = null;

            foreach (UIView uv in uiviews)
            {
                if (uv.ViewId.Equals(view.Id))
                {
                    uiview = uv;
                    break;
                }
            }
            return uiview;
        }
    }
}

