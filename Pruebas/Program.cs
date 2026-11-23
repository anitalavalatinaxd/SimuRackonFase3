using SimuRackon.Core;

static void Verificar(bool condicion, string caso)
{
    if (!condicion) throw new InvalidOperationException(caso);
}

var reloj = new RelojDePrueba();
var ronda = new Simulacion(new Random(17), reloj);
var primero = ronda.Vuelos[0];
var segundo = ronda.Vuelos[2];
var pista = primero.Pista!.Value;
var terminal = Aeropuerto.TerminalesDe(pista)[0];
var llegada = ronda.Vuelos[1];
int notificados = 0;
ronda.VuelosIncorporados += nuevos => notificados += nuevos.Count;

Verificar(ronda.Autorizar(llegada, pista, terminal)?.Aceptada == false, "Rechazar una pista ocupada");
Verificar(ronda.SegundosParaReemplazo(1) == null, "Un rechazo no programa otro vuelo");
Verificar(ronda.Autorizar(primero)?.Aceptada == true, "Primer despegue");
Verificar(ronda.Autorizar(llegada, pista, terminal)?.Aceptada == true, "Aterrizaje simultáneo");
Verificar(ronda.Autorizar(primero) is null, "Evitar doble puntuación");
reloj.Avanzar(3000);
Verificar(ronda.Autorizar(segundo)?.Aceptada == true, "Segundo despegue a los tres segundos");
reloj.Avanzar(6999);
ronda.ActualizarTrafico();
Verificar(ronda.TotalGenerados == 6 && ronda.Ocupante(terminal) == llegada, "Antes de diez segundos");
reloj.Avanzar(1);
ronda.ActualizarTrafico();
Verificar(ronda.TotalGenerados == 8 && ronda.Completados == 3 && ronda.Puntos == 150, "Dos reemplazos simultáneos y marcador acumulado");
Verificar(ronda.Vuelos[0] != primero && ronda.Ocupante(pista) == ronda.Vuelos[0], "Nueva salida en la pista liberada");
Verificar(ronda.Vuelos[1] != llegada && ronda.Ocupante(terminal) is null, "Liberar la terminal al retirar la llegada");
Verificar(ronda.Vuelos[2] == segundo && ronda.SegundosParaReemplazo(2) == 3, "Independencia entre plazos");
reloj.Avanzar(3000);
ronda.ActualizarTrafico();
ronda.ActualizarTrafico();
Verificar(ronda.TotalGenerados == 9 && notificados == 3 && ronda.Vuelos[2] != segundo, "Reposición única al segundo trece");
Verificar(ronda.Autorizar(primero) is null, "Un vuelo sustituido no se puede volver a autorizar");

var otroReloj = new RelojDePrueba();
var todos = new Simulacion(new Random(22), otroReloj);
foreach (var v in todos.Vuelos.Where(v => v.Operacion == TipoOperacion.Salida).ToArray())
    Verificar(todos.Autorizar(v)?.Aceptada == true, "Liberar las tres pistas");
for (int par = 0; par < 3; par++)
    Verificar(todos.Autorizar(todos.Vuelos[2 * par + 1], Aeropuerto.Pistas[par], Aeropuerto.Terminales[par])?.Aceptada == true, "Aterrizaje válido");
Verificar(todos.Completados == 6 && !todos.Finalizada, "Seguir tras completar los primeros seis");
otroReloj.Avanzar(10000);
todos.ActualizarTrafico();
Verificar(todos.TotalGenerados == 12 && todos.Vuelos.All(v => !v.Completado), "Seis nuevos vuelos");
Verificar(todos.Autorizar(todos.Vuelos[0])?.Aceptada == true && todos.Completados == 7, "Más de seis aciertos");

var relojLimite = new RelojDePrueba();
var limite = new Simulacion(new Random(4), relojLimite);
relojLimite.Avanzar(170000);
limite.Autorizar(limite.Vuelos[0]);
relojLimite.Avanzar(10000);
limite.ActualizarTrafico();
Verificar(limite.TotalGenerados == 6 && limite.Finalizada && limite.TiempoRestante == TimeSpan.Zero,
    "Un reemplazo que coincide con 00:00 no se incorpora");
Verificar(limite.Autorizar(limite.Vuelos[2]) is null && limite.Completados == 1, "Mantener la puntuación al cierre");
var reinicio = new Simulacion(new Random(5), relojLimite);
reinicio.ActualizarTrafico();
Verificar(reinicio.TotalGenerados == 6 && reinicio.Completados == 0 && reinicio.Puntos == 100 &&
    reinicio.TiempoRestante == TimeSpan.FromMinutes(3), "Reinicio limpio");
Console.WriteLine("Pruebas del tráfico continuo completadas.");

sealed class RelojDePrueba : TimeProvider
{
    private long milisegundos;
    public override long TimestampFrequency => 1000;
    public override long GetTimestamp() => milisegundos;
    public void Avanzar(int intervaloMs) => milisegundos += intervaloMs;
}
