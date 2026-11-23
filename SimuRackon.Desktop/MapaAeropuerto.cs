using System.Drawing.Drawing2D;
using SimuRackon.Core;

namespace SimuRackon.Desktop;

internal sealed class MapaAeropuerto : Control
{
    private const float Ancho = 840;
    private const float Alto = 465;
    private const float Angulo = -14;
    private const float Radianes = Angulo * MathF.PI / 180;

    private static readonly (Pista Pista, float X, float Y, float Largo)[] TrazosPistas =
    [
        (Pista.A, 420, 111, 506), (Pista.B, 421, 239, 559), (Pista.C, 416, 361, 505)
    ];
    private static readonly (Terminal Terminal, PointF Centro)[] Edificios =
    [
        (Terminal.T1, new PointF(374, 181)), (Terminal.T2, new PointF(502, 289)),
        (Terminal.T3, new PointF(585, 348))
    ];
    private static readonly (PointF Entrada, PointF Centro, PointF Destino)[] Aproximaciones =
    [
        (new(0, 42), new(139, 91), new(190, 126)),
        (new(840, 410), new(717, 363), new(678, 327)),
        (new(0, 333), new(77, 287), new(145, 246))
    ];

    private Simulacion? simulacion;
    private Vuelo? vueloElegido;
    private Pista? pistaElegida;
    private Terminal? terminalElegida;
    public event Action<Vuelo>? VueloSeleccionado;

    public MapaAeropuerto()
    {
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Paleta.Fondo;
        AccessibleName = "Mapa esquemático del aeropuerto";
        AccessibleDescription = "Los vuelos y el estado de pistas y terminales también están disponibles como texto.";
        TabStop = false;
    }

    public void Actualizar(Simulacion escenario, Vuelo? vuelo, Pista? pista, Terminal? terminal)
    {
        simulacion = escenario;
        vueloElegido = vuelo;
        pistaElegida = pista;
        terminalElegida = terminal;
        Invalidate();
    }

    private float Escala => Math.Min(ClientSize.Width / Ancho, ClientSize.Height / Alto);
    private PointF Origen(float escala) =>
        new((ClientSize.Width - escala * Ancho) / 2, (ClientSize.Height - escala * Alto) / 2);

    private Vuelo? EncontrarVuelo(Point punto)
    {
        float escala = Escala;
        if (simulacion is null || simulacion.Finalizada || escala <= 0) return null;
        var origen = Origen(escala);
        var posicion = new PointF((punto.X - origen.X) / escala, (punto.Y - origen.Y) / escala);
        for (int i = 0; i < simulacion.Vuelos.Count; i++)
        {
            var vuelo = simulacion.Vuelos[i];
            if (!vuelo.Completado && EtiquetaVuelo(vuelo, i).Contains(posicion)) return vuelo;
        }
        return null;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = EncontrarVuelo(e.Location) is null ? Cursors.Default : Cursors.Hand;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && EncontrarVuelo(e.Location) is Vuelo vuelo)
            VueloSeleccionado?.Invoke(vuelo);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var escenario = simulacion;
        float escala = Escala;
        if (escenario is null || escala <= 0) return;
        Graphics g = e.Graphics;
        var origen = Origen(escala);
        var estado = g.Save();
        g.TranslateTransform(origen.X, origen.Y);
        g.ScaleTransform(escala, escala);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DibujarTerreno(g, escenario.Clima);
        foreach (var trazo in TrazosPistas) DibujarPista(g, escenario, trazo);
        foreach (var edificio in Edificios) DibujarTerminal(g, escenario, edificio.Terminal, edificio.Centro);
        for (int i = 0; i < escenario.Vuelos.Count; i++)
        {
            if (!escenario.Vuelos[i].Completado) DibujarVuelo(g, escenario.Vuelos[i], i);
        }

