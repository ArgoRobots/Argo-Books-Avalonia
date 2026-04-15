using ArgoBooks.Services;
using ArgoBooks.ViewModels.Dashboard;
using Avalonia.Controls;

namespace ArgoBooks.Controls.Dashboard;

public partial class WidgetHost : UserControl
{
    public WidgetHost()
    {
        InitializeComponent();
    }

    public void SetWidgetContent(WidgetHostViewModel hostVm)
    {
        var widgetView = WidgetFactory.CreateView(hostVm.WidgetType);
        widgetView.DataContext = hostVm.WidgetViewModel;
        WidgetContent.Child = widgetView;
    }
}
