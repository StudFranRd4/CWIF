using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using FacturaApp.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace FacturaApp.WinForms.Forms
{
    public partial class FormAsignarUsuarios : Form
    {
        private readonly IServiceProvider _provider;
        private readonly BillingDbContext _db;
        private Guid _proyectoId;

        private ListBox lbDisponibles;
        private ListBox lbAsignados;
        private Button btnAgregar;
        private Button btnQuitar;
        private Button btnGuardar;
        private Label lblProyecto;

        public FormAsignarUsuarios(IServiceProvider provider, Guid proyectoId)
        {
            _provider = provider;
            _db = provider.GetService<BillingDbContext>() ?? throw new InvalidOperationException("BillingDbContext no registrado");
            _proyectoId = proyectoId;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "Asignar usuarios al proyecto";
            Width = 700;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            lblProyecto = new Label { Left = 12, Top = 12, Width = 600 };

            lbDisponibles = new ListBox { Left = 12, Top = 40, Width = 280, Height = 260 };
            lbAsignados = new ListBox { Left = 390, Top = 40, Width = 280, Height = 260 };

            btnAgregar = new Button { Text = ">>", Left = 310, Top = 120, Width = 60, Height = 30 };
            btnQuitar = new Button { Text = "<<", Left = 310, Top = 160, Width = 60, Height = 30 };

            btnGuardar = new Button { Text = "Guardar", Left = 560, Top = 320, Width = 110 };

            btnAgregar.Click += BtnAgregar_Click;
            btnQuitar.Click += BtnQuitar_Click;
            btnGuardar.Click += BtnGuardar_Click;

            Controls.Add(lblProyecto);
            Controls.Add(lbDisponibles);
            Controls.Add(lbAsignados);
            Controls.Add(btnAgregar);
            Controls.Add(btnQuitar);
            Controls.Add(btnGuardar);
        }

        private void LoadData()
        {
            var proyecto = _db.Proyectos.Find(_proyectoId);
            if (proyecto == null)
            {
                MessageBox.Show("Proyecto no encontrado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            lblProyecto.Text = $"Proyecto: {proyecto.Nombre}";

            // Usuarios asignados actualmente
            var asignados = _db.ProyectoUsuarios
                .Where(pu => pu.ProyectoId == _proyectoId)
                .Select(pu => pu.Usuario)
                .Where(u => u != null)
                .ToList()!;

            // Usuarios disponibles = todos menos los asignados
            var asignadosIds = new HashSet<Guid>(asignados.Select(u => u!.Id));
            var disponibles = _db.Usuarios
                .Where(u => !asignadosIds.Contains(u.Id))
                .ToList();

            lbDisponibles.DisplayMember = "Nombre";
            lbDisponibles.ValueMember = "Id";
            lbDisponibles.DataSource = disponibles;

            lbAsignados.DisplayMember = "Nombre";
            lbAsignados.ValueMember = "Id";
            lbAsignados.DataSource = asignados;
        }

        private void BtnAgregar_Click(object? sender, EventArgs e)
        {
            if (lbDisponibles.SelectedItem is Usuario u)
            {
                var listAsignados = lbAsignados.DataSource as List<Usuario> ?? new List<Usuario>();
                listAsignados.Add(u);

                var listDisponibles = lbDisponibles.DataSource as List<Usuario>;
                listDisponibles?.Remove(u);

                RefreshLists(listDisponibles, listAsignados);
            }
        }

        private void BtnQuitar_Click(object? sender, EventArgs e)
        {
            if (lbAsignados.SelectedItem is Usuario u)
            {
                var listDisponibles = lbDisponibles.DataSource as List<Usuario> ?? new List<Usuario>();
                listDisponibles.Add(u);

                var listAsignados = lbAsignados.DataSource as List<Usuario>;
                listAsignados?.Remove(u);

                RefreshLists(listDisponibles, listAsignados);
            }
        }

        private void RefreshLists(List<Usuario>? disponibles, List<Usuario>? asignados)
        {
            lbDisponibles.DataSource = null;
            lbAsignados.DataSource = null;

            if (disponibles == null) disponibles = _db.Usuarios.ToList();
            if (asignados == null) asignados = new List<Usuario>();

            lbDisponibles.DataSource = disponibles;
            lbDisponibles.DisplayMember = "Nombre";
            lbDisponibles.ValueMember = "Id";

            lbAsignados.DataSource = asignados;
            lbAsignados.DisplayMember = "Nombre";
            lbAsignados.ValueMember = "Id";
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                // eliminar asignaciones anteriores
                var actuales = _db.ProyectoUsuarios.Where(pu => pu.ProyectoId == _proyectoId).ToList();
                _db.ProyectoUsuarios.RemoveRange(actuales);

                // agregar las nuevas (desde lbAsignados)
                var asignados = lbAsignados.DataSource as List<Usuario> ?? new List<Usuario>();
                foreach (var u in asignados)
                {
                    var pu = new ProyectoUsuario
                    {
                        Id = Guid.NewGuid(),
                        ProyectoId = _proyectoId,
                        UsuarioId = u.Id,
                        FechaAsignacion = DateTime.Now
                    };
                    _db.ProyectoUsuarios.Add(pu);
                }

                _db.SaveChanges();
                MessageBox.Show("Asignaciones guardadas", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando asignaciones: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}