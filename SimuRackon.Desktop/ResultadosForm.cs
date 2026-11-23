using SimuRackon.Core;

namespace SimuRackon.Desktop;

internal sealed class ResultadosForm : Form
{
    private readonly Font fuenteNormal = new("Segoe UI", 10);
    private readonly Font fuentePuntos = new("Segoe UI", 30, FontStyle.Bold);
    private readonly Font fuenteMotivo = new("Segoe UI", 13, FontStyle.Bold);

    public ResultadosForm(Simulacion ronda, string causa)
    {
        Text = "Resultado de la simulación";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = fuenteNormal;
        ClientSize = new Size(460, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = MinimizeBox = false;
        BackColor = Paleta.Panel;
        ForeColor = Paleta.Texto;

        var contenido = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(23, 16, 23, 16)
        };
        foreach (int alto in new[] { 34, 25, 60, 28 })
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, alto));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        contenido.Controls.Add(Etiqueta(causa, fuenteMotivo, Paleta.Texto), 0, 0);
        contenido.Controls.Add(Etiqueta("Puntuación final", fuenteNormal, Paleta.Secundario), 0, 1);
        contenido.Controls.Add(Etiqueta($"{ronda.Puntos} pts", fuentePuntos, Paleta.Acento), 0, 2);
        contenido.Controls.Add(Etiqueta($"Vuelos completados  {ronda.Completados} de {ronda.TotalGenerados} generados",
            fuenteNormal, Paleta.Texto), 0, 3);
        string balance = ronda.Porcentaje is int porcentaje
            ? $"{ronda.Aciertos} aciertos · {ronda.Errores} errores · {porcentaje} % de aciertos"
            : "Sin decisiones registradas";
        contenido.Controls.Add(Etiqueta(balance, fuenteNormal, Paleta.Secundario), 0, 4);

        var acciones = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 2, RowCount = 1 };
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        acciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61));
        var cerrar = Boton("Ver bitácora", DialogResult.Cancel, Paleta.Control, Paleta.Texto);
        var reiniciar = Boton("Iniciar otra simulación", DialogResult.Retry, Paleta.Acento, Paleta.Fondo);
        cerrar.Margin = new Padding(0, 0, 8, 0);
        acciones.Controls.Add(cerrar, 0, 0);
        acciones.Controls.Add(reiniciar, 1, 0);
        contenido.Controls.Add(acciones, 0, 5);
        Controls.Add(contenido);
        CancelButton = cerrar;
        AcceptButton = reiniciar;
        ActiveControl = reiniciar;
    }

    private static Label Etiqueta(string texto, Font fuente, Color color) => new()
    {
        Text = texto, Font = fuente, ForeColor = color, Dock = DockStyle.Fill,
        Margin = Padding.Empty, TextAlign = ContentAlignment.MiddleLeft
    };

    private static Button Boton(string texto, DialogResult respuesta, Color fondo, Color frente) => new()
    {
        Text = texto, DialogResult = respuesta, Dock = DockStyle.Fill, Margin = Padding.Empty,
        FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderColor = fondo },
        BackColor = fondo, ForeColor = frente, Cursor = Cursors.Hand, UseVisualStyleBackColor = false
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        fuenteNormal.Dispose();
        fuentePuntos.Dispose();
        fuenteMotivo.Dispose();
    }
}
