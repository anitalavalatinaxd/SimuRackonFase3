using System.Globalization;
using System.Text;
using SimuRackon.Core;

namespace SimuRackon.Desktop;

internal static class RegistroSesiones
{
    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimuRackon", "sesiones.csv");

    public static void Guardar(Simulacion simulacion)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ruta)!);
        using var archivo = new FileStream(Ruta, FileMode.Append, FileAccess.Write, FileShare.Read);
        bool escribirEncabezado = archivo.Length == 0;
        using var csv = new StreamWriter(archivo, new UTF8Encoding(true));
        if (escribirEncabezado) csv.WriteLine("Fecha;Puntos;Aciertos;Errores;Porcentaje;Vuelos completados");
        string fecha = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        csv.WriteLine(FormattableString.Invariant(
            $"{fecha};{simulacion.Puntos};{simulacion.Aciertos};{simulacion.Errores};{simulacion.Porcentaje};{simulacion.Completados}"));
    }
}
