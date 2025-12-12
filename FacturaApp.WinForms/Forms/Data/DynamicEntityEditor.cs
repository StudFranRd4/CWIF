using FacturaApp.Core.Models;
using FacturaApp.Data;
using FacturaApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

public class DynamicEntityEditor<T> : Form where T : class, new()
{
    public T Entity { get; private set; }
    protected IServiceProvider _provider;
    protected Dictionary<PropertyInfo, Control> _controls = new();
    protected BillingDbContext _dbContext;
    protected T _originalEntity;
    protected T TrackedEntity => _originalEntity;
    private readonly bool _autoSave;
    protected Dictionary<PropertyInfo, PropertyInfo> _fkToNav = new();
    protected Dictionary<PropertyInfo, List<BaseEntity>> _fkEntities = new();

public DynamicEntityEditor(IServiceProvider provider, T? entity = null, bool autoSave = false)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _dbContext = _provider.GetService(typeof(BillingDbContext)) as BillingDbContext;
        _autoSave = autoSave;

        if (entity == null)
        {
            Entity = new T();
            _originalEntity = Entity;
        }
        else
        {
            if (_dbContext != null)
            {
                var tracked = _dbContext.Set<T>().Local.FirstOrDefault(x => x == entity) ?? entity;
                _originalEntity = tracked;
                Entity = CloneForUi(tracked);
            }
            else
            {
                _originalEntity = entity;
                Entity = CloneForUi(entity);
            }
        }

        Text = entity == null ? "Nuevo: " + typeof(T).Name : "Editar: " + typeof(T).Name;
        Width = 700;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        BuildUi();
    }

private T CloneForUi(T source)
{
    var clone = new T();
    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var p in props.Where(p => p.CanWrite && p.CanRead))
    {
        // Evitar copiar objetos de navegación (BaseEntity)
        if (typeof(BaseEntity).IsAssignableFrom(p.PropertyType))
            continue;

        // Copiar FKs y valores simples
        var val = p.GetValue(source);
        p.SetValue(clone, val);
    }

    return clone;
}

private void BuildUi()
{
    var panel = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };
    Controls.Add(panel);

    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && p.Name != "Id" && p.Name != "RowVersion")
        .ToList();

    // FK COMBOS (prop termina en Id) - Primero construir todos los combos
    var fkProperties = props.Where(p => p.Name.EndsWith("Id")).ToList();
    var fkCombos = new List<ComboBox>();
    
    foreach (var fk in fkProperties)
    {
        string navName = fk.Name[..^2];
        var navProp = typeof(T).GetProperty(navName);
        if (navProp != null && typeof(BaseEntity).IsAssignableFrom(navProp.PropertyType))
        {
            var combo = CreateFkCombo(fk, navProp);
            panel.Controls.Add(new Label { Text = navName, Width = 650 });
            panel.Controls.Add(combo);
            _controls[fk] = combo;
            _fkToNav[fk] = navProp;
            fkCombos.Add(combo);
        }
    }

    // OTROS CAMPOS
    foreach (var p in props.Where(p =>
        !p.Name.EndsWith("Id") &&
        !(typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))))
    {
        if (typeof(BaseEntity).IsAssignableFrom(p.PropertyType)) continue;

        panel.Controls.Add(new Label { Text = p.Name, Width = 650 });
        var control = CreateControlForProperty(p);
        panel.Controls.Add(control);
        _controls[p] = control;
    }

    // Forzar actualización de los combos después de crear todos
    foreach (var combo in fkCombos)
    {
        combo.Refresh();
    }

    var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft };
    var ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
    var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel };
    btnPanel.Controls.AddRange(new Control[] { ok, cancel });
    Controls.Add(btnPanel);

    AcceptButton = ok;
    CancelButton = cancel;

    ok.Click += (s, e) =>
    {
        if (!ValidateAndBind())
            DialogResult = DialogResult.None;
        else
            SaveChanges();
    };
}

    private Control CreateControlForProperty(PropertyInfo p)
    {
        object? val = p.GetValue(Entity);
        var baseType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        if (baseType == typeof(bool))
            return new CheckBox { Checked = val != null && (bool)val, Width = 650 };

        if (baseType == typeof(DateTime))
            return new DateTimePicker { Width = 650, Value = val != null ? (DateTime)val : DateTime.Now };

        if (baseType == typeof(Guid))
            return new TextBox { Width = 650, Text = val?.ToString() ?? string.Empty, ReadOnly = true };

        if (baseType.IsEnum)
        {
            var cb = new ComboBox { Width = 650, DropDownStyle = ComboBoxStyle.DropDownList };
            cb.DataSource = Enum.GetValues(baseType);
            if (val != null) cb.SelectedItem = val;
            return cb;
        }

        if (baseType == typeof(int) || baseType == typeof(long) || baseType == typeof(short)
            || baseType == typeof(decimal) || baseType == typeof(double) || baseType == typeof(float))
        {
            var num = new NumericUpDown
            {
                Width = 650,
                Minimum = 0,
                Maximum = decimal.MaxValue,
                DecimalPlaces = 2
            };
            if (val != null)
            {
                try
                {
                    decimal decimalValue = Convert.ToDecimal(val);
                    if (decimalValue < num.Minimum) num.Value = num.Minimum;
                    else if (decimalValue > num.Maximum) num.Value = num.Maximum;
                    else num.Value = decimalValue;
                }
                catch
                {
                    num.Value = 0;
                }
            }
            return num;
        }

        return new TextBox { Width = 650, Text = val?.ToString() ?? string.Empty };
    }

