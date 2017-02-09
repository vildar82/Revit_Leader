using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leader
{
    public class ExternalEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            InsertTextLeader.Insert(app);
        }

        public string GetName()
        {
            return "Leader";
        }
    }
}
