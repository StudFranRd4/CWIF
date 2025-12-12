namespace FacturaApp.WinForms.Forms
{
    partial class UserForm
    {
        private System.ComponentModel.IContainer components = null;

        private DataGridView dgvUsers;
        private TextBox txtNombre;
        private TextBox txtUsername;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private ComboBox cbRol;
        private Button btnGuardar;
        private Button btnEliminar;
        private Button btnEditar;

        private Label lblNombre;
        private Label lblUsername;
        private Label lblEmail;
        private Label lblPassword;
        private Label lblRol;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            dgvUsers.CellClick += dgvUsers_CellClick;

            lblNombre = new Label { Text = "Nombre:", Left = 20, Top = 0, Width = 150 };
            txtNombre = new TextBox
            {
                Left = 20, Top = 20, Width = 200,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblUsername = new Label { Text = "Usuario:", Left = 20, Top = 50, Width = 150 };
            txtUsername = new TextBox
            {
                Left = 20, Top = 70, Width = 200,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblEmail = new Label { Text = "Email:", Left = 20, Top = 100, Width = 150 };
            txtEmail = new TextBox
            {
                Left = 20, Top = 120, Width = 200,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblPassword = new Label { Text = "Contraseña:", Left = 20, Top = 150, Width = 150 };
            txtPassword = new TextBox
            {
                Left = 20, Top = 170, Width = 200,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '*'
            };

            lblRol = new Label { Text = "Rol:", Left = 20, Top = 200, Width = 150 };
            cbRol = new ComboBox
            {
                Left = 20, Top = 220, Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnGuardar = new Button { Text = "Guardar", Left = 250, Top = 20, Width = 100 };
            btnGuardar.Click += btnGuardar_Click;

            btnEditar = new Button { Text = "Editar", Left = 250, Top = 70, Width = 100 };
            btnEditar.Click += btnEditar_Click;

            btnEliminar = new Button { Text = "Eliminar", Left = 250, Top = 120, Width = 100 };
            btnEliminar.Click += btnEliminar_Click;

            Controls.AddRange(new Control[]
            {
                dgvUsers,
                lblNombre, txtNombre,
                lblUsername, txtUsername,
                lblEmail, txtEmail,
                lblPassword, txtPassword,
                lblRol, cbRol,
                btnGuardar, btnEditar, btnEliminar
            });

            Text = "Gestión de Usuarios";
            Width = 600;
            Height = 520;
        }
    }
}