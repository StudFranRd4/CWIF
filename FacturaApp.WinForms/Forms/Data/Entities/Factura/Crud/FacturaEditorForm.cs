using FacturaApp.Core.Models;
using FacturaApp.Data;
using FacturaApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

public class FacturaEditorForm : DynamicEntityEditor<Factura>
{
    private Button _btnDetalles;
    private Button _btnPagos;

    private Factura _facturaTrackeada;
    private IRepository<Producto> _productoRepo;
    private IRepository<FormaPago> _formaPagoRepo;
    private IRepository<Cliente> _clienteRepo;

    private List<Producto> _productosCargados;
    private List<FormaPago> _formasPagoCargadas;
    private List<Cliente> _clientesCargados;

    private Label _lblSubtotal;
    private Label _lblTotal;
    private Label _lblTotalValor;
    private Label _lblSubtotalValor;
    private Panel _panelTotales;
    private Label _lblTotalPagado;
    private Label _lblTotalPagadoValor;
    private Label _lblSaldoPendiente;
    private Label _lblSaldoPendienteValor;

    private bool _isNew = false;

    // El DbContext que efectivamente usas en varios métodos
    private readonly BillingDbContext _dbContext;

    // PROPIEDAD PÚBLICA para acceder a la entidad actualizada
    public Factura FacturaActualizada => _facturaTrackeada ?? Entity;

    public FacturaEditorForm(IServiceProvider provider, Factura factura, bool createNew)
        : base(provider, factura, autoSave: false)
    {
        _isNew = createNew;

        // Inicializar el DbContext (estaba siendo usado pero no declarado)
        _dbContext = provider.GetService(typeof(BillingDbContext)) as BillingDbContext
            ?? throw new InvalidOperationException("BillingDbContext no registrado");

        // Título dinámico según el modo
        Text = _isNew ? "Nueva Factura" : "Editar Factura";

        _productoRepo = _provider.GetService(typeof(IRepository<Producto>)) as IRepository<Producto>;
        _formaPagoRepo = _provider.GetService(typeof(IRepository<FormaPago>)) as IRepository<FormaPago>;
        _clienteRepo = _provider.GetService(typeof(IRepository<Cliente>)) as IRepository<Cliente>;

        CargarProductosSincrono();
        CargarFormasPagoSincrono();
        CargarClientesSincrono();

        // Si es nueva, asegurar colecciones y Id
        if (_isNew)
        {
            if (Entity.Id == Guid.Empty) Entity.Id = Guid.NewGuid();
            _facturaTrackeada = Entity;
            if (Entity.Detalles == null) Entity.Detalles = new List<FacturaDetalle>();
            if (Entity.Pagos == null) Entity.Pagos = new List<FacturaPago>();
        }
        else
        {
            CargarEntidadDesdeDb();
        }

        OcultarCamposCalculadosDeForm();
        AgregarPanelTotales();
        AgregarBotonesListas();
        RefrescarBotones();
        CalcularTotales();
    }

    private void CargarFormasPagoSincrono()
    {
        if (_formaPagoRepo != null)
        {
            try
            {
                var task = _formaPagoRepo.GetAllAsync();
                task.Wait();
                _formasPagoCargadas = task.Result.ToList();
            }
            catch (Exception ex)
            {
                _formasPagoCargadas = new List<FormaPago>();
                Console.WriteLine($"Error al cargar formas de pago: {ex.Message}");
            }
        }
        else
        {
            _formasPagoCargadas = new List<FormaPago>();
        }
    }

    private void CargarClientesSincrono()
    {
        if (_clienteRepo != null)
        {
            try
            {
                var task = _clienteRepo.GetAllAsync();
                task.Wait();
                _clientesCargados = task.Result.ToList();
            }
            catch (Exception ex)
            {
                _clientesCargados = new List<Cliente>();
                Console.WriteLine($"Error al cargar clientes: {ex.Message}");
            }
        }
        else
        {
            _clientesCargados = new List<Cliente>();
        }
    }

    private void AgregarPanelTotales()
    {
        _panelTotales = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 120,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            ColumnStyles = {
                new ColumnStyle(SizeType.Percent, 50F),
                new ColumnStyle(SizeType.Percent, 50F)
            },
            RowStyles = {
                new RowStyle(SizeType.Percent, 25F),
                new RowStyle(SizeType.Percent, 25F),
                new RowStyle(SizeType.Percent, 25F),
                new RowStyle(SizeType.Percent, 25F)
            }
        };

