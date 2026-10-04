using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Durante la selección interactiva solo permite elegir elementos que puedan alojar armado
    /// o elementos de armado (para resolver su anfitrión).
    /// </summary>
    internal sealed class RebarHostSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if (element == null)
            {
                return false;
            }

            if (!(ReinforcementFinder.GetHostId(element) is null))
            {
                return true;
            }

            return RebarHostData.IsValidHost(element);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
