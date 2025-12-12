using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using FacturaApp.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace FacturaApp.WinForms.Forms
{
    public class FormProyectos : Form
    {
        private readonly BillingDbContext _db;
        private readonly IServiceProvider _provider;
        private DataGridView _grid;
        private Button _btnNuevo, _btnEditar, _btnEliminar, _btnRefrescar, _btnAsignar;

        public FormProyectos(IServiceProvider provider)
        {
            _provider = provider;
            _db = provider.GetService<BillingDbContext>() ?? throw new InvalidOperationException("BillingDbContext no registrado");

            InitializeComponent();
            LoadProyectos();
        }

        private void InitializeComponent()
        {
            Text = "Proyectos";
            Width = 900;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;

            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

            var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), FlowDirection = FlowDirection.LeftToRight };
            _btnNuevo = new Button { Text = "Nuevo", Width = 90 };
            _btnEditar = new Button { Text = "Editar", Width = 90 };
            _btnEliminar = new Button { Text = "Eliminar", Width = 90 };
            _btnRefrescar = new Button { Text = "Refrescar", Width = 90 };
            _btnAsignar = new Button { Text = "Asignar usuarios", Width = 130 };

            panel.Controls.AddRange(new Control[] { _btnNuevo, _btnEditar, _btnEliminar, _btnRefrescar, _btnAsignar });

            Controls.Add(_grid);
            Controls.Add(panel);

            _btnNuevo.Click += BtnNuevo_Click;
            _btnEditar.Click += BtnEditar_Click;
            _btnEliminar.Click += BtnEliminar_Click;
            _btnRefrescar.Click += (_, __) => LoadProyectos();
            _btnAsignar.Click += BtnAsignar_Click;
        }

        private void LoadProyectos()
        {
            var list = _db.Proyectos.OrderBy(p => p.Nombre).ToList();
            _grid.DataSource = list.Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Descripcion,
                FechaInicio = p.FechaInicio.ToString("yyyy-MM-dd"),
                FechaFin = p.FechaFin?.ToString("yyyy-MM-dd") ?? ""
            }).ToList();
        }

        private Guid? GetSelectedProyectoId()
        {
            if (_grid.CurrentRow == null) return null;
            if (_grid.CurrentRow.Cells["Id"].Value == null) return null;
            return Guid.Parse(_grid.CurrentRow.Cells["Id"].Value.ToString()!);
        }

        private void BtnNuevo_Click(object? sender, EventArgs e)
        {
            using var editor = new FormEditarProyecto(_db);
            if (editor.ShowDialog(this) == DialogResult.OK)
                LoadProyectos();
        }

        private void BtnEditar_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedProyectoId();
            if (id == null) return;
            var proyecto = _db.Proyectos.Find(id.Value);
            if (proyecto == null) return;
            using var editor = new FormEditarProyecto(_db, proyecto);
            if (editor.ShowDialog(this) == DialogResult.OK)
                LoadProyectos();
        }

        private void BtnEliminar_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedProyectoId();
            if (id == null) return;
            if (MessageBox.Show("Eliminar proyecto?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var p = _db.Proyectos.Find(id.Value);
                if (p != null)
                {
                    _db.Proyectos.Remove(p);
                    _db.SaveChanges();
                    LoadProyectos();
                }
            }
        }

        private void BtnAsignar_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedProyectoId();
            if (id == null)
            {
                MessageBox.Show("Seleccione un proyecto primero", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var form = new FormAsignarUsuarios(_provider, id.Value);
            form.ShowDialog(this);
        }
    }

    // Editor simple de proyectos
    public class FormEditarProyecto : Form
    {
        private readonly BillingDbContext _db;
        private Proyecto? _proyecto;
        private TextBox txtNombre, txtDescripcion;
        private DateTimePicker dtInicio, dtFin;
        private Button btnGuardar, btnCancelar;

        public FormEditarProyecto(BillingDbContext db, Proyecto? proyecto = null)
        {
            _db = db;
            _proyecto = proyecto;
            InitializeComponent();
            if (_proyecto != null) LoadEntity();
        }

        private void InitializeComponent()
        {
            Text = _proyecto == null ? "Nuevo Proyecto" : "Editar Proyecto";
            Width = 420;
            Height = 300;
            StartPosition = FormStartPosition.CenterParent;

            var lbl1 = new Label { Text = "Nombre", Left = 12, Top = 14, Width = 80 };
            txtNombre = new TextBox { Left = 100, Top = 12, Width = 280 };

            var lbl2 = new Label { Text = "Descripción", Left = 12, Top = 48, Width = 80 };
            txtDescripcion = new TextBox { Left = 100, Top = 46, Width = 280 };

            var lbl3 = new Label { Text = "Inicio", Left = 12, Top = 84, Width = 80 };
            dtInicio = new DateTimePicker { Left = 100, Top = 80, Width = 160 };

            var lbl4 = new Label { Text = "Fin", Left = 12, Top = 120, Width = 80 };
            dtFin = new DateTimePicker { Left = 100, Top = 116, Width = 160, ShowCheckBox = true };

            btnGuardar = new Button { Text = "Guardar", Left = 260, Top = 200, Width = 100 };
            btnCancelar = new Button { Text = "Cancelar", Left = 140, Top = 200, Width = 100 };

            btnGuardar.Click += BtnGuardar_Click;
            btnCancelar.Click += (_, __) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lbl1, txtNombre, lbl2, txtDescripcion, lbl3, dtInicio, lbl4, dtFin, btnGuardar, btnCancelar });
        }

        private void LoadEntity()
        {
            if (_proyecto == null) return;
            txtNombre.Text = _proyecto.Nombre;
            txtDescripcion.Text = _proyecto.Descripcion ?? "";
            dtInicio.Value = _proyecto.FechaInicio;
            if (_proyecto.FechaFin.HasValue)
            {
                dtFin.Checked = true;
                dtFin.Value = _proyecto.FechaFin.Value;
            }
            else dtFin.Checked = false;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Nombre requerido", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_proyecto == null)
            {
                _proyecto = new Proyecto
                {
                    Id = Guid.NewGuid(),
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text.Trim(),
                    FechaInicio = dtInicio.Value,
                    FechaFin = dtFin.Checked ? dtFin.Value : (DateTime?)null
                };
                _db.Proyectos.Add(_proyecto);
            }
            else
            {
                _proyecto.Nombre = txtNombre.Text.Trim();
                _proyecto.Descripcion = txtDescripcion.Text.Trim();
                _proyecto.FechaInicio = dtInicio.Value;
                _proyecto.FechaFin = dtFin.Checked ? dtFin.Value : (DateTime?)null;
                _db.Proyectos.Update(_proyecto);
            }

            _db.SaveChanges();
            DialogResult = DialogResult.OK;
        }
    }
}
