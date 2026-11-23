# SimuRackon

Simulador educativo de control de tráfico aéreo desarrollado en C# con Windows Forms para **Técnicas de Programación, Facultad de Ingeniería de la UNAM**. Durante una ronda de **3 minutos**, tomas el papel de la torre de control de un aeropuerto ficticio. Tu objetivo es atender llegadas y salidas, comprobar qué instalaciones están libres y explicar cómo cada decisión afecta al tráfico y a tu puntuación.

## Ejecutar en Windows

1. Extrae la carpeta completa del proyecto.
2. Abre `SimuRackon.sln` en Visual Studio 2022, versión 17.8 o posterior, con el SDK de .NET 8 y la carga de trabajo **Desarrollo de escritorio de .NET**.
3. En el Explorador de soluciones, selecciona **SimuRackon** como proyecto de inicio y presiona **F5**. La ronda comienza al abrirse la torre.

También puedes iniciar la aplicación desde una terminal de Windows, en la carpeta que contiene `SimuRackon.sln`.

```powershell
dotnet run --project SimuRackon.Desktop
```

## Tu primera autorización

Empiezas con **100 puntos y seis vuelos**. Tres esperan despegar, uno en cada pista, y tres esperan aterrizar. Arriba están el tiempo, el clima y los indicadores de desempeño. El mapa muestra el estado del aeropuerto. A la derecha están las tarjetas de vuelos y los controles de autorización.

1. Busca la tarjeta de **Salida** que dice **En pista A**. Selecciónala y pulsa **Autorizar despegue**. Con el clima soleado inicial, la pista A queda libre y el marcador sube a **120**.
2. Antes de que pasen 10 segundos, selecciona una **Llegada**, elige **A** y comprueba que la terminal seleccionada sea **T1**. Pulsa **Autorizar aterrizaje**. Si ambas siguen libres, el marcador sube a **140** y T1 queda ocupada.
3. Observa la **Bitácora de decisiones**. El mensaje de cada operación identifica el vuelo, explica lo que ocurrió e indica los puntos ganados o perdidos.
4. Mira la tarjeta del primer vuelo. Al vencer su cuenta de **10 segundos**, aparece otro identificador. Si la nueva salida ya ocupa A, puedes atenderla para volver a liberar esa pista.

También puedes seleccionar un vuelo pulsando su etiqueta en el mapa. Las distancias a A, B y C aparecen en kilómetros en el panel de autorización. La distancia te orienta al elegir una pista. Comprueba también su disponibilidad y la de la terminal.

## Pistas, clima y puntuación

| Pista | Terminales conectadas |
| --- | --- |
| A | T1 |
| B | T1 y T2 |
| C | T2 y T3 |

El clima pasa de **Soleado** a **Nublado**, después a **Lluvioso** y vuelve a **Soleado**. Cambia cada 45 segundos. El botón **Cambiar** del indicador de clima permite adelantar el cambio durante una práctica. En este escenario, la lluvia suspende los despegues. Las llegadas pueden aterrizar si la pista y la terminal están libres.

| Decisión | Puntos | ¿Qué pasa con el vuelo? |
| --- | ---: | --- |
| Autorizar un despegue con clima soleado o nublado | +20 | Despega y libera su pista. |
| Autorizar una llegada a una pista y una terminal conectada, ambas libres | +20 | Aterriza y ocupa la terminal. |
| Intentar un aterrizaje en una pista o terminal ocupada | −10 | Sigue esperando. La bitácora identifica qué instalación está ocupada. |
| Intentar un despegue con lluvia | −20 | Permanece en la pista hasta que puedas autorizarlo en condiciones permitidas. |

Cada vuelo completado programa **su propio reemplazo** en la misma tarjeta, 10 segundos después. La salida nueva ocupa la misma pista. Cuando se reemplaza una llegada, su terminal vuelve a quedar libre. Si completas los seis vuelos visibles, el reloj continúa y puedes seguir sumando puntos con los siguientes. Solo ingresan reemplazos antes de **00:00**.

## Consultar el resultado, reiniciar o salir

- Al llegar a **00:00**, la ventana de resultados muestra puntuación, vuelos completados respecto al total generado, aciertos, errores y porcentaje de decisiones correctas. Por ejemplo, dos aciertos y un error dan **67 %**.
- **Ver bitácora** cierra el resumen y vuelve al tablero de la ronda terminada. **Iniciar otra simulación** abre una ronda con seis vuelos nuevos, 3 minutos y 100 puntos.
- Para empezar de nuevo desde el tablero, pulsa **Nueva simulación**. Si ya tomaste decisiones, puedes cancelar el reinicio o confirmarlo. Para salir de la aplicación, cierra la ventana principal con la **X**.
- El resultado de cada ronda que llega a cero se guarda en `%LOCALAPPDATA%\SimuRackon\sesiones.csv`. Si Windows impide la escritura, la bitácora muestra **Historial sin guardar** y puedes consultar el resultado en pantalla.

## Cómo está organizado el código

| Componente | Responsabilidad |
| --- | --- |
| [Vuelo.cs](SimuRackon.Core/Vuelo.cs) | Representa un vuelo con su identificador, estado, pista, terminal y distancias. |
| [Aeropuerto.cs](SimuRackon.Core/Aeropuerto.cs) | Define las pistas, las conexiones con terminales y el cálculo de distancias. |
| [Simulacion.cs](SimuRackon.Core/Simulacion.cs) | Valida autorizaciones, calcula la puntuación y controla los reemplazos y el fin de ronda. |
| [TorreForm.cs](SimuRackon.Desktop/TorreForm.cs) y [MapaAeropuerto.cs](SimuRackon.Desktop/MapaAeropuerto.cs) | Reciben la selección del usuario y actualizan el tablero y el mapa. |
| [ResultadosForm.cs](SimuRackon.Desktop/ResultadosForm.cs) y [RegistroSesiones.cs](SimuRackon.Desktop/RegistroSesiones.cs) | Presentan el balance final y guardan el historial local. |
| [Pruebas/Program.cs](Pruebas/Program.cs) | Usa un reloj controlado para comprobar tiempos y reglas de la simulación. |

## Comprobar las reglas

Desde la carpeta de la solución, ejecuta las pruebas con el SDK de .NET 8.

```powershell
dotnet run --project Pruebas
```

Las pruebas comprueban, entre otros casos, dos vuelos con reemplazos simultáneos, dos con plazos distintos, la liberación de la terminal y el límite de **00:00**. Cuando pasan, la consola muestra `Pruebas del tráfico continuo completadas.`

## Generar el ejecutable para Windows x64

```powershell
dotnet publish SimuRackon.Desktop -c Release -r win-x64 --self-contained false -o Aplicacion
```

Distribuye la carpeta `Aplicacion` completa. Para abrir `SimuRackon.exe`, la computadora de destino necesita **.NET Desktop Runtime 8 de 64 bits**.
