using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Ejecuta el borrado dentro de una única transacción (un solo paso de "Deshacer").
    /// </summary>
    internal static class ReinforcementDeleter
    {
        private const string TransactionName = "Borrar armado";

        /// <summary>
        /// Borra el armado del informe. Devuelve el número total de elementos eliminados por Revit,
        /// que puede ser mayor que el solicitado si había elementos dependientes (cotas, etiquetas...).
        /// </summary>
        public static int Delete(Document doc, ReinforcementReport report)
        {
            using (var transaction = new Transaction(doc, TransactionName))
            {
                FailureHandlingOptions options = transaction.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new WarningSwallower());
                options.SetClearAfterRollback(true);
                transaction.SetFailureHandlingOptions(options);

                transaction.Start();

                ICollection<ElementId> deleted;
                try
                {
                    deleted = doc.Delete(report.ElementIds);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                {
                    transaction.RollBack();
                    throw new InvalidOperationException(
                        "Revit no pudo eliminar parte del armado. " +
                        "Compruebe que los elementos no estén en un vínculo ni bloqueados por otro usuario.\n\n" + ex.Message, ex);
                }

                TransactionStatus status = transaction.Commit();
                if (status != TransactionStatus.Committed)
                {
                    throw new InvalidOperationException(
                        "La transacción no se pudo confirmar (estado: " + status + ").");
                }

                return deleted.Count;
            }
        }
    }
}