        _lblSubtotal = new Label
        {
            Text = "Subtotal:",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        _lblSubtotalValor = new Label
        {
            Text = "0.00",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        _lblTotal = new Label
        {
            Text = "Total Factura:",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        _lblTotalValor = new Label
        {
            Text = "0.00",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        _lblTotalPagado = new Label
        {
            Text = "Total Pagado:",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        _lblTotalPagadoValor = new Label
        {
            Text = "0.00",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        _lblSaldoPendiente = new Label
        {
            Text = "Saldo Pendiente:",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        _lblSaldoPendienteValor = new Label
        {
            Text = "0.00",
            Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = System.Drawing.Color.Red
        };

        layout.Controls.Add(_lblSubtotal, 0, 0);
        layout.Controls.Add(_lblSubtotalValor, 1, 0);
        layout.Controls.Add(_lblTotal, 0, 1);
        layout.Controls.Add(_lblTotalValor, 1, 1);
        layout.Controls.Add(_lblTotalPagado, 0, 2);
        layout.Controls.Add(_lblTotalPagadoValor, 1, 2);
        layout.Controls.Add(_lblSaldoPendiente, 0, 3);
        layout.Controls.Add(_lblSaldoPendienteValor, 1, 3);

        _panelTotales.Controls.Add(layout);
        Controls.Add(_panelTotales);
    }

    private void MostrarTotalesEnPanel()
    {
        // Calcular total pagado y saldo pendiente
        decimal totalPagado = 0;
        decimal saldoPendiente = Entity.Total;

        if (Entity.Pagos != null && Entity.Pagos.Any())
        {
            totalPagado = Entity.Pagos.Sum(p => p.Monto);
            saldoPendiente = Entity.Total - totalPagado;
        }

        _lblSubtotalValor.Text = Entity.Subtotal.ToString("C2");
        _lblTotalValor.Text = Entity.Total.ToString("C2");
        _lblTotalPagadoValor.Text = totalPagado.ToString("C2");
        _lblSaldoPendienteValor.Text = saldoPendiente.ToString("C2");

        // Cambiar color del saldo pendiente
        if (saldoPendiente > 0)
        {
            _lblSaldoPendienteValor.ForeColor = System.Drawing.Color.Red;
            _lblSaldoPendienteValor.Font = new System.Drawing.Font(
                _lblSaldoPendienteValor.Font,
                System.Drawing.FontStyle.Bold);
        }
        else if (saldoPendiente == 0)
        {
            _lblSaldoPendienteValor.ForeColor = System.Drawing.Color.Green;
            _lblSaldoPendienteValor.Font = new System.Drawing.Font(
                _lblSaldoPendienteValor.Font,
                System.Drawing.FontStyle.Regular);
        }
        else
        {
            // Si saldo es negativo (sobrepago)
            _lblSaldoPendienteValor.ForeColor = System.Drawing.Color.Orange;
            _lblSaldoPendienteValor.Font = new System.Drawing.Font(
                _lblSaldoPendienteValor.Font,
                System.Drawing.FontStyle.Regular);
        }
    }

    private void CargarProductosSincrono()
    {
        if (_productoRepo != null)
        {
            try
            {
                var task = _productoRepo.GetAllAsync();
                task.Wait();
                _productosCargados = task.Result.ToList();
            }
            catch (Exception ex)
            {
                _productosCargados = new List<Producto>();
                Console.WriteLine($"Error al cargar productos: {ex.Message}");
            }
        }
        else
        {
            _productosCargados = new List<Producto>();
        }
    }

    private void CargarEntidadDesdeDb()
    {
        if (_dbContext == null) return;

        Guid facturaId = Entity.Id;

        if (facturaId != Guid.Empty)
        {
            try
            {
                // Limpiar el ChangeTracker primero
                _dbContext.ChangeTracker.Clear();

                // Cargar la factura SIN TRACKING para evitar conflictos
                var facturaCargada = _dbContext.Facturas
                    .AsNoTracking()
                    .Include(f => f.Detalles)
                    .Include(f => f.Pagos)
                    .FirstOrDefault(f => f.Id == facturaId);

                if (facturaCargada != null)
                {
                    // Copiar propiedades básicas
                    Entity.Id = facturaCargada.Id;
                    Entity.ClienteId = facturaCargada.ClienteId;
                    Entity.Fecha = facturaCargada.Fecha;
                    Entity.Subtotal = facturaCargada.Subtotal;
                    Entity.Total = facturaCargada.Total;
                    Entity.RowVersion = facturaCargada.RowVersion;

                    // Para las colecciones, crear nuevas instancias para evitar conflictos de tracking
                    Entity.Detalles = facturaCargada.Detalles?.Select(d => new FacturaDetalle
                    {
                        Id = d.Id,
                        FacturaId = d.FacturaId,
                        ProductoId = d.ProductoId,
                        Cantidad = d.Cantidad,
                        Precio = d.Precio,
                        RowVersion = d.RowVersion
                    }).ToList() ?? new List<FacturaDetalle>();

                    Entity.Pagos = facturaCargada.Pagos?.Select(p => new FacturaPago
                    {
                        Id = p.Id,
                        FacturaId = p.FacturaId,
                        FormaPagoId = p.FormaPagoId,
                        Monto = p.Monto,
                        RowVersion = p.RowVersion
                    }).ToList() ?? new List<FacturaPago>();

                    // Ahora cargamos las entidades rastreadas para usar durante el guardado
                    _facturaTrackeada = _dbContext.Facturas
                        .FirstOrDefault(f => f.Id == facturaId);
                }
                else
                {
                    _facturaTrackeada = Entity;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la factura: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _facturaTrackeada = Entity;
            }
        }
        else
        {
            _facturaTrackeada = Entity;
        }
    }

    private void OcultarCamposCalculadosDeForm()
    {
        foreach (var p in typeof(Factura).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.Name == "Subtotal" || x.Name == "Total"))
        {
            if (_controls.ContainsKey(p))
            {
                var ctrl = _controls[p];
                ctrl.Visible = false;

                var parent = ctrl.Parent;
                if (parent != null)
                {
                    var lbl = parent.Controls.OfType<Label>().FirstOrDefault(x => x.Text == p.Name);
                    if (lbl != null) lbl.Visible = false;
                }
            }
        }
    }

    private void AgregarBotonesListas()
    {
        _btnDetalles = new Button
        {
            Text = "Editar Detalles (0)",
            Width = 180,
            Margin = new Padding(5)
        };
        _btnDetalles.Click += (_, _) => {
            AbrirDetalles();
            RefrescarBotones();
        };

        _btnPagos = new Button
        {
            Text = "Editar Pagos (0)",
            Width = 180,
            Margin = new Padding(5)
        };
        _btnPagos.Click += (_, _) => {
            AbrirPagos();
            RefrescarBotones();
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10)
        };

        panel.Controls.Add(_btnDetalles);
        panel.Controls.Add(_btnPagos);

        // Insertar el panel de botones encima del panel de totales
        panel.BringToFront();
        Controls.Add(panel);
    }

    private void RefrescarBotones()
    {
        _btnDetalles.Text = $"Editar Detalles ({Entity.Detalles?.Count ?? 0})";
        _btnPagos.Text = $"Editar Pagos ({Entity.Pagos?.Count ?? 0})";
    }

    protected override bool ValidateAndBind()
    {
        if (!base.ValidateAndBind())
            return false;

        // Validar que el cliente existe
        if (Entity.ClienteId == Guid.Empty)
        {
            MessageBox.Show("Debe seleccionar un cliente para la factura.",
                "Error de Validación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        // Verificar que el cliente existe en la base de datos
        var clienteExiste = _clientesCargados?.Any(c => c.Id == Entity.ClienteId) ?? false;
        if (!clienteExiste)
        {
            MessageBox.Show("El cliente seleccionado no existe en la base de datos.",
                "Error de Validación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    protected void AbrirDetalles()
    {
        var detallesList = Entity.Detalles?.ToList() ?? new List<FacturaDetalle>();

        var form = new FacturaDetallesEditor(_provider, detallesList, Entity.Id, _productosCargados);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            Entity.Detalles = form.Detalles;

            // Asegurar que los detalles tengan la referencia a la factura
            foreach (var detalle in Entity.Detalles)
            {
                detalle.FacturaId = Entity.Id;

                // Validar que el producto existe
                if (detalle.ProductoId != Guid.Empty)
                {
                    var productoExiste = _productosCargados?.Any(p => p.Id == detalle.ProductoId) ?? false;
                    if (!productoExiste)
                    {
                        MessageBox.Show($"El producto con ID {detalle.ProductoId} no existe en la base de datos.",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            CalcularTotales();
            RefrescarBotones();
        }
    }

    protected void AbrirPagos()
    {
        var pagosList = Entity.Pagos?.ToList() ?? new List<FacturaPago>();

        var form = new FacturaPagosForm(_provider, pagosList, Entity.Id);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            Entity.Pagos = form.Pagos;

            // Asegurar que los pagos tengan la referencia a la factura
            foreach (var pago in Entity.Pagos)
            {
                pago.FacturaId = Entity.Id;

                // Validar que la forma de pago existe
                if (pago.FormaPagoId != Guid.Empty)
                {
                    var formaPagoExiste = _formasPagoCargadas?.Any(fp => fp.Id == pago.FormaPagoId) ?? false;
                    if (!formaPagoExiste)
                    {
                        MessageBox.Show($"La forma de pago con ID {pago.FormaPagoId} no existe en la base de datos.",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            CalcularTotales();
            RefrescarBotones();
        }
    }

    private void CalcularTotales()
    {
        // Calcular subtotal basado en detalles
        if (Entity.Detalles != null && Entity.Detalles.Any())
        {
            Entity.Subtotal = Entity.Detalles.Sum(d => d.Importe);
        }
        else
        {
            Entity.Subtotal = 0;
        }

        Entity.Total = Entity.Subtotal;
        if (Entity.Total < 0) Entity.Total = 0;

        // Actualizar también la entidad rastreada si existe
        if (_facturaTrackeada != null)
        {
            _facturaTrackeada.Subtotal = Entity.Subtotal;
            _facturaTrackeada.Total = Entity.Total;
        }

        // Actualizar el panel de totales
        MostrarTotalesEnPanel();
    }

    protected override void SaveChanges()
    {
        // Mejor manejo: usar un nuevo DbContext para el guardado
        using (var dbContext = new BillingDbContext() )
        using (var tx = dbContext.Database.BeginTransaction())
        {
            try
            {
                // Recalcular totales antes de guardar
                CalcularTotales();

                // Validaciones
                if (!_clientesCargados.Any(c => c.Id == Entity.ClienteId))
                {
                    MessageBox.Show("El cliente seleccionado no existe en la base de datos.",
                        "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.None;
                    return;
                }

                // Validar productos en detalles
                foreach (var detalle in Entity.Detalles)
                {
                    if (!_productosCargados.Any(p => p.Id == detalle.ProductoId))
                    {
                        MessageBox.Show($"El producto con ID {detalle.ProductoId} no existe en la base de datos.",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DialogResult = DialogResult.None;
                        return;
                    }
                }

                // Validar formas de pago
                foreach (var pago in Entity.Pagos)
                {
                    if (!_formasPagoCargadas.Any(fp => fp.Id == pago.FormaPagoId))
                    {
                        MessageBox.Show($"La forma de pago con ID {pago.FormaPagoId} no existe en la base de datos.",
                            "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DialogResult = DialogResult.None;
                        return;
                    }
                }

                if (_isNew)
                {
                    // NUEVA FACTURA
                    if (Entity.Id == Guid.Empty) Entity.Id = Guid.NewGuid();

                    // Crear nueva entidad para evitar conflictos de tracking
                    var nuevaFactura = new Factura
                    {
                        Id = Entity.Id,
                        ClienteId = Entity.ClienteId,
                        Fecha = Entity.Fecha,
                        Subtotal = Entity.Subtotal,
                        Total = Entity.Total
                    };

                    dbContext.Facturas.Add(nuevaFactura);

                    // Agregar detalles
                    foreach (var detalle in Entity.Detalles)
                    {
                        detalle.Id = Guid.NewGuid();
                        detalle.FacturaId = nuevaFactura.Id;
                        dbContext.FacturaDetalles.Add(detalle);
                    }

                    // Agregar pagos
                    foreach (var pago in Entity.Pagos)
                    {
                        pago.Id = Guid.NewGuid();
                        pago.FacturaId = nuevaFactura.Id;
                        dbContext.FacturaPagos.Add(pago);
                    }

                    dbContext.SaveChanges();

                    // Cargar la entidad recién guardada para obtener el RowVersion
                    _facturaTrackeada = dbContext.Facturas
                        .AsNoTracking()
                        .FirstOrDefault(f => f.Id == nuevaFactura.Id);

                    if (_facturaTrackeada != null)
                    {
                        Entity.RowVersion = _facturaTrackeada.RowVersion;
                    }

                    tx.Commit();
                }
                else
                {
                    // FACTURA EXISTENTE
                    var facturaExistente = dbContext.Facturas
                        .FirstOrDefault(f => f.Id == Entity.Id);

                    if (facturaExistente == null)
                    {
                        MessageBox.Show("La factura no existe en la base de datos.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DialogResult = DialogResult.None;
                        return;
                    }

                    // Actualizar propiedades
                    facturaExistente.ClienteId = Entity.ClienteId;
                    facturaExistente.Fecha = Entity.Fecha;
                    facturaExistente.Subtotal = Entity.Subtotal;
                    facturaExistente.Total = Entity.Total;

                    // Eliminar detalles existentes
                    var detallesExistentes = dbContext.FacturaDetalles
                        .Where(d => d.FacturaId == Entity.Id)
                        .ToList();
                    dbContext.FacturaDetalles.RemoveRange(detallesExistentes);

                    // Agregar nuevos detalles
                    foreach (var detalle in Entity.Detalles)
                    {
                        var nuevoDetalle = new FacturaDetalle
                        {
                            Id = Guid.NewGuid(),
                            FacturaId = Entity.Id,
                            ProductoId = detalle.ProductoId,
                            Cantidad = detalle.Cantidad,
                            Precio = detalle.Precio
                        };
                        dbContext.FacturaDetalles.Add(nuevoDetalle);
                    }

                    // Eliminar pagos existentes
                    var pagosExistentes = dbContext.FacturaPagos
                        .Where(p => p.FacturaId == Entity.Id)
                        .ToList();
                    dbContext.FacturaPagos.RemoveRange(pagosExistentes);

                    // Agregar nuevos pagos
                    foreach (var pago in Entity.Pagos)
                    {
                        var nuevoPago = new FacturaPago
                        {
                            Id = Guid.NewGuid(),
                            FacturaId = Entity.Id,
                            FormaPagoId = pago.FormaPagoId,
                            Monto = pago.Monto
                        };
                        dbContext.FacturaPagos.Add(nuevoPago);
                    }

                    dbContext.SaveChanges();

                    // Actualizar entidad rastreada
                    _facturaTrackeada = dbContext.Facturas
                        .AsNoTracking()
                        .FirstOrDefault(f => f.Id == Entity.Id);

                    if (_facturaTrackeada != null)
                    {
                        Entity.RowVersion = _facturaTrackeada.RowVersion;
                    }

                    tx.Commit();
                }

                // Éxito
                DialogResult = DialogResult.OK;
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                tx.Rollback();
                MessageBox.Show("La factura ha sido modificada por otro usuario. Por favor, recargue la factura y vuelva a intentarlo.",
                    "Error de Concurrencia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }
            catch (DbUpdateException dbEx)
            {
                tx.Rollback();
                // Verificar si es un error de FK
                if (dbEx.InnerException?.Message?.Contains("FOREIGN KEY") == true)
                {
                    MessageBox.Show("Error de integridad referencial: Uno o más registros referenciados no existen en la base de datos.\n\n" +
                                  "Por favor, verifique que:\n" +
                                  "1. El cliente seleccionado existe\n" +
                                  "2. Los productos en los detalles existen\n" +
                                  "3. Las formas de pago en los pagos existen",
                        "Error de Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Error de base de datos: {dbEx.Message}\n\nDetalles: {dbEx.InnerException?.Message}",
                        "Error de Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                DialogResult = DialogResult.None;
                return;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show($"Error al guardar la factura: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }
        } // using tx
    }
}

// Agrega esta extensión si no la tienes para obtener las opciones del DbContext
public static class DbContextExtensions
{
    public static DbContextOptions<BillingDbContext> GetDbContextOptions(this BillingDbContext dbContext)
    {
        return ((IInfrastructure<IServiceProvider>)dbContext).GetService<DbContextOptions<BillingDbContext>>()
            ?? throw new InvalidOperationException("No se pudieron obtener las opciones del DbContext");
    }
}