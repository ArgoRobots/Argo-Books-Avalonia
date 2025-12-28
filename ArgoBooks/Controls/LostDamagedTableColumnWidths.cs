using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Lost/Damaged table.
/// </summary>
public partial class LostDamagedTableColumnWidths : ObservableObject, ITableColumnWidths
{
    private double _availableWidth = 800;
    private bool _isUpdating;
    private bool _hasManualOverflow;

    private readonly Dictionary<string, ColumnDef> _columns = new();
    private readonly string[] _columnOrder = { "Id", "Type", "Product", "Date", "Reason", "Staff", "Loss", "Status", "Actions" };

    public class ColumnDef
    {
        public string Name { get; set; } = "";
        public double StarValue { get; set; } = 1.0;
        public double MinWidth { get; set; } = 50;
        public double MaxWidth { get; set; } = double.PositiveInfinity;
        public bool IsVisible { get; set; } = true;
        public bool IsFixed { get; set; }
        public double FixedWidth { get; set; } = 100;
        public double CurrentWidth { get; set; }
        public double PreferredWidth { get; set; } = 120;
        public double MeasuredContentWidth { get; set; }
    }

    #region Column Width Properties

    [ObservableProperty]
    private double _idColumnWidth = 90;

    [ObservableProperty]
    private double _typeColumnWidth = 70;

    [ObservableProperty]
    private double _productColumnWidth = 150;

    [ObservableProperty]
    private double _dateColumnWidth = 85;

    [ObservableProperty]
    private double _reasonColumnWidth = 100;

    [ObservableProperty]
    private double _staffColumnWidth = 100;

    [ObservableProperty]
    private double _lossColumnWidth = 80;

    [ObservableProperty]
    private double _statusColumnWidth = 85;

    [ObservableProperty]
    private double _actionsColumnWidth = 70;

    [ObservableProperty]
    private double _minimumTotalWidth = 0;

    [ObservableProperty]
    private bool _needsHorizontalScroll = false;

    #endregion

    public LostDamagedTableColumnWidths()
    {
        InitializeColumns();
    }

    private void InitializeColumns()
    {
        _columns["Id"] = new ColumnDef { Name = "Id", StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 };
        _columns["Type"] = new ColumnDef { Name = "Type", StarValue = 0.6, MinWidth = 60, PreferredWidth = 70 };
        _columns["Product"] = new ColumnDef { Name = "Product", StarValue = 1.2, MinWidth = 100, PreferredWidth = 150 };
        _columns["Date"] = new ColumnDef { Name = "Date", StarValue = 0.7, MinWidth = 70, PreferredWidth = 85 };
        _columns["Reason"] = new ColumnDef { Name = "Reason", StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 };
        _columns["Staff"] = new ColumnDef { Name = "Staff", StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 };
        _columns["Loss"] = new ColumnDef { Name = "Loss", StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 };
        _columns["Status"] = new ColumnDef { Name = "Status", StarValue = 0.7, MinWidth = 70, PreferredWidth = 85 };
        _columns["Actions"] = new ColumnDef { Name = "Actions", IsFixed = true, FixedWidth = 70, MinWidth = 70 };
    }