        using var fuente = new Font("Consolas", 14, FontStyle.Regular, GraphicsUnit.Pixel);
        using var tinta = new SolidBrush(Paleta.Secundario);
        g.DrawString("Esquema · sin escala", fuente, tinta, 13, 439);
        g.DrawString("N", fuente, tinta, 791, 21);
        using var brujula = new Pen(Paleta.Secundario, 1.5f);
        Point[] aguja = [new(797, 43), new(791, 61), new(797, 57), new(803, 61)];
        g.DrawPolygon(brujula, aguja);
        g.Restore(estado);
    }

    private static void DibujarTerreno(Graphics g, CondicionClimatica clima)
    {
        Color centro = clima switch
        {
            CondicionClimatica.Lluvioso => Color.FromArgb(42, 56, 77),
            CondicionClimatica.Nublado => Color.FromArgb(48, 48, 67),
            _ => Color.FromArgb(44, 43, 68)
        };
        var limites = new RectangleF(0, 0, Ancho, Alto);
        using var gradiente = new LinearGradientBrush(limites, Paleta.Fondo, centro, 30);
        g.FillRectangle(gradiente, limites);
        using var cuadricula = new Pen(Color.FromArgb(28, Paleta.Lavanda));
        for (int x = 4; x < Ancho; x += 32) g.DrawLine(cuadricula, x, 0, x, Alto);
        for (int y = 13; y < Alto; y += 32) g.DrawLine(cuadricula, 0, y, Ancho, y);
        for (int radio = 72; radio <= 360; radio += 72)
            g.DrawEllipse(cuadricula, 419 - radio, 234 - radio, 2 * radio, 2 * radio);

        PointF[] perimetro =
        [
            new(124, 149), new(648, 20), new(718, 43), new(745, 119), new(698, 155),
            new(727, 197), new(682, 244), new(699, 303), new(667, 343), new(190, 456),
            new(153, 433), new(132, 347), new(119, 307), new(169, 280), new(194, 207), new(148, 203)
        ];
        using var relleno = new SolidBrush(Color.FromArgb(43, 40, 63));
        using var borde = new Pen(Color.FromArgb(87, 77, 116), 1.5f) { DashStyle = DashStyle.Dash };
        g.FillPolygon(relleno, perimetro);
        g.DrawPolygon(borde, perimetro);
        PointF[][] rodajes =
        [
            [new(208, 164), new(230, 214), new(265, 229), new(608, 144), new(632, 120), new(623, 64)],
            [new(256, 279), new(249, 243), new(289, 199), new(563, 132), new(607, 155), new(618, 190)],
            [new(209, 405), new(202, 359), new(226, 334), new(593, 244), new(625, 260), new(635, 315)],
            [new(290, 269), new(305, 304), new(337, 321), new(599, 256), new(625, 229), new(616, 195)],
            [new(367, 181), new(343, 151), new(331, 133)], [new(502, 287), new(515, 326), new(508, 344)],
            [new(585, 348), new(562, 332), new(555, 327)]
        ];
        using var rodaje = new Pen(Color.FromArgb(81, 72, 111), 10) { LineJoin = LineJoin.Round };
        using var eje = new Pen(Color.FromArgb(105, 96, 130)) { DashStyle = DashStyle.Dash };
        foreach (var puntos in rodajes)
        {
            g.DrawLines(rodaje, puntos);
            g.DrawLines(eje, puntos);
        }
    }

    private void DibujarPista(Graphics g, Simulacion escenario, (Pista Pista, float X, float Y, float Largo) trazo)
    {
        var (pista, x, y, largo) = trazo;
        bool elegida = pistaElegida == pista;
        var estado = g.Save();
        g.TranslateTransform(x, y);
        g.RotateTransform(Angulo);
        using var relleno = new SolidBrush(elegida ? Color.FromArgb(74, 82, 66) : Color.FromArgb(61, 58, 84));
        Color marca = elegida ? Paleta.Acento : Paleta.Lavanda;
        using var borde = new Pen(marca, 1.3f);
        using var centro = new Pen(marca, 1.3f) { DashPattern = [10, 8] };
        using var tinta = new SolidBrush(marca);
        g.FillRectangle(relleno, -largo / 2, -15, largo, 30);
        g.DrawRectangle(borde, -largo / 2, -15, largo, 30);
        g.DrawLine(centro, -largo / 2 + 24, 0, largo / 2 - 24, 0);
        for (int i = 0; i < 6; i++)
        {
            g.FillRectangle(tinta, -largo / 2 + 7, -10 + i * 3.6f, 11, 1.5f);
            g.FillRectangle(tinta, largo / 2 - 18, -10 + i * 3.6f, 11, 1.5f);
        }
        g.Restore(estado);
        DibujarEtiqueta(g, new RectangleF(x + 104, y - 45, 95, 24), "PISTA " + pista,
            Paleta.Fondo, escenario.Ocupante(pista) is null ? marca : Paleta.Alerta, 15);
    }

    private void DibujarTerminal(Graphics g, Simulacion escenario, Terminal terminal, PointF centro)
    {
        bool ocupada = escenario.Ocupante(terminal) is not null;
        Color bordeColor = terminalElegida == terminal ? Paleta.Acento : ocupada ? Paleta.Alerta : Paleta.Lavanda;
        var estado = g.Save();
        g.TranslateTransform(centro.X, centro.Y);
        g.RotateTransform(Angulo);
        using var relleno = new SolidBrush(Color.FromArgb(89, 77, 122));
        using var borde = new Pen(bordeColor);
        g.FillRectangle(relleno, -32, -14, 64, 28);
        g.DrawRectangle(borde, -32, -14, 64, 28);
        for (int x = -22; x <= 22; x += 22) g.DrawLine(borde, x, 14, x, 25);
        g.Restore(estado);
        DibujarEtiqueta(g, new RectangleF(centro.X - 20, centro.Y - 12, 40, 24),
            terminal.ToString(), Color.FromArgb(76, 67, 103), bordeColor, 16);
    }

    private void DibujarVuelo(Graphics g, Vuelo vuelo, int indice)
    {
        var rectangulo = EtiquetaVuelo(vuelo, indice);
        if (vuelo.Operacion == TipoOperacion.Llegada)
        {
            using var aproximacion = new Pen(Color.FromArgb(145, Paleta.Lavanda), 1.6f) { DashStyle = DashStyle.Dash };
            var ruta = Aproximaciones[indice / 2];
            g.DrawLines(aproximacion, [ruta.Entrada, ruta.Centro, ruta.Destino]);
        }
        bool elegido = vuelo == vueloElegido;
        DibujarEtiqueta(g, rectangulo, vuelo.Identificador, elegido ? Paleta.Acento : Paleta.Control,
            elegido ? Paleta.Fondo : Paleta.Lavanda, 17);
    }

    private static RectangleF EtiquetaVuelo(Vuelo vuelo, int indice)
    {
        PointF centro;
        if (vuelo.Pista is Pista pista)
        {
            var trazo = TrazosPistas[(int)pista];
            centro = new PointF(trazo.X - MathF.Cos(Radianes) * 145, trazo.Y - MathF.Sin(Radianes) * 145);
        }
        else centro = Aproximaciones[indice / 2].Centro;
        return new RectangleF(centro.X - 43, centro.Y - 14, 86, 28);
    }

    private static void DibujarEtiqueta(Graphics g, RectangleF rectangulo, string texto, Color fondo,
        Color frente, float tamano)
    {
        using var relleno = new SolidBrush(fondo);
        using var tinta = new SolidBrush(frente);
        using var borde = new Pen(frente, 1.2f);
        using var fuente = new Font("Consolas", tamano, FontStyle.Bold, GraphicsUnit.Pixel);
        using var alineacion = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.FillRectangle(relleno, rectangulo);
        g.DrawRectangle(borde, rectangulo.X, rectangulo.Y, rectangulo.Width, rectangulo.Height);
        g.DrawString(texto, fuente, tinta, rectangulo, alineacion);
    }
}
