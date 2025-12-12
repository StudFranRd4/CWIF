using FacturaApp.Core.Models;
using FacturaApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    public partial class MainForm : Form
    {
        private readonly IServiceProvider _provider;

        public MainForm(IServiceProvider provider)
        {
            _provider = provider;

            InitializeComponent();

            InitializeMenu();
			ApplyMenuPermissions();
			
            InitializeBottomToolStrip();

            LoadDashboardControl();
        }

// Mapa fijo de permisos por roles
	
private static readonly Dictionary<UserRole, Dictionary<string, HashSet<string>>> Permisos = new()
{
	    
		{
        UserRole.Admin,
        new Dictionary<string, HashSet<string>>
        {
            { "Consultas", new HashSet<string> { "*" } },
            { "Mantenimientos", new HashSet<string> { "*" } },
            { "Inventario", new HashSet<string> { "*" } },
            { "Facturación", new HashSet<string> { "*" } },
            { "Empresas", new HashSet<string> { "*" } },
            { "Usuarios", new HashSet<string> { "*" } },
            { "Salir", null }
        }
    },
	
    {
        UserRole.Vendedor,
        new Dictionary<string, HashSet<string>>
        {
            { "Consultas", new HashSet<string> { "Clientes", "Ventas máster" } },
            { "Mantenimientos", new HashSet<string> { "Clientes" } },
            { "Facturación", new HashSet<string> { "Facturar" } },
            { "Salir", null }
        }
    },
    {
        UserRole.Inventario,
        new Dictionary<string, HashSet<string>>
        {
            { "Consultas", new HashSet<string> { "Compras máster", "Productos", "Inventario", "Proveedores" } },
            { "Mantenimientos", new HashSet<string> { "Proveedores", "Compras máster" } },
            { "Inventario", new HashSet<string> { "Productos", "Categorías", "Control de inventario" } },
            { "Salir", null }
        }
    },
    {
        UserRole.Contable,
        new Dictionary<string, HashSet<string>>
        {
            { "Consultas", new HashSet<string> { "Compras máster", "Ventas máster" } },
            { "Mantenimientos", new HashSet<string> { "Compras máster" } },
            { "Salir", null }
        }
    },
    {
        UserRole.Consulta,
        new Dictionary<string, HashSet<string>>
        {
            { "Consultas", new HashSet<string> { "*" } }, // Puede ver todo dentro
            { "Salir", null }
        }
    }
};

// Ocultar opciones segun rol

private void ApplyMenuPermissions()
{
    if (Session.CurrentUser == null)
        return;

    var rol = Session.CurrentUser.Rol;

    if (!Permisos.ContainsKey(rol))
        return;

    var permisosRol = Permisos[rol];

    foreach (ToolStripMenuItem padre in menu.Items)
    {
        bool visiblePadre = permisosRol.ContainsKey(padre.Text);

        padre.Visible = visiblePadre;

        if (!visiblePadre)
            continue;

        var subPermisos = permisosRol[padre.Text];

        // Si no tiene submenús o es un menú directo
        if (subPermisos == null)
            continue;

        foreach (ToolStripItem child in padre.DropDownItems)
        {
            if (subPermisos.Contains("*"))
            {
                child.Visible = true;
            }
            else
            {
                child.Visible = subPermisos.Contains(child.Text);
            }
        }
    }

    // Strip inferior (esto queda como lo tienes)
    foreach (ToolStripItem item in tareasStrip.Items)
    {
        if (rol != UserRole.Admin && item is ToolStripButton btn)
        {
            if (btn.Text == "Proyectos" || btn.Text == "Tareas")
                btn.Enabled = false;
        }
    }
}


        private void InitializeMenu()
        {
            // ------------------ CONSULTAS ------------------
            var menuConsultas = new ToolStripMenuItem("Consultas")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            menuConsultas.DropDownItems.Add(CreateQueryForm("Clientes", typeof(Cliente)));
            menuConsultas.DropDownItems.Add(CreateQueryForm("Compras máster", typeof(Compra)));
            menuConsultas.DropDownItems.Add(CreateQueryForm("Ventas máster", typeof(Factura)));
            menuConsultas.DropDownItems.Add(CreateQueryForm("Productos", typeof(Producto)));
            menuConsultas.DropDownItems.Add(CreateQueryForm("Inventario", typeof(InventarioMovimiento)));
            menuConsultas.DropDownItems.Add(CreateQueryForm("Proveedores", typeof(Proveedor)));
            menu.Items.Add(menuConsultas);

            // ------------------ MANTENIMIENTOS ------------------
            var menuMantenimientos = new ToolStripMenuItem("Mantenimientos")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            menuMantenimientos.DropDownItems.Add(CreateCrudForm("Clientes", typeof(Cliente)));
            menuMantenimientos.DropDownItems.Add(CreateCrudForm("Proveedores", typeof(Proveedor)));
            menuMantenimientos.DropDownItems.Add(CreateCompraFormMenu());
            menu.Items.Add(menuMantenimientos);

            // ------------------ INVENTARIO ------------------
            var menuInventario = new ToolStripMenuItem("Inventario")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            menuInventario.DropDownItems.Add(CreateCrudForm("Productos", typeof(Producto)));
            menuInventario.DropDownItems.Add(CreateCrudForm("Categorías", typeof(Categoria)));

            var subMenuControlInv = new ToolStripMenuItem("Control de inventario")
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(5)
            };
            subMenuControlInv.Click += MenuInventario_Click;

            menuInventario.DropDownItems.Add(subMenuControlInv);
            menu.Items.Add(menuInventario);

            // ------------------ FACTURACIÓN ------------------
            var menuFacturacion = new ToolStripMenuItem("Facturación")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            menuFacturacion.DropDownItems.Add(CreateCrudForm("Impuestos", typeof(Impuesto)));
            menuFacturacion.DropDownItems.Add(CreateCrudForm("Formas de pago", typeof(FormaPago)));

            var subMenuFacturar = new ToolStripMenuItem("Facturar")
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(5)
            };
            subMenuFacturar.Click += MenuFacturacion_Click;
            menuFacturacion.DropDownItems.Add(subMenuFacturar);

            menu.Items.Add(menuFacturacion);

            // ------------------ EMPRESAS ------------------
            var menuEmpresas = new ToolStripMenuItem("Empresas")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            menuEmpresas.DropDownItems.Add(CreateCrudForm("Sucursales", typeof(Sucursal)));
            menuEmpresas.DropDownItems.Add(CreateCrudForm("Gestionar negocios", typeof(Empresa)));
            menu.Items.Add(menuEmpresas);

            // ------------------ USUARIOS ------------------
            var menuUsuarios = new ToolStripMenuItem("Usuarios")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };
            menuUsuarios.Click += MenuUsuarios_Click;
            menu.Items.Add(menuUsuarios);

            // ------------------ SALIR ------------------
            var menuSalir = new ToolStripMenuItem("Salir")
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };
            menuSalir.Click += MenuSalir_Click;
            menu.Items.Add(menuSalir);
        }

        private void InitializeBottomToolStrip()
        {
            tareasStrip.Items.Add(new ToolStripLabel("  "));

            var btnProyectos = new ToolStripButton("Proyectos")
            {
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Padding = new Padding(15, 5, 15, 5)
            };
            btnProyectos.Click += (s, e) => OpenAsWindow(typeof(FormProyectos));
            tareasStrip.Items.Add(btnProyectos);
            tareasStrip.Items.Add(new ToolStripSeparator());

            var btnTareas = new ToolStripButton("Tareas")
            {
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Padding = new Padding(15, 5, 15, 5)
            };
            btnTareas.Click += (s, e) => OpenAsWindow(typeof(FormListaTareas));
            tareasStrip.Items.Add(btnTareas);
            tareasStrip.Items.Add(new ToolStripSeparator());

            var btnReportes = new ToolStripButton("Reportes")
            {
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Padding = new Padding(15, 5, 15, 5)
            };
            btnReportes.Click += (s, e) => OpenAsWindow(typeof(ReportesForm));
            tareasStrip.Items.Add(btnReportes);

            tareasStrip.Items.Add(new ToolStripLabel("  "));
        }

        // ==============================================
        // DASHBOARD
        // ==============================================
        private void LoadDashboardControl()
        {
            panelMain.Controls.Clear();
            dashboardWrapper = new Panel
            {
                BackColor = Color.White,
                AutoSize = false,
                Anchor = AnchorStyles.None
            };

            try
            {
                var dashboard = new DashboardTareasControl(_provider)
                {
                    Dock = DockStyle.Fill
                };

                int topPad = menu?.Height ?? 0;
                int bottomPad = tareasStrip?.Height ?? 45;

                panelMain.Padding = new Padding(20, topPad + 10, 20, bottomPad + 10);

                int maxWidth = Math.Max(300, panelMain.ClientSize.Width - 40);
                int maxHeight = Math.Max(200, panelMain.ClientSize.Height - topPad - bottomPad - 40);

                dashboardWrapper.Size = new Size(
                    Math.Min(1000, maxWidth),
                    Math.Min(700, maxHeight)
                );

                dashboardWrapper.Controls.Add(dashboard);
                panelMain.Controls.Add(dashboardWrapper);
                dashboardWrapper.PerformLayout();
                CenterDashboard();
            }
            catch (Exception ex)
            {
                var lblError = new Label
                {
                    Text = $"Error cargando dashboard: {ex.Message}",
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 10),
                    AutoSize = true,
                    Location = new Point(20, 20)
                };
                panelMain.Controls.Add(lblError);
            }
        }

        private void CenterDashboard()
        {
            if (dashboardWrapper == null)
                return;

            int topOffset = menu?.Height ?? 0;
            int bottomOffset = tareasStrip?.Height ?? 45;

            int usableHeight = panelMain.ClientSize.Height - topOffset - bottomOffset;

            dashboardWrapper.Left = Math.Max(0, (panelMain.ClientSize.Width - dashboardWrapper.Width) / 2);
            dashboardWrapper.Top = Math.Max(5, topOffset + Math.Max(0, (usableHeight - dashboardWrapper.Height) / 2));

            if (dashboardWrapper.Right > panelMain.ClientSize.Width)
                dashboardWrapper.Left = Math.Max(0, panelMain.ClientSize.Width - dashboardWrapper.Width - 10);

            if (dashboardWrapper.Bottom > panelMain.ClientSize.Height - bottomOffset)
                dashboardWrapper.Top = Math.Max(5, panelMain.ClientSize.Height - bottomOffset - dashboardWrapper.Height - 10);
        }

        // ==============================================
        // EVENTOS
        // ==============================================
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterDashboard();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            CenterDashboard();
        }

        // ==============================================
        // MENÚS ESPECÍFICOS
        // ==============================================
        private void MenuInventario_Click(object sender, EventArgs e)
        {
            var repo = _provider.GetService<IRepository<InventarioMovimiento>>();
            var logger = _provider.GetService<ILogger<FormInventario>>();

            if (repo == null || logger == null)
            {
                MessageBox.Show("No se pudo obtener el repositorio o logger de Inventario");
                return;
            }

            using var form = new FormInventario(_provider, repo, logger);
            form.ShowDialog();
        }

        private ToolStripMenuItem CreateCompraFormMenu()
        {
            var item = new ToolStripMenuItem("Compras máster")
            {
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(5)
            };

            item.Click += (s, e) =>
            {
                var repo = _provider.GetRequiredService<IRepository<Compra>>();
                var logger = _provider.GetRequiredService<ILogger<GenericCrudForm<Compra>>>();

                using var form = new CompraForm(_provider, repo, logger);
                form.ShowDialog(this);
            };

            return item;
        }

        private void MenuFacturacion_Click(object sender, EventArgs e)
        {
            using var form = ActivatorUtilities.CreateInstance<FormFacturacion>(_provider);
            form.ShowDialog(this);
        }

        private void MenuUsuarios_Click(object sender, EventArgs e)
        {
            var repo = _provider.GetService<IRepository<Usuario>>();

            if (repo == null)
            {
                MessageBox.Show("No se pudo obtener el repositorio de Usuarios");
                return;
            }

            using var form = new UserForm(repo);
            form.ShowDialog(this);
        }

        private void MenuSalir_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Seguro que deseas cerrar sesión?", "Confirmación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Session.CurrentUser = null;
                this.Close(); // Esto devuelve el control al bucle de Program.cs
            }
        }

        // ==============================================
        // FACTORÍAS
        // ==============================================
        private ToolStripMenuItem CreateCrudForm(string text, Type entityType)
        {
            var formType = typeof(GenericCrudForm<>).MakeGenericType(entityType);
            return CreateMenuItem(text, formType);
        }

        private ToolStripMenuItem CreateQueryForm(string text, Type entityType)
        {
            var formType = typeof(GenericQueryForm<>).MakeGenericType(entityType);
            return CreateMenuItem(text, formType);
        }

        private ToolStripMenuItem CreateMenuItem(string text, Type formType)
        {
            var item = new ToolStripMenuItem(text)
            {
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(5)
            };

            item.Click += (s, e) =>
            {
                using var form = (Form)ActivatorUtilities.CreateInstance(_provider, formType);
                form.ShowDialog(this);
            };

            return item;
        }

        private void OpenAsWindow(Type formType)
        {
            try
            {
                var form = (Form)ActivatorUtilities.CreateInstance(_provider, formType);
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo ventana: {ex.Message}");
            }
        }
    }
}
