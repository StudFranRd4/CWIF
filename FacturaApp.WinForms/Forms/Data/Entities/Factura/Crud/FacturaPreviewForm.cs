using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FacturaApp.Core.Models;

namespace FacturaApp.WinForms.Forms
{
    public class FacturaPreviewForm : Form
    {
        private Factura _factura;
        private Empresa _empresa;
        private int _numeroFactura;
        private RichTextBox _rtbPreview;
        private Button _btnImprimir;
        private Button _btnVistaPrevia;
        private Button _btnCerrar;
        private PrintDocument _printDocument;
        private PrintPreviewDialog _printPreviewDialog;

        public FacturaPreviewForm(Factura factura, Empresa empresa, int numeroFactura)
        {
            _factura = factura;
            _empresa = empresa;
            _numeroFactura = numeroFactura;
            InitializeComponent();
            CargarDatosFactura();
        }

        private void InitializeComponent()
        {
            this.Text = "Factura: impresion";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // Panel principal
            var panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // RichTextBox para la vista previa
            _rtbPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Courier New", 10),
                BackColor = Color.White,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Panel de botones
            var panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10, 5, 10, 5)
            };

            _btnImprimir = new Button
            {
                Text = "&Imprimir",
                Width = 100,
                Height = 30,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            _btnImprimir.Click += BtnImprimir_Click;

            _btnVistaPrevia = new Button
            {
                Text = "&Vista Previa",
                Width = 110,
                Height = 30,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            _btnVistaPrevia.Click += BtnVistaPrevia_Click;

            _btnCerrar = new Button
            {
                Text = "&Cerrar",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };
            _btnCerrar.Click += (s, e) => this.Close();

            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false
            };

            flowLayout.Controls.Add(_btnCerrar);
            flowLayout.Controls.Add(_btnVistaPrevia);
            flowLayout.Controls.Add(_btnImprimir);
            panelBotones.Controls.Add(flowLayout);

            panelPrincipal.Controls.Add(_rtbPreview);
            this.Controls.Add(panelPrincipal);
            this.Controls.Add(panelBotones);

            // Configurar impresión
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += PrintDocument_PrintPage;

            _printPreviewDialog = new PrintPreviewDialog
            {
                Document = _printDocument,
                WindowState = FormWindowState.Maximized,
                Text = "Vista Previa de Impresión"
            };
        }

        private void CargarDatosFactura()
        {
            string contenido = GenerarContenidoFactura();
            _rtbPreview.Text = contenido;
        }

        private string GenerarContenidoFactura()
        {
            string numeroFacturaFormateado = _numeroFactura.ToString("D4");

            // Obtener la configuración de fecha local
            bool usarFormato24Horas = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("H");
            string formatoFecha = usarFormato24Horas ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy hh:mm tt";
            string fechaFactura = _factura.Fecha.ToString(formatoFecha, CultureInfo.CurrentCulture);

            // Datos del cliente
            string clienteNombre = _factura.Cliente?.Nombre ?? "N/A";
            string clienteTelefono = !string.IsNullOrWhiteSpace(_factura.Cliente?.Telefono)
                ? $"Tel: {_factura.Cliente.Telefono}"
                : "";
            string clienteDireccion = !string.IsNullOrWhiteSpace(_factura.Cliente?.Direccion)
                ? $"Dir: {_factura.Cliente.Direccion}"
                : "";

            // Datos de la empresa
            string empresaNombre = _empresa?.Nombre ?? "<sin empresa>";
            string empresaRnc = !string.IsNullOrWhiteSpace(_empresa?.Rnc)
                ? $"RNC: {_empresa.Rnc}"
                : "";
            string empresaDireccion = !string.IsNullOrWhiteSpace(_empresa?.Direccion)
                ? $"Dirección: {_empresa.Direccion}"
                : "";

            // Formatear cantidades monetarias con formato local
            string subtotalFormateado = _factura.Subtotal.ToString("C2", CultureInfo.CurrentCulture);
            string totalFormateado = _factura.Total.ToString("C2", CultureInfo.CurrentCulture);

            // Calcular el ancho para alinear columnas
            int anchoNumero = 15; // Ancho para los números
            int anchoSubtotal = Math.Max(subtotalFormateado.Length, 10);
            int anchoTotal = Math.Max(totalFormateado.Length, 10);
            int anchoMaximo = Math.Max(anchoSubtotal, anchoTotal);

            return $@"
=============================================
               {empresaNombre.ToUpper()}
=============================================

{empresaRnc}
{empresaDireccion}

=============================================

Fecha: {fechaFactura}
No. {numeroFacturaFormateado}

=============================================

Cliente: {clienteNombre}
{(!string.IsNullOrEmpty(clienteTelefono) ? $"Teléfono: {clienteTelefono.Replace("Tel: ", "")}\n" : "")}{(!string.IsNullOrEmpty(clienteDireccion) ? $"Dirección: {clienteDireccion.Replace("Dir: ", "")}" : "")}

=============================================
DESCRIPCIÓN                      CANT   PRECIO
=============================================

{GenerarDetalles()}
=============================================

Subtotal:                    {subtotalFormateado.PadLeft(anchoMaximo)}
Total:                       {totalFormateado.PadLeft(anchoMaximo)}

=============================================
            ¡GRACIAS POR SU COMPRA! :)
=============================================
";
        }

        private string GenerarDetalles()
        {
            if (_factura.Detalles == null || !_factura.Detalles.Any())
                return "No hay detalles".PadLeft(20) + "\n";

            string detalles = "";
            foreach (var detalle in _factura.Detalles)
            {
                string nombre = detalle.Producto?.Nombre ?? "Producto N/A";
                if (nombre.Length > 30) nombre = nombre.Substring(0, 27) + "...";

                // Solo cantidad y precio, sin importe (como se solicita)
                string precioFormateado = detalle.Precio.ToString("C2", CultureInfo.CurrentCulture);
                detalles += $"{nombre,-30} {detalle.Cantidad,5} {precioFormateado,10}\n";
            }
            return detalles;
        }

        private void BtnVistaPrevia_Click(object sender, EventArgs e)
        {
            try
            {
                _printPreviewDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar vista previa: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog
                {
                    Document = _printDocument,
                    AllowSomePages = true,
                    AllowSelection = true,
                    UseEXDialog = true // Usar el diálogo de impresión moderno
                };

                // Configurar página por defecto
                PageSetupDialog pageSetupDialog = new PageSetupDialog
                {
                    Document = _printDocument,
                    EnableMetric = true // Usar métricas (milímetros)
                };

                var menuImpresion = new ContextMenuStrip();
                menuImpresion.Items.Add("Configurar página...", null, (s, args) =>
                {
                    if (pageSetupDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _printDocument.DefaultPageSettings = pageSetupDialog.PageSettings;
                    }
                });

                menuImpresion.Items.Add("Imprimir...", SystemIcons.Hand.ToBitmap(), (s, args) =>
                {
                    if (printDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _printDocument.Print();
                    }
                });

                // Mostrar menú contextual
                menuImpresion.Show(_btnImprimir, new Point(0, _btnImprimir.Height));
            }
            catch (InvalidPrinterException)
            {
                MessageBox.Show("No se encontró ninguna impresora configurada. Por favor, configure una impresora en el sistema.",
                    "Error de Impresión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Configurar la fuente para impresión
            Font font = new Font("Courier New", 10);
            Font fontTitulo = new Font("Courier New", 12, FontStyle.Bold);
            float yPos = e.MarginBounds.Top;
            float leftMargin = e.MarginBounds.Left;
            float rightMargin = e.MarginBounds.Right;
            float anchoDisponible = rightMargin - leftMargin;

            // Imprimir cada línea del contenido
            string[] lines = _rtbPreview.Text.Split('\n');

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    yPos += font.GetHeight() / 2; // Espacio más pequeño para líneas vacías
                    continue;
                }

                // Medir el texto
                SizeF textSize = e.Graphics.MeasureString(line.TrimEnd(), font);

                // Determinar si es una línea que debe centrarse
                bool esLineaCentrada = line.Contains("FACTURA #") ||
                                      line.Contains(_empresa?.Nombre?.ToUpper() ?? "") ||
                                      line.Contains("¡GRACIAS POR SU COMPRA!") ||
                                      line.Contains("=========");

                // Usar fuente más grande para títulos
                Font fontActual = font;
                if (line.Contains("FACTURA #") || line.Contains(_empresa?.Nombre?.ToUpper() ?? ""))
                {
                    fontActual = fontTitulo;
                    textSize = e.Graphics.MeasureString(line.TrimEnd(), fontTitulo);
                }

                if (esLineaCentrada && !string.IsNullOrWhiteSpace(line.Trim()))
                {
                    float xPos = leftMargin + (anchoDisponible - textSize.Width) / 2;
                    e.Graphics.DrawString(line.TrimEnd(), fontActual, Brushes.Black, xPos, yPos);
                }
                else
                {
                    e.Graphics.DrawString(line, fontActual, Brushes.Black, leftMargin, yPos);
                }

                yPos += fontActual.GetHeight();

                // Verificar si hay más espacio en la página
                if (yPos >= e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;
        }
    }
}