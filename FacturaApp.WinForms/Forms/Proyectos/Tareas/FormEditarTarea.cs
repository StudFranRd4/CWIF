using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using FacturaApp.Core.Models;

namespace FacturaApp.WinForms.Forms
{
    public class FormEditarTarea : Form
    {
        private readonly BillingDbContext _db;
        private TareaProyecto? _tarea;
        private ComboBox cbProyecto, cbUsuario, cbEstado, cbPrioridad;
        private TextBox txtTitulo;
        private TextBox txtDescripcion;
        private DateTimePicker dtLimite;
        private Button btnGuardar, btnCancelar;

        public FormEditarTarea(BillingDbContext db, Guid? tareaId = null)
        {
            _db = db;
            if (tareaId.HasValue) _tarea = _db.Tareas.Find(tareaId.Value);
            InitializeComponent();
            LoadData();
            if (_tarea != null) LoadEntity();
        }

        private void InitializeComponent()
        {
            Text = _tarea == null ? "Nueva Tarea" : "Editar Tarea";
            Width = 600;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;

            var lblProyecto = new Label { Text = "Proyecto", Left = 12, Top = 14, Width = 80 };
            cbProyecto = new ComboBox { Left = 100, Top = 10, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblTitulo = new Label { Text = "Título", Left = 12, Top = 52, Width = 80 };
            txtTitulo = new TextBox { Left = 100, Top = 48, Width = 360 };

            var lblDesc = new Label { Text = "Descripción", Left = 12, Top = 90, Width = 80 };
            txtDescripcion = new TextBox { Left = 100, Top = 86, Width = 360, Height = 100, Multiline = true };

            var lblUsuario = new Label { Text = "Asignado a", Left = 12, Top = 200, Width = 80 };
            cbUsuario = new ComboBox { Left = 100, Top = 196, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblEstado = new Label { Text = "Estado", Left = 12, Top = 236, Width = 80 };
            cbEstado = new ComboBox { Left = 100, Top = 232, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblPrioridad = new Label { Text = "Prioridad", Left = 260, Top = 236, Width = 80 };
            cbPrioridad = new ComboBox { Left = 340, Top = 232, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblLimite = new Label { Text = "Fecha límite", Left = 12, Top = 274, Width = 80 };
            dtLimite = new DateTimePicker { Left = 100, Top = 270, Width = 160, ShowCheckBox = true };

            btnGuardar = new Button { Text = "Guardar", Left = 360, Top = 350, Width = 100 };
            btnCancelar = new Button { Text = "Cancelar", Left = 240, Top = 350, Width = 100 };

            btnGuardar.Click += BtnGuardar_Click;
            btnCancelar.Click += (_, __) => DialogResult = DialogResult.Cancel;
            cbProyecto.SelectedIndexChanged += (_, __) => LoadUsuariosAsignables();

            Controls.AddRange(new Control[] {
                lblProyecto, cbProyecto, lblTitulo, txtTitulo, lblDesc, txtDescripcion,
                lblUsuario, cbUsuario, lblEstado, cbEstado, lblPrioridad, cbPrioridad,
                lblLimite, dtLimite, btnGuardar, btnCancelar
            });
        }

        private void LoadData()
        {
            cbProyecto.DataSource = _db.Proyectos.OrderBy(p => p.Nombre).ToList();
            cbProyecto.DisplayMember = "Nombre";
            cbProyecto.ValueMember = "Id";

            cbEstado.DataSource = new[] { "Pendiente", "EnProgreso", "Completada" }.ToList();
            cbPrioridad.DataSource = new[] { "Baja", "Media", "Alta" }.ToList();

            if (_tarea == null && cbProyecto.Items.Count > 0)
                cbProyecto.SelectedIndex = 0;

            LoadUsuariosAsignables();
        }

        private void LoadUsuariosAsignables()
        {
            if (cbProyecto.SelectedItem is Proyecto proyecto)
            {
                var usuariosIds = _db.ProyectoUsuarios
                    .Where(pu => pu.ProyectoId == proyecto.Id)
                    .Select(pu => pu.UsuarioId)
                    .ToList();

                var usuarios = _db.Usuarios.Where(u => usuariosIds.Contains(u.Id)).OrderBy(u => u.Nombre).ToList();
                usuarios.Insert(0, new Usuario { Id = Guid.Empty, Nombre = "-- Sin asignar --" });
                cbUsuario.DataSource = usuarios;
                cbUsuario.DisplayMember = "Nombre";
                cbUsuario.ValueMember = "Id";
            }
        }

        private void LoadEntity()
        {
            if (_tarea == null) return;
            cbProyecto.SelectedValue = _tarea.ProyectoId;
            txtTitulo.Text = _tarea.Titulo;
            txtDescripcion.Text = _tarea.Descripcion ?? "";
            cbUsuario.SelectedValue = _tarea.UsuarioId;
            cbEstado.SelectedItem = _tarea.Estado;
            cbPrioridad.SelectedItem = _tarea.Prioridad;
            if (_tarea.FechaCierre.HasValue)
            {
                dtLimite.Checked = true;
                dtLimite.Value = _tarea.FechaCierre.Value;
            }
            else dtLimite.Checked = false;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text))
            {
                MessageBox.Show("Título requerido", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var proyecto = cbProyecto.SelectedItem as Proyecto ?? throw new InvalidOperationException("Seleccione proyecto");
            var usuarioId = (Guid)cbUsuario.SelectedValue;
            Guid? usuario = usuarioId == Guid.Empty ? null : usuarioId;

            if (_tarea == null)
            {
                _tarea = new TareaProyecto
                {
                    Id = Guid.NewGuid(),
                    ProyectoId = proyecto.Id,
                    Titulo = txtTitulo.Text.Trim(),
                    Descripcion = txtDescripcion.Text.Trim(),
                    UsuarioId = usuario.Value,
                    Estado = cbEstado.SelectedItem!.ToString()!,
                    Prioridad = cbPrioridad.SelectedItem!.ToString()!,
                    FechaCreacion = DateTime.Now,
                    FechaCierre = dtLimite.Checked ? dtLimite.Value : null
                };
                _db.Tareas.Add(_tarea);
            }
            else
            {
                _tarea.ProyectoId = proyecto.Id;
                _tarea.Titulo = txtTitulo.Text.Trim();
                _tarea.Descripcion = txtDescripcion.Text.Trim();
                _tarea.UsuarioId = usuario.Value;
                _tarea.Estado = cbEstado.SelectedItem!.ToString()!;
                _tarea.Prioridad = cbPrioridad.SelectedItem!.ToString()!;
                _tarea.FechaCierre = dtLimite.Checked ? dtLimite.Value : null;
                _db.Tareas.Update(_tarea);
            }

            _db.SaveChanges();
            DialogResult = DialogResult.OK;
        }
    }
}