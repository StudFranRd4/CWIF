
using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using Microsoft.Extensions.DependencyInjection;

public class FormSeleccionProyecto : Form
{
    private readonly BillingDbContext _db;
    private ListBox lb;
    private Button btnOk;
    public Guid SelectedProyectoId { get; private set; }

    public FormSeleccionProyecto(IServiceProvider provider)
    {
        _db = provider.GetService<BillingDbContext>() ?? throw new InvalidOperationException("BillingDbContext no registrado");
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text = "Seleccionar proyecto";
        Width = 400;
        Height = 350;
        StartPosition = FormStartPosition.CenterParent;

        lb = new ListBox { Left = 12, Top = 12, Width = 360, Height = 250 };
        btnOk = new Button { Text = "Seleccionar", Left = 260, Top = 270, Width = 110 };
        btnOk.Click += BtnOk_Click;

        Controls.Add(lb);
        Controls.Add(btnOk);
    }

    private void LoadData()
    {
        var list = _db.Proyectos.OrderBy(p => p.Nombre).ToList();
        lb.DataSource = list;
        lb.DisplayMember = "Nombre";
        lb.ValueMember = "Id";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (lb.SelectedItem is FacturaApp.Core.Models.Proyecto p)
        {
            SelectedProyectoId = p.Id;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Seleccione un proyecto", "Información", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}