    public void SetColumnVisibility(string columnName, bool isVisible)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            col.IsVisible = isVisible;
            RecalculateWidths();
        }
    }

    public void SetAvailableWidth(double width)
    {
        if (Math.Abs(_availableWidth - width) < 1) return;

        if (_hasManualOverflow)
        {
            var totalWidth = _columns.Values.Where(c => c.IsVisible).Sum(c => c.CurrentWidth) + 48;
            if (width < totalWidth + 50)
            {
                MinimumTotalWidth = totalWidth;
                NeedsHorizontalScroll = true;
                return;
            }
            _hasManualOverflow = false;
        }

        _availableWidth = width;
        RecalculateWidths();
    }

    public void ResizeColumn(string columnName, double delta)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;
        if (Math.Abs(delta) < 0.5) return;

        var visibleColumns = _columnOrder.Where(name => _columns.TryGetValue(name, out var c) && c.IsVisible).ToList();
        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return;

        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();
        double totalCurrentWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 48;

        var newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, col.CurrentWidth + delta));
        var actualDelta = newColWidth - col.CurrentWidth;
        if (Math.Abs(actualDelta) < 0.5) return;

        if (totalCurrentWidth + actualDelta > maxTotalWidth) _hasManualOverflow = true;

        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        double shrinkNeeded = Math.Max(0, actualDelta - Math.Max(0, maxTotalWidth - totalCurrentWidth));
        if (shrinkNeeded > 0.5)
        {
            var shrinkableColumns = columnsToRight.Where(name => !_columns[name].IsFixed).ToList();
            foreach (var rightColName in shrinkableColumns)
            {
                var rightCol = _columns[rightColName];
                double availableShrink = rightCol.CurrentWidth - rightCol.MinWidth;
                double actualShrink = Math.Min(shrinkNeeded, availableShrink);
                rightCol.CurrentWidth -= actualShrink;
                ApplyWidthToProperty(rightColName, rightCol.CurrentWidth);
                shrinkNeeded -= actualShrink;
                if (shrinkNeeded < 0.5) break;
            }
        }

        UpdateScrollState(visibleColumns);
    }

    private void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth) + 48;
        if (totalWidth > _availableWidth + 1)
        {
            _hasManualOverflow = true;
            NeedsHorizontalScroll = true;
            MinimumTotalWidth = totalWidth;
        }
        else
        {
            NeedsHorizontalScroll = false;
            MinimumTotalWidth = _columns.Values.Where(c => c.IsVisible).Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;
        }
    }

    public void AutoSizeColumn(string columnName)
    {
        if (!_columns.TryGetValue(columnName, out var col) || !col.IsVisible || col.IsFixed) return;
        double targetWidth = Math.Max(col.MinWidth, col.MeasuredContentWidth > 0 ? col.MeasuredContentWidth : col.PreferredWidth);
        var delta = targetWidth - col.CurrentWidth;
        if (Math.Abs(delta) > 0.5) ResizeColumn(columnName, delta);
    }

    public void RecalculateWidths()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var visibleColumns = _columns.Values.Where(c => c.IsVisible).ToList();
            if (visibleColumns.Count == 0) return;

            double minTotalWidth = visibleColumns.Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;

            if (_hasManualOverflow)
            {
                double totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 48;
                if (totalWidth + 50 > _availableWidth) { MinimumTotalWidth = totalWidth; NeedsHorizontalScroll = true; return; }
                _hasManualOverflow = false;
            }

            MinimumTotalWidth = minTotalWidth;
            NeedsHorizontalScroll = _availableWidth < minTotalWidth;

            double fixedTotal = visibleColumns.Where(c => c.IsFixed).Sum(c => c.FixedWidth);
            double totalStars = visibleColumns.Where(c => !c.IsFixed).Sum(c => c.StarValue);
            double availableForProportional = Math.Max(100, _availableWidth - fixedTotal - 48);

            if (NeedsHorizontalScroll)
            {
                foreach (var col in visibleColumns) { col.CurrentWidth = col.IsFixed ? col.FixedWidth : col.MinWidth; ApplyWidthToProperty(col.Name, col.CurrentWidth); }
                return;
            }

            double widthPerStar = totalStars > 0 ? availableForProportional / totalStars : 0;
            foreach (var col in visibleColumns)
            {
                col.CurrentWidth = col.IsFixed ? col.FixedWidth : Math.Max(col.MinWidth, Math.Min(col.MaxWidth, col.StarValue * widthPerStar));
                ApplyWidthToProperty(col.Name, col.CurrentWidth);
            }
        }
        finally { _isUpdating = false; }
    }

    private void ApplyWidthToProperty(string columnName, double width)
    {
        switch (columnName)
        {
            case "Id": IdColumnWidth = width; break;
            case "Type": TypeColumnWidth = width; break;
            case "Product": ProductColumnWidth = width; break;
            case "Date": DateColumnWidth = width; break;
            case "Reason": ReasonColumnWidth = width; break;
            case "Staff": StaffColumnWidth = width; break;
            case "Loss": LossColumnWidth = width; break;
            case "Status": StatusColumnWidth = width; break;
            case "Actions": ActionsColumnWidth = width; break;
        }
    }
}
