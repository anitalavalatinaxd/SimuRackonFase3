namespace SimuRackon.Core;

public enum Pista { A, B, C }
public enum Terminal { T1, T2, T3 }

public static class Aeropuerto
{
    public static IReadOnlyList<Pista> Pistas { get; } = Array.AsReadOnly(Enum.GetValues<Pista>());
    public static IReadOnlyList<Terminal> Terminales { get; } = Array.AsReadOnly(Enum.GetValues<Terminal>());

    private static readonly IReadOnlyDictionary<Pista, IReadOnlyList<Terminal>> Conexiones =
        new Dictionary<Pista, IReadOnlyList<Terminal>>
        {
            [Pista.A] = Array.AsReadOnly(new[] { Terminal.T1 }),
            [Pista.B] = Array.AsReadOnly(new[] { Terminal.T1, Terminal.T2 }),
            [Pista.C] = Array.AsReadOnly(new[] { Terminal.T2, Terminal.T3 })
        };

    // Puntos de referencia de un aeropuerto ficticio, expresados en kilómetros.
    private static readonly IReadOnlyDictionary<Pista, (double X, double Y)> Posiciones =
        new Dictionary<Pista, (double, double)>
        {
            [Pista.A] = (0, 0),
            [Pista.B] = (30, 0),
            [Pista.C] = (18.75, Math.Sqrt(25 * 25 - 18.75 * 18.75))
        };

    public static IReadOnlyList<Terminal> TerminalesDe(Pista pista) => Conexiones[pista];
    internal static IReadOnlyDictionary<Pista, int> DistanciasDesdePista(Pista pista) =>
        DistanciasDesde(Posiciones[pista].X, Posiciones[pista].Y);

    internal static IReadOnlyDictionary<Pista, int> DistanciasDesde(double x, double y)
    {
        var distancias = new Dictionary<Pista, int>();
        foreach (var pista in Pistas)
        {
            var posicion = Posiciones[pista];
            double dx = x - posicion.X;
            double dy = y - posicion.Y;
            distancias.Add(pista, (int)Math.Round(Math.Sqrt(dx * dx + dy * dy)));
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<Pista, int>(distancias);
    }
}
