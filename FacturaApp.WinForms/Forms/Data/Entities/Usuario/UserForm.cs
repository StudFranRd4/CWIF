using FacturaApp.Core.Models;
using FacturaApp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    public partial class UserForm : Form
    {
        private readonly IRepository<Usuario> _repo;
        private Guid? _selectedId;

        public UserForm(IRepository<Usuario> repo)
        {
            _repo = repo;
            InitializeComponent();
            LoadForm();
        }

        private async void LoadForm()
        {
            LoadRoles();
            await LoadUsers();
        }

        private void LoadRoles()
        {
            cbRol.DataSource = Enum.GetValues(typeof(UserRole));
        }

        private async Task LoadUsers()
        {
            var usuarios = await _repo.GetAllAsync();

            dgvUsers.DataSource = usuarios
                .Select(x => new
                {
                    x.Id,
                    x.Nombre,
                    x.Username,
                    x.Email,
                    Rol = x.Rol.ToString()
                })
                .ToList();

            _selectedId = null;
        }

        private bool ValidateForm(bool editing)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Falta el nombre.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Falta el usuario.");
                return false;
            }

            if (!editing && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("La contraseña es obligatoria.");
                return false;
            }

            return true;
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(editing: false))
                return;

            try
            {
                var usuarios = await _repo.GetAllAsync();

                if (usuarios.Any(u => u.Username == txtUsername.Text.Trim()))
                {
                    MessageBox.Show("Ese usuario ya existe.");
                    return;
                }

                var nuevo = new Usuario
                {
                    Nombre = txtNombre.Text.Trim(),
                    Username = txtUsername.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Password = PasswordHelper.HashPassword(txtPassword.Text.Trim()),
                    Rol = (UserRole)cbRol.SelectedItem
                };

                await _repo.CreateAsync(nuevo);

                MessageBox.Show("Usuario guardado.");
                await LoadUsers();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando usuario: " + ex.Message);
            }
        }

        private async void btnEditar_Click(object sender, EventArgs e)
        {
            if (_selectedId == null)
            {
                MessageBox.Show("Selecciona un usuario primero.");
                return;
            }

            if (!ValidateForm(editing: true))
                return;

            var user = await _repo.GetAsync(_selectedId.Value);
            if (user == null)
            {
                MessageBox.Show("El usuario ya no existe.");
                await LoadUsers();
                return;
            }

            user.Nombre = txtNombre.Text.Trim();
            user.Username = txtUsername.Text.Trim();
            user.Email = txtEmail.Text.Trim();
            user.Rol = (UserRole)cbRol.SelectedItem;

            if (!string.IsNullOrWhiteSpace(txtPassword.Text))
                user.Password = PasswordHelper.HashPassword(txtPassword.Text.Trim());

            await _repo.UpdateAsync(user);

            MessageBox.Show("Usuario actualizado.");
            await LoadUsers();
            ClearForm();
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId == null)
            {
                MessageBox.Show("Selecciona un usuario primero.");
                return;
            }

            var user = await _repo.GetAsync(_selectedId.Value);
            if (user == null)
            {
                MessageBox.Show("El usuario ya no existe.");
                await LoadUsers();
                return;
            }

            await _repo.DeleteAsync(user.Id);

            MessageBox.Show("Usuario eliminado.");
            await LoadUsers();
            ClearForm();
        }

        private void dgvUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvUsers.Rows[e.RowIndex];

            _selectedId = (Guid)row.Cells["Id"].Value;

            txtNombre.Text = row.Cells["Nombre"].Value?.ToString() ?? "";
            txtUsername.Text = row.Cells["Username"].Value?.ToString() ?? "";
            txtEmail.Text = row.Cells["Email"].Value?.ToString() ?? "";
            cbRol.SelectedItem = Enum.Parse(typeof(UserRole), row.Cells["Rol"].Value.ToString());

            txtPassword.Text = "";
        }

        private void ClearForm()
        {
            txtNombre.Clear();
            txtUsername.Clear();
            txtEmail.Clear();
            txtPassword.Clear();
            cbRol.SelectedIndex = 0;
            _selectedId = null;
        }
    }
}