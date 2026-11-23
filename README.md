# SimuRackon

Simulador educativo de control de tráfico aéreo en C# para Técnicas de Programación, FI UNAM.

## Abrir el proyecto

Extrae la carpeta completa. En Windows, abre `SimuRackon.sln` con Visual Studio 2022 (17.8 o posterior), la carga **Desarrollo de escritorio de .NET** y .NET 8. Selecciona **SimuRackon** como proyecto de inicio y presiona **F5**. También puedes ejecutar `dotnet run --project SimuRackon.Desktop` desde la carpeta de la solución.

## Atender el tráfico

Cada ronda comienza en 03:00 con 100 puntos y seis vuelos, tres llegadas y tres salidas. Puedes seleccionar cada vuelo desde el mapa o su tarjeta. Las pistas A, B y C están ocupadas por un despegue cada una al inicio.

Al autorizar un despegue se libera la pista. Al aterrizar, el vuelo ocupa una terminal conectada a la pista. A conecta con T1, B con T1 y T2, y C con T2 y T3. Soleado, Nublado y Lluvioso se alternan cada 45 segundos. Durante la lluvia se suspenden los despegues.

Cada autorización correcta suma 20 puntos y programa un vuelo nuevo en esa tarjeta diez segundos después. Los plazos corren de manera independiente. Al reaparecer una salida, entra en su pista. Al reaparecer una llegada, la terminal anterior queda libre tras el desembarque. Un intento en pista o terminal ocupada resta 10 puntos. Intentar despegar bajo la lluvia resta 20 puntos.

El marcador acumula todos los vuelos atendidos. A 00:00 se cierra el tráfico, se guarda el resultado en `%LOCALAPPDATA%\SimuRackon\sesiones.csv` y aparece la ventana de puntuación. **Iniciar otra simulación** empieza una ronda nueva.

## Generar el ejecutable para Windows

Para generar una copia lista para Windows x64, ejecuta `dotnet publish SimuRackon.Desktop -c Release -r win-x64 --self-contained false -o Aplicacion`. La computadora que abra `Aplicacion/SimuRackon.exe` necesita .NET Desktop Runtime 8 de 64 bits.

## Comprobar los tiempos de reposición

Ejecuta `dotnet run --project Pruebas` con el SDK .NET 8. Las pruebas usan un reloj controlado para verificar los 10 segundos, la coincidencia de vuelos, el vencimiento y el reinicio.

