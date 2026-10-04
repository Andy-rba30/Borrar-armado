# Borrar Armado — plugin para Autodesk Revit

Un clic. Un elemento. Cero armado.

**Borrar Armado** es un complemento de Revit escrito en C# que elimina de golpe todo el refuerzo de acero
alojado en los elementos que elijas: vigas, columnas, muros, losas, cimentaciones o cualquier otro
anfitrión de armado.

## Qué borra

Con un único recorrido del modelo localiza y elimina, para cada anfitrión seleccionado:

| Tipo de armado                         | Clase de la API de Revit |
|----------------------------------------|--------------------------|
| Barras de refuerzo                     | `Rebar`                  |
| Contenedores de barras                 | `RebarContainer`         |
| Refuerzos de área                      | `AreaReinforcement`      |
| Refuerzos de trayectoria               | `PathReinforcement`      |
| Áreas de malla electrosoldada          | `FabricArea`             |
| Hojas de malla electrosoldada sueltas  | `FabricSheet`            |

Las barras que pertenecen a un refuerzo de área o trayectoria y las hojas que pertenecen a un área de malla
se eliminan junto con su sistema, sin contarlas dos veces.

## Cómo se usa

1. Abre un proyecto de Revit. En la cinta aparece la pestaña **Armado** con el botón **Borrar Armado**.
2. Dos formas de elegir los elementos:
   - **Preselección**: selecciona uno o varios elementos y pulsa el botón.
   - **Selección interactiva**: pulsa el botón sin nada seleccionado, haz clic sobre los elementos y pulsa
     **Finalizar**. Solo se pueden elegir elementos que puedan alojar armado.
   - Si seleccionas directamente una barra o malla, el plugin toma su elemento anfitrión.
3. Se muestra un resumen con el número de elementos de armado por tipo y por anfitrión. Confirma con **Sí**.
4. El armado se borra en **una sola transacción**: un único **Ctrl+Z** lo recupera todo.

Las advertencias de Revit durante el borrado (por ejemplo, etiquetas que pierden su referencia) se descartan
automáticamente para no interrumpir el flujo. Los errores reales se siguen mostrando.

## Compatibilidad

| Revit       | Framework          | Configuración de compilación |
|-------------|--------------------|------------------------------|
| 2021 – 2024 | .NET Framework 4.8 | `Release R2021` … `Release R2024` |
| 2025 – 2026 | .NET 8             | `Release R2025`, `Release R2026`  |
| 2027        | .NET 10            | `Release R2027`                   |

## Compilación

Requisitos: [SDK de .NET 10](https://dotnet.microsoft.com/download) para Revit 2027, o SDK de .NET 8 para
versiones anteriores (Visual Studio 2022 o superior también sirve). No hace falta
tener Revit instalado para compilar: los ensamblados de la API se descargan de NuGet
(`Nice3point.Revit.Api.*`).

```powershell
# Revit 2027 (configuración predeterminada)
dotnet build src/BorrarArmado/BorrarArmado.csproj -c "Release R2027"

# Otras versiones: cambia el sufijo, por ejemplo Revit 2025
dotnet build src/BorrarArmado/BorrarArmado.csproj -c "Release R2025"
```

En Windows, al terminar la compilación el plugin se copia automáticamente a:

```
%AppData%\Autodesk\Revit\Addins\<versión>\BorrarArmado.addin
%AppData%\Autodesk\Revit\Addins\<versión>\BorrarArmado\BorrarArmado.dll
```

Reinicia Revit y la pestaña **Armado** aparecerá en la cinta.

## Instalación manual

Si ya tienes la DLL compilada:

1. Copia `BorrarArmado.addin` a `%AppData%\Autodesk\Revit\Addins\<versión>\`.
2. Copia `BorrarArmado.dll` a `%AppData%\Autodesk\Revit\Addins\<versión>\BorrarArmado\`.
3. Reinicia Revit. La primera vez Revit pedirá confirmación para cargar el complemento.

## Estructura del proyecto

```
src/BorrarArmado/
├── App.cs                          Registro de la pestaña y el botón en la cinta
├── BorrarArmado.addin              Manifiesto del complemento
├── BorrarArmado.csproj             Proyecto multi-versión (2021-2026)
├── Commands/
│   └── BorrarArmadoCommand.cs      Comando: selección → búsqueda → confirmación → borrado
├── Core/
│   ├── HostResolver.cs             Resuelve los anfitriones (preselección o selección en pantalla)
│   ├── RebarHostSelectionFilter.cs Filtro de selección: solo anfitriones válidos o armado
│   ├── ReinforcementFinder.cs      Localiza todo el armado alojado en los anfitriones
│   ├── ReinforcementReport.cs      Conteos por tipo y por anfitrión; textos de los diálogos
│   ├── ReinforcementDeleter.cs     Borrado en una transacción con manejo de fallos
│   └── WarningSwallower.cs         Descarta las advertencias de Revit durante el borrado
└── Resources/                      Iconos del botón (16 y 32 px)
```

## Limitaciones conocidas

- No borra armado de modelos vinculados (los elementos de un vínculo no se pueden modificar).
- En proyectos compartidos (worksharing), el borrado falla si el armado está prestado a otro usuario.
- Solo funciona en documentos de proyecto, no en el editor de familias.
