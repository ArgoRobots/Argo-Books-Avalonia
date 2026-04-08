using ArgoBooks.Services;
using ArgoBooks.ViewModels.Dashboard;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

namespace ArgoBooks.Controls.Dashboard;

public partial class WidgetHost : UserControl
{
    public WidgetHost()
    {
        InitializeComponent();

        // Click-outside-to-close for config popover
        ConfigBackdrop.PointerPressed += OnConfigBackdropPressed;
    }

    private void OnConfigBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is WidgetHostViewModel hostVm && hostVm.IsConfigOpen)
        {
            hostVm.IsConfigOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Sets the widget view content directly, bypassing DataTemplate resolution
    /// to avoid DataContext/compiled binding conflicts.
    /// </summary>
    public void SetWidgetContent(WidgetHostViewModel hostVm)
    {
        // Create the widget view and set its DataContext to the widget-specific ViewModel
        var widgetView = WidgetFactory.CreateView(hostVm.WidgetType);
        widgetView.DataContext = hostVm.WidgetViewModel;
        WidgetContent.Child = widgetView;

        // Set config content DataContext for the config popover DataTemplates
        ConfigContent.Content = hostVm.WidgetViewModel;
    }
}
