namespace SimuRackon.Core;

public enum CondicionClimatica { Soleado, Nublado, Lluvioso }
public enum FinDeRonda { VuelosCompletados, TiempoAgotado }

public sealed record ResultadoAutorizacion(string Titulo, string Explicacion, int CambioPuntos)
{
    public bool Aceptada => CambioPuntos > 0;
}

public sealed class Simulacion
{
    public const int CantidadVuelos = 6;
    public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan DemoraReemplazo = TimeSpan.FromSeconds(10);

    private static readonly string[] Prefijos = ["AMX", "VOI", "VIV", "MXA", "LNE", "RKN"];
    private readonly Vuelo[] vuelos = new Vuelo[CantidadVuelos];
    private readonly TimeSpan?[] entradas = new TimeSpan?[CantidadVuelos];
    private readonly Random aleatorio;
    private readonly TimeProvider reloj;
    private readonly long inicio;
    private readonly int primerPrefijo;
    private int siguienteNumero;

    public IReadOnlyList<Vuelo> Vuelos { get; }
    public event Action<IReadOnlyList<Vuelo>>? VuelosIncorporados;
    public CondicionClimatica Clima { get; private set; } = CondicionClimatica.Soleado;
    public int Puntos { get; private set; } = 100;
    public int Aciertos { get; private set; }
    public int Errores { get; private set; }
    public int Completados => Aciertos;
    public int TotalGenerados { get; private set; } = CantidadVuelos;
    public int? Porcentaje => Aciertos + Errores == 0
        ? null
        : (int)Math.Round(100.0 * Aciertos / (Aciertos + Errores), MidpointRounding.AwayFromZero);

    public TimeSpan TiempoRestante
    {
        get
        {
            TimeSpan restante = Duracion - reloj.GetElapsedTime(inicio);
            return restante > TimeSpan.Zero ? restante : TimeSpan.Zero;
        }
    }

    public bool Finalizada => TiempoRestante == TimeSpan.Zero;
    public FinDeRonda? MotivoDeFin => Finalizada ? FinDeRonda.TiempoAgotado : null;

    public string Visibilidad => Clima switch
    {
        CondicionClimatica.Soleado => "Buena",
        CondicionClimatica.Nublado => "Reducida",
        _ => "Regular"
    };

    public Simulacion(Random? aleatorio = null, TimeProvider? reloj = null)
    {
        this.aleatorio = aleatorio ?? Random.Shared;
        this.reloj = reloj ?? TimeProvider.System;
        Pista[] pistas = Aeropuerto.Pistas.ToArray();
        this.aleatorio.Shuffle(pistas);
        primerPrefijo = this.aleatorio.Next(Prefijos.Length);
        siguienteNumero = this.aleatorio.Next(120, 800);
        for (int i = 0; i < CantidadVuelos; i += 2)
        {
            vuelos[i] = CrearVuelo(i, pistas[i / 2]);
            vuelos[i + 1] = CrearVuelo(i + 1, null);
        }
        Vuelos = Array.AsReadOnly(vuelos);
        inicio = this.reloj.GetTimestamp();
    }

    private Vuelo CrearVuelo(int tarjeta, Pista? pistaSalida)
    {
        string id = Prefijos[(primerPrefijo + tarjeta) % Prefijos.Length] + siguienteNumero++;
        if (pistaSalida is Pista pista)
            return new Vuelo(id, TipoOperacion.Salida, Aeropuerto.DistanciasDesdePista(pista), pista);

        var (x, y) = (tarjeta / 2) switch
        {
            0 => (aleatorio.Next(-12, -3), aleatorio.Next(-12, 13)),
            1 => (aleatorio.Next(34, 44), aleatorio.Next(7, 25)),
            _ => (aleatorio.Next(-16, -5), aleatorio.Next(17, 29))
        };
        return new Vuelo(id, TipoOperacion.Llegada, Aeropuerto.DistanciasDesde(x, y));
    }

    public int? SegundosParaReemplazo(int tarjeta)
    {
        TimeSpan ahora = reloj.GetElapsedTime(inicio);
        if (entradas[tarjeta] is not TimeSpan prevista || prevista >= Duracion || ahora >= Duracion)
            return null;
        return (int)Math.Ceiling(Math.Max(0, (prevista - ahora).TotalSeconds));
    }

