namespace SimuRackon.Core;

public enum TipoOperacion { Llegada, Salida }
public enum EstadoVuelo { EsperandoPista, EsperandoDespegue, Aterrizado, Despegado }

public sealed class Vuelo
{
    public string Identificador { get; }
    public TipoOperacion Operacion { get; }
    public EstadoVuelo Estado { get; private set; }
    public Pista? Pista { get; private set; }
    public Terminal? Terminal { get; private set; }
    public IReadOnlyDictionary<Pista, int> Distancias { get; }
    public bool Completado => Estado is EstadoVuelo.Aterrizado or EstadoVuelo.Despegado;

    public string EstadoVisible => Estado switch
    {
        EstadoVuelo.EsperandoPista => "Esperando pista",
        EstadoVuelo.EsperandoDespegue => "Esperando autorización para despegar",
        EstadoVuelo.Aterrizado => "Aterrizó en la terminal " + Terminal,
        EstadoVuelo.Despegado => "Despegó de la pista " + Pista,
        _ => throw new InvalidOperationException("Estado del vuelo desconocido.")
    };

    internal Vuelo(string identificador, TipoOperacion operacion,
        IReadOnlyDictionary<Pista, int> distancias, Pista? pista = null)
    {
        Identificador = identificador;
        Operacion = operacion;
        Distancias = distancias;
        Pista = pista;
        Estado = operacion == TipoOperacion.Llegada ? EstadoVuelo.EsperandoPista : EstadoVuelo.EsperandoDespegue;
    }

    internal void Aterrizar(Pista pista, Terminal terminal)
    {
        Pista = pista;
        Terminal = terminal;
        Estado = EstadoVuelo.Aterrizado;
    }

    internal void Despegar() => Estado = EstadoVuelo.Despegado;
}
