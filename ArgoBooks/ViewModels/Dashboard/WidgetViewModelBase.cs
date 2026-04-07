using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels.Dashboard;

public abstract partial class WidgetViewModelBase : ObservableObject
{
    public abstract WidgetType WidgetType { get; }

    protected CompanyManager? CompanyManager => App.CompanyManager;

    public virtual void Initialize(Dictionary<string, string> config) { }
    public virtual void LoadData() { }
    public virtual void Cleanup() { }
    public virtual Dictionary<string, string> GetConfig() => new();
    public virtual void ApplyConfig(Dictionary<string, string> config) { }
}
