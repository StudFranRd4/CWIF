using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using FacturaApp.Core.Models;
using Microsoft.Extensions.Logging;
using FacturaApp.Services;

namespace FacturaApp.WinForms.Forms
{
    public class GenericQueryForm<T> : Form where T : BaseEntity, new()
    {
        private readonly IRepository<T> _repo;
        private readonly ILogger<GenericQueryForm<T>> _logger;

        private readonly BindingList<T> _list = new();
        private readonly DataGridView _grid = new();

        public GenericQueryForm(IServiceProvider provider, IRepository<T> repo,
                                ILogger<GenericQueryForm<T>> logger)
        {
            _repo = repo;
            _logger = logger;

            Text = $"Consulta: {typeof(T).Name}";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            ConfigureGrid();
            Controls.Add(_grid);

            Load += async (s, e) => await LoadDataAsync();
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = true;

            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _grid.RowHeadersVisible = false;

            _grid.EnableHeadersVisualStyles = false;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _list.Clear();
                var items = await _repo.GetAllAsync();
                foreach (var it in items)
                    _list.Add(it);

                _grid.DataSource = _list;

                if(_grid.Columns.Contains("Id"))
                _grid.Columns["Id"].Visible = false;
			
			    if(_grid.Columns.Contains("RowVersion"))
                _grid.Columns["RowVersion"].Visible = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando datos");
            }
        }
    }
}