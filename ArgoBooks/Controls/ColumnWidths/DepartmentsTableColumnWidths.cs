using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Departments table.
/// </summary>
public partial class DepartmentsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _departmentColumnWidth = 200;

    [ObservableProperty]
    private double _descriptionColumnWidth = 280;

    [ObservableProperty]
    private double _employeesColumnWidth = 120;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public DepartmentsTableColumnWidths()
    {
        ColumnOrder = ["Department", "Description", "Employees", "Actions"];

        RegisterColumn("Department", new ColumnDef { StarValue = 1.2, MinWidth = 140, PreferredWidth = 200 }, w => DepartmentColumnWidth = w);
        RegisterColumn("Description", new ColumnDef { StarValue = 1.6, MinWidth = 160, PreferredWidth = 280 }, w => DescriptionColumnWidth = w);
        RegisterColumn("Employees", new ColumnDef { StarValue = 0.8, MinWidth = 90, PreferredWidth = 120 }, w => EmployeesColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 120, MinWidth = 120 }, w => ActionsColumnWidth = w);
    }
}
