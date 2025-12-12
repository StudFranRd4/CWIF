using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using FacturaApp.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace FacturaApp.WinForms.Forms
{
    public class FormListaTareas : Form
    {
        private readonly BillingDbContext _db;
        private DataGridView _grid;
        private ComboBox cbProyecto, cbEstado, cbUsuario;
        private Button btnNuevo, btnEditar, btnDetalle, btnRefrescar;

        public FormListaTareas(IServiceProvider provider)
        {
            _db = provider.GetService<BillingDbContext>() ?? throw new InvalidOperationException("BillingDbContext no registrado");
            InitializeComponent();
            LoadFilters();
            LoadTareas();
        }

        private void InitializeComponent()
        {
            Text = "Tareas";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8) };
            cbProyecto = new ComboBox { Left = 8, Top = 10, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cbUsuario = new ComboBox { Left = 280, Top = 10, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cbEstado = new ComboBox { Left = 510, Top = 10, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

            btnNuevo = new Button { Text = "Nueva", Left = 670, Top = 8, Width = 80 };
            btnEditar = new Button { Text = "Editar", Left = 760, Top = 8, Width = 80 };
            btnDetalle = new Button { Text = "Detalle", Left = 850, Top = 8, Width = 80 };
            btnRefrescar = new Button { Text = "Refrescar", Left = 850, Top = 34, Width = 80 };

            panelTop.Controls.AddRange(new Control[] { cbProyecto, cbUsuario, cbEstado, btnNuevo, btnEditar, btnDetalle, btnRefrescar });

            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

            Controls.Add(_grid);
            Controls.Add(panelTop);

            cbProyecto.SelectedIndexChanged += (_, __) => LoadTareas();
            cbUsuario.SelectedIndexChanged += (_, __) => LoadTareas();
            cbEstado.SelectedIndexChanged += (_, __) => LoadTareas();

            btnNuevo.Click += BtnNuevo_Click;
            btnEditar.Click += BtnEditar_Click;
            btnDetalle.Click += BtnDetalle_Click;
            btnRefrescar.Click += (_, __) => LoadTareas();
        }

        private void LoadFilters()
        {
            var proyectos = _db.Proyectos.OrderBy(p => p.Nombre).ToList();
            proyectos.Insert(0, new Proyecto { Id = Guid.Empty, Nombre = "-- Todos --" });
            cbProyecto.DataSource = proyectos;
            cbProyecto.DisplayMember = "Nombre";
            cbProyecto.ValueMember = "Id";

            var usuarios = _db.Usuarios.OrderBy(u => u.Nombre).ToList();
            usuarios.Insert(0, new Usuario { Id = Guid.Empty, Nombre = "-- Todos --" });
            cbUsuario.DataSource = usuarios;
            cbUsuario.DisplayMember = "Nombre";
            cbUsuario.ValueMember = "Id";

            var estados = new List<string> { "-- Todos --", "Pendiente", "EnProgreso", "Completada" };
            cbEstado.DataSource = estados;
        }

        private void LoadTareas()
        {
            var q = _db.Tareas.AsQueryable();

            var proyectoId = (Guid)cbProyecto.SelectedValue;
            if (proyectoId != Guid.Empty)
                q = q.Where(t => t.ProyectoId == proyectoId);

            var usuarioId = (Guid)cbUsuario.SelectedValue;
            if (usuarioId != Guid.Empty)
                q = q.Where(t => t.UsuarioId == usuarioId);

            var estado = cbEstado.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(estado) && estado != "-- Todos --")
                q = q.Where(t => t.Estado == estado);

            var list = q.OrderByDescending(t => t.FechaCreacion)
                        .Select(t => new
                        {
                            t.Id,
                            Proyecto = _db.Proyectos.Where(p => p.Id == t.ProyectoId).Select(p => p.Nombre).FirstOrDefault(),
                            t.Titulo,
                            Asignado = _db.Usuarios.Where(u => u.Id == t.UsuarioId).Select(u => u.Nombre).FirstOrDefault(),
                            t.Estado,
                            t.Prioridad,
                            FechaLimite = t.FechaCierre.HasValue ? t.FechaCierre.Value.ToString("yyyy-MM-dd") : ""
                        }).ToList();

            _grid.DataSource = list;
        }

        private Guid? GetSelectedTareaId()
        {
            if (_grid.CurrentRow == null) return null;
            return Guid.Parse(_grid.CurrentRow.Cells["Id"].Value.ToString()!);
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            using var editor = new FormEditarTarea(_db);
            if (editor.ShowDialog(this) == DialogResult.OK)
                LoadTareas();
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedTareaId();
            if (id == null) return;
            using var editor = new FormEditarTarea(_db, id.Value);
            if (editor.ShowDialog(this) == DialogResult.OK)
                LoadTareas();
        }

        private void BtnDetalle_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedTareaId();
            if (id == null) return;
            using var det = new FormDetalleTarea(_db, id.Value);
            det.ShowDialog(this);
            LoadTareas();
        }
    }
}
