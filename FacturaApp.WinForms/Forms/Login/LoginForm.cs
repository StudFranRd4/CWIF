using System;
using System.Windows.Forms;
using FacturaApp.Core.Models;
using FacturaApp.Services;

public partial class LoginForm : Form
{
    private readonly AuthService _auth;
    private readonly IRepository<Usuario> _repo;

    public Usuario? UsuarioAutenticado { get; private set; }

    public LoginForm(AuthService auth, IRepository<Usuario> repo)
    {
        _auth = auth;
        _repo = repo;

        InitializeForm();
    }

    private void InitializeForm()
    {
        this.Text = "CWIF: acceder";
        this.Width = 400;
        this.Height = 300;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // TabControl
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabLogin = new TabPage("Iniciar sesión");
        var tabRegister = new TabPage("Registrarse");
        tabs.TabPages.Add(tabLogin);
        tabs.TabPages.Add(tabRegister);
        this.Controls.Add(tabs);

        // ---------------- LOGIN ----------------
        var lblUser = new Label { Text = "Usuario:", Top = 20, Left = 30, Width = 80 };
        var txtUser = new TextBox { Name = "txtUser", Top = 20, Left = 120, Width = 200 };
        var lblPass = new Label { Text = "Contraseña:", Top = 60, Left = 30, Width = 80 };
        var txtPass = new TextBox { Name = "txtPass", Top = 60, Left = 120, Width = 200, PasswordChar = '*' };
        var btnLogin = new Button { Text = "Ingresar", Top = 100, Left = 120, Width = 100 };
        btnLogin.Click += (s, e) => Login(txtUser.Text, txtPass.Text);

        tabLogin.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, btnLogin });

        // ---------------- REGISTRO ----------------
        var lblRegName = new Label { Text = "Nombre:", Top = 20, Left = 30, Width = 80 };
        var txtRegName = new TextBox { Name = "txtRegName", Top = 20, Left = 120, Width = 200 };
        var lblRegUser = new Label { Text = "Usuario:", Top = 60, Left = 30, Width = 80 };
        var txtRegUser = new TextBox { Name = "txtRegUser", Top = 60, Left = 120, Width = 200 };
        var lblRegPass = new Label { Text = "Contraseña:", Top = 100, Left = 30, Width = 80 };
        var txtRegPass = new TextBox { Name = "txtRegPass", Top = 100, Left = 120, Width = 200, PasswordChar = '*' };
        var lblRegRole = new Label { Text = "Rol:", Top = 140, Left = 30, Width = 80 };
        var cbRole = new ComboBox { Name = "cbRole", Top = 140, Left = 120, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cbRole.Items.AddRange(Enum.GetNames(typeof(UserRole)));
        cbRole.SelectedIndex = 0;
        var btnRegister = new Button { Text = "Registrar", Top = 180, Left = 120, Width = 100 };
        btnRegister.Click += (s, e) => Register(txtRegName.Text, txtRegUser.Text, txtRegPass.Text, cbRole.SelectedIndex);

        tabRegister.Controls.AddRange(new Control[] { lblRegName, txtRegName, lblRegUser, txtRegUser, lblRegPass, txtRegPass, lblRegRole, cbRole, btnRegister });
    }

    // ================= LOGIN =================
    private async Task Login(string username, string password)
    {
        var user = await _auth.LoginAsync(username, password);
        if (user != null)
        {
            UsuarioAutenticado = user;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Usuario o contraseña incorrectos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void InitializeComponent()
    {

    }

    // ================= REGISTRO =================

    public async void Register(string nombre, string username, string password, int roleIndex)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Todos los campos son obligatorios.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Obtener todos los usuarios de forma asíncrona
        var usuarios = await _repo.GetAllAsync();
        if (usuarios.Any(u => u.Username == username))
        {
            MessageBox.Show("El usuario ya existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Username = username,
            Password = PasswordHelper.HashPassword(password),
            Rol = (UserRole)roleIndex + 1
        };

        await _repo.CreateAsync(usuario);
        MessageBox.Show("Usuario registrado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
		
		await Login(username, password);
    }
}