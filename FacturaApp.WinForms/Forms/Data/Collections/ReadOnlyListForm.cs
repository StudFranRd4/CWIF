using System.Collections;
using System.ComponentModel;
using System.Reflection;

public class ReadOnlyListForm<TItem> : Form where TItem : class
{
    private readonly IList _list;
    private readonly DataGridView _grid = new();
    private readonly Dictionary<string, PropertyInfo> _navMap = new();

    public ReadOnlyListForm(IList list)
    {
        _list = list;
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        Initialize();
        Text = $"Listado de {typeof(TItem).Name}s";
    }

    private void Initialize()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = true;
        _grid.RowHeadersVisible = false;
        _grid.AutoGenerateColumns = false;

        // Ajuste automático COMPLETO
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

        _grid.CellFormatting += Grid_CellFormatting;

        Controls.Add(_grid);

        LoadData();
    }

    private void LoadData()
    {
        if (_list.Count == 0)
            return;

        var binding = new BindingList<TItem>(_list.Cast<TItem>().ToList());
        CreateColumns();
        _grid.DataSource = new BindingSource(binding, null);
    }

    private void CreateColumns()
    {
        var props = typeof(TItem).GetProperties();

        bool isFacturaDetalle = typeof(TItem).Name == "FacturaDetalle";

        foreach (var prop in props)
        {
            if (prop.Name == "RowVersion")
                continue;

            if (prop.Name.EndsWith("Id") && prop.Name != "Id")
                continue;

            if (isFacturaDetalle && prop.Name == "Id")
                continue;

            if (IsNavigation(prop))
            {
                _navMap[prop.Name] = prop;

                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = prop.Name,
                    HeaderText = prop.Name,

                    // IMPORTANTE: si usas CellFormatting,
                    // el DataPropertyName debe ser string.Empty
                    DataPropertyName = string.Empty
                });

                continue;
            }

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = prop.Name,
                HeaderText = prop.Name,
                DataPropertyName = prop.Name
            });
        }
    }

    private bool IsNavigation(PropertyInfo p)
    {
        var t = p.PropertyType;

        if (t == typeof(string))
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string))
            return false;

        if (t.IsClass && !t.IsValueType)
            return true;

        return false;
    }

    private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        var col = _grid.Columns[e.ColumnIndex];

        if (!_navMap.TryGetValue(col.Name, out var navProp))
            return;

        var rowObj = _grid.Rows[e.RowIndex].DataBoundItem;
        if (rowObj == null)
            return;

        var related = navProp.GetValue(rowObj);

        if (related == null)
        {
            e.Value = "";
            return;
        }

        e.Value = ResolveDisplay(related);
        e.FormattingApplied = true;
    }

    private string ResolveDisplay(object obj)
    {
        var type = obj.GetType();

        var pNombre = type.GetProperty("Nombre");
        if (pNombre != null)
        {
            var nombre = pNombre.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(nombre))
                return nombre;
        }

        var pId = type.GetProperty("Id");
        if (pId != null)
        {
            var id = pId.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return "[?]";
    }
}