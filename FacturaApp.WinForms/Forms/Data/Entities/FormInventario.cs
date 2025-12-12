using System;
using System.Windows.Forms;
using FacturaApp.Core.Models;
using FacturaApp.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FacturaApp.WinForms.Forms
{
    public class FormInventario : GenericCrudForm<InventarioMovimiento>
    {
        public FormInventario(
            IServiceProvider provider,
            IRepository<InventarioMovimiento> repo,
            ILogger<FormInventario> logger)
            : base(provider, repo, logger)
        {
            Text = "Control de inventario";
        }

        protected override async Task NewAsync()
        {
            var editor = new DynamicEntityEditor<InventarioMovimiento>(_provider);

            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                await _repo.CreateAsync(editor.Entity);
                await LoadDataAsync();
            }
        }

        protected override async Task EditAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is InventarioMovimiento selected)
            {
                var editor = new DynamicEntityEditor<InventarioMovimiento>(_provider, selected);

                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    await _repo.UpdateAsync(editor.Entity);
                    await LoadDataAsync();
                }
            }
        }
    }
}