using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Localiza todo el armado del modelo alojado en un conjunto de elementos anfitriones.
    /// Cubre: barras (Rebar), contenedores de barras, refuerzo de área, refuerzo de trayectoria,
    /// áreas de malla y hojas de malla electrosoldada.
    /// </summary>
    internal static class ReinforcementFinder
    {
        private static readonly IList<Type> ReinforcementTypes = new List<Type>
        {
            typeof(Rebar),
            typeof(RebarContainer),
            typeof(AreaReinforcement),
            typeof(PathReinforcement),
            typeof(FabricArea),
            typeof(FabricSheet),
        };

        public static ReinforcementReport FindByHosts(Document doc, ICollection<ElementId> hostIds)
        {
            var hostSet = new HashSet<ElementId>(hostIds);
            var report = new ReinforcementReport(doc, hostIds);

            // Un único recorrido del modelo: se recoge todo el armado y se filtra por anfitrión.
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .WherePasses(new ElementMulticlassFilter(ReinforcementTypes))
                .WhereElementIsNotElementType();

            foreach (Element element in collector)
            {
                ElementId hostId = GetHostId(element);
                if (hostId is null || !hostSet.Contains(hostId))
                {
                    continue;
                }

                if (IsDeletedWithItsOwner(element))
                {
                    continue;
                }

                report.Add(element, hostId);
            }

            return report;
        }

        /// <summary>
        /// Devuelve el anfitrión de un elemento de armado, o null si el elemento no es armado.
        /// </summary>
        public static ElementId GetHostId(Element element)
        {
            switch (element)
            {
                case Rebar rebar:
                    return rebar.GetHostId();
                case RebarInSystem rebarInSystem:
                    return rebarInSystem.GetHostId();
                case RebarContainer container:
                    return container.GetHostId();
                case AreaReinforcement areaReinforcement:
                    return areaReinforcement.GetHostId();
                case PathReinforcement pathReinforcement:
                    return pathReinforcement.GetHostId();
                case FabricSheet fabricSheet:
                    return fabricSheet.HostId;
                case FabricArea fabricArea:
                    return fabricArea.HostId;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Las hojas de malla que pertenecen a un área de malla se eliminan junto con su área,
        /// así que no se incluyen por separado (evita contarlas dos veces).
        /// </summary>
        private static bool IsDeletedWithItsOwner(Element element)
        {
            var fabricSheet = element as FabricSheet;
            if (fabricSheet == null)
            {
                return false;
            }

            ElementId ownerId = fabricSheet.FabricAreaOwnerId;
            return !(ownerId is null) && ownerId != ElementId.InvalidElementId;
        }
    }
}
