using FacturaApp.Data;
using Microsoft.Extensions.DependencyInjection;

public class DashboardTareasControl : UserControl
{
    private readonly BillingDbContext _db;
    private PieChartControl pieChartTareas;
    private BarChartControl barChartProyectos;

    private TableLayoutPanel container;
    private Panel statsPanel;
    private Panel chartsPanel;

    // Tamaño por defecto razonable para que el wrapper pueda dimensionarlo
    private readonly Size DefaultSize = new Size(900, 520);

    public DashboardTareasControl(IServiceProvider provider)
    {
        _db = provider.GetService<BillingDbContext>()
            ?? throw new InvalidOperationException("BillingDbContext no registrado");

        // NO hacemos Dock.Fill aquí. El MainForm controlará el Dock en el wrapper.
        Anchor = AnchorStyles.None;

        InitializeUI();
        // asignar tamaño por defecto para que el wrapper no reciba 0,0
        this.Size = DefaultSize;

        LoadCharts();
    }

    private void InitializeUI()
    {
        BackColor = Color.White;

        // CONTENEDOR PRINCIPAL: ocupará TODO el espacio del control (Fill)
        container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, // importante: que llene el control
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(10),
            AutoSize = false
        };

        // PANEL DE STATS
        statsPanel = CreateStatsPanel();
        statsPanel.Dock = DockStyle.Top;
        statsPanel.Margin = new Padding(0);

        // PANEL DE GRÁFICOS
        chartsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 12, 0, 0)
        };

        // SPLIT CON GRÁFICOS
        var split = new SplitContainer
        {
            Orientation = Orientation.Horizontal,
            IsSplitterFixed = false,
            SplitterDistance = 240,
            Dock = DockStyle.Fill
        };

        pieChartTareas = new PieChartControl
        {
            Dock = DockStyle.Fill,
            ShowLabels = true,
            ShowPercentages = true
        };

        barChartProyectos = new BarChartControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };

        split.Panel1.Controls.Add(pieChartTareas);
        split.Panel2.Controls.Add(barChartProyectos);

        chartsPanel.Controls.Add(split);

        // ENSAMBLAR TODO
        container.Controls.Add(statsPanel);
        container.Controls.Add(chartsPanel);

        // TableLayout: filas automáticas - ajustamos RowStyles
        container.RowStyles.Clear();
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, statsPanel.Height + 20)); // espacio para stats
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // charts ocupan resto

        Controls.Add(container);
    }

    private Panel CreateStatsPanel()
    {
        var panel = new Panel
        {
            Height = 80,
            BackColor = Color.FromArgb(248, 249, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Top
        };

        var tareas = _db.Tareas.ToList();
        int total = tareas.Count;
        int pendientes = tareas.Count(t => t.Estado == "Pendiente");
        int progreso = tareas.Count(t => t.Estado == "EnProgreso");
        int completadas = tareas.Count(t => t.Estado == "Completada");

        var stats = new[]
        {
            new { Text = "Tareas", Value = total, Color = Color.SteelBlue },
            new { Text = "Pendientes", Value = pendientes, Color = Color.Orange },
            new { Text = "En Progreso", Value = progreso, Color = Color.DodgerBlue },
            new { Text = "Completadas", Value = completadas, Color = Color.ForestGreen }
        };

        int spacing = 20;
        int cardWidth = 180;

        for (int i = 0; i < stats.Length; i++)
        {
            var card = CreateStatCard(stats[i].Text, stats[i].Value, stats[i].Color);
            card.Width = cardWidth;
            card.Location = new Point(spacing + i * (cardWidth + spacing), 10);
            panel.Controls.Add(card);
        }

        return panel;
    }

    private Panel CreateStatCard(string title, int value, Color color)
    {
        var card = new Panel
        {
            Height = 60,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            Location = new Point(10, 5),
            AutoSize = true
        };

        var lblValue = new Label
        {
            Text = value.ToString(),
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = color,
            Location = new Point(10, 25),
            AutoSize = true
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        return card;
    }

    private void LoadCharts()
    {
        var tareas = _db.Tareas.ToList();

        int pendientes = tareas.Count(t => t.Estado == "Pendiente");
        int progreso = tareas.Count(t => t.Estado == "EnProgreso");
        int completadas = tareas.Count(t => t.Estado == "Completada");

        // Evitar excepción si pieChartTareas es null (por si llaman antes de InitializeUI)
        if (pieChartTareas != null)
        {
            pieChartTareas.Values = new[] { pendientes, progreso, completadas };
            pieChartTareas.Labels = new[] { "Pendientes", "En progreso", "Completadas" };
            pieChartTareas.Colors = new[] { Color.Orange, Color.DodgerBlue, Color.ForestGreen };
            pieChartTareas.Animate();
        }

        var tareasPorProyecto = _db.Tareas
            .Join(_db.Proyectos,
                  t => t.ProyectoId,
                  p => p.Id,
                  (t, p) => new { p.Nombre })
            .GroupBy(x => x.Nombre)
            .Select(g => new { Proyecto = g.Key, Total = g.Count() })
            .ToList();

        if (barChartProyectos != null)
        {
            if (tareasPorProyecto.Any())
            {
                barChartProyectos.Values = tareasPorProyecto.Select(x => x.Total).ToArray();
                barChartProyectos.Labels = tareasPorProyecto.Select(x => x.Proyecto).ToArray();
            }
            else
            {
                barChartProyectos.Values = new[] { 0 };
                barChartProyectos.Labels = new[] { "Sin datos" };
            }

            barChartProyectos.BarColor = Color.FromArgb(70, 130, 180);
            barChartProyectos.Animate();
        }
    }
}