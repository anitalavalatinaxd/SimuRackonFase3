using SimuRackon.Core;

namespace SimuRackon.Desktop;

internal sealed class TorreForm : Form
{
    private readonly Font fuenteNormal = new("Segoe UI", 10);
    private readonly Font fuenteSeccion = new("Segoe UI", 11, FontStyle.Bold);
    private readonly Font fuenteTitulo = new("Segoe UI", 21, FontStyle.Bold);
    private readonly Font fuenteIndicador = new("Segoe UI", 16, FontStyle.Bold);
    private readonly System.Windows.Forms.Timer relojClima = new() { Interval = 45000 };
    private readonly System.Windows.Forms.Timer relojRonda = new() { Interval = 200 };
    private readonly MapaAeropuerto mapa = new() { Dock = DockStyle.Fill };
    private readonly Label[] disponibilidad = new Label[Aeropuerto.Pistas.Count + Aeropuerto.Terminales.Count];
    private readonly Button[] tarjetas = new Button[Simulacion.CantidadVuelos];
    private readonly Button[] botonesPista = new Button[3];
    private readonly Label[] distancias = new Label[3];
    private readonly Label tiempoValor = new();
    private readonly Label climaValor = new();
    private readonly Label climaDetalle = new();
    private readonly Label puntosValor = new();
    private readonly Label avanceValor = new();
    private readonly Label precisionValor = new();
    private readonly Label precisionDetalle = new();
    private readonly Button cambiarClima = new();
    private readonly Label vueloActual = new();
    private readonly Label indicacion = new();
    private readonly ComboBox terminales = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button autorizar = new();
    private readonly Label respuesta = new();
    private readonly Label resumen = new();
    private readonly RichTextBox bitacora = new()
    {
        ReadOnly = true, WordWrap = true, DetectUrls = false, BorderStyle = BorderStyle.None,
        ScrollBars = RichTextBoxScrollBars.Vertical, Dock = DockStyle.Fill
    };

    private Control[] controlesDistancia = [];
    private Control[] controlesLlegada = [];
    private Simulacion simulacion = null!;
    private bool resultadoMostrado;
    private TaskDialogPage? confirmacionReinicio;
    private Vuelo? elegido;
    private Pista? pistaElegida;
    private bool actualizandoTerminales;

    public TorreForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = fuenteNormal;
        ClientSize = new Size(1210, 792);
        MinimumSize = new Size(915, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SimuRackon · Torre de control";
        BackColor = Paleta.Fondo;
        ForeColor = Paleta.Texto;
        ConstruirInterfaz();
        mapa.VueloSeleccionado += SeleccionarVuelo;
        relojClima.Tick += (_, _) => AvanzarClima();
        relojRonda.Tick += (_, _) =>
        {
            ActualizarTiempo();
            if (RevisarFin()) return;
            simulacion.ActualizarTrafico();
            ActualizarCuentaDeReemplazos();
        };
        Shown += (_, _) => IniciarRonda();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        relojClima.Dispose();
        relojRonda.Dispose();
        fuenteNormal.Dispose();
        fuenteSeccion.Dispose();
        fuenteTitulo.Dispose();
        fuenteIndicador.Dispose();
    }

    private void ConstruirInterfaz()
    {
        var raiz = Tabla(1, 4);
        raiz.Padding = new Padding(16, 12, 16, 5);
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));

        var encabezado = Tabla(3, 2);
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 174));
        encabezado.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        encabezado.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        encabezado.Controls.Add(Texto("SimuRackon · Torre de control", fuenteTitulo, Paleta.Texto), 0, 0);
        encabezado.Controls.Add(Texto("Coordina las llegadas y las salidas del aeropuerto.", fuenteNormal, Paleta.Secundario), 0, 1);
        var reloj = Tabla(1, 2);
        reloj.BackColor = Paleta.Panel;
        reloj.Margin = new Padding(0, 3, 0, 3);
        reloj.Padding = new Padding(10, 2, 8, 2);
        reloj.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));
        reloj.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        reloj.Controls.Add(Texto("Tiempo restante", fuenteNormal, Paleta.Secundario), 0, 0);
        EstiloTexto(tiempoValor, fuenteIndicador, Paleta.Acento);
        tiempoValor.AccessibleName = "Tiempo restante";
        reloj.Controls.Add(tiempoValor, 0, 1);
        encabezado.Controls.Add(reloj, 1, 0);
        encabezado.SetRowSpan(reloj, 2);
        var reiniciar = Boton("Nueva simulación");
        reiniciar.Margin = new Padding(8, 6, 0, 3);
        reiniciar.Click += (_, _) => SolicitarNuevaRonda();
        encabezado.Controls.Add(reiniciar, 2, 0);
        raiz.Controls.Add(encabezado, 0, 0);

        var indicadores = Tabla(4, 1);
        indicadores.Margin = new Padding(0, 6, 0, 10);
        for (int i = 0; i < 4; i++) indicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        var clima = Indicador("Clima", climaValor, climaDetalle, conBoton: true);
        var puntuacion = Indicador("Puntuación", puntosValor);
        var avance = Indicador("Vuelos completados", avanceValor);
        var precision = Indicador("Decisiones correctas", precisionValor, precisionDetalle);
        puntosValor.ForeColor = Paleta.Acento;
        precision.Margin = Padding.Empty;
        indicadores.Controls.Add(clima, 0, 0);
        indicadores.Controls.Add(puntuacion, 1, 0);
        indicadores.Controls.Add(avance, 2, 0);
        indicadores.Controls.Add(precision, 3, 0);
        raiz.Controls.Add(indicadores, 0, 1);

        var paneles = Tabla(2, 1);
        paneles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        paneles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        paneles.Controls.Add(PanelAeropuerto(), 0, 0);
        paneles.Controls.Add(PanelControl(), 1, 0);
        raiz.Controls.Add(paneles, 0, 2);
        raiz.Controls.Add(Texto("Técnicas de Programación · Facultad de Ingeniería, UNAM   |   Escenario ficticio",
            fuenteNormal, Paleta.Secundario), 0, 3);
        Controls.Add(raiz);
    }

    private TableLayoutPanel Indicador(string titulo, Label valor, Label? detalle = null, bool conBoton = false)
    {
        var panel = Tabla(conBoton ? 2 : 1, 3);
        panel.BackColor = Paleta.Panel;
        panel.Margin = new Padding(0, 0, 8, 0);
        panel.Padding = new Padding(9, 3, 8, 3);
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (conBoton) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.Controls.Add(Texto(titulo, fuenteNormal, Paleta.Secundario), 0, 0);
        EstiloTexto(valor, fuenteIndicador, Paleta.Texto);
        panel.Controls.Add(valor, 0, 1);
        if (detalle is not null)
        {
            EstiloTexto(detalle, fuenteNormal, Paleta.Secundario);
            panel.Controls.Add(detalle, 0, 2);
        }
        if (conBoton)
        {
            EstiloBoton(cambiarClima, "Cambiar", Paleta.Control, Paleta.Texto);
            cambiarClima.AccessibleName = "Cambiar clima";
            cambiarClima.AccessibleDescription = "El clima también cambia automáticamente cada 45 segundos.";
            cambiarClima.Margin = new Padding(1, 14, 0, 14);
            cambiarClima.Click += (_, _) => AvanzarClima();
            panel.Controls.Add(cambiarClima, 1, 0);
            panel.SetRowSpan(cambiarClima, 3);
        }
        return panel;
    }

    private TableLayoutPanel PanelAeropuerto()
    {
        var izquierda = Tabla(1, 2);
        izquierda.Margin = new Padding(0, 0, 10, 0);
        izquierda.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        izquierda.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
        var panelMapa = Tabla(1, 3);
        panelMapa.Padding = new Padding(8, 2, 8, 4);
        panelMapa.BackColor = Paleta.Panel;
        panelMapa.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        panelMapa.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panelMapa.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panelMapa.Controls.Add(Texto("Mapa del aeropuerto", fuenteSeccion, Paleta.Texto), 0, 0);
        panelMapa.Controls.Add(mapa, 0, 1);
        var estadoAeropuerto = Tabla(disponibilidad.Length, 1);
        for (int i = 0; i < disponibilidad.Length; i++)
        {
            estadoAeropuerto.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / disponibilidad.Length));
            disponibilidad[i] = Texto("", fuenteNormal, Paleta.Secundario, ContentAlignment.MiddleCenter);
            estadoAeropuerto.Controls.Add(disponibilidad[i], i, 0);
        }
        panelMapa.Controls.Add(estadoAeropuerto, 0, 2);
        izquierda.Controls.Add(panelMapa, 0, 0);
        var panelBitacora = Tabla(1, 2);
        panelBitacora.BackColor = Paleta.Panel;
        panelBitacora.Margin = new Padding(0, 9, 0, 0);
        panelBitacora.Padding = new Padding(10, 3, 10, 8);
        panelBitacora.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panelBitacora.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bitacora.BackColor = Paleta.Panel;
        bitacora.ForeColor = Paleta.Texto;
        bitacora.AccessibleName = "Bitácora de la simulación";
        panelBitacora.Controls.Add(Texto("Bitácora de decisiones", fuenteSeccion, Paleta.Texto), 0, 0);
        panelBitacora.Controls.Add(bitacora, 0, 1);
        izquierda.Controls.Add(panelBitacora, 0, 1);
        return izquierda;
    }

    private TableLayoutPanel PanelControl()
    {
        var derecha = Tabla(1, 2);
        derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, 187));
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        const int columnas = 2;
        int filas = (tarjetas.Length + columnas - 1) / columnas;
        var lista = Tabla(columnas, filas + 1);
        for (int i = 0; i < columnas; i++) lista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnas));
        lista.BackColor = Paleta.Panel;
        lista.Padding = new Padding(8, 2, 8, 7);
        lista.Margin = new Padding(0, 0, 0, 9);
        lista.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var tituloLista = Texto("Vuelos de la ronda", fuenteSeccion, Paleta.Texto);
        lista.Controls.Add(tituloLista, 0, 0);
        lista.SetColumnSpan(tituloLista, columnas);
        for (int i = 0; i < filas; i++) lista.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / filas));
        for (int i = 0; i < tarjetas.Length; i++)
        {
            int indice = i;
            tarjetas[i] = Boton("");
            tarjetas[i].TextAlign = ContentAlignment.MiddleLeft;
            tarjetas[i].Padding = new Padding(8, 0, 0, 0);
            tarjetas[i].Margin = new Padding(0, 1, 3, 3);
            tarjetas[i].Click += (_, _) => SeleccionarVuelo(simulacion.Vuelos[indice]);
            lista.Controls.Add(tarjetas[i], i % columnas, i / columnas + 1);
        }
        derecha.Controls.Add(lista, 0, 0);
        derecha.Controls.Add(PanelAutorizacion(), 0, 1);
        return derecha;
    }

    private Panel PanelAutorizacion()
    {
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Paleta.Panel, Margin = Padding.Empty };
        var formulario = Tabla(1, 11);
        formulario.AutoSize = true;
        formulario.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        formulario.Dock = DockStyle.Top;
        formulario.Padding = new Padding(10, 3, 10, 7);
        for (int i = 0; i < formulario.RowCount; i++) formulario.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        EstiloTexto(vueloActual, fuenteSeccion, Paleta.Texto);
        vueloActual.AutoSize = true;
        vueloActual.MinimumSize = new Size(0, 29);
        formulario.Controls.Add(vueloActual, 0, 0);
        EstiloTexto(indicacion, fuenteNormal, Paleta.Secundario);
        indicacion.AutoSize = true;
        indicacion.MinimumSize = new Size(0, 27);
        formulario.Controls.Add(indicacion, 0, 1);
        var tituloDistancias = Texto("Distancias a las pistas", fuenteNormal, Paleta.Secundario);
        tituloDistancias.MinimumSize = new Size(0, 23);
        formulario.Controls.Add(tituloDistancias, 0, 2);
        var mediciones = Tabla(3, 1);
        mediciones.MinimumSize = new Size(0, 27);
        for (int i = 0; i < distancias.Length; i++)
        {
            mediciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / distancias.Length));
            distancias[i] = Texto("", fuenteNormal, Paleta.Lavanda, ContentAlignment.MiddleCenter);
            distancias[i].BackColor = Paleta.Control;
            distancias[i].Margin = new Padding(0, 0, 5, 0);
            mediciones.Controls.Add(distancias[i], i, 0);
        }
        formulario.Controls.Add(mediciones, 0, 3);

        var tituloPistas = Texto("Selecciona una pista", fuenteNormal, Paleta.Secundario);
        tituloPistas.MinimumSize = new Size(0, 27);
        formulario.Controls.Add(tituloPistas, 0, 4);
        var pistas = Tabla(3, 1);
        pistas.MinimumSize = new Size(0, 36);
        for (int i = 0; i < botonesPista.Length; i++)
        {
            Pista pista = Aeropuerto.Pistas[i];
            pistas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / botonesPista.Length));
            botonesPista[i] = Boton("");
            botonesPista[i].Margin = new Padding(0, 0, 5, 0);
            botonesPista[i].Click += (_, _) => SeleccionarPista(pista);
            pistas.Controls.Add(botonesPista[i], i, 0);
        }
        formulario.Controls.Add(pistas, 0, 5);
        var tituloTerminal = Texto("Terminal", fuenteNormal, Paleta.Secundario);
        tituloTerminal.MinimumSize = new Size(0, 24);
        formulario.Controls.Add(tituloTerminal, 0, 6);
        terminales.Dock = DockStyle.Fill;
        terminales.IntegralHeight = true;
        terminales.BackColor = Paleta.Control;
        terminales.ForeColor = Paleta.Texto;
        terminales.Margin = Padding.Empty;
        terminales.AccessibleName = "Terminal de la llegada";
        terminales.SelectedIndexChanged += (_, _) =>
        {
            if (!actualizandoTerminales) ActualizarAutorizacion();
        };
        formulario.Controls.Add(terminales, 0, 7);

        EstiloBoton(autorizar, "Autorizar", Paleta.Acento, Paleta.Fondo);
        autorizar.MinimumSize = new Size(0, 36);
        autorizar.Margin = new Padding(0, 8, 0, 0);
        autorizar.Click += (_, _) => AutorizarOperacion();
        formulario.Controls.Add(autorizar, 0, 8);
        EstiloTexto(resumen, fuenteNormal, Paleta.Acento);
        resumen.AutoSize = true;
        resumen.Margin = new Padding(0, 7, 0, 6);
        formulario.Controls.Add(resumen, 0, 9);
        EstiloTexto(respuesta, fuenteNormal, Paleta.Texto);
        respuesta.AutoSize = true;
        respuesta.Margin = new Padding(0, 9, 0, 4);
        formulario.Controls.Add(respuesta, 0, 10);
        controlesDistancia = [tituloDistancias, mediciones];
        controlesLlegada = [tituloPistas, pistas, tituloTerminal, terminales];
        panel.Controls.Add(formulario);
        panel.ClientSizeChanged += (_, _) =>
        {
            int ancho = Math.Max(100, panel.ClientSize.Width - formulario.Padding.Horizontal - 4);
            foreach (Label etiqueta in new[] { indicacion, resumen, respuesta }) etiqueta.MaximumSize = new Size(ancho, 0);
        };
        return panel;
    }
    
    private void SolicitarNuevaRonda()
    {
        if (!resultadoMostrado)
        {
            if (RevisarFin()) return;
            if (simulacion.Aciertos + simulacion.Errores > 0)
            {
                var reiniciar = new TaskDialogButton("Reiniciar");
                var cancelar = TaskDialogButton.Cancel;
                confirmacionReinicio = new TaskDialogPage
                {
                    Caption = "Nueva simulación",
                    Heading = "¿Reiniciar la ronda actual?",
                    Text = "Se reiniciarán el tiempo, la puntuación y la bitácora.",
                    AllowCancel = true,
                    Buttons = { cancelar, reiniciar },
                    DefaultButton = cancelar
                };
                TaskDialogButton decision;
                try { decision = TaskDialog.ShowDialog(this, confirmacionReinicio); }
                finally { confirmacionReinicio = null; }
                if (RevisarFin() || decision != reiniciar) return;
            }
        }
        IniciarRonda();
    }

    private void IniciarRonda()
    {
        relojRonda.Stop();
        relojClima.Stop();
        if (simulacion is not null) simulacion.VuelosIncorporados -= IncorporarVuelos;
        simulacion = new Simulacion();
        simulacion.VuelosIncorporados += IncorporarVuelos;
        resultadoMostrado = false;
        elegido = simulacion.Vuelos[0];
        pistaElegida = null;
        respuesta.Text = "";
        resumen.Text = "";
        bitacora.Clear();
        LlenarTerminales();
        AgregarEvento("Simulación iniciada", "Tres llegadas esperan pista. Autoriza los despegues para liberar las pistas.", Paleta.Lavanda);
        ActualizarTiempo();
        ActualizarVista();
        relojClima.Start();
        relojRonda.Start();
    }

    private void IncorporarVuelos(IReadOnlyList<Vuelo> nuevos)
    {
        foreach (var vuelo in nuevos)
        {
            string ubicacion = vuelo.Operacion == TipoOperacion.Salida
                ? $"Espera para despegar en la pista {vuelo.Pista}."
                : "En aproximación, esperando pista.";
            AgregarEvento("Nuevo vuelo · " + vuelo.Identificador, ubicacion, Paleta.Lavanda);
        }

        elegido ??= nuevos[0];
        // Una nueva llegada puede liberar una terminal que ya figura en el selector.
        LlenarTerminales(conservarSeleccion: true);
        ActualizarVista();
    }

    private void ActualizarCuentaDeReemplazos()
    {
        for (int i = 0; i < tarjetas.Length; i++)
        {
            var vuelo = simulacion.Vuelos[i];
            if (simulacion.SegundosParaReemplazo(i) is not int segundos) continue;
            string texto = $"{vuelo.Identificador} · Atendido{Environment.NewLine}Nuevo en {segundos} s";
            if (tarjetas[i].Text != texto) tarjetas[i].Text = texto;
        }
    }

    private void SeleccionarVuelo(Vuelo vuelo)
    {
        if (RevisarFin() || vuelo.Completado || vuelo == elegido) return;
        elegido = vuelo;
        pistaElegida = null;
        respuesta.Text = "";
        LlenarTerminales();
        ActualizarVista();
    }

    private void SeleccionarPista(Pista pista)
    {
        if (RevisarFin() || elegido?.Operacion != TipoOperacion.Llegada || pistaElegida == pista) return;
        pistaElegida = pista;
        LlenarTerminales();
        ActualizarAutorizacion();
    }

    private void LlenarTerminales(bool conservarSeleccion = false)
    {
        Terminal? anterior = conservarSeleccion ? TerminalElegida : null;
        actualizandoTerminales = true;
        terminales.BeginUpdate();
        terminales.Items.Clear();
        if (pistaElegida is Pista pista)
        {
            var opciones = Aeropuerto.TerminalesDe(pista);
            foreach (var terminal in opciones)
            {
                terminales.Items.Add(new OpcionTerminal(terminal, simulacion.Ocupante(terminal) is not null));
            }
            int seleccion = opciones.Count == 1 ? 0 : -1;
            for (int i = 0; i < opciones.Count; i++)
                if (opciones[i] == anterior) seleccion = i;
            terminales.SelectedIndex = seleccion;
        }
        terminales.EndUpdate();
        actualizandoTerminales = false;
    }

    private void AvanzarClima()
    {
        simulacion.CambiarClima();
        if (RevisarFin()) return;
        relojClima.Stop();
        relojClima.Start();
        string efecto = simulacion.Clima == CondicionClimatica.Lluvioso ? " Los despegues están suspendidos." : "";
        AgregarEvento("Cambio de clima · " + simulacion.Clima,
            "Visibilidad " + simulacion.Visibilidad.ToLowerInvariant() + "." + efecto, Paleta.Lavanda);
        ActualizarVista();
    }

    private void AutorizarOperacion()
    {
        if (RevisarFin() || elegido is null) return;
        var resultado = simulacion.Autorizar(elegido, pistaElegida, TerminalElegida);
        if (resultado is null)
        {
            RevisarFin();
            return;
        }
        Color color = resultado.Aceptada ? Paleta.Acento : Paleta.Alerta;
        string puntos = resultado.CambioPuntos > 0 ? "+" + resultado.CambioPuntos : "−" + Math.Abs(resultado.CambioPuntos);
        string titulo = resultado.Titulo + " · " + puntos + " pts";
        respuesta.ForeColor = color;
        respuesta.Text = titulo + Environment.NewLine + resultado.Explicacion;
        AgregarEvento(titulo, resultado.Explicacion, color);
        if (resultado.Aceptada)
        {
            elegido = simulacion.Vuelos.FirstOrDefault(vuelo => !vuelo.Completado);
            pistaElegida = null;
            LlenarTerminales();
        }
        if (!RevisarFin()) ActualizarVista();
    }

    private void ActualizarTiempo()
    {
        int segundos = (int)Math.Ceiling(simulacion.TiempoRestante.TotalSeconds);
        tiempoValor.Text = $"{segundos / 60:00}:{segundos % 60:00}";
        tiempoValor.ForeColor = segundos <= 30 ? Paleta.Alerta : Paleta.Acento;
    }

    private bool RevisarFin()
    {
        if (!simulacion.Finalizada) return false;
        if (resultadoMostrado) return true;
        if (confirmacionReinicio is not null)
        {
            confirmacionReinicio.BoundDialog?.Close();
            return true;
        }
        resultadoMostrado = true;
        relojRonda.Stop();
        relojClima.Stop();
        elegido = null;
        pistaElegida = null;
        LlenarTerminales();
        const string causa = "Se agotó el tiempo.";
        resumen.Text = $"{causa} Puntuación final de {simulacion.Puntos} puntos.";
        AgregarEvento("Fin de la ronda", resumen.Text, Paleta.Acento);
        ActualizarTiempo();
        ActualizarVista();
        try
        {
            RegistroSesiones.Guardar(simulacion);
            AgregarEvento("Resultado guardado", "Historial en " + RegistroSesiones.Ruta, Paleta.Acento);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            AgregarEvento("Historial sin guardar", "No se pudo escribir en " + RegistroSesiones.Ruta, Paleta.Alerta);
        }
        using var ventana = new ResultadosForm(simulacion, causa);
        if (ventana.ShowDialog(this) == DialogResult.Retry) IniciarRonda();
        return true;
    }

    private Terminal? TerminalElegida => (terminales.SelectedItem as OpcionTerminal)?.Id;

    private void ActualizarVista()
    {
        climaValor.Text = simulacion.Clima.ToString();
        climaValor.ForeColor = simulacion.Clima == CondicionClimatica.Lluvioso ? Paleta.Lavanda : Paleta.Texto;
        climaDetalle.Text = "Visibilidad " + simulacion.Visibilidad.ToLowerInvariant();
        puntosValor.Text = simulacion.Puntos + " pts";
        avanceValor.Text = simulacion.Completados.ToString();
        precisionValor.Text = simulacion.Porcentaje is int porcentaje ? porcentaje + " %" : "—";
        precisionDetalle.Text = simulacion.Porcentaje.HasValue
            ? $"{simulacion.Aciertos} correctas · {simulacion.Errores} erróneas" : "Sin intentos";
        cambiarClima.Enabled = !simulacion.Finalizada;
        for (int i = 0; i < tarjetas.Length; i++)
        {
            var vuelo = simulacion.Vuelos[i];
            string estado = vuelo.Estado switch
            {
                EstadoVuelo.EsperandoDespegue => $"En pista {vuelo.Pista}",
                EstadoVuelo.Aterrizado => $"En terminal {vuelo.Terminal}",
                EstadoVuelo.Despegado => "En vuelo",
                _ => vuelo.EstadoVisible
            };
            tarjetas[i].Text = $"{vuelo.Identificador} · {vuelo.Operacion}{Environment.NewLine}{estado}";
            tarjetas[i].BackColor = vuelo == elegido ? Paleta.Seleccion : Paleta.Control;
            tarjetas[i].FlatAppearance.BorderColor = vuelo == elegido ? Paleta.Acento : Paleta.Borde;
            tarjetas[i].AccessibleDescription = (vuelo == elegido ? "Seleccionado. " : "") + vuelo.EstadoVisible;
            tarjetas[i].Enabled = !resultadoMostrado && !vuelo.Completado;
        }
        for (int i = 0; i < Aeropuerto.Pistas.Count; i++)
        {
            var pista = Aeropuerto.Pistas[i];
            MostrarOcupacion(disponibilidad[i], pista.ToString(), simulacion.Ocupante(pista) is not null);
        }
        for (int i = 0; i < Aeropuerto.Terminales.Count; i++)
        {
            var terminal = Aeropuerto.Terminales[i];
            MostrarOcupacion(disponibilidad[i + Aeropuerto.Pistas.Count], terminal.ToString(),
                simulacion.Ocupante(terminal) is not null);
        }

        ActualizarAutorizacion();
        if (!simulacion.Finalizada) ActualizarCuentaDeReemplazos();
    }

    private void ActualizarAutorizacion()
    {
        var vuelo = elegido;
        bool hayVuelo = vuelo is not null;
        bool llegada = vuelo?.Operacion == TipoOperacion.Llegada;
        bool esperando = !hayVuelo && !simulacion.Finalizada;
        vueloActual.Text = hayVuelo ? "Autorización · " + vuelo!.Identificador
            : esperando ? "Esperando tráfico" : "Resultado de la ronda";
        foreach (var control in controlesDistancia) control.Visible = hayVuelo;
        foreach (var control in controlesLlegada) control.Visible = llegada;
        indicacion.Visible = hayVuelo || esperando;
        if (esperando)
        {
            indicacion.ForeColor = Paleta.Secundario;
            indicacion.Text = "Todos los vuelos visibles están atendidos. La cuenta regresiva continúa.";
        }
        autorizar.Visible = hayVuelo;
        resumen.Visible = simulacion.Finalizada;
        respuesta.Visible = respuesta.Text.Length > 0;

        if (vuelo is not null)
        {
            indicacion.Text = llegada ? "Elige pista y terminal para la llegada."
                : simulacion.Clima == CondicionClimatica.Lluvioso
                    ? "La lluvia suspende los despegues. Puedes esperar a que cambie el clima."
                    : $"El vuelo espera en la pista {vuelo.Pista}.";
            indicacion.ForeColor = !llegada && simulacion.Clima == CondicionClimatica.Lluvioso
                ? Paleta.Alerta : Paleta.Secundario;
            for (int i = 0; i < distancias.Length; i++)
            {
                Pista pista = Aeropuerto.Pistas[i];
                distancias[i].Text = $"{pista} · {vuelo.Distancias[pista]} km";
                bool ocupada = simulacion.Ocupante(pista) is not null;
                botonesPista[i].Text = $"{pista} · {(ocupada ? "Ocupada" : "Libre")}";
                botonesPista[i].AccessibleName = "Pista " + botonesPista[i].Text;
                botonesPista[i].BackColor = pista == pistaElegida ? Paleta.Seleccion : Paleta.Control;
                botonesPista[i].FlatAppearance.BorderColor = pista == pistaElegida ? Paleta.Acento : Paleta.Borde;
                botonesPista[i].ForeColor = ocupada ? Paleta.Alerta : Paleta.Texto;
            }
            terminales.Enabled = pistaElegida.HasValue;
            autorizar.Text = llegada ? "Autorizar aterrizaje" : "Autorizar despegue";
            autorizar.Enabled = !llegada || TerminalElegida.HasValue;
            autorizar.BackColor = autorizar.Enabled ? Paleta.Acento : Paleta.Control;
            autorizar.ForeColor = autorizar.Enabled ? Paleta.Fondo : Paleta.Secundario;
        }
        mapa.Actualizar(simulacion, vuelo, pistaElegida, TerminalElegida);
    }

    private void AgregarEvento(string titulo, string texto, Color color)
    {
        bitacora.Select(0, 0);
        bitacora.SelectionColor = color;
        bitacora.SelectedText = titulo + Environment.NewLine;
        bitacora.SelectionColor = Paleta.Secundario;
        bitacora.SelectedText = texto + Environment.NewLine + Environment.NewLine;
        bitacora.Select(0, 0);
        bitacora.ScrollToCaret();
    }

    private static void MostrarOcupacion(Label etiqueta, string nombre, bool ocupada)
    {
        etiqueta.Text = nombre + (ocupada ? " · Ocupada" : " · Libre");
        etiqueta.ForeColor = ocupada ? Paleta.Alerta : Paleta.Secundario;
    }

    private static TableLayoutPanel Tabla(int columnas, int filas) => new()
    {
        Dock = DockStyle.Fill, ColumnCount = columnas, RowCount = filas, Margin = Padding.Empty,
        GrowStyle = TableLayoutPanelGrowStyle.FixedSize
    };

    private static Label Texto(string texto, Font fuente, Color color,
        ContentAlignment alineacion = ContentAlignment.MiddleLeft)
    {
        var etiqueta = new Label { Text = texto };
        EstiloTexto(etiqueta, fuente, color);
        etiqueta.TextAlign = alineacion;
        return etiqueta;
    }

    private static void EstiloTexto(Label etiqueta, Font fuente, Color color)
    {
        etiqueta.Dock = DockStyle.Fill;
        etiqueta.Font = fuente;
        etiqueta.ForeColor = color;
        etiqueta.Margin = Padding.Empty;
        etiqueta.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static Button Boton(string texto)
    {
        var boton = new Button();
        EstiloBoton(boton, texto, Paleta.Control, Paleta.Texto);
        return boton;
    }

    private static void EstiloBoton(Button boton, string texto, Color fondo, Color frente)
    {
        boton.Text = texto;
        boton.Dock = DockStyle.Fill;
        boton.FlatStyle = FlatStyle.Flat;
        boton.FlatAppearance.BorderColor = Paleta.Borde;
        boton.UseVisualStyleBackColor = false;
        boton.BackColor = fondo;
        boton.ForeColor = frente;
        boton.Cursor = Cursors.Hand;
    }

    private sealed record OpcionTerminal(Terminal Id, bool Ocupada)
    {
        public override string ToString() => $"Terminal {Id} · {(Ocupada ? "Ocupada" : "Libre")}";
    }
}
