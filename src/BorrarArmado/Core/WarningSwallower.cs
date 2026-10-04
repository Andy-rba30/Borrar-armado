using Autodesk.Revit.DB;

namespace BorrarArmado.Core
{
    /// <summary>
    /// Descarta las advertencias que Revit genera al borrar (por ejemplo, etiquetas que pierden su referencia)
    /// para que el usuario no tenga que cerrar varios avisos. Los errores reales siguen mostrándose.
    /// </summary>
    internal sealed class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            failuresAccessor.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }
}