// Create ComboBox for Foreign Key (BUG)

private ComboBox CreateFkCombo(PropertyInfo idProp, PropertyInfo navProp)
{
    var repoType = typeof(IRepository<>).MakeGenericType(navProp.PropertyType);
    var repoObj = _provider.GetService(repoType) ?? throw new Exception($"No existe repositorio para {navProp.PropertyType.Name}");

    var rawList = ((dynamic)repoObj).GetAllAsync().GetAwaiter().GetResult();
    var items = (rawList as IEnumerable)?.Cast<BaseEntity>().ToList() ?? new List<BaseEntity>();

    _fkEntities[idProp] = items;

    string displayProp =
        items.Any(i => i.GetType().GetProperty("Nombre") != null) ? "Nombre" :
        items.Any(i => i.GetType().GetProperty("Descripcion") != null) ? "Descripcion" :
        items.Any(i => i.GetType().GetProperty("NombreCompleto") != null) ? "NombreCompleto" :
        "Id";

    var combo = new ComboBox
    {
        Width = 650,
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = "Text",
        ValueMember = "Id"
    };

    // Añadir placeholder primero
    combo.Items.Add(new FkComboItem(Guid.Empty, "(Seleccionar)"));

    // Añadir todos los items
    foreach (var item in items)
    {
        var displayValue = item.GetType().GetProperty(displayProp)?.GetValue(item)?.ToString() ?? item.Id.ToString();
        combo.Items.Add(new FkComboItem(item.Id, displayValue));
    }

    // Obtener el Id REAL del objeto editado
    Guid realId = (Guid)(idProp.GetValue(Entity) ?? Guid.Empty);

    // FIX: Usar SelectedItem en lugar de SelectedIndex
    if (realId != Guid.Empty)
    {
        // Buscar el ítem por Id
        foreach (var item in combo.Items)
        {
            if (item is FkComboItem comboItem && comboItem.Id == realId)
            {
                combo.SelectedItem = item;
                break;
            }
        }
        
        // Si no se encontró, seleccionar placeholder
        if (combo.SelectedItem == null)
        {
            combo.SelectedIndex = 0;
        }
    }
    else
    {
        combo.SelectedIndex = 0; // Placeholder
    }

    combo.SelectedIndexChanged += (s, e) =>
    {
        if (combo.SelectedItem is FkComboItem selItem)
        {
            // Placeholder
            if (selItem.Id == Guid.Empty)
            {
                idProp.SetValue(Entity, null);
                if (_fkToNav.TryGetValue(idProp, out var nav))
                    nav.SetValue(Entity, null);
                return;
            }

            // FK normal
            idProp.SetValue(Entity, selItem.Id);

            if (_fkToNav.TryGetValue(idProp, out var navProp2))
            {
                var navEntity = _fkEntities[idProp].FirstOrDefault(x => x.Id == selItem.Id);
                navProp2.SetValue(Entity, navEntity);
            }
        }
    };

    return combo;
}

    protected record FkComboItem(Guid Id, string Text) { public override string ToString() => Text; }

    protected virtual bool ValidateAndBind()
    {
        foreach (var kv in _controls)
        {
            var prop = kv.Key;
            var ctrl = kv.Value;

            try
            {
                if (ctrl is TextBox tb && (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) != typeof(Guid))
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    object? converted;

                    if (string.IsNullOrWhiteSpace(tb.Text))
                        converted = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                    else if (targetType.IsEnum)
                        converted = Enum.Parse(targetType, tb.Text);
                    else
                        converted = Convert.ChangeType(tb.Text, targetType);

                    prop.SetValue(Entity, converted);
                }
                else if (ctrl is NumericUpDown num)
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    object val = targetType switch
                    {
                        Type t when t == typeof(int) => Decimal.ToInt32(num.Value),
                        Type t when t == typeof(long) => Convert.ToInt64(num.Value),
                        Type t when t == typeof(short) => Convert.ToInt16(num.Value),
                        Type t when t == typeof(decimal) => num.Value,
                        Type t when t == typeof(double) => Convert.ToDouble(num.Value),
                        Type t when t == typeof(float) => Convert.ToSingle(num.Value),
                        _ => num.Value
                    };
                    prop.SetValue(Entity, val);
                }
                else if (ctrl is CheckBox chk)
                    prop.SetValue(Entity, chk.Checked);
                else if (ctrl is DateTimePicker dt)
                    prop.SetValue(Entity, dt.Value);
                else if (ctrl is ComboBox cb && prop.Name.EndsWith("Id"))
                {
                    if (cb.SelectedItem is FkComboItem selItem)
                    {
                        prop.SetValue(Entity, selItem.Id);

                        if (_fkToNav.TryGetValue(prop, out var nav))
                        {
                            var navEntity = _fkEntities[prop].FirstOrDefault(x => x.Id == selItem.Id);
                            nav.SetValue(Entity, navEntity);
                        }
                    }
                }

                foreach (var attr in prop.GetCustomAttributes(true).OfType<ValidationAttribute>())
                    if (!attr.IsValid(prop.GetValue(Entity)))
                    {
                        MessageBox.Show(attr.ErrorMessage ?? $"Falta el campo: {prop.Name}", "CAMPOS OBLIGATORIOS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error binding", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        return true;
    }

    protected virtual void SaveChanges()
    {
        var dbContext = _provider.GetService(typeof(BillingDbContext)) as BillingDbContext;
        if (dbContext != null)
        {
            try
            {
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.Name != "RowVersion"))
                    prop.SetValue(_originalEntity, prop.GetValue(Entity));

                if (_autoSave)
                {
                    if (dbContext.Entry(_originalEntity).State == EntityState.Detached &&
                        dbContext.Set<T>().Local.All(e => e != _originalEntity))
                        dbContext.Set<T>().Add(_originalEntity);

                    try { dbContext.SaveChanges(); }
                    catch (DbUpdateConcurrencyException)
                    {
                        MessageBox.Show("La entidad ha sido modificada por otro usuario. Recargue y vuelva a intentar.", "Error de concurrencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error aplicando cambios al contexto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

}