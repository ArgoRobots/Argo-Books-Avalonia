using Avalonia.Controls;

namespace ArgoBooks.Controls.Dashboard;

public partial class DashboardRowHost : UserControl
{
    public DashboardRowPanel Panel => RowPanel;
    public Button AddButton => AddWidgetToRowButton;
    public Button DeleteButton => DeleteRowButton;

    public DashboardRowHost()
    {
        InitializeComponent();
    }
}
