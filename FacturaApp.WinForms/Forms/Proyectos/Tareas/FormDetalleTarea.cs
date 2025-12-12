using System;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Data;
using FacturaApp.Core.Models;

namespace FacturaApp.WinForms.Forms
{
    public class FormDetalleTarea : Form
    {
        private readonly BillingDbContext _db;
        private readonly Guid _tareaId;
        private TareaProyecto? _tarea;

        private Label lblTitulo, lblProyecto, lblAsignado, lblEstado, lblPrioridad, lblFechas;
        private TextBox txtDescripcion;
        private ListBox lbComentarios;
        private TextBox txtNuevoComentario;
        private Button btnAgregarComentario, btnCerrar;

        public FormDetalleTarea(BillingDbContext db, Guid tareaId)
        {
            _db = db;
            _tareaId = tareaId;
            InitializeComponent();
            LoadDetalle();
        }

        private void InitializeComponent()
        {
            Text = "Detalle de tarea";
            Width = 700;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            lblTitulo = new Label { Left = 12, Top = 12, Width = 640, Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold) };
            lblProyecto = new Label { Left = 12, Top = 48, Width = 640 };
            lblAsignado = new Label { Left = 12, Top = 72, Width = 640 };
            lblEstado = new Label { Left = 12, Top = 96, Width = 640 };
            lblPrioridad = new Label { Left = 12, Top = 120, Width = 640 };
            lblFechas = new Label { Left = 12, Top = 144, Width = 640 };

            var lblDesc = new Label { Left = 12, Top = 176, Text = "Descripción:" };
            txtDescripcion = new TextBox { Left = 12, Top = 196, Width = 640, Height = 120, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            var lblCom = new Label { Left = 12, Top = 330, Text = "Comentarios:" };
            lbComentarios = new ListBox { Left = 12, Top = 352, Width = 640, Height = 140 };

            txtNuevoComentario = new TextBox { Left = 12, Top = 500, Width = 520, Height = 40, Multiline = false };
            btnAgregarComentario = new Button { Left = 540, Top = 500, Width = 112, Text = "Agregar" };
            btnAgregarComentario.Click += BtnAgregarComentario_Click;

            btnCerrar = new Button { Left = 540, Top = 540, Width = 112, Text = "Cerrar" };
            btnCerrar.Click += (_, __) => Close();

            Controls.AddRange(new Control[] {
                lblTitulo, lblProyecto, lblAsignado, lblEstado, lblPrioridad, lblFechas,
                lblDesc, txtDescripcion, lblCom, lbComentarios, txtNuevoComentario, btnAgregarComentario, btnCerrar
            });
        }

        // Reemplaza la consulta LINQ en LoadDetalle para evitar el operador de propagación NULL dentro de la expresión lambda.
        // Primero obtén los comentarios y luego proyecta fuera del LINQ usando ToList().
        private void LoadDetalle()
        {
            _tarea = _db.Tareas.Find(_tareaId);
            if (_tarea == null)
            {
                MessageBox.Show("Tarea no encontrada", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            var proyecto = _db.Proyectos.Find(_tarea.ProyectoId);
            var usuario = _db.Usuarios.Find(_tarea.UsuarioId);

            lblTitulo.Text = _tarea.Titulo;
            lblProyecto.Text = $"Proyecto: {proyecto?.Nombre}";
            lblAsignado.Text = $"Asignado a: {usuario?.Nombre ?? "-- No asignado --"}";
            lblEstado.Text = $"Estado: {_tarea.Estado}";
            lblPrioridad.Text = $"Prioridad: {_tarea.Prioridad}";
            lblFechas.Text = $"Creada: {_tarea.FechaCreacion:yyyy-MM-dd}  |  Límite: {_tarea.FechaCierre:yyyy-MM-dd}";

            txtDescripcion.Text = _tarea.Descripcion ?? "";

            // Solución: primero obtén los comentarios, luego proyecta fuera del LINQ
            var comentariosEntidades = _db.TareaComentarios
                .Where(c => c.TareaId == _tareaId)
                .OrderByDescending(c => c.Fecha)
                .ToList();

            var comentarios = comentariosEntidades
                .Select(c =>
                {
                    var usuarioComentario = _db.Usuarios.Find(c.UsuarioId);
                    return $"{c.Fecha:yyyy-MM-dd HH:mm} - {(usuarioComentario != null ? usuarioComentario.Nombre : "Usuario")}: {c.Texto}";
                })
                .ToList();

            lbComentarios.DataSource = comentarios;
        }

        private void BtnAgregarComentario_Click(object? sender, EventArgs e)
        {
            var texto = txtNuevoComentario.Text?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                MessageBox.Show("Comentario vacío", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Para ejemplo sencillo: asignar comentario al primer usuario "Admin" si no existe sesión
            var usuario = _db.Usuarios.FirstOrDefault();
            if (usuario == null)
            {
                MessageBox.Show("No hay usuarios en el sistema para asociar el comentario", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var com = new TareaComentario
            {
                Id = Guid.NewGuid(),
                TareaId = _tareaId,
                UsuarioId = usuario.Id,
                Texto = texto,
                Fecha = DateTime.Now
            };

            _db.TareaComentarios.Add(com);
            _db.SaveChanges();

            txtNuevoComentario.Clear();
            LoadDetalle();
        }
    }
}