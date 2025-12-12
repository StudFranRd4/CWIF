using FacturaApp.Core.Models;
using FacturaApp.Data;
using FacturaApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace FacturaApp.WinForms.Forms
{
    public class GenericCrudForm<T> : Form where T : BaseEntity, new()
    {
        protected IServiceProvider _provider;
        protected IRepository<T> _repo;
        protected ILogger<GenericCrudForm<T>> _logger;
        protected BillingDbContext _dbContext;

        private readonly BindingList<T> _list = new();
        protected DataGridView _grid = new();
        private TableLayoutPanel _layout = new();

        // Diccionario para mapear columnas a propiedades de colección
        private Dictionary<string, PropertyInfo> _collectionColumns = new Dictionary<string, PropertyInfo>();
        private List<PropertyInfo> _collectionProperties = new List<PropertyInfo>();

        public GenericCrudForm(IServiceProvider provider, IRepository<T> repo, ILogger<GenericCrudForm<T>> logger)
        {
            _provider = provider;
            _repo = repo;
            _logger = logger;
            _dbContext = provider.GetService<BillingDbContext>();  // <-- Usar BillingDbContext específico

            // Si aún es null, intentar obtenerlo de otra manera
            if (_dbContext == null)
            {
                // Buscar en el contenedor de forma más agresiva
                var dbContextType = typeof(BillingDbContext);
                var services = provider.GetService<IServiceProvider>();
                _dbContext = services?.GetService(dbContextType) as BillingDbContext;
            }

            if (_dbContext == null)
            {
                _logger.LogWarning("BillingDbContext no encontrado en el contenedor de servicios");
            }

            // Detectar propiedades de colección al inicio
            DetectCollectionProperties();

            Text = $"Mantenimiento: {typeof(T).Name}";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            InitializeLayout();
            ConfigureGrid();

            Load += async (_, _) => await LoadDataAsync();
        }

        private void DetectCollectionProperties()
        {
            _collectionProperties = typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                           (p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                .ToList();
        }

        protected virtual void InitializeLayout()
        {
            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = System.Drawing.Color.White
            };

            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var panelTop = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                Padding = new Padding(8),
                BackColor = System.Drawing.Color.WhiteSmoke
            };

            var btnNew = new Button { Text = "Nuevo", Width = 90 };
            var btnEdit = new Button { Text = "Editar", Width = 90 };
            var btnDel = new Button { Text = "Eliminar", Width = 90 };
            var btnRefresh = new Button { Text = "Refrescar", Width = 90 };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };
            flow.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDel, btnRefresh });
            panelTop.Controls.Add(flow);

            var gridPanel = new Panel { Dock = DockStyle.Fill };
            _grid.Dock = DockStyle.Fill;
            gridPanel.Controls.Add(_grid);

            _layout.Controls.Add(panelTop, 0, 0);
            _layout.Controls.Add(gridPanel, 0, 1);

            Controls.Add(_layout);

            btnNew.Click += async (_, _) => await NewAsync();
            btnEdit.Click += async (_, _) => await EditAsync();
            btnDel.Click += async (_, _) => await DeleteAsync();
            btnRefresh.Click += async (_, _) => await LoadDataAsync();
        }

        protected virtual void ConfigureGrid()
        {
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;

            _grid.AutoGenerateColumns = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            _grid.ColumnHeadersVisible = true;
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 235, 235);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            _grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Manejar clics en el grid
            _grid.CellClick += Grid_CellClick;
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += (s, e) => OnGridSelectionChanged();
        }

        protected virtual void OnGridSelectionChanged()
        {
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var column = _grid.Columns[e.ColumnIndex];
            if (column is DataGridViewButtonColumn)
            {
                e.Value = "Ver";
                e.CellStyle.BackColor = Color.FromArgb(52, 152, 219);
                e.CellStyle.ForeColor = Color.White;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                e.CellStyle.SelectionBackColor = Color.FromArgb(41, 128, 185);
                e.CellStyle.SelectionForeColor = Color.White;
            }
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var column = _grid.Columns[e.ColumnIndex];

            if (column is DataGridViewButtonColumn &&
                _collectionColumns.TryGetValue(column.Name, out var collectionProperty))
            {
                if (_grid.Rows[e.RowIndex].DataBoundItem is not T entity) return;

                try
                {
                    ShowCollectionForEntity(entity, collectionProperty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al mostrar colección {CollectionName}", collectionProperty.Name);
                    MessageBox.Show($"Error al mostrar la colección: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected async void ShowCollectionForEntity(T entity, PropertyInfo collectionProperty)
        {
            try
            {
                if (_dbContext == null)
                {
                    MessageBox.Show("No se encontró el contexto de base de datos. Asegúrese de que BillingDbContext esté registrado en el contenedor de servicios.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Obtener el tipo de elemento de la colección
                var itemType = collectionProperty.PropertyType.GetGenericArguments()[0];
                _logger.LogInformation("Mostrando colección {CollectionName} de tipo {ItemType} para entidad {EntityId}",
                    collectionProperty.Name, itemType.Name, entity.Id);

                // Obtener la propiedad FK usando reflexión
                var fkProperty = GetForeignKeyProperty(itemType, typeof(T));
                if (fkProperty == null)
                {
                    MessageBox.Show($"No se pudo determinar la relación entre {typeof(T).Name} y {itemType.Name}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Filtrar los datos usando el DbSet
                var collectionItems = await FilterCollectionItems(itemType, fkProperty, entity.Id);

                if (collectionItems == null || collectionItems.Count == 0)
                {
                    MessageBox.Show($"La colección '{collectionProperty.Name}' está vacía.",
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Crear y mostrar el formulario de solo lectura
                ShowCollectionForm(collectionItems, collectionProperty.Name, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mostrar colección {CollectionName} para entidad {EntityId}",
                    collectionProperty.Name, entity.Id);
                MessageBox.Show($"Error al cargar la colección: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PropertyInfo GetForeignKeyProperty(Type childType, Type parentType)
        {
            // Buscar propiedades que puedan ser FK
            var candidateProperties = childType.GetProperties()
                .Where(p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?))
                .ToList();

            // 1. Buscar por convención: [NombreEntidad]Id
            var propertyName1 = parentType.Name + "Id";
            var propertyName2 = parentType.Name.Replace("Entity", "") + "Id";

            var property = candidateProperties.FirstOrDefault(p =>
                p.Name.Equals(propertyName1, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals(propertyName2, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                _logger.LogDebug("Encontrada FK por convención: {PropertyName}", property.Name);
                return property;
            }

            // 2. Buscar por atributos de navegación
            var navProperties = childType.GetProperties()
                .Where(p => p.PropertyType == parentType || parentType.IsAssignableFrom(p.PropertyType))
                .ToList();

            foreach (var navProp in navProperties)
            {
                var fkPropName = navProp.Name + "Id";
                var fkProp = candidateProperties.FirstOrDefault(p =>
                    p.Name.Equals(fkPropName, StringComparison.OrdinalIgnoreCase));

                if (fkProp != null)
                {
                    _logger.LogDebug("Encontrada FK por navegación: {PropertyName}", fkProp.Name);
                    return fkProp;
                }
            }

            _logger.LogWarning("No se encontró propiedad FK para relación entre {ParentType} y {ChildType}",
                parentType.Name, childType.Name);
            return null;
        }

        private async Task<List<object>> FilterCollectionItems(Type itemType, PropertyInfo fkProperty, Guid parentId)
        {
            try
            {
                // Usar el método Set del DbContext para obtener el DbSet dinámicamente
                var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes);
                var genericSetMethod = setMethod?.MakeGenericMethod(itemType);
                var dbSet = genericSetMethod?.Invoke(_dbContext, null) as IQueryable<object>;

                if (dbSet == null)
                {
                    _logger.LogError("No se pudo obtener DbSet para {ItemType}", itemType.Name);
                    return new List<object>();
                }

                // Construir expresión lambda dinámica: x => x.FK == parentId
                var parameter = System.Linq.Expressions.Expression.Parameter(itemType, "x");
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, fkProperty);
                var constant = System.Linq.Expressions.Expression.Constant(parentId);
                var equals = System.Linq.Expressions.Expression.Equal(propertyAccess, constant);
                var lambda = System.Linq.Expressions.Expression.Lambda(equals, parameter);

                // Aplicar Where dinámicamente
                var whereMethod = typeof(Queryable).GetMethods()
                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(itemType);

                var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, lambda });

                // Ejecutar la consulta
                var toListMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == "ToListAsync" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(itemType);

                var task = toListMethod.Invoke(null, new object[] { filteredQuery, default(CancellationToken) }) as Task;

                await task.ConfigureAwait(false);

                // Obtener el resultado
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task) as List<object> ?? new List<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al filtrar items de colección");
                // Fallback: buscar manualmente
                return await FilterCollectionItemsFallback(itemType, fkProperty, parentId);
            }
        }

        private async Task<List<object>> FilterCollectionItemsFallback(Type itemType, PropertyInfo fkProperty, Guid parentId)
        {
            try
            {
                // Obtener todos los items y filtrar manualmente
                var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes);
                var genericSetMethod = setMethod?.MakeGenericMethod(itemType);
                var dbSet = genericSetMethod?.Invoke(_dbContext, null) as IQueryable<object>;

                if (dbSet == null) return new List<object>();

                var allItems = await dbSet.ToListAsync();
                return allItems.Where(item =>
                {
                    var fkValue = fkProperty.GetValue(item);
                    return fkValue != null && fkValue.Equals(parentId);
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en fallback al filtrar items de colección");
                return new List<object>();
            }
        }

        private void ShowCollectionForm(List<object> items, string collectionName, T parentEntity)
        {
            var itemType = items.FirstOrDefault()?.GetType();
            if (itemType == null) return;

            // Crear lista tipada
            var listType = typeof(List<>).MakeGenericType(itemType);
            var typedList = Activator.CreateInstance(listType) as IList;

            foreach (var item in items)
            {
                typedList.Add(item);
            }

            // Crear y mostrar el formulario
            var formType = typeof(ReadOnlyListForm<>).MakeGenericType(itemType);
            var form = Activator.CreateInstance(formType, typedList) as Form;
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(this);
        }

        private string GetEntityDisplayName(T entity)
        {
            try
            {
                var nameProp = typeof(T).GetProperty("Nombre");
                var descProp = typeof(T).GetProperty("Descripcion");
                var nombreCompletoProp = typeof(T).GetProperty("NombreCompleto");
                var codigoProp = typeof(T).GetProperty("Codigo");

                if (nameProp != null)
                    return nameProp.GetValue(entity)?.ToString() ?? entity.Id.ToString().Substring(0, 8);
                else if (descProp != null)
                    return descProp.GetValue(entity)?.ToString() ?? entity.Id.ToString().Substring(0, 8);
                else if (nombreCompletoProp != null)
                    return nombreCompletoProp.GetValue(entity)?.ToString() ?? entity.Id.ToString().Substring(0, 8);
                else if (codigoProp != null)
                    return codigoProp.GetValue(entity)?.ToString() ?? entity.Id.ToString().Substring(0, 8);
                else
                    return entity.Id.ToString().Substring(0, 8);
            }
            catch
            {
                return entity.Id.ToString().Substring(0, 8);
            }
        }

        protected virtual async Task LoadDataAsync()
        {
            try
            {
                var items = (await _repo.GetAllAsync()).ToList();

                // Cargar relaciones por cada item usando los repositorios disponibles en el provider
                foreach (var it in items)
                    TryLoadNavigationProperties(it);

                if (_grid.DataSource == null)
                {
                    BuildColumns();
                    _grid.DataSource = _list;
                }

                _list.RaiseListChangedEvents = false;
                _list.Clear();
                foreach (var it in items)
                    _list.Add(it);
                _list.RaiseListChangedEvents = true;
                _list.ResetBindings();

                FillRelationColumns();

                _grid.ClearSelection();
                if (_grid.Rows.Count > 0)
                    _grid.FirstDisplayedScrollingRowIndex = 0;

                _grid.Refresh();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando datos");
                MessageBox.Show($"Error al cargar datos: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            _collectionColumns.Clear();

            var props = typeof(T).GetProperties();

            // Columnas para propiedades simples
            foreach (var p in props)
            {
                if (p.Name == "RowVersion" || p.Name.EndsWith("Id"))
                    continue;

                if (typeof(BaseEntity).IsAssignableFrom(p.PropertyType))
                    continue;

                // Omitir colecciones de las columnas de texto
                if (_collectionProperties.Contains(p))
                    continue;

                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = p.Name,
                    HeaderText = GetDisplayName(p),
                    DataPropertyName = p.Name,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    MinimumWidth = 100
                });
            }

            // Columnas para propiedades de navegación (relaciones 1:1 o N:1)
            foreach (var nav in props.Where(x => typeof(BaseEntity).IsAssignableFrom(x.PropertyType)))
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = nav.Name + "_Nombre",
                    HeaderText = GetDisplayName(nav),
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    MinimumWidth = 100
                });
            }

            // Columnas de botón para propiedades de colección (relaciones 1:N)
            foreach (var collectionProp in _collectionProperties)
            {
                var buttonColumn = new DataGridViewButtonColumn
                {
                    Name = collectionProp.Name,
                    HeaderText = GetDisplayName(collectionProp),
                    Text = "Ver",
                    UseColumnTextForButtonValue = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    Width = 80,
                    FlatStyle = FlatStyle.Flat
                };

                _grid.Columns.Add(buttonColumn);
                _collectionColumns[collectionProp.Name] = collectionProp;
            }

            // Ajustar la última columna para llenar el espacio restante
            if (_grid.Columns.Count > 0)
            {
                _grid.Columns[_grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private string GetDisplayName(PropertyInfo property)
        {
            var displayAttr = property.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
            if (displayAttr != null)
                return displayAttr.DisplayName;

            return property.Name;
        }

        private void FillRelationColumns()
        {
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                if (row.DataBoundItem is not T item) continue;

                // Llenar columnas de navegación
                foreach (var p in typeof(T).GetProperties().Where(x => typeof(BaseEntity).IsAssignableFrom(x.PropertyType)))
                {
                    var nav = p.GetValue(item) as BaseEntity;
                    string colName = p.Name + "_Nombre";
                    if (!_grid.Columns.Contains(colName)) continue;

                    string? name = null;
                    if (nav != null)
                    {
                        var nombreProp = nav.GetType().GetProperty("Nombre");
                        var descripcionProp = nav.GetType().GetProperty("Descripcion");
                        var nombreCompletoProp = nav.GetType().GetProperty("NombreCompleto");

                        if (nombreProp != null)
                            name = nombreProp.GetValue(nav)?.ToString();
                        else if (descripcionProp != null)
                            name = descripcionProp.GetValue(nav)?.ToString();
                        else if (nombreCompletoProp != null)
                            name = nombreCompletoProp.GetValue(nav)?.ToString();
                        else
                            name = nav.Id.ToString();
                    }

                    row.Cells[colName].Value = name ?? string.Empty;
                }
            }
        }

        protected void TryLoadNavigationProperties(T entity)
        {
            var navProps = typeof(T).GetProperties()
                .Where(p => typeof(BaseEntity).IsAssignableFrom(p.PropertyType) && !_collectionProperties.Contains(p));

            foreach (var navProp in navProps)
            {
                var fkProp = typeof(T).GetProperty(navProp.Name + "Id");
                if (fkProp == null) continue;

                var fkValue = fkProp.GetValue(entity);
                if (fkValue is not Guid guid || guid == Guid.Empty) continue;

                try
                {
                    var repoType = typeof(IRepository<>).MakeGenericType(navProp.PropertyType);
                    var repoObj = _provider.GetService(repoType);
                    if (repoObj == null) continue;

                    var navEntity = ((dynamic)repoObj).GetAsync(guid).GetAwaiter().GetResult();
                    if (navEntity != null)
                        navProp.SetValue(entity, navEntity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cargando propiedad de navegación {NavProp} para entidad {EntityId}",
                        navProp.Name, entity.Id);
                }
            }
        }

        protected virtual async Task NewAsync()
        {
            var editor = new DynamicEntityEditor<T>(_provider, null, false);

            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                if (editor.Entity.Id == Guid.Empty)
                    editor.Entity.Id = Guid.NewGuid();

                TryLoadNavigationProperties(editor.Entity);

                await _repo.CreateAsync(editor.Entity);
                await LoadDataAsync();
            }
        }

        protected virtual async Task EditAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not T selected) return;

            try
            {
                var reloaded = await _repo.GetAsync(selected.Id);
                if (reloaded == null)
                {
                    MessageBox.Show("El registro ya no existe", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await LoadDataAsync();
                    return;
                }

                TryLoadNavigationProperties(reloaded);

                var editor = new DynamicEntityEditor<T>(_provider, reloaded, false);

                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    await _repo.UpdateAsync(editor.Entity);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex) when (ex.InnerException is DbUpdateConcurrencyException)
            {
                MessageBox.Show("Error de concurrencia. Los datos han sido modificados por otro usuario.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadDataAsync();
            }
            catch (NullReferenceException nre)
            {
                _logger.LogError(nre, "NullReferenceException al editar {EntityType}", typeof(T).Name);
                MessageBox.Show($"Error interno: {nre.Message}\n\nVerifique que todos los datos requeridos estén completos.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar {EntityType}", typeof(T).Name);
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected virtual async Task DeleteAsync()
        {
            if (_grid.CurrentRow?.DataBoundItem is not T selected) return;

            if (MessageBox.Show("¿Eliminar registro?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await _repo.DeleteAsync(selected.Id);
                await LoadDataAsync();
            }
        }
    }
}