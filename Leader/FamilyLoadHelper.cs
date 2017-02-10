using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Leader
{
    public static class FamilyLoadHelper
    {
        public static Family LoadFromCurrentDllLocation(Document doc, string annoName)
        {
            Family family;
            var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), annoName + ".rfa");
            using (var t = new Transaction(doc, "Обновление семейства"))
            {
                t.Start();
                doc.LoadFamily(file, new FamilyOption(), out family);
                t.Commit();
            }
            return family;
        }

        public class FamilyOption : IFamilyLoadOptions
        {
            bool IFamilyLoadOptions.OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            bool IFamilyLoadOptions.OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
