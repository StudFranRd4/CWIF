using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FacturaApp.Core.Models;
using FacturaApp.Services;
using FacturaApp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace FacturaApp.WinForms.Forms
{
    public class FormFacturacion : GenericCrudForm<Factura>
    {
        private BillingDbContext _dbContext;
        private Button _btnImprimir;
        private FlowLayoutPanel _toolbarFlowPanel;

        public FormFacturacion(IServiceProvider provider, IRepository<Factura> repo,
                               ILogger<FormFacturacion> logger, BillingDbContext context)
            : base(provider, repo, logger)
        {
            _dbContext = context;
        }

        protected override void ConfigureGrid()
        {
            base.ConfigureGrid();

            if (_grid.Columns.Contains("Subtotal"))
            {
                _grid.Columns["Subtotal"].DefaultCellStyle.Format = "C2";
                _grid.Columns["Subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _grid.Columns["Subtotal"].Width = 100;
            }

            if (_grid.Columns.Contains("Total"))
            {
                _grid.Columns["Total"].DefaultCellStyle.Format = "C2";
                _grid.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                _grid.Columns["Total"].Width = 100;
                _grid.Columns["Total"].DefaultCellStyle.Font = new System.Drawing.Font(_grid.Font, System.Drawing.FontStyle.Bold);
            }

            // Asegurar que la columna del cliente tenga el nombre correcto
            if (_grid.Columns.Contains("Cliente_Nombre"))
            {
                _grid.Columns["Cliente_Nombre"].HeaderText = "Cliente";
                _grid.Columns["Cliente_Nombre"].Width = 150;
            }

            if (_grid.Columns.Contains("Fecha"))
            {
                _grid.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                _grid.Columns["Fecha"].Width = 120;
            }
        }

        protected override void InitializeLayout()
        {
            base.InitializeLayout();

            // Encontrar el FlowLayoutPanel de botones para agregar el botón de impresión
            FindToolbarPanel();
            AddPrintButton();
        }

        private void FindToolbarPanel()
        {
            // Buscar el FlowLayoutPanel que contiene los botones
            _toolbarFlowPanel = FindControl<FlowLayoutPanel>(this);
            if (_toolbarFlowPanel == null)
            {
                // Si no se encuentra, creamos nuestro propio panel
                CreateToolbarPanel();
            }
        }

        private T FindControl<T>(Control parent) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control is T foundControl)
                {
                    return foundControl;
                }

                var childResult = FindControl<T>(control);
                if (childResult != null)
                {
                    return childResult;
                }
            }
            return null;
        }

        private void CreateToolbarPanel()
        {
            // Buscar el panel superior
            var topPanel = FindControl<Panel>(this);
            if (topPanel != null)
            {
                _toolbarFlowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    WrapContents = false
                };

                // Mover los botones existentes al nuevo FlowLayoutPanel
                var existingButtons = topPanel.Controls.OfType<Button>().ToList();
                foreach (var button in existingButtons)
                {
                    topPanel.Controls.Remove(button);
                    _toolbarFlowPanel.Controls.Add(button);
                }

                topPanel.Controls.Add(_toolbarFlowPanel);
            }
        }

        private void AddPrintButton()
        {
            if (_toolbarFlowPanel == null) return;

            _btnImprimir = new Button
            {
                Text = "Imprimir",
                Width = 90,
                Height = 30,
                Enabled = false,
                // Image = GetPrintIcon(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };

            _btnImprimir.Click += BtnImprimir_Click;
            _toolbarFlowPanel.Controls.Add(_btnImprimir);

            // Actualizar la selección inicial
            UpdatePrintButtonState();
        }

        private void UpdatePrintButtonState()
        {
            _btnImprimir.Enabled = _grid.CurrentRow != null && _grid.CurrentRow.DataBoundItem is Factura;
        }

        private async void BtnImprimir_Click(object sender, EventArgs e)
        {
            await ImprimirFacturaAsync();
        }

        private async Task ImprimirFacturaAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Factura selected) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                // Cargar la factura completa con todas sus relaciones
                var facturaCompleta = await _dbContext.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(f => f.Id == selected.Id);

                if (facturaCompleta == null)
                {
                    MessageBox.Show("No se pudo cargar la factura para imprimir.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Obtener la empresa emisora (primera empresa de la base de datos)
                var empresa = await _dbContext.Empresas
                    .FirstOrDefaultAsync();

                if (empresa == null)
                {
                    MessageBox.Show("No se ha configurado una empresa emisora. Por favor, configure una empresa primero.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Calcular el número correlativo de la factura
                // Buscamos todas las facturas ordenadas por fecha para determinar la posición
                var todasFacturas = await _dbContext.Facturas
                    .OrderBy(f => f.Fecha)
                    .Select(f => f.Id)
                    .ToListAsync();

                int numeroFactura = 1; // Si es la primera factura
                if (todasFacturas.Count > 0)
                {
                    // Encontrar la posición de esta factura en la lista ordenada
                    var index = todasFacturas.IndexOf(selected.Id);
                    numeroFactura = index + 1; // +1 para que empiece en 1 en lugar de 0
                }

                // Crear y mostrar el formulario de vista previa
                using (var previewForm = new FacturaPreviewForm(facturaCompleta, empresa, numeroFactura))
                {
                    previewForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la factura para impresión: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        protected override async Task NewAsync()
        {
            try
            {
                // Limpiar el ChangeTracker para evitar conflictos
                if (_dbContext != null)
                {
                    _dbContext.ChangeTracker.Clear();
                }

                var factura = new Factura
                {
                    Id = Guid.NewGuid(),
                    Fecha = DateTime.Now,
                    Subtotal = 0,
                    Total = 0,
                    Detalles = new System.Collections.Generic.List<FacturaDetalle>(),
                    Pagos = new System.Collections.Generic.List<FacturaPago>()
                };

                var editor = new FacturaEditorForm(_provider, factura, true);

                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        // Obtener la entidad actualizada del editor
                        var facturaActualizada = editor.FacturaActualizada;

                        // Guardar usando el repositorio
                        await _repo.CreateAsync(facturaActualizada);

                        // Forzar recarga desde la base de datos
                        await LoadDataAsync();
                        RefreshGridImmediately();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear factura: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override async Task EditAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Factura selected) return;

            try
            {
                // Limpiar el ChangeTracker para evitar conflictos
                if (_dbContext != null)
                {
                    _dbContext.ChangeTracker.Clear();
                }

                // Cargar la factura desde la base de datos SIN AsNoTracking
                var facturaExistente = await _dbContext.Facturas
                    .Include(f => f.Detalles)
                    .Include(f => f.Pagos)
                    .FirstOrDefaultAsync(f => f.Id == selected.Id);

                if (facturaExistente == null)
                {
                    MessageBox.Show("La factura ya no existe.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var editor = new FacturaEditorForm(_provider, facturaExistente, false);

                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        // Obtener la entidad actualizada del editor
                        var facturaActualizada = editor.FacturaActualizada;

                        // Guardar usando el repositorio
                        await _repo.UpdateAsync(facturaActualizada);

                        // Forzar recarga desde la base de datos
                        await LoadDataAsync();
                        RefreshGridImmediately();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al actualizar: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGridImmediately()
        {
            _grid.Refresh();
            _grid.Update();
            Application.DoEvents();

            // Actualizar estado del botón de impresión
            UpdatePrintButtonState();
        }

        protected override async Task DeleteAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Factura selected) return;

            if (MessageBox.Show("¿Está seguro de eliminar la factura seleccionada?",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _repo.DeleteAsync(selected.Id);
                    await LoadDataAsync();
                    RefreshGridImmediately();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Override para manejar cambios en la selección del grid
        protected override void OnGridSelectionChanged()
        {
            base.OnGridSelectionChanged();
            UpdatePrintButtonState();
        }
    }
}