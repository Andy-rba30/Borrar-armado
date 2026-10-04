using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Decide qué elementos anfitriones procesar:
    ///  - Si ya hay elementos seleccionados, se usan esos.
    ///  - Si no, se pide al usuario que los seleccione en pantalla.
    /// Si el usuario selecciona directamente una barra o malla, se toma su elemento anfitrión.
    /// </summary>
    internal static class HostResolver
    {
        private const string PickPrompt =
            "Seleccione los elementos cuyo armado desea borrar y pulse Finalizar (Esc para cancelar).";

        public static ICollection<ElementId> Resolve(UIDocument uiDoc)
        {
            Document doc = uiDoc.Document;
            var hosts = new HashSet<ElementId>();

            // Preselección.
            foreach (ElementId id in uiDoc.Selection.GetElementIds())
            {
                AddHostFrom(doc.GetElement(id), hosts);
            }

            if (hosts.Count > 0)
            {
                return hosts;
            }

            // Selección interactiva (lanza OperationCanceledException si se pulsa Esc).
            IList<Reference> references = uiDoc.Selection.PickObjects(
                ObjectType.Element,
                new RebarHostSelectionFilter(),
                PickPrompt);

            foreach (Reference reference in references)
            {
                AddHostFrom(doc.GetElement(reference), hosts);
            }

            return hosts;
        }

        private static void AddHostFrom(Element element, HashSet<ElementId> hosts)
        {
            if (element == null)
            {
                return;
            }

            // Si es un elemento de armado, usar su anfitrión.
            ElementId hostOfReinforcement = ReinforcementFinder.GetHostId(element);
            if (!(hostOfReinforcement is null) && hostOfReinforcement != ElementId.InvalidElementId)
            {
                hosts.Add(hostOfReinforcement);
                return;
            }

            // Si es un anfitrión válido de armado (viga, columna, muro, losa, cimentación...), usarlo.
            if (RebarHostData.IsValidHost(element))
            {
                hosts.Add(element.Id);
            }
        }
    }
}
