using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Printing;
using FacturaApp.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace FacturaApp
{
    public partial class ReportesForm : Form
    {
        private readonly BillingDbContext _db;
        private int[] valores = Array.Empty<int>();
        private string[] etiquetas = Array.Empty<string>();

        public ReportesForm(BillingDbContext db)
        {
            _db = db;
            InitializeComponent();
            ConfigurarControles();
        }

        private void ConfigurarControles()
        {
            // Configurar gráficos

            barChart.BarColor = Color.SteelBlue;
    
            pieChart.ShowLabels = true;
            pieChart.ShowPercentages = true;
            
            // Configurar comboBox
            cmbTipo.Items.AddRange(new string[] {
                "Reporte de Compras",
                "Reporte de Ventas",
                "Productos Más Vendidos",
                "Clientes Más Fieles"
            });
            cmbTipo.SelectedIndex = 0;
            
            // Configurar fechas por defecto (último mes)
            dtHasta.Value = DateTime.Today;
            dtDesde.Value = DateTime.Today.AddMonths(-1);
        }

        private void btnGenerar_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime desde = dtDesde.Value.Date;
                DateTime hasta = dtHasta.Value.Date;

                if (desde > hasta)
                {
                    MessageBox.Show("La fecha 'Desde' no puede ser mayor a 'Hasta'", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                switch (cmbTipo.SelectedIndex)
                {
                    case 0:
                        CargarReporteCompras(desde, hasta);
                        break;
                    case 1:
                        CargarReporteVentas(desde, hasta);
                        break;
                    case 2:
                        CargarProductosMasVendidos(desde, hasta);
                        break;
                    case 3:
                        CargarClientesFieles(desde, hasta);
                        break;
                    default:
                        MessageBox.Show("Selecciona un tipo de reporte.", "Advertencia",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                }

                if (valores.Length == 0)
                {
                    MessageBox.Show("No hay datos para el período seleccionado", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Limpiar gráficos
                    barChart.Values = new int[] { 0 };
                    barChart.Labels = new string[] { "Sin datos" };
                    
                    pieChart.Values = new int[] { 0 };
                    pieChart.Labels = new string[] { "Sin datos" };
                    return;
                }

                // Actualizar gráficos
                barChart.Values = valores;
                barChart.Labels = etiquetas;
                barChart.Animate();

                pieChart.Values = valores;
                pieChart.Labels = etiquetas;
                pieChart.Animate();

                // Actualizar el DataGridView
                ActualizarDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarReporteCompras(DateTime desde, DateTime hasta)
        {
            var datos = _db.Compras
                .Where(x => x.Fecha.Date >= desde && x.Fecha.Date <= hasta)
                .Select(x => new 
                { 
                    x.Id, 
                    x.Fecha,
                    Cantidad = x.Detalles.Sum(d => d.Cantidad),
                    Total = x.Detalles.Sum(d => d.Cantidad * d.Precio)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            valores = datos.Select(x => (int)x.Total).ToArray();
            etiquetas = datos.Select(x => x.Fecha.ToString("dd/MM")).ToArray();
            
            // Almacenar datos para DataGrid
            dataGridView1.DataSource = datos.Select(x => new 
            {
                CompraID = x.Id,
                Fecha = x.Fecha.ToString("dd/MM/yyyy"),
                Cantidad = x.Cantidad,
                Total = x.Total.ToString("C")
            }).ToList();
        }

        private void CargarReporteVentas(DateTime desde, DateTime hasta)
        {
            var datos = _db.Facturas
                .Where(x => x.Fecha.Date >= desde && x.Fecha.Date <= hasta)
                .OrderBy(x => x.Fecha)
                .ToList();

            valores = datos.Select(x => (int)x.Total).ToArray();
            etiquetas = datos.Select(x => x.Fecha.ToString("dd/MM")).ToArray();

            dataGridView1.DataSource = datos.Select((x, idx) => new
            {
                FacturaID = x.Id,
                Numero = idx + 1,
                Fecha = x.Fecha.ToString("dd/MM/yyyy"),
                Cliente = x.Cliente != null ? x.Cliente.Nombre : "",
                Total = x.Total.ToString("C")
            }).ToList();
        }

        private void CargarProductosMasVendidos(DateTime desde, DateTime hasta)
        {
            var datos = _db.FacturaDetalles
                .Where(x => x.Factura.Fecha.Date >= desde && x.Factura.Fecha.Date <= hasta)
                .GroupBy(x => new { x.Producto.Id, x.Producto.Nombre })
                .Select(g => new 
                { 
                    g.Key.Id,
                    g.Key.Nombre,
                    Cantidad = g.Sum(c => c.Cantidad),
                    Total = g.Sum(c => c.Cantidad * c.Precio)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(10)
                .ToList();

            valores = datos.Select(x => x.Cantidad).ToArray();
            etiquetas = datos.Select(x => x.Nombre).ToArray();
            
            dataGridView1.DataSource = datos.Select(x => new 
            {
                ProductoID = x.Id,
                Nombre = x.Nombre,
                Cantidad = x.Cantidad,
                Total = x.Total.ToString("C")
            }).ToList();
        }

        private void CargarClientesFieles(DateTime desde, DateTime hasta)
        {
            var datos = _db.Facturas
                .Where(x => x.Fecha.Date >= desde && x.Fecha.Date <= hasta)
                .GroupBy(x => new { x.Cliente.Id, x.Cliente.Nombre })
                .Select(g => new 
                { 
                    g.Key.Id,
                    g.Key.Nombre,
                    Compras = g.Count(),
                    Total = g.Sum(f => f.Total)
                })
                .OrderByDescending(x => x.Compras)
                .Take(10)
                .ToList();

            valores = datos.Select(x => x.Compras).ToArray();
            etiquetas = datos.Select(x => x.Nombre).ToArray();
            
            dataGridView1.DataSource = datos.Select(x => new 
            {
                ClienteID = x.Id,
                Nombre = x.Nombre,
                Compras = x.Compras,
                Total = x.Total.ToString("C")
            }).ToList();
        }

        private void ActualizarDataGrid()
        {
            // Configurar DataGridView
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoResizeColumns();
            dataGridView1.Refresh();
        }

        private void btnExportarPDF_Click(object sender, EventArgs e)
        {
            try
            {
                if (valores.Length == 0)
                {
                    MessageBox.Show("No hay datos para exportar. Genere un reporte primero.", 
                        "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Reporte_{cmbTipo.SelectedItem}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                    Title = "Guardar Reporte PDF"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new PdfWriter(sfd.FileName))
                    using (var pdf = new PdfDocument(writer))
                    using (var doc = new Document(pdf, PageSize.A4.Rotate()))
                    {
                        // Título
                        doc.Add(new Paragraph("REPORTE DEL SISTEMA") );


PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                        doc.Add(new Paragraph("REPORTE DEL SISTEMA")
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                            .SetFontSize(16)
                            .SetFont(boldFont));
                        
                        doc.Add(new Paragraph($"Tipo: {cmbTipo.SelectedItem}")
                            .SetFontSize(12));
                        
                        doc.Add(new Paragraph($"Período: {dtDesde.Value:dd/MM/yyyy} - {dtHasta.Value:dd/MM/yyyy}")
                            .SetFontSize(12));

                        PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                        doc.Add(new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .SetFontSize(10)
                            .SetFont(italicFont));
                        
                        doc.Add(new Paragraph(" "));

                        // Tabla de datos
                        Table table = new Table(2);
                        table.AddHeaderCell("Ítem");
                        table.AddHeaderCell("Valor");

                        for (int i = 0; i < valores.Length && i < etiquetas.Length; i++)
                        {
                            table.AddCell(etiquetas[i]);
                            table.AddCell(valores[i].ToString());
                        }

                        doc.Add(table);
                    }

                    MessageBox.Show("PDF generado correctamente.", "Éxito", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar PDF: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImprimir_Click(object sender, EventArgs e)
        {
            try
            {
                if (valores.Length == 0)
                {
                    MessageBox.Show("No hay datos para imprimir. Genere un reporte primero.",
                        "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    float y = ev.MarginBounds.Top;
                    float leftMargin = ev.MarginBounds.Left;

                    // Encabezado
                    ev.Graphics.DrawString("REPORTE IMPRESO", 
                        new Font("Arial", 16, FontStyle.Bold), 
                        Brushes.Black, leftMargin, y);
                    y += 30;

                    ev.Graphics.DrawString($"Tipo: {cmbTipo.SelectedItem}", 
                        new Font("Arial", 11), Brushes.Black, leftMargin, y);
                    y += 20;

                    ev.Graphics.DrawString($"Período: {dtDesde.Value:dd/MM/yyyy} - {dtHasta.Value:dd/MM/yyyy}", 
                        new Font("Arial", 11), Brushes.Black, leftMargin, y);
                    y += 30;

                    // Datos
                    for (int i = 0; i < valores.Length; i++)
                    {
                        if (y > ev.MarginBounds.Bottom - 20)
                        {
                            ev.HasMorePages = true;
                            return;
                        }

                        ev.Graphics.DrawString($"{etiquetas[i]}: {valores[i]}",
                            new Font("Arial", 10), Brushes.Black, leftMargin, y);
                        y += 20;
                    }

                    ev.HasMorePages = false;
                };

                PrintDialog dlg = new PrintDialog 
                { 
                    Document = pd,
                    AllowSomePages = true
                };
                
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pd.Print();
                    MessageBox.Show("Reporte enviado a impresión.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}