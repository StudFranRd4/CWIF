using FacturaApp.Core.Models;
using FacturaApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

public class FacturaDetallesEditor : Form
{
    private readonly IServiceProvider _provider;
    private readonly Guid _facturaId;
    private BindingList<FacturaDetalle> _bindingList;
    private DataGridView _grid;
    private List<Producto> _productos;
    
    public List<FacturaDetalle> Detalles { get; private set; }

    public FacturaDetallesEditor(IServiceProvider provider, List<FacturaDetalle> detalles,
                                  Guid facturaId, List<Producto> productos)
    {
        _provider = provider;
        _facturaId = facturaId;
        _productos = productos ?? new List<Producto>();
        
        // Clonar los detalles para preservar las cantidades originales
        Detalles = new List<FacturaDetalle>();
        foreach (var detalle in detalles)
        {
            Detalles.Add(new FacturaDetalle
            {
                Id = detalle.Id,
                FacturaId = detalle.FacturaId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad, // Preservar la cantidad real
                Precio = detalle.Precio
            });
        }
        
        // Cargar precios para los detalles existentes
        CargarPreciosExistentes();
        
        Text = $"Editar Detalles de Factura - Total: {CalcularTotalDetalles(Detalles):C2}";
        Width = 900;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        
        Initialize();
    }

    private decimal CalcularTotalDetalles(List<FacturaDetalle> detalles)
    {
        return detalles.Sum(d => d.Importe);
    }

    private void CargarPreciosExistentes()
    {
        if (_productos == null || !_productos.Any()) return;
        
        foreach (var detalle in Detalles)
        {
            if (detalle.ProductoId != Guid.Empty)
            {
                var producto = _productos.FirstOrDefault(p => p.Id == detalle.ProductoId);
                if (producto != null && detalle.Precio == 0)
                {
                    detalle.Precio = producto.Precio;
                }
            }
        }
    }

