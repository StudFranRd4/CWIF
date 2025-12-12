using FacturaApp.Core.Models;
using FacturaApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

public class FacturaPagosForm : Form
{
    private readonly IServiceProvider _provider;
    private readonly Guid _facturaId;
    private BindingList<FacturaPago> _bindingList;
    private DataGridView _grid;
    private List<FormaPago> _formasPago;
    private IRepository<FormaPago> _formaPagoRepo;
    
    public List<FacturaPago> Pagos { get; private set; }

    public FacturaPagosForm(IServiceProvider provider, List<FacturaPago> pagos, Guid facturaId)
    {
        _provider = provider;
        _facturaId = facturaId;
        _formaPagoRepo = _provider.GetService(typeof(IRepository<FormaPago>)) as IRepository<FormaPago>;

        // Clonar los pagos para preservar los montos originales
        Pagos = new List<FacturaPago>();
        foreach (var pago in pagos)
        {
            Pagos.Add(new FacturaPago
            {
                Id = pago.Id,
                FacturaId = pago.FacturaId,
                FormaPagoId = pago.FormaPagoId,
                Monto = pago.Monto
            });
        }
        
        Text = $"Editar Pagos de Factura - Total Pagado: {CalcularTotalPagos(Pagos):C2}";
        Width = 700;
        Height = 450;
        StartPosition = FormStartPosition.CenterParent;
        
        CargarFormasPagoSincrono();
        Initialize();
    }

    private decimal CalcularTotalPagos(List<FacturaPago> pagos)
    {
        return pagos.Sum(p => p.Monto);
    }

    private void CargarFormasPagoSincrono()
    {
        if (_formaPagoRepo != null)
        {
            try
            {
                var task = _formaPagoRepo.GetAllAsync();
                task.Wait();
                _formasPago = task.Result.ToList();
            }
            catch (Exception ex)
            {
                _formasPago = new List<FormaPago>();
                Console.WriteLine($"Error al cargar formas de pago: {ex.Message}");
            }
        }
        else
        {
            _formasPago = new List<FormaPago>();
        }
    }

    private void Initialize()
    {
        _bindingList = new BindingList<FacturaPago>(Pagos);
        
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        
        _grid.DataError += (s, e) =>
        {
            e.ThrowException = false;
        };
        
        ConfigurarGrid();
        layout.Controls.Add(_grid, 0, 0);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 50,
            Padding = new Padding(6)
        };
        layout.Controls.Add(btnPanel, 0, 1);

        var btnOk = new Button { Text = "Aceptar", Width = 110, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancelar", Width = 110, DialogResult = DialogResult.Cancel };
        var btnAdd = new Button { Text = "Agregar", Width = 110 };
        var btnDel = new Button { Text = "Eliminar", Width = 110 };

        btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel, btnDel, btnAdd });

        btnAdd.Click += BtnAdd_Click;
        btnDel.Click += BtnDel_Click;

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        ActualizarTitulo();
        ActualizarGrid();
    }

    private void ActualizarTitulo()
    {
        decimal total = Pagos.Sum(p => p.Monto);
        Text = $"Editar Pagos de Factura - Total Pagado: {total:C2}";
    }

    private void ConfigurarGrid()
    {
        _grid.Columns.Clear();
        
        // Columna FormaPago (combobox)
        var formaPagoCol = new DataGridViewComboBoxColumn
        {
            Name = "FormaPagoId",
            HeaderText = "Forma de Pago",
            Width = 300,
            DisplayMember = "Nombre",
            ValueMember = "Id",
            FlatStyle = FlatStyle.Flat,
            DataSource = _formasPago
        };
        _grid.Columns.Add(formaPagoCol);

        // Columna Monto
        var montoCol = new DataGridViewTextBoxColumn
        {
            Name = "Monto",
            HeaderText = "Monto",
            Width = 150,
            DefaultCellStyle = new DataGridViewCellStyle { 
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "C2"
            }
        };
        _grid.Columns.Add(montoCol);

        SuscribirEventosGrid();
    }

    private void ActualizarGrid()
    {
        _grid.Rows.Clear();
        
        foreach (var pago in Pagos)
        {
            int rowIndex = _grid.Rows.Add();
            var row = _grid.Rows[rowIndex];
            
            row.Cells["FormaPagoId"].Value = pago.FormaPagoId;
            row.Cells["Monto"].Value = pago.Monto;
        }
    }

    private void SuscribirEventosGrid()
    {
        _grid.EditingControlShowing += (s, e) =>
        {
            if (e.Control is ComboBox combo && _grid.CurrentCell.ColumnIndex == 0)
            {
                combo.SelectedIndexChanged -= ComboFormaPago_SelectedIndexChanged;
                combo.SelectedIndexChanged += ComboFormaPago_SelectedIndexChanged;
            }
        };

        _grid.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                ActualizarFila(e.RowIndex);
            }
        };

        _grid.CellEndEdit += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                ActualizarFila(e.RowIndex);
            }
        };

        _grid.CellValidating += (s, e) =>
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0) // Columna Monto
            {
                var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    if (!decimal.TryParse(cell.Value.ToString(), out decimal monto) || monto < 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show("El monto debe ser un número decimal positivo.", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        };

        _grid.CellFormatting += Grid_CellFormatting;
    }

    private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            if (_grid.Columns[e.ColumnIndex].Name == "FormaPagoId" && e.Value != null)
            {
                if (Guid.TryParse(e.Value.ToString(), out Guid formaPagoId) && formaPagoId != Guid.Empty)
                {
                    var formaPago = _formasPago.FirstOrDefault(fp => fp.Id == formaPagoId);
                    if (formaPago != null)
                    {
                        e.Value = formaPago.Nombre;
                        e.FormattingApplied = true;
                    }
                }
                else if (e.Value == null || e.Value.ToString() == Guid.Empty.ToString())
                {
                    e.Value = "Seleccione una forma de pago";
                    e.FormattingApplied = true;
                }
            }
        }
    }

    private void ComboFormaPago_SelectedIndexChanged(object sender, EventArgs e)
    {
        var combo = sender as ComboBox;
        if (combo != null && _grid.CurrentCell != null && _grid.CurrentCell.RowIndex >= 0)
        {
            var rowIndex = _grid.CurrentCell.RowIndex;
            var formaPagoId = combo.SelectedValue as Guid?;
            
            if (formaPagoId.HasValue && formaPagoId.Value != Guid.Empty)
            {
                // Actualizar el pago en la lista
                if (rowIndex < Pagos.Count)
                {
                    Pagos[rowIndex].FormaPagoId = formaPagoId.Value;
                }
            }
            else
            {
                if (rowIndex < Pagos.Count)
                {
                    Pagos[rowIndex].FormaPagoId = Guid.Empty;
                }
            }
            
            ActualizarTitulo();
        }
    }

    private void ActualizarFila(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
        
        var row = _grid.Rows[rowIndex];
        
        // Obtener valores del grid
        var formaPagoIdCell = row.Cells["FormaPagoId"].Value;
        var montoCell = row.Cells["Monto"].Value;
        
        Guid formaPagoId = Guid.Empty;
        if (formaPagoIdCell != null && formaPagoIdCell is Guid)
            formaPagoId = (Guid)formaPagoIdCell;
        else if (formaPagoIdCell != null && Guid.TryParse(formaPagoIdCell.ToString(), out Guid parsedId))
            formaPagoId = parsedId;
        
        decimal monto = 0;
        if (montoCell != null && decimal.TryParse(montoCell.ToString(), out decimal parsedMonto))
            monto = parsedMonto;
        
        // Validar monto positivo
        if (monto < 0)
        {
            monto = 0;
            row.Cells["Monto"].Value = 0;
        }
        
        // Actualizar o crear pago en la lista
        if (rowIndex < Pagos.Count)
        {
            Pagos[rowIndex].FormaPagoId = formaPagoId;
            Pagos[rowIndex].Monto = monto;
        }
        else
        {
            Pagos.Add(new FacturaPago
            {
                Id = Guid.NewGuid(),
                FacturaId = _facturaId,
                FormaPagoId = formaPagoId,
                Monto = monto
            });
        }
        
        _grid.InvalidateRow(rowIndex);
        ActualizarTitulo();
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        var nuevoPago = new FacturaPago
        {
            Id = Guid.NewGuid(),
            FacturaId = _facturaId,
            FormaPagoId = Guid.Empty,
            Monto = 0m
        };
        
        Pagos.Add(nuevoPago);
        
        int rowIndex = _grid.Rows.Add();
        var row = _grid.Rows[rowIndex];
        row.Cells["Monto"].Value = 0;
        
        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            var lastIndex = _grid.Rows.Count - 1;
            _grid.Rows[lastIndex].Selected = true;
            _grid.CurrentCell = _grid.Rows[lastIndex].Cells[0];
            _grid.BeginEdit(true);
        }
        
        ActualizarTitulo();
    }

    private void BtnDel_Click(object sender, EventArgs e)
    {
        if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0)
        {
            int rowIndex = _grid.CurrentRow.Index;
            
            if (MessageBox.Show("¿Eliminar este pago?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (rowIndex < Pagos.Count)
                {
                    Pagos.RemoveAt(rowIndex);
                }
                
                _grid.Rows.RemoveAt(rowIndex);
                ActualizarTitulo();
            }
        }
    }
}