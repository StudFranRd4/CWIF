using FacturaApp.Core.Models;
using FacturaApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    public class CompraForm : GenericCrudForm<Compra>
    {
        public CompraForm(IServiceProvider provider, IRepository<Compra> repo, ILogger<GenericCrudForm<Compra>> logger)
            : base(provider, repo, logger)
        {
            Text = "Gestión de Compras";
        }

        protected override async Task EditAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not Compra selected) return;

            try
            {
                var reloaded = await _repo.GetAsync(selected.Id);
                if (reloaded == null)
                {
                    MessageBox.Show("La compra ya no existe", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await LoadDataAsync();
                    return;
                }

                // Cargar relaciones
                TryLoadNavigationProperties(reloaded);
                
                // Si la compra tiene detalles, cargarlos
                if (_dbContext != null && reloaded.Detalles == null)
                {
                    await _dbContext.Entry(reloaded)
                        .Collection(c => c.Detalles)
                        .Query()
                        .Include(d => d.Producto)
                        .LoadAsync();
                }

                var editor = new CompraEditorForm(_provider, reloaded);
                
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    await _repo.UpdateAsync(editor.Compra);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex) when (ex.InnerException is DbUpdateConcurrencyException)
            {
                MessageBox.Show("Error de concurrencia. Los datos han sido modificados por otro usuario.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar compra");
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override async Task NewAsync()
        {
            var nuevaCompra = new Compra
            {
                Fecha = DateTime.Now
            };

            var editor = new CompraEditorForm(_provider, nuevaCompra);
            
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                if (editor.Compra.Id == Guid.Empty)
                    editor.Compra.Id = Guid.NewGuid();

                await _repo.CreateAsync(editor.Compra);
                await LoadDataAsync();
            }
        }

        protected new void ShowCollectionForEntity(Compra entity, PropertyInfo collectionProperty)
        {
            if (collectionProperty.Name == nameof(Compra.Detalles) && entity is Compra compra)
            {
                ShowDetallesCompra(compra);
            }
            else
            {
                base.ShowCollectionForEntity(entity, collectionProperty);
            }
        }

        private void ShowDetallesCompra(Compra compra)
        {
            try
            {
                if (_dbContext == null)
                {
                    MessageBox.Show("No se encontró el contexto de base de datos",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Cargar detalles si no están cargados
                if (compra.Detalles == null)
                {
                    _dbContext.Entry(compra)
                        .Collection(c => c.Detalles)
                        .Query()
                        .Include(d => d.Producto)
                        .Load();
                }

                var editor = new CollectionEditorForm<CompraDetalle>(
                    compra.Detalles ?? new List<CompraDetalle>(),
                    _provider,
                    () => new CompraDetalle { CompraId = compra.Id })
                {
                    OverrideEditorCreation = (detalle) => new CompraDetalleEditorForm(detalle, _provider)
                };

                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    // Actualizar la colección de detalles
                    compra.Detalles = editor.ResultList.ToList();
                    
                    // Recalcular totales si es necesario
                    RecalcularTotalesCompra(compra);
                    
                    // Guardar cambios
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _repo.UpdateAsync(compra);
                            await LoadDataAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al guardar detalles de compra");
                            Invoke(() => MessageBox.Show($"Error al guardar: {ex.Message}", 
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar detalles de compra");
                MessageBox.Show($"Error: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecalcularTotalesCompra(Compra compra)
        {
            // Esta función puede ser extendida para calcular totales si la entidad Compra tiene campos para ello
            if (compra.Detalles != null)
            {
                // Ejemplo: calcular total de la compra si la entidad tiene esa propiedad
                // compra.Total = compra.Detalles.Sum(d => d.Cantidad * d.Precio);
            }
        }
    }
}