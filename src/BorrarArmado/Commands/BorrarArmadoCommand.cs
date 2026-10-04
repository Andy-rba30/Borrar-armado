using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BorrarArmado.Core;

namespace BorrarArmado.Commands
{
    /// <summary>
    /// Comando principal: elige elementos anfitriones y borra todo el armado que contienen.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BorrarArmadoCommand : IExternalCommand
    {
        private const string Title = "Borrar Armado";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            if (uiDoc == null)
            {
                message = "No hay ningún documento abierto.";
                return Result.Cancelled;
            }

            Document doc = uiDoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show(Title, "Este comando solo funciona en proyectos, no en familias.");
                return Result.Cancelled;
            }

            try
            {
                // 1. Resolver los elementos anfitriones (preselección o selección interactiva).
                ICollection<ElementId> hostIds = HostResolver.Resolve(uiDoc);
                if (hostIds.Count == 0)
                {
                    return Result.Cancelled;
                }

                // 2. Localizar todo el armado alojado en esos elementos.
                ReinforcementReport report = ReinforcementFinder.FindByHosts(doc, hostIds);
                if (report.IsEmpty)
                {
                    var noneDialog = new TaskDialog(Title)
                    {
                        MainInstruction = "Los elementos seleccionados no contienen armado.",
                        MainContent = report.BuildHostList(),
                        CommonButtons = TaskDialogCommonButtons.Close,
                    };
                    noneDialog.Show();
                    return Result.Succeeded;
                }

                // 3. Confirmar con el usuario.
                if (!Confirm(report))
                {
                    return Result.Cancelled;
                }

                // 4. Borrar dentro de una transacción.
                int deletedCount = ReinforcementDeleter.Delete(doc, report);

                // 5. Resumen final y dejar los anfitriones seleccionados.
                ShowSummary(report, deletedCount);
                uiDoc.Selection.SetElementIds(hostIds);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // El usuario pulsó Esc durante la selección.
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static bool Confirm(ReinforcementReport report)
        {
            var dialog = new TaskDialog(Title)
            {
                TitleAutoPrefix = false,
                MainInstruction = string.Format(
                    "Se eliminarán {0} elementos de armado de {1} elemento(s) anfitrión(es).",
                    report.TotalCount,
                    report.HostCount),
                MainContent = report.BuildKindBreakdown() + "\n\n¿Desea continuar?",
                ExpandedContent = report.BuildHostDetails(),
                FooterText = "La operación se puede deshacer con Ctrl+Z.",
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                DefaultButton = TaskDialogResult.No,
            };

            return dialog.Show() == TaskDialogResult.Yes;
        }

        private static void ShowSummary(ReinforcementReport report, int deletedCount)
        {
            var dialog = new TaskDialog(Title)
            {
                TitleAutoPrefix = false,
                MainInstruction = string.Format("Armado eliminado: {0} elemento(s).", report.TotalCount),
                MainContent = report.BuildKindBreakdown(),
                ExpandedContent = report.BuildHostDetails(),
                CommonButtons = TaskDialogCommonButtons.Close,
            };

            if (deletedCount > report.TotalCount)
            {
                dialog.FooterText = string.Format(
                    "Revit eliminó además {0} elemento(s) dependientes (cotas, etiquetas, etc.).",
                    deletedCount - report.TotalCount);
            }

            dialog.Show();
        }
    }
}
