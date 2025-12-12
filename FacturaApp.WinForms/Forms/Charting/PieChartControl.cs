using Timer = System.Windows.Forms.Timer;

public class PieChartControl : UserControl
{
    public int[] Values = new int[0];
    public string[] Labels = new string[0];
    public Color[] Colors = new Color[0];
    public bool ShowLabels = true;
    public bool ShowPercentages = true;

    private float animationProgress = 0f;
    private Timer animationTimer;

    public void Animate()
    {
        animationProgress = 0f;
        animationTimer = new Timer { Interval = 20 };
        animationTimer.Tick += (s, e) =>
        {
            animationProgress = Math.Min(animationProgress + 0.03f, 1f);
            Invalidate();
            if (animationProgress >= 1f)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
        };
        animationTimer.Start();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Values.Length == 0 || Values.Sum() == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Calcular área del gráfico (cuadrado centrado)
        int margin = 20;
        int legendWidth = ShowLabels ? 120 : 0;
        int availableWidth = Width - margin * 2 - legendWidth;
        int availableHeight = Height - margin * 2;

        // Tamaño del gráfico (cuadrado)
        int chartSize = Math.Min(availableWidth, availableHeight);
        if (chartSize <= 0) return;

        // Posición centrada
        int chartX = margin + (availableWidth - chartSize) / 2;
        int chartY = margin + (availableHeight - chartSize) / 2;

        float total = Values.Sum();
        float startAngle = 0f;
        float totalSweep = 0f;

        using (var labelFont = new Font(Font.FontFamily, 8))
        {
            // Dibujar sectores
            for (int i = 0; i < Values.Length; i++)
            {
                float sweepAngle = Values[i] / total * 360f;
                float animatedSweep = sweepAngle * animationProgress;

                var brush = Colors.Length > i ? new SolidBrush(Colors[i]) :
                    new SolidBrush(GetDefaultColor(i));

                g.FillPie(brush, chartX, chartY, chartSize, chartSize,
                         startAngle + totalSweep, animatedSweep);

                startAngle += sweepAngle;
            }

            // Dibujar etiquetas y leyenda
            if (ShowLabels && animationProgress > 0.5f)
            {
                startAngle = 0f;
                float legendY = chartY;

                for (int i = 0; i < Values.Length; i++)
                {
                    float percentage = Values[i] / total * 100f;
                    float sweepAngle = Values[i] / total * 360f;

                    // Leyenda a la derecha
                    if (legendWidth > 0)
                    {
                        int legendX = chartX + chartSize + 10;
                        var color = Colors.Length > i ? Colors[i] : GetDefaultColor(i);

                        // Color sample
                        g.FillRectangle(new SolidBrush(color), legendX, legendY, 15, 15);
                        g.DrawRectangle(Pens.Black, legendX, legendY, 15, 15);

                        // Texto
                        string labelText = Labels.Length > i ? Labels[i] : $"Item {i + 1}";
                        if (ShowPercentages)
                            labelText += $" ({percentage:0.0}%)";

                        g.DrawString(labelText, labelFont, Brushes.Black,
                                    legendX + 20, legendY);

                        legendY += 20;
                    }

                    // Etiquetas en el gráfico (solo si el sector es suficientemente grande)
                    if (percentage > 5 && chartSize > 100)
                    {
                        float midAngle = startAngle + sweepAngle / 2;
                        float rad = midAngle * (float)Math.PI / 180f;

                        // Posición dentro del gráfico
                        float labelRadius = chartSize * 0.35f;
                        float labelX = chartX + chartSize / 2 + labelRadius * (float)Math.Cos(rad);
                        float labelY = chartY + chartSize / 2 + labelRadius * (float)Math.Sin(rad);

                        string percentText = $"{percentage:0}%";
                        var textSize = g.MeasureString(percentText, labelFont);

                        // Centrar texto
                        g.DrawString(percentText, labelFont, Brushes.White,
                                    labelX - textSize.Width / 2, labelY - textSize.Height / 2);
                    }

                    startAngle += sweepAngle;
                }
            }
        }
    }

    private Color GetDefaultColor(int index)
    {
        Color[] defaultColors = {
            Color.FromArgb(65, 105, 225), // CornflowerBlue
            Color.FromArgb(220, 20, 60),  // Crimson
            Color.FromArgb(34, 139, 34),  // ForestGreen
            Color.FromArgb(255, 140, 0),  // DarkOrange
            Color.FromArgb(138, 43, 226), // BlueViolet
            Color.FromArgb(255, 215, 0),  // Gold
            Color.FromArgb(30, 144, 255), // DodgerBlue
            Color.FromArgb(50, 205, 50)   // LimeGreen
        };
        return defaultColors[index % defaultColors.Length];
    }
}