using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Customers table.
/// </summary>
public partial class CustomersTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _customerColumnWidth = 180;

    [ObservableProperty]
    private double _emailColumnWidth = 150;

    [ObservableProperty]
    private double _phoneColumnWidth = 110;

    [ObservableProperty]
    private double _addressColumnWidth = 180;

    [ObservableProperty]
    private double _paymentStatusColumnWidth = 110;

    [ObservableProperty]
    private double _outstandingColumnWidth = 100;

    [ObservableProperty]
    private double _lastRentalColumnWidth = 110;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public CustomersTableColumnWidths()
    {
        ColumnOrder = new[] { "Customer", "Email", "Phone", "Address", "PaymentStatus", "Outstanding", "LastRental", "Actions" };

        RegisterColumn("Customer", new ColumnDef { StarValue = 1.2, MinWidth = 120, PreferredWidth = 180 }, w => CustomerColumnWidth = w);
        RegisterColumn("Email", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 150 }, w => EmailColumnWidth = w);
        RegisterColumn("Phone", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 110 }, w => PhoneColumnWidth = w);
        RegisterColumn("Address", new ColumnDef { StarValue = 1.2, MinWidth = 120, PreferredWidth = 180 }, w => AddressColumnWidth = w);
        RegisterColumn("PaymentStatus", new ColumnDef { StarValue = 0.8, MinWidth = 90, PreferredWidth = 110 }, w => PaymentStatusColumnWidth = w);
        RegisterColumn("Outstanding", new ColumnDef { StarValue = 0.7, MinWidth = 80, PreferredWidth = 100 }, w => OutstandingColumnWidth = w);
        RegisterColumn("LastRental", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 110 }, w => LastRentalColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 120, MinWidth = 120 }, w => ActionsColumnWidth = w);
    }
}
