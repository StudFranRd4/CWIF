public class SimpleItemEditor<TItem> : Form where TItem : class, new()
{
    public TItem Entity { get; private set; }
    private readonly IServiceProvider _provider;

    public SimpleItemEditor(TItem item, IServiceProvider provider)
    {
        Entity = item ?? throw new ArgumentNullException(nameof(item));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        // Init del form "dummy"
        Text = $"Editar {typeof(TItem).Name}";
        Width = 800;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        Load += async (s, e) => await OpenDynamicEditor();
    }

    private async Task OpenDynamicEditor()
    {
        // note: DynamicEntityEditor debe aceptar la entidad y el provider
        using var editor = new DynamicEntityEditor<TItem>(_provider, Entity);

        var result = editor.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            // SimpleItemEditor simplemente devuelve la entidad modificada
            Entity = editor.Entity;
            DialogResult = DialogResult.OK;
        }
        else
        {
            DialogResult = DialogResult.Cancel;
        }

        Close();
    }
}
