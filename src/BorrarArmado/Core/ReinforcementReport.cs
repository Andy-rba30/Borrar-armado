using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Resultado de la búsqueda de armado: qué elementos se van a borrar, de qué tipo son
    /// y en qué anfitrión están. Genera los textos de los cuadros de diálogo.
    /// </summary>
    internal sealed class ReinforcementReport
    {
        private readonly Document _doc;
        private readonly List<ElementId> _ids = new List<ElementId>();
        private readonly Dictionary<string, int> _byKind = new Dictionary<string, int>();
        private readonly Dictionary<ElementId, Dictionary<string, int>> _byHost =
            new Dictionary<ElementId, Dictionary<string, int>>();

        public ReinforcementReport(Document doc, ICollection<ElementId> hostIds)
        {
            _doc = doc;
            HostIds = hostIds;
        }

        public ICollection<ElementId> HostIds { get; }

        public ICollection<ElementId> ElementIds => _ids;

        public int TotalCount => _ids.Count;

        public int HostCount => HostIds.Count;

        public bool IsEmpty => _ids.Count == 0;

        public void Add(Element element, ElementId hostId)
        {
            _ids.Add(element.Id);

            string kind = KindOf(element);
            Increment(_byKind, kind);

            if (!_byHost.TryGetValue(hostId, out Dictionary<string, int> perHost))
            {
                perHost = new Dictionary<string, int>();
                _byHost[hostId] = perHost;
            }

            Increment(perHost, kind);
        }

        /// <summary>Desglose por tipo de armado, por ejemplo "Barras: 42".</summary>
        public string BuildKindBreakdown()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in _byKind.OrderByDescending(p => p.Value))
            {
                sb.AppendLine(string.Format("  • {0}: {1}", pair.Key, pair.Value));
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>Detalle por anfitrión con el armado que contiene cada uno.</summary>
        public string BuildHostDetails()
        {
            var sb = new StringBuilder();
            foreach (ElementId hostId in HostIds)
            {
                sb.AppendLine(DescribeHost(hostId));

                if (_byHost.TryGetValue(hostId, out Dictionary<string, int> perHost))
                {
                    foreach (KeyValuePair<string, int> pair in perHost.OrderByDescending(p => p.Value))
                    {
                        sb.AppendLine(string.Format("      {0}: {1}", pair.Key, pair.Value));
                    }
                }
                else
                {
                    sb.AppendLine("      (sin armado)");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>Lista simple de anfitriones, usada cuando no se encontró armado.</summary>
        public string BuildHostList()
        {
            var sb = new StringBuilder();
            foreach (ElementId hostId in HostIds)
            {
                sb.AppendLine(DescribeHost(hostId));
            }

            return sb.ToString().TrimEnd();
        }

        private string DescribeHost(ElementId hostId)
        {
            Element host = _doc.GetElement(hostId);
            if (host == null)
            {
                return string.Format("  Elemento Id {0}", hostId);
            }

            string category = host.Category != null ? host.Category.Name : "Elemento";
            return string.Format("  {0}: {1} (Id {2})", category, host.Name, hostId);
        }

        private static string KindOf(Element element)
        {
            switch (element)
            {
                case Rebar _:
                    return "Barras de refuerzo";
                case RebarContainer _:
                    return "Contenedores de barras";
                case AreaReinforcement _:
                    return "Refuerzos de área";
                case PathReinforcement _:
                    return "Refuerzos de trayectoria";
                case FabricArea _:
                    return "Áreas de malla electrosoldada";
                case FabricSheet _:
                    return "Hojas de malla electrosoldada";
                default:
                    return "Otro armado";
            }
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int current);
            counts[key] = current + 1;
        }
    }
}