    public void ActualizarTrafico() => ActualizarTrafico(reloj.GetElapsedTime(inicio));

    private void ActualizarTrafico(TimeSpan ahora)
    {
        if (ahora >= Duracion) return;
        List<Vuelo>? nuevos = null;
        for (int i = 0; i < vuelos.Length; i++)
        {
            if (entradas[i] is not TimeSpan prevista || ahora < prevista) continue;
            var anterior = vuelos[i];
            var nuevo = CrearVuelo(i, anterior.Operacion == TipoOperacion.Salida ? anterior.Pista : null);
            // Al retirar la llegada anterior, su terminal queda libre tras el desembarque.
            vuelos[i] = nuevo;
            entradas[i] = null;
            TotalGenerados++;
            (nuevos ??= new List<Vuelo>()).Add(nuevo);
        }
        if (nuevos is not null) VuelosIncorporados?.Invoke(nuevos);
    }

    public void CambiarClima()
    {
        if (Finalizada) return;
        Clima = Clima switch
        {
            CondicionClimatica.Soleado => CondicionClimatica.Nublado,
            CondicionClimatica.Nublado => CondicionClimatica.Lluvioso,
            _ => CondicionClimatica.Soleado
        };
    }

    public Vuelo? Ocupante(Pista pista) =>
        Vuelos.FirstOrDefault(vuelo => !vuelo.Completado && vuelo.Pista == pista);
    public Vuelo? Ocupante(Terminal terminal) =>
        Vuelos.FirstOrDefault(vuelo => vuelo.Terminal == terminal);

    public ResultadoAutorizacion? Autorizar(Vuelo vuelo, Pista? pista = null, Terminal? terminal = null)
    {
        TimeSpan ahora = reloj.GetElapsedTime(inicio);
        if (ahora >= Duracion) return null;
        // Validar con el tráfico vigente incluso si el temporizador gráfico se retrasó.
        ActualizarTrafico(ahora);
        int tarjeta = Array.IndexOf(vuelos, vuelo);
        if (tarjeta < 0 || vuelo.Completado) return null;
        var resultado = vuelo.Operacion == TipoOperacion.Salida
            ? EvaluarDespegue(vuelo) : EvaluarAterrizaje(vuelo, pista, terminal);
        if (resultado is null) return null;
        Puntos += resultado.CambioPuntos;
        if (resultado.Aceptada)
        {
            Aciertos++;
            entradas[tarjeta] = ahora + DemoraReemplazo;
        }
        else Errores++;
        return resultado;
    }

    private ResultadoAutorizacion EvaluarDespegue(Vuelo vuelo)
    {
        if (Clima == CondicionClimatica.Lluvioso)
            return new("Despegue rechazado",
                $"La lluvia suspende los despegues. {vuelo.Identificador} permanece en la pista {vuelo.Pista}.", -20);
        vuelo.Despegar();
        return new("Despegue autorizado",
            $"{vuelo.Identificador} despegó y liberó la pista {vuelo.Pista}.", 20);
    }

    private ResultadoAutorizacion? EvaluarAterrizaje(Vuelo vuelo, Pista? pista, Terminal? terminal)
    {
        if (pista is not Pista destino || terminal is not Terminal estacion ||
            !Aeropuerto.Pistas.Contains(destino) || !Aeropuerto.TerminalesDe(destino).Contains(estacion))
            return null;
        if (Ocupante(destino) is Vuelo enPista)
            return new("Aterrizaje rechazado",
                $"La pista {destino} está ocupada por {enPista.Identificador}. {vuelo.Identificador} sigue esperando pista.", -10);
        if (Ocupante(estacion) is Vuelo enTerminal)
            return new("Aterrizaje rechazado",
                $"La terminal {estacion} está ocupada por {enTerminal.Identificador}. {vuelo.Identificador} sigue esperando pista.", -10);
        vuelo.Aterrizar(destino, estacion);
        return new("Aterrizaje autorizado",
            $"{vuelo.Identificador} aterrizó por la pista {destino} y ocupa la terminal {estacion}.", 20);
    }
}
