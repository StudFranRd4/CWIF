using Timer = System.Windows.Forms.Timer;

public class BarChartControl : UserControl
{
    public int[] Values = new int[0];
    public string[] Labels = new string[0];
    public Color BarColor = Color.CornflowerBlue;
    public Color BorderColor = Color.Black;

    private float[] currentValues;
    private Timer animationTimer;
    private int padding = 10;

    public void Animate()
    {
        if (Values.Length == 0) return;
        currentValues = new float[Values.Length];
        animationTimer = new Timer { Interval = 20 };
        animationTimer.Tick += AnimateStep;
        animationTimer.Start();
    }

    private void AnimateStep(object sender, EventArgs e)
    {
        bool finished = true;

        for (int i = 0; i < Values.Length; i++)
        {
            currentValues[i] += Math.Min(Values[i] * 0.05f, Values[i] - currentValues[i]);
            if (currentValues[i] < Values[i]) finished = false;
        }

        Invalidate();

        if (finished)
        {
            animationTimer.Stop();
            animationTimer.Dispose();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Values.Length == 0 || currentValues == null) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Calcular dimensiones dinámicas
        int labelHeight = 20;
        int topMargin = 30; // Espacio para título si lo hubiera
        int bottomMargin = labelHeight + padding;
        int leftMargin = padding;
        int rightMargin = padding;

        int drawWidth = Width - leftMargin - rightMargin;
        int drawHeight = Height - topMargin - bottomMargin;

        if (drawWidth <= 0 || drawHeight <= 0) return;

        // Calcular ancho de barras dinámicamente
        int barSpacing = Math.Max(5, drawWidth / (Values.Length * 10)); // Espaciado dinámico
        int barWidth = Math.Max(15, (drawWidth - (Values.Length - 1) * barSpacing) / Values.Length);

        // Asegurar que no se salga del área
        barWidth = Math.Min(barWidth, 50); // Ancho máximo

        int maxVal = Values.Length > 0 ? Values.Max() : 1;

        using (var barBrush = new SolidBrush(BarColor))
        using (var borderPen = new Pen(BorderColor, 1))
        using (var labelFont = new Font(Font.FontFamily, 8))
        {
            for (int i = 0; i < Values.Length; i++)
            {
                // Calcular posición y altura de la barra
                int barHeight = maxVal > 0 ? (int)(currentValues[i] / maxVal * drawHeight) : 0;
                int x = leftMargin + i * (barWidth + barSpacing);
                int y = topMargin + drawHeight - barHeight;

                // Dibujar barra
                g.FillRectangle(barBrush, x, y, barWidth, barHeight);
                g.DrawRectangle(borderPen, x, y, barWidth, barHeight);

                // Dibujar etiqueta
                if (Labels.Length > i && !string.IsNullOrEmpty(Labels[i]))
                {
                    // Rotar texto si hay muchas barras
                    string label = Labels[i];
                    var labelSize = g.MeasureString(label, labelFont);

                    if (barWidth < labelSize.Width)
                    {
                        // Rotar 45 grados si el espacio es limitado
                        g.TranslateTransform(x + barWidth / 2, Height - bottomMargin + 10);
                        g.RotateTransform(-45);
                        g.DrawString(label, labelFont, Brushes.Black, -labelSize.Width / 2, 0);
                        g.ResetTransform();
                    }
                    else
                    {
                        // Centrar etiqueta normalmente
                        float labelX = x + (barWidth - labelSize.Width) / 2;
                        float labelY = Height - bottomMargin + 5;
                        g.DrawString(label, labelFont, Brushes.Black, labelX, labelY);
                    }
                }

                // Mostrar valor encima de la barra si hay espacio
                if (barHeight > 20)
                {
                    string valueText = currentValues[i].ToString("0");
                    var valueSize = g.MeasureString(valueText, labelFont);
                    float valueX = x + (barWidth - valueSize.Width) / 2;
                    float valueY = y - valueSize.Height - 2;
                    g.DrawString(valueText, labelFont, Brushes.DarkBlue, valueX, valueY);
                }
            }
        }

        // Dibujar eje Y (opcional)
        using (var axisPen = new Pen(Color.Gray, 1))
        {
            g.DrawLine(axisPen, leftMargin - 5, topMargin, leftMargin - 5, topMargin + drawHeight);
            g.DrawLine(axisPen, leftMargin, topMargin + drawHeight, leftMargin + drawWidth, topMargin + drawHeight);
        }
    }
}