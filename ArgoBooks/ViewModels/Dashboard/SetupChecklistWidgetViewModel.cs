using ArgoBooks.Core.Models.Dashboard;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class SetupChecklistWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.SetupChecklist;

    public SetupChecklistViewModel Checklist { get; } = new();

    /// <summary>
    /// Forwarded navigation event from the checklist.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    public override void Initialize(Dictionary<string, string> config)
    {
        Checklist.NavigationRequested += OnChecklistNavigationRequested;
    }

    public override void LoadData()
    {
        Checklist.Refresh();
    }

    public override void Cleanup()
    {
        Checklist.NavigationRequested -= OnChecklistNavigationRequested;
    }

    private void OnChecklistNavigationRequested(object? sender, string target)
    {
        NavigationRequested?.Invoke(this, target);
    }
}
