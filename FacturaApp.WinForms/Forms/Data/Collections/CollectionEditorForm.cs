using FacturaApp.Core.Models;
using System.ComponentModel;
using System.Reflection;

public class CollectionEditorForm<TItem> : Form where TItem : class, new()
{
    private readonly IServiceProvider? _provider;
    private readonly Func<TItem>? _createNew;
    private BindingList<TItem> _bindingList;
    private DataGridView _grid;
    private IList<TItem> _resultList;

    public CollectionEditorForm(ICollection<TItem> list, IServiceProvider? provider = null,
                                Func<TItem>? createNew = null)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));

        _provider = provider;
        _createNew = createNew;

        // Crear una copia profunda para editar
        _resultList = DeepCopyList(list);
        _bindingList = new BindingList<TItem>(_resultList);

        Text = $"Editar colección: {typeof(TItem).Name}";
        Width = 800;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        Initialize();
    }

    public IList<TItem> ResultList => _resultList;

    private IList<TItem> DeepCopyList(ICollection<TItem> source)
    {
        var result = new List<TItem>();

        foreach (var item in source)
        {
            if (item is BaseEntity)
            {
                // Crear nueva instancia
                var newItem = new TItem();

                // Copiar todas las propiedades públicas que sean escriturables
                var properties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite);

                foreach (var prop in properties)
                {
                    // No copiar propiedades de navegación (colecciones o referencias a otras entidades)
                    if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) &&
                        !typeof(BaseEntity).IsAssignableFrom(prop.PropertyType) &&
                        prop.Name != "RowVersion")
                    {
                        var value = prop.GetValue(item);
                        prop.SetValue(newItem, value);
                    }
                }

                result.Add(newItem);
            }
            else
            {
                // Si no es BaseEntity, intentar clonar copia superficial
                var newItem = new TItem();
                var properties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite);
                foreach (var prop in properties)
                {
                    if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) &&
                        prop.Name != "RowVersion")
                    {
                        var value = prop.GetValue(item);
                        prop.SetValue(newItem, value);
                    }
                }
                result.Add(newItem);
            }
        }

        return result;
    }

    private void Initialize()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            DataSource = _bindingList,
            AllowUserToAddRows = false
        };

        ConfigureGridColumns();
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
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();

        // Configurar columnas basadas en propiedades simples
        var props = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                       !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) &&
                       !typeof(BaseEntity).IsAssignableFrom(p.PropertyType) &&
                       p.Name != "RowVersion" &&
                       p.Name != "Id" && !p.Name.EndsWith("Id"))
            .ToList();

        foreach (var prop in props)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = prop.Name,
                HeaderText = prop.Name,
                DataPropertyName = prop.Name,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            _grid.Columns.Add(col);
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        try
        {
            TItem newItem;

            if (_createNew != null)
            {
                newItem = _createNew();
            }
            else
            {
                newItem = new TItem();
                if (newItem is BaseEntity baseEntity)
                {
                    baseEntity.Id = Guid.NewGuid();
                }
            }

            using (var editor = CreateDynamicEditor(newItem))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    _bindingList.Add(newItem);
                    _resultList.Add(newItem);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al agregar: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not TItem selected)
        {
            MessageBox.Show("Seleccione un elemento para editar", "Advertencia",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using (var editor = CreateDynamicEditor(selected))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    // Actualizar el item en la lista de resultados
                    int index = _resultList.IndexOf(selected);
                    if (index >= 0)
                    {
                        _resultList[index] = selected;
                    }

                    _bindingList.ResetBindings();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al editar: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDel_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is not TItem selected) return;

        if (MessageBox.Show("¿Eliminar elemento?", "Confirmar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _bindingList.Remove(selected);
            _resultList.Remove(selected);
        }
    }

    public Func<TItem, Form> OverrideEditorCreation;

private Form CreateDynamicEditor(TItem entity)
{
    if (OverrideEditorCreation != null)
    {
        return OverrideEditorCreation(entity);
    }
    
    var editorType = typeof(DynamicEntityEditor<>).MakeGenericType(typeof(TItem));
    // Pasar explícitamente los 3 parámetros (provider, entity, autoSave)
    return (Form)Activator.CreateInstance(editorType, _provider!, entity, false)!;
}

}