    private void Initialize()
    {
        _bindingList = new BindingList<FacturaDetalle>(Detalles);
        
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
        var btnEdit = new Button { Text = "Editar", Width = 110 };
        var btnDel = new Button { Text = "Eliminar", Width = 110 };

        btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel, btnDel, btnEdit, btnAdd });

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDel.Click += BtnDel_Click;

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        ActualizarTitulo();
        ActualizarGrid();
    }

    private void ActualizarTitulo()
    {
        decimal total = Detalles.Sum(d => d.Importe);
        Text = $"Editar Detalles de Factura - Total: {total:C2}";
    }

    private void ConfigurarGrid()
    {
        _grid.Columns.Clear();
        
        // Columna Producto (combobox)
        var productoCol = new DataGridViewComboBoxColumn
        {
            Name = "ProductoId",
            HeaderText = "Producto",
            Width = 300,
            DisplayMember = "Nombre",
            ValueMember = "Id",
            FlatStyle = FlatStyle.Flat,
            DataSource = _productos
        };
        _grid.Columns.Add(productoCol);

        // Columna Cantidad
        var cantidadCol = new DataGridViewTextBoxColumn
        {
            Name = "Cantidad",
            HeaderText = "Cantidad",
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { 
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        };
        _grid.Columns.Add(cantidadCol);

        // Columna Precio (readonly)
        var precioCol = new DataGridViewTextBoxColumn
        {
            Name = "Precio",
            HeaderText = "Pcio. Unit.",
            Width = 150,
            ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle { 
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "C2"
            }
        };
        _grid.Columns.Add(precioCol);

        // Columna Importe (readonly, calculado)
        var importeCol = new DataGridViewTextBoxColumn
        {
            Name = "Importe",
            HeaderText = "Importe",
            Width = 150,
            ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle { 
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "C2"
            }
        };
        _grid.Columns.Add(importeCol);
        
        SuscribirEventosGrid();
    }

    private void ActualizarGrid()
    {
        _grid.Rows.Clear();
        
        foreach (var detalle in Detalles)
        {
            int rowIndex = _grid.Rows.Add();
            var row = _grid.Rows[rowIndex];
            
            // Asignar valores a las celdas - IMPORTANTE: Usar la cantidad real
            row.Cells["ProductoId"].Value = detalle.ProductoId;
            row.Cells["Cantidad"].Value = detalle.Cantidad;
            row.Cells["Precio"].Value = detalle.Precio;
            row.Cells["Importe"].Value = detalle.Importe;
        }
    }

    private void SuscribirEventosGrid()
    {
        _grid.EditingControlShowing += (s, e) =>
        {
            if (e.Control is ComboBox combo && _grid.CurrentCell.ColumnIndex == 0)
            {
                combo.SelectedIndexChanged -= Combo_SelectedIndexChanged;
                combo.SelectedIndexChanged += Combo_SelectedIndexChanged;
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
            if (e.ColumnIndex == 1 && e.RowIndex >= 0) // Columna Cantidad
            {
                var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    if (!int.TryParse(cell.Value.ToString(), out int cantidad) || cantidad <= 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show("La cantidad debe ser un número entero positivo.", "Error", 
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
            if (_grid.Columns[e.ColumnIndex].Name == "ProductoId" && e.Value != null)
            {
                if (Guid.TryParse(e.Value.ToString(), out Guid productoId) && productoId != Guid.Empty)
                {
                    var producto = _productos.FirstOrDefault(p => p.Id == productoId);
                    if (producto != null)
                    {
                        e.Value = producto.Nombre;
                        e.FormattingApplied = true;
                    }
                }
                else if (e.Value == null || e.Value.ToString() == Guid.Empty.ToString())
                {
                    e.Value = "Seleccione un producto";
                    e.FormattingApplied = true;
                }
            }
        }
    }

    private void Combo_SelectedIndexChanged(object sender, EventArgs e)
    {
        var combo = sender as ComboBox;
        if (combo != null && _grid.CurrentCell != null && _grid.CurrentCell.RowIndex >= 0)
        {
            var rowIndex = _grid.CurrentCell.RowIndex;
            var productoId = combo.SelectedValue as Guid?;
            
            if (productoId.HasValue && productoId.Value != Guid.Empty)
            {
                var producto = _productos.FirstOrDefault(p => p.Id == productoId.Value);
                if (producto != null)
                {
                    if (rowIndex < Detalles.Count)
                    {
                        var detalle = Detalles[rowIndex];
                        detalle.ProductoId = productoId.Value;
                        detalle.Precio = producto.Precio;
                        
                        _grid.Rows[rowIndex].Cells["Precio"].Value = detalle.Precio;
                        _grid.Rows[rowIndex].Cells["Importe"].Value = detalle.Importe;
                        
                        // NO sobreescribir la cantidad existente
                        // Solo actualizar el importe con la nueva cantidad y precio
                        _grid.InvalidateRow(rowIndex);
                    }
                }
            }
            else
            {
                if (rowIndex < Detalles.Count)
                {
                    var detalle = Detalles[rowIndex];
                    detalle.ProductoId = Guid.Empty;
                    detalle.Precio = 0;
                    _grid.Rows[rowIndex].Cells["Precio"].Value = 0;
                    _grid.Rows[rowIndex].Cells["Importe"].Value = 0;
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
        var productoIdCell = row.Cells["ProductoId"].Value;
        var cantidadCell = row.Cells["Cantidad"].Value;
        var precioCell = row.Cells["Precio"].Value;
        
        Guid productoId = Guid.Empty;
        if (productoIdCell != null && productoIdCell is Guid)
            productoId = (Guid)productoIdCell;
        else if (productoIdCell != null && Guid.TryParse(productoIdCell.ToString(), out Guid parsedId))
            productoId = parsedId;
        
        // OBTENER LA CANTIDAD CORRECTA - si no se puede parsear, mantener la existente
        int cantidad = 1;
        if (rowIndex < Detalles.Count)
        {
            cantidad = Detalles[rowIndex].Cantidad; // Mantener la cantidad existente por defecto
        }
        
        if (cantidadCell != null && int.TryParse(cantidadCell.ToString(), out int parsedCantidad))
        {
            cantidad = parsedCantidad; // Usar la nueva cantidad si se puede parsear
        }
        else if (cantidadCell != null && !string.IsNullOrEmpty(cantidadCell.ToString()))
        {
            // Si hay un valor pero no se puede parsear, mostrar error
            MessageBox.Show("La cantidad debe ser un número válido.", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            row.Cells["Cantidad"].Value = cantidad; // Restaurar valor anterior
        }
        
        // Validar que la cantidad sea positiva
        if (cantidad <= 0)
        {
            cantidad = 1;
            row.Cells["Cantidad"].Value = 1;
        }
        
        decimal precio = 0;
        if (precioCell != null && decimal.TryParse(precioCell.ToString(), out decimal parsedPrecio))
            precio = parsedPrecio;
        
        // Si hay producto seleccionado pero precio es 0, buscar precio actual
        if (productoId != Guid.Empty && precio == 0)
        {
            var producto = _productos.FirstOrDefault(p => p.Id == productoId);
            if (producto != null)
            {
                precio = producto.Precio;
                row.Cells["Precio"].Value = precio;
            }
        }
        
        // Calcular importe
        decimal importe = cantidad * precio;
        row.Cells["Importe"].Value = importe;
        
        // Actualizar o crear detalle en la lista
        if (rowIndex < Detalles.Count)
        {
            var detalle = Detalles[rowIndex];
            detalle.ProductoId = productoId;
            detalle.Cantidad = cantidad; // Guardar la cantidad correcta
            detalle.Precio = precio;
        }
        else
        {
            Detalles.Add(new FacturaDetalle
            {
                Id = Guid.NewGuid(),
                FacturaId = _facturaId,
                ProductoId = productoId,
                Cantidad = cantidad, // Guardar la cantidad correcta
                Precio = precio
            });
        }
        
        _grid.InvalidateRow(rowIndex);
        ActualizarTitulo();
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        var nuevoDetalle = new FacturaDetalle
        {
            Id = Guid.NewGuid(),
            FacturaId = _facturaId,
            Cantidad = 1,
            Precio = 0m
        };
        
        Detalles.Add(nuevoDetalle);
        
        int rowIndex = _grid.Rows.Add();
        var row = _grid.Rows[rowIndex];
        row.Cells["Cantidad"].Value = 1;
        row.Cells["Precio"].Value = 0;
        row.Cells["Importe"].Value = 0;
        
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

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_grid.CurrentRow != null)
        {
            _grid.BeginEdit(true);
        }
    }

    private void BtnDel_Click(object sender, EventArgs e)
    {
        if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0)
        {
            int rowIndex = _grid.CurrentRow.Index;
            
            if (MessageBox.Show("¿Eliminar este detalle?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (rowIndex < Detalles.Count)
                {
                    Detalles.RemoveAt(rowIndex);
                }
                
                _grid.Rows.RemoveAt(rowIndex);
                ActualizarTitulo();
            }
        }
    }
}