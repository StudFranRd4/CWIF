using FacturaApp.Core.Models;
using FacturaApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    public class CompraEditorForm : Form
    {
        private readonly Compra _compra;
        private readonly IServiceProvider _provider;
        private readonly BillingDbContext _dbContext;
        
        private DateTimePicker _dtpFecha;
        private ComboBox _comboProveedores;
        private Button _btnEditarDetalles;
        private DataGridView _gridDetalles;
        private BindingList<CompraDetalle> _detallesBinding;
        
        public Compra Compra => _compra;
        
        public CompraEditorForm(IServiceProvider provider, Compra compra)
        {
            _compra = compra;
            _provider = provider;
            _dbContext = provider.GetRequiredService<BillingDbContext>();
            
            InitializeComponent();
            LoadProveedores();
            BindData();
            LoadDetalles();
        }
        
        private void InitializeComponent()
        {
            this.Text = _compra.Id == Guid.Empty ? "Nueva Compra" : "Editar Compra";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var layout = new TableLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                ColumnCount = 2, 
                RowCount = 4,
                Padding = new Padding(10)
            };
            
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // Fecha
            layout.Controls.Add(new Label { Text = "Fecha:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 0);
            _dtpFecha = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_dtpFecha, 1, 0);
            
            // Proveedor
            layout.Controls.Add(new Label { Text = "Proveedor:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 1);
            _comboProveedores = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            layout.Controls.Add(_comboProveedores, 1, 1);
            
            // Botón para editar detalles
            _btnEditarDetalles = new Button { Text = "Editar Detalles...", Width = 150 };
            layout.Controls.Add(_btnEditarDetalles, 0, 2);
            layout.SetColumnSpan(_btnEditarDetalles, 2);
            
            // Grid de detalles (solo lectura para vista previa)
            _gridDetalles = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            layout.Controls.Add(_gridDetalles, 0, 3);
            layout.SetColumnSpan(_gridDetalles, 2);
            
            // Botones de formulario
            var panelBotones = new FlowLayoutPanel 
            { 
                FlowDirection = FlowDirection.RightToLeft, 
                AutoSize = true,
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };
            
            var btnAceptar = new Button { Text = "Aceptar", Width = 100, DialogResult = DialogResult.OK };
            var btnCancelar = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
            
            panelBotones.Controls.Add(btnAceptar);
            panelBotones.Controls.Add(btnCancelar);
            
            // Layout principal
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            
            mainLayout.Controls.Add(layout, 0, 0);
            mainLayout.Controls.Add(panelBotones, 0, 1);
            
            this.Controls.Add(mainLayout);
            
            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;
            
            // Eventos
            _btnEditarDetalles.Click += BtnEditarDetalles_Click;
            
            ConfigureGridDetalles();
        }
        
        private async void LoadProveedores()
        {
            try
            {
                var proveedores = await _dbContext.Set<Proveedor>()
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
                
                _comboProveedores.DisplayMember = "Nombre";
                _comboProveedores.ValueMember = "Id";
                _comboProveedores.DataSource = proveedores;
                
                if (_compra.ProveedorId != Guid.Empty)
                {
                    _comboProveedores.SelectedValue = _compra.ProveedorId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BindData()
        {
            _dtpFecha.Value = _compra.Fecha;
        }
        
        private void LoadDetalles()
        {
            if (_compra.Detalles == null)
            {
                _detallesBinding = new BindingList<CompraDetalle>();
            }
            else
            {
                _detallesBinding = new BindingList<CompraDetalle>(_compra.Detalles.ToList());
            }
            
            _gridDetalles.DataSource = _detallesBinding;
        }
        
        private void ConfigureGridDetalles()
        {
            _gridDetalles.Columns.Clear();
            
            _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Producto",
                HeaderText = "Producto",
                DataPropertyName = "ProductoNombre",
                ReadOnly = true,
                Width = 200
            });
            
            _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Cantidad",
                HeaderText = "Cantidad",
                DataPropertyName = "Cantidad",
                ReadOnly = true,
                Width = 80
            });
            
            _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Precio",
                HeaderText = "Precio",
                DataPropertyName = "Precio",
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            
            _gridDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Importe",
                HeaderText = "Importe",
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
        }
        
        private void BtnEditarDetalles_Click(object sender, EventArgs e)
        {
            if (_comboProveedores.SelectedValue == null)
            {
                MessageBox.Show("Primero seleccione un proveedor", "Validación", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Crear lista temporal para edición
            var listaDetalles = _compra.Detalles?.ToList() ?? new List<CompraDetalle>();
            
            var editor = new CollectionEditorForm<CompraDetalle>(
                listaDetalles,
                _provider,
                () => new CompraDetalle { CompraId = _compra.Id })
            {
                OverrideEditorCreation = (detalle) => new CompraDetalleEditorForm(detalle, _provider)
            };
            
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                _compra.Detalles = editor.ResultList.ToList();
                LoadDetalles();
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (_comboProveedores.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar un proveedor", "Validación", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
                
                _compra.Fecha = _dtpFecha.Value;
                _compra.ProveedorId = (Guid)_comboProveedores.SelectedValue;
                
                // Si no hay detalles, mostrar advertencia
                if (_compra.Detalles == null || !_compra.Detalles.Any())
                {
                    if (MessageBox.Show("La compra no tiene detalles. ¿Desea continuar?", 
                        "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            }
            
            base.OnFormClosing(e);
        }
    }
}