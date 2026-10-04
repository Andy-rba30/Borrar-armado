using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using BorrarArmado.Commands;

namespace BorrarArmado
{
    /// <summary>
    /// Punto de entrada del plugin. Crea la pestaña "Armado" y el botón "Borrar Armado" en la cinta de Revit.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TabName = "Armado";
        private const string PanelName = "Refuerzo";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // La pestaña ya existe (otro plugin la creó). Se reutiliza.
            }

            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var buttonData = new PushButtonData(
                "BorrarArmado_Cmd",
                "Borrar\nArmado",
                assemblyPath,
                typeof(BorrarArmadoCommand).FullName)
            {
                ToolTip = "Elimina todo el armado de acero de los elementos seleccionados.",
                LongDescription =
                    "Seleccione una o varias vigas, columnas, muros, losas o cimentaciones y el comando " +
                    "borrará todas las barras, contenedores de barras, refuerzos de área, refuerzos de trayectoria " +
                    "y mallas electrosoldadas alojadas en ellos. Se muestra un resumen antes de borrar y la operación " +
                    "se puede deshacer con Ctrl+Z.",
                LargeImage = LoadIcon("icon32.png"),
                Image = LoadIcon("icon16.png"),
            };

            panel.AddItem(buttonData);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Carga un icono incrustado en el ensamblado. Si falla, devuelve null y el botón se muestra sin icono.
        /// </summary>
        private static BitmapImage LoadIcon(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = typeof(App).Namespace + ".Resources." + fileName;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
