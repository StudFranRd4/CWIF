using FacturaApp.Core.Models;
using FacturaApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    public class CompraDetalleEditorForm : Form
    {
        private readonly CompraDetalle _detalle;
        private readonly IServiceProvider _provider;
        private readonly BillingDbContext _dbContext;
        
        private ComboBox _comboProductos;
        private NumericUpDown _numCantidad;
        private NumericUpDown _numPrecio;
        private Label _lblImporte;
        
        public CompraDetalle Detalle => _detalle;
        
        public CompraDetalleEditorForm(CompraDetalle detalle, IServiceProvider provider)
        {
            _detalle = detalle;
            _provider = provider;
            _dbContext = provider.GetRequiredService<BillingDbContext>();
            
            InitializeComponent();
            LoadProductos();
            BindData();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Detalle de Compra";
            this.Width = 450;
            this.Height = 300;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var layout = new TableLayoutPanel 
            { 
                Dock = DockStyle.Fill, 
                ColumnCount = 2, 
                RowCount = 5,
                Padding = new Padding(10)
            };
            
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // Producto
            layout.Controls.Add(new Label { Text = "Producto:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 0);
            _comboProductos = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            layout.Controls.Add(_comboProductos, 1, 0);
            
            // Cantidad
            layout.Controls.Add(new Label { Text = "Cantidad:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 1);
            _numCantidad = new NumericUpDown { Minimum = 0, Maximum = 1000000, Dock = DockStyle.Fill };
            layout.Controls.Add(_numCantidad, 1, 1);
            
            // Precio
            layout.Controls.Add(new Label { Text = "Precio:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 2);
            _numPrecio = new NumericUpDown { Minimum = 0, Maximum = 10000000, DecimalPlaces = 2, Increment = 0.01m, Dock = DockStyle.Fill };
            layout.Controls.Add(_numPrecio, 1, 2);
            
            // Importe
            layout.Controls.Add(new Label { Text = "Importe:", TextAlign = ContentAlignment.MiddleRight, AutoSize = true }, 0, 3);
            _lblImporte = new Label { Text = "0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(_lblImporte, 1, 3);
            
            // Botones
            var panelBotones = new FlowLayoutPanel 
            { 
                FlowDirection = FlowDirection.RightToLeft, 
                AutoSize = true,
                Height = 40
            };
            
            var btnAceptar = new Button { Text = "Aceptar", Width = 100, DialogResult = DialogResult.OK };
            var btnCancelar = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
            
            panelBotones.Controls.Add(btnAceptar);
            panelBotones.Controls.Add(btnCancelar);
            
            layout.Controls.Add(panelBotones, 0, 4);
            layout.SetColumnSpan(panelBotones, 2);
            
            this.Controls.Add(layout);
            
            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCancelar;
            
            // Eventos
            _numCantidad.ValueChanged += (s, e) => CalcularImporte();
            _numPrecio.ValueChanged += (s, e) => CalcularImporte();
        }
        
        private async void LoadProductos()
        {
            try
            {
                var productos = await _dbContext.Set<Producto>()
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();
                
                _comboProductos.DisplayMember = "Nombre";
                _comboProductos.ValueMember = "Id";
                _comboProductos.DataSource = productos;
                
                if (_detalle.ProductoId != Guid.Empty)
                {
                    _comboProductos.SelectedValue = _detalle.ProductoId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BindData()
        {
            _numCantidad.Value = _detalle.Cantidad;
            _numPrecio.Value = _detalle.Precio;
            CalcularImporte();
        }
        
        private void CalcularImporte()
        {
            var importe = _numCantidad.Value * _numPrecio.Value;
            _lblImporte.Text = importe.ToString("N2");
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (_comboProductos.SelectedValue == null)
                {
                    MessageBox.Show("Debe seleccionar un producto", "Validación", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
                
                _detalle.ProductoId = (Guid)_comboProductos.SelectedValue;
                _detalle.Cantidad = (int)_numCantidad.Value;
                _detalle.Precio = _numPrecio.Value;
            }
            
            base.OnFormClosing(e);
        }
    }
}