using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Purchase Orders modals (Add, Edit, View, Receive, Delete).
/// </summary>
public partial class PurchaseOrdersModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when a purchase order is saved (added or edited).
    /// </summary>
    public event EventHandler? OrderSaved;

    /// <summary>
    /// Raised when a purchase order is deleted.
    /// </summary>
    public event EventHandler? OrderDeleted;

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Raised when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    #endregion

    #region Add/Edit Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _editingOrderId;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private DateTimeOffset? _orderDate = new DateTimeOffset(DateTime.Today);

    [ObservableProperty]
    private DateTimeOffset? _expectedDeliveryDate = new DateTimeOffset(DateTime.Today.AddDays(7));

    [ObservableProperty]
    private string _shippingCost = "0";

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _addModalError;

    [ObservableProperty]
    private bool _hasSupplierError;

    // Original values for change detection in edit mode
    private Supplier? _originalSupplier;
    private DateTimeOffset? _originalOrderDate;
    private DateTimeOffset? _originalExpectedDeliveryDate;
    private string _originalShippingCost = "0";
    private string _originalNotes = string.Empty;
    private List<(string ProductId, string Quantity, string UnitCost)> _originalLineItems = [];

    /// <summary>
    /// Returns true if any data has been entered in the Add modal (when not in edit mode).
    /// </summary>
    public bool HasAddModalEnteredData =>
        !IsEditMode && (
            SelectedSupplier != null ||
            ShippingCost != "0" ||
            !string.IsNullOrWhiteSpace(Notes) ||
            LineItems.Any(li => !string.IsNullOrWhiteSpace(li.ProductId)));

    /// <summary>
    /// Returns true if any changes have been made in the Edit modal.
    /// </summary>
    public bool HasEditModalChanges
    {
        get
        {
            if (!IsEditMode) return false;

            if (SelectedSupplier?.Id != _originalSupplier?.Id) return true;
            if (OrderDate != _originalOrderDate) return true;
            if (ExpectedDeliveryDate != _originalExpectedDeliveryDate) return true;
            if (ShippingCost != _originalShippingCost) return true;
            if (Notes != _originalNotes) return true;

            // Compare line items
            if (LineItems.Count != _originalLineItems.Count) return true;
            for (int i = 0; i < LineItems.Count; i++)
            {
                var current = LineItems[i];
                var original = _originalLineItems[i];
                if (current.ProductId != original.ProductId ||
                    current.Quantity != original.Quantity ||
                    current.UnitCost != original.UnitCost)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Line items for the order being created/edited.
    /// </summary>
    public ObservableCollection<OrderLineItemViewModel> LineItems { get; } = [];

    /// <summary>
    /// Available suppliers for selection.
    /// </summary>
    public ObservableCollection<Supplier> AvailableSuppliers { get; } = [];

    /// <summary>
    /// Available products for adding line items.
    /// </summary>
    public ObservableCollection<Product> AvailableProducts { get; } = [];

    /// <summary>
    /// Calculated subtotal.
    /// </summary>
    public decimal CalculatedSubtotal => LineItems.Sum(li => li.Total);

    /// <summary>
    /// Calculated total.
    /// </summary>
    public decimal CalculatedTotal => CalculatedSubtotal + (decimal.TryParse(ShippingCost, out var sc) ? sc : 0);

    /// <summary>
    /// Subtotal display string.
    /// </summary>
    public string SubtotalDisplay => $"${CalculatedSubtotal:N2}";

    /// <summary>
    /// Total display string.
    /// </summary>
    public string TotalDisplay => $"${CalculatedTotal:N2}";

    /// <summary>
    /// Modal title based on mode.
    /// </summary>
    public string AddModalTitle => IsEditMode ? "Edit Purchase Order" : "Create Purchase Order";

    partial void OnShippingCostChanged(string value)
    {
        OnPropertyChanged(nameof(CalculatedTotal));
        OnPropertyChanged(nameof(TotalDisplay));
    }

    partial void OnIsEditModeChanged(bool value) => OnPropertyChanged(nameof(AddModalTitle));

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value != null)
        {
            HasSupplierError = false;
        }
    }

    private void UpdateCalculatedTotals()
    {
        OnPropertyChanged(nameof(CalculatedSubtotal));
        OnPropertyChanged(nameof(CalculatedTotal));
        OnPropertyChanged(nameof(SubtotalDisplay));
        OnPropertyChanged(nameof(TotalDisplay));
    }

    #endregion

    #region View Modal State

    [ObservableProperty]
    private bool _isViewModalOpen;

    [ObservableProperty]
    private PurchaseOrderDisplayItem? _viewingOrder;

    /// <summary>
    /// Line items for the order being viewed.
    /// </summary>
    public ObservableCollection<ViewLineItemDisplay> ViewLineItems { get; } = [];

    #endregion

    #region Receive Modal State

    [ObservableProperty]
    private bool _isReceiveModalOpen;

    [ObservableProperty]
    private PurchaseOrderDisplayItem? _receivingOrder;

    [ObservableProperty]
    private string? _receiveModalError;

    /// <summary>
    /// Line items for receiving.
    /// </summary>
    public ObservableCollection<ReceiveLineItemViewModel> ReceiveLineItems { get; } = [];

    #endregion

    #region Delete Confirmation State

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private PurchaseOrderDisplayItem? _deletingOrder;

    #endregion

    #region Add/Edit Modal Commands

    /// <summary>
    /// Opens the add purchase order modal.
    /// </summary>
    [RelayCommand]
    public void OpenAddModal()
    {
        LoadSuppliers();
        LoadProducts();
        ClearAddModalFields();
        IsEditMode = false;
        EditingOrderId = null;

        // Add a default empty line item
        AddLineItem();

        IsAddModalOpen = true;
    }

    /// <summary>
    /// Opens the edit purchase order modal.
    /// </summary>
    public void OpenEditModal(PurchaseOrderDisplayItem item)
    {
        LoadSuppliers();
        LoadProducts();
        ClearAddModalFields();

        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == item.Id);
        if (order == null) return;

        // Populate fields
        IsEditMode = true;
        EditingOrderId = order.Id;
        SelectedSupplier = AvailableSuppliers.FirstOrDefault(s => s.Id == order.SupplierId);
        OrderDate = new DateTimeOffset(order.OrderDate);
        ExpectedDeliveryDate = new DateTimeOffset(order.ExpectedDeliveryDate);
        ShippingCost = order.ShippingCost.ToString("F2");
        Notes = order.Notes;

        // Populate line items
        var products = companyData?.Products ?? [];
        foreach (var lineItem in order.LineItems)
        {
            var product = products.FirstOrDefault(p => p.Id == lineItem.ProductId);
            var vm = new OrderLineItemViewModel
            {
                ProductId = lineItem.ProductId,
                ProductName = product?.Name ?? "Unknown Product",
                Quantity = lineItem.Quantity.ToString(),
                UnitCost = lineItem.UnitCost.ToString("F2")
            };
            vm.PropertyChanged += (_, _) => UpdateCalculatedTotals();
            LineItems.Add(vm);
        }
        UpdateCalculatedTotals();

        // Store original values for change detection
        _originalSupplier = SelectedSupplier;
        _originalOrderDate = OrderDate;
        _originalExpectedDeliveryDate = ExpectedDeliveryDate;
        _originalShippingCost = ShippingCost;
        _originalNotes = Notes;
        _originalLineItems = LineItems.Select(li => (li.ProductId, li.Quantity, li.UnitCost)).ToList();

        IsAddModalOpen = true;
    }

    /// <summary>
    /// Closes the add/edit modal.
    /// </summary>
    [RelayCommand]
    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearAddModalFields();
    }

    /// <summary>
    /// Requests to close the Add/Edit modal, showing confirmation if data was entered or changes were made.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseAddModalAsync()
    {
        var hasChanges = IsEditMode ? HasEditModalChanges : HasAddModalEnteredData;
        if (hasChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = IsEditMode
                        ? "You have unsaved changes that will be lost. Are you sure you want to close?".Translate()
                        : "You have entered data that will be lost. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }
        }

        CloseAddModal();
    }

    /// <summary>
    /// Navigates to Suppliers page and opens the create supplier modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateSupplier()
    {
        IsAddModalOpen = false;
        App.NavigationService?.NavigateTo("Suppliers", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Navigates to Products page and opens the create product modal.
    /// </summary>
    [RelayCommand]
    private void NavigateToCreateProduct()
    {
        IsAddModalOpen = false;
        App.NavigationService?.NavigateTo("Products", new Dictionary<string, object?> { { "openAddModal", true } });
    }

    /// <summary>
    /// Adds a new line item to the order.
    /// </summary>
    [RelayCommand]
    private void AddLineItem()
    {
        var vm = new OrderLineItemViewModel();
        vm.PropertyChanged += (_, _) => UpdateCalculatedTotals();
        LineItems.Add(vm);
        UpdateCalculatedTotals();
    }

    /// <summary>
    /// Removes a line item from the order.
    /// </summary>
    [RelayCommand]
    private void RemoveLineItem(OrderLineItemViewModel? item)
    {
        if (item == null) return;
        LineItems.Remove(item);
        UpdateCalculatedTotals();
    }

    /// <summary>
    /// Saves the purchase order.
    /// </summary>
    [RelayCommand]
    private void SaveOrder()
    {
        AddModalError = null;
        HasSupplierError = false;

        // Clear all line item errors
        foreach (var li in LineItems)
        {
            li.HasProductError = false;
        }

        // Validate all fields before returning
        var hasErrors = false;

        // Validate supplier
        if (SelectedSupplier == null)
        {
            HasSupplierError = true;
            hasErrors = true;
        }

        // Validate line items
        if (LineItems.Count == 0)
        {
            AddModalError = "Please add at least one line item.".Translate();
            hasErrors = true;
        }
        else
        {
            foreach (var li in LineItems)
            {
                if (string.IsNullOrWhiteSpace(li.ProductId))
                {
                    li.HasProductError = true;
                    hasErrors = true;
                }
                if (!int.TryParse(li.Quantity, out var qty) || qty <= 0)
                {
                    AddModalError = "Please enter valid quantities for all line items.".Translate();
                    hasErrors = true;
                }
                if (!decimal.TryParse(li.UnitCost, out var cost) || cost < 0)
                {
                    AddModalError = "Please enter valid unit costs for all line items.".Translate();
                    hasErrors = true;
                }
            }
        }

        if (!decimal.TryParse(ShippingCost, out var shipping) || shipping < 0)
        {
            AddModalError = "Please enter a valid shipping cost.".Translate();
            hasErrors = true;
        }

        if (hasErrors) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        if (IsEditMode && !string.IsNullOrEmpty(EditingOrderId))
        {
            SaveEditedOrder(companyData, shipping);
        }
        else
        {
            SaveNewOrder(companyData, shipping);
        }

        OrderSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    private void SaveNewOrder(CompanyData companyData, decimal shipping)
    {
        // Generate ID
        companyData.IdCounters.PurchaseOrder++;
        var orderId = $"PO-{companyData.IdCounters.PurchaseOrder:D5}";
        var poNumber = $"#PO-{DateTime.Now.Year}-{companyData.IdCounters.PurchaseOrder:D3}";

        // Create line items
        var lineItems = LineItems.Select(li => new PurchaseOrderLineItem
        {
            ProductId = li.ProductId,
            Quantity = int.Parse(li.Quantity),
            UnitCost = decimal.Parse(li.UnitCost),
            QuantityReceived = 0
        }).ToList();

        var subtotal = lineItems.Sum(li => li.Total);
        var total = subtotal + shipping;

        var order = new PurchaseOrder
        {
            Id = orderId,
            PoNumber = poNumber,
            SupplierId = SelectedSupplier!.Id,
            OrderDate = OrderDate?.DateTime ?? DateTime.Today,
            ExpectedDeliveryDate = ExpectedDeliveryDate?.DateTime ?? DateTime.Today.AddDays(7),
            LineItems = lineItems,
            Subtotal = subtotal,
            ShippingCost = shipping,
            Total = total,
            Status = PurchaseOrderStatus.Draft,
            Notes = Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        companyData.PurchaseOrders.Add(order);
        companyData.MarkAsModified();

        // Record undo action
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Create order '{poNumber}'",
            () =>
            {
                companyData.PurchaseOrders.Remove(order);
                companyData.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData.PurchaseOrders.Add(order);
                companyData.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            }));
    }

    private void SaveEditedOrder(CompanyData companyData, decimal shipping)
    {
        var order = companyData.PurchaseOrders.FirstOrDefault(o => o.Id == EditingOrderId);
        if (order == null) return;

        // Store old values for undo
        var oldSupplierId = order.SupplierId;
        var oldOrderDate = order.OrderDate;
        var oldExpectedDate = order.ExpectedDeliveryDate;
        var oldLineItems = order.LineItems.ToList();
        var oldSubtotal = order.Subtotal;
        var oldShipping = order.ShippingCost;
        var oldTotal = order.Total;
        var oldNotes = order.Notes;

        // Update order
        order.SupplierId = SelectedSupplier!.Id;
        order.OrderDate = OrderDate?.DateTime ?? DateTime.Today;
        order.ExpectedDeliveryDate = ExpectedDeliveryDate?.DateTime ?? DateTime.Today.AddDays(7);
        order.LineItems = LineItems.Select(li => new PurchaseOrderLineItem
        {
            ProductId = li.ProductId,
            Quantity = int.Parse(li.Quantity),
            UnitCost = decimal.Parse(li.UnitCost),
            QuantityReceived = 0
        }).ToList();
        order.Subtotal = order.LineItems.Sum(li => li.Total);
        order.ShippingCost = shipping;
        order.Total = order.Subtotal + shipping;
        order.Notes = Notes;
        order.UpdatedAt = DateTime.UtcNow;
        companyData.MarkAsModified();

        // Record undo action
        var editedOrder = order;
        var newLineItems = order.LineItems.ToList();
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Edit order '{order.PoNumber}'",
            () =>
            {
                editedOrder.SupplierId = oldSupplierId;
                editedOrder.OrderDate = oldOrderDate;
                editedOrder.ExpectedDeliveryDate = oldExpectedDate;
                editedOrder.LineItems = oldLineItems;
                editedOrder.Subtotal = oldSubtotal;
                editedOrder.ShippingCost = oldShipping;
                editedOrder.Total = oldTotal;
                editedOrder.Notes = oldNotes;
                companyData.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                editedOrder.SupplierId = SelectedSupplier!.Id;
                editedOrder.OrderDate = OrderDate?.DateTime ?? DateTime.Today;
                editedOrder.ExpectedDeliveryDate = ExpectedDeliveryDate?.DateTime ?? DateTime.Today.AddDays(7);
                editedOrder.LineItems = newLineItems;
                editedOrder.Subtotal = order.Subtotal;
                editedOrder.ShippingCost = shipping;
                editedOrder.Total = order.Total;
                editedOrder.Notes = Notes;
                companyData.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            }));
    }

    private void LoadSuppliers()
    {
        AvailableSuppliers.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null) return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            AvailableSuppliers.Add(supplier);
        }
    }

    private void LoadProducts()
    {
        AvailableProducts.Clear();
        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Products == null) return;

        foreach (var product in companyData.Products.OrderBy(p => p.Name))
        {
            AvailableProducts.Add(product);
        }
    }

    private void ClearAddModalFields()
    {
        SelectedSupplier = null;
        OrderDate = new DateTimeOffset(DateTime.Today);
        ExpectedDeliveryDate = new DateTimeOffset(DateTime.Today.AddDays(7));
        ShippingCost = "0";
        Notes = string.Empty;
        AddModalError = null;
        HasSupplierError = false;
        LineItems.Clear();
        IsEditMode = false;
        EditingOrderId = null;
    }

    #endregion

    #region View Modal Commands

    /// <summary>
    /// Opens the view order modal.
    /// </summary>
    public void OpenViewModal(PurchaseOrderDisplayItem item)
    {
        ViewingOrder = item;
        LoadViewLineItems(item.Id);
        IsViewModalOpen = true;
    }

    /// <summary>
    /// Closes the view modal.
    /// </summary>
    [RelayCommand]
    private void CloseViewModal()
    {
        IsViewModalOpen = false;
        ViewingOrder = null;
        ViewLineItems.Clear();
    }

    private void LoadViewLineItems(string orderId)
    {
        ViewLineItems.Clear();
        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == orderId);
        if (order == null) return;

        var products = companyData?.Products ?? [];
        foreach (var li in order.LineItems)
        {
            var product = products.FirstOrDefault(p => p.Id == li.ProductId);
            ViewLineItems.Add(new ViewLineItemDisplay
            {
                ProductName = product?.Name ?? "Unknown Product",
                ProductSku = product?.Sku ?? "-",
                Quantity = li.Quantity,
                QuantityReceived = li.QuantityReceived,
                UnitCost = li.UnitCost,
                Total = li.Total
            });
        }
    }

    #endregion

    #region Receive Modal Commands

    /// <summary>
    /// Opens the receive order modal.
    /// </summary>
    public void OpenReceiveModal(PurchaseOrderDisplayItem item)
    {
        ReceivingOrder = item;
        LoadReceiveLineItems(item.Id);
        ReceiveModalError = null;
        IsReceiveModalOpen = true;
    }

    /// <summary>
    /// Closes the receive modal.
    /// </summary>
    [RelayCommand]
    private void CloseReceiveModal()
    {
        IsReceiveModalOpen = false;
        ReceivingOrder = null;
        ReceiveLineItems.Clear();
        ReceiveModalError = null;
    }

    /// <summary>
    /// Confirms receiving items.
    /// </summary>
    [RelayCommand]
    private void ConfirmReceive()
    {
        ReceiveModalError = null;

        if (ReceivingOrder == null) return;

        // Validate quantities
        foreach (var li in ReceiveLineItems)
        {
            if (!int.TryParse(li.ReceivingQuantity, out var qty) || qty < 0)
            {
                ReceiveModalError = "Please enter valid quantities.".Translate();
                return;
            }
            if (qty > li.Remaining)
            {
                ReceiveModalError = $"Cannot receive more than remaining quantity ({li.Remaining}) for {li.ProductName}.";
                return;
            }
        }

        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == ReceivingOrder.Id);
        if (order == null) return;

        // Store old values for undo
        var oldLineItemReceived = order.LineItems.Select(li => li.QuantityReceived).ToList();
        var oldStatus = order.Status;

        // Apply received quantities
        for (var i = 0; i < ReceiveLineItems.Count && i < order.LineItems.Count; i++)
        {
            if (int.TryParse(ReceiveLineItems[i].ReceivingQuantity, out var qty) && qty > 0)
            {
                order.LineItems[i].QuantityReceived += qty;

                // Update inventory
                var inventoryItem = companyData?.Inventory?.FirstOrDefault(inv =>
                    inv.ProductId == order.LineItems[i].ProductId);
                if (inventoryItem != null)
                {
                    inventoryItem.InStock += qty;
                    inventoryItem.Status = inventoryItem.CalculateStatus();
                    inventoryItem.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        // Update order status
        if (order.IsFullyReceived)
        {
            order.Status = PurchaseOrderStatus.Received;
        }
        else if (order.LineItems.Any(li => li.QuantityReceived > 0))
        {
            order.Status = PurchaseOrderStatus.PartiallyReceived;
        }

        order.UpdatedAt = DateTime.UtcNow;
        companyData?.MarkAsModified();

        // Record undo action
        var receivedOrder = order;
        var newLineItemReceived = order.LineItems.Select(li => li.QuantityReceived).ToList();
        var newStatus = order.Status;
        var receivedQuantities = ReceiveLineItems.Select(li =>
            int.TryParse(li.ReceivingQuantity, out var q) ? q : 0).ToList();

        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Receive items for '{order.PoNumber}'",
            () =>
            {
                // Undo: restore old quantities and revert inventory
                for (var i = 0; i < receivedOrder.LineItems.Count; i++)
                {
                    var diff = newLineItemReceived[i] - oldLineItemReceived[i];
                    receivedOrder.LineItems[i].QuantityReceived = oldLineItemReceived[i];

                    // Revert inventory
                    var inventoryItem = companyData?.Inventory?.FirstOrDefault(inv =>
                        inv.ProductId == receivedOrder.LineItems[i].ProductId);
                    if (inventoryItem != null && diff > 0)
                    {
                        inventoryItem.InStock -= diff;
                        inventoryItem.Status = inventoryItem.CalculateStatus();
                    }
                }
                receivedOrder.Status = oldStatus;
                companyData?.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                // Redo: re-apply received quantities
                for (var i = 0; i < receivedOrder.LineItems.Count && i < receivedQuantities.Count; i++)
                {
                    var diff = receivedQuantities[i];
                    receivedOrder.LineItems[i].QuantityReceived = newLineItemReceived[i];

                    var inventoryItem = companyData?.Inventory?.FirstOrDefault(inv =>
                        inv.ProductId == receivedOrder.LineItems[i].ProductId);
                    if (inventoryItem != null && diff > 0)
                    {
                        inventoryItem.InStock += diff;
                        inventoryItem.Status = inventoryItem.CalculateStatus();
                    }
                }
                receivedOrder.Status = newStatus;
                companyData?.MarkAsModified();
                OrderSaved?.Invoke(this, EventArgs.Empty);
            }));

        OrderSaved?.Invoke(this, EventArgs.Empty);
        CloseReceiveModal();
    }

    private void LoadReceiveLineItems(string orderId)
    {
        ReceiveLineItems.Clear();
        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == orderId);
        if (order == null) return;

        var products = companyData?.Products ?? [];
        foreach (var li in order.LineItems)
        {
            var product = products.FirstOrDefault(p => p.Id == li.ProductId);
            ReceiveLineItems.Add(new ReceiveLineItemViewModel
            {
                ProductId = li.ProductId,
                ProductName = product?.Name ?? "Unknown Product",
                Ordered = li.Quantity,
                Received = li.QuantityReceived,
                Remaining = li.Quantity - li.QuantityReceived,
                ReceivingQuantity = "0"
            });
        }
    }

    #endregion

    #region Delete Confirmation Commands

    /// <summary>
    /// Opens the delete confirmation dialog.
    /// </summary>
    public async void OpenDeleteConfirm(PurchaseOrderDisplayItem item)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Delete Purchase Order".Translate(),
            Message = "Are you sure you want to delete this purchase order?\n\nPO #: {0}\nTotal: {1}".TranslateFormat(item.PoNumber, item.TotalDisplay),
            PrimaryButtonText = "Delete".Translate(),
            CancelButtonText = "Cancel".Translate(),
            IsPrimaryDestructive = true
        });

        if (result != ConfirmationResult.Primary) return;

        var companyData = App.CompanyManager?.CompanyData;

        var order = companyData?.PurchaseOrders.FirstOrDefault(o => o.Id == item.Id);
        if (order == null) return;

        companyData?.PurchaseOrders.Remove(order);
        companyData?.MarkAsModified();

        // Record undo action
        var orderPoNumber = item.PoNumber;
        App.UndoRedoManager.RecordAction(new DelegateAction(
            $"Delete order '{orderPoNumber}'",
            () =>
            {
                companyData?.PurchaseOrders.Add(order);
                companyData?.MarkAsModified();
                OrderDeleted?.Invoke(this, EventArgs.Empty);
            },
            () =>
            {
                companyData?.PurchaseOrders.Remove(order);
                companyData?.MarkAsModified();
                OrderDeleted?.Invoke(this, EventArgs.Empty);
            }));

        OrderDeleted?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Filter Modal

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private DateTimeOffset? _filterStartDate;

    [ObservableProperty]
    private DateTimeOffset? _filterEndDate;

    [ObservableProperty]
    private string _filterSupplier = "All";

    [ObservableProperty]
    private string _filterStatus = "All";

    /// <summary>
    /// Supplier options for filter dropdown.
    /// </summary>
    public ObservableCollection<string> FilterSupplierOptions { get; } = ["All"];

    /// <summary>
    /// Status options for filter dropdown.
    /// </summary>
    public ObservableCollection<string> FilterStatusOptions { get; } =
        ["All", "Draft", "Pending", "Approved", "Sent", "On Order", "Partially Received", "Received", "Cancelled"];

    /// <summary>
    /// Returns true if any filter has been changed from its default value.
    /// </summary>
    public bool HasFilterChanges =>
        FilterStartDate != null ||
        FilterEndDate != null ||
        FilterSupplier != "All" ||
        FilterStatus != "All";

    // Original filter values for change detection
    private DateTimeOffset? _originalFilterStartDate;
    private DateTimeOffset? _originalFilterEndDate;
    private string _originalFilterSupplier = "All";
    private string _originalFilterStatus = "All";

    /// <summary>
    /// Returns true if any filter has been changed from its original value when the modal was opened.
    /// </summary>
    public bool HasFilterModalChanges =>
        FilterStartDate != _originalFilterStartDate ||
        FilterEndDate != _originalFilterEndDate ||
        FilterSupplier != _originalFilterSupplier ||
        FilterStatus != _originalFilterStatus;

    /// <summary>
    /// Captures the current filter values as the original values for change detection.
    /// </summary>
    private void CaptureOriginalFilterValues()
    {
        _originalFilterStartDate = FilterStartDate;
        _originalFilterEndDate = FilterEndDate;
        _originalFilterSupplier = FilterSupplier;
        _originalFilterStatus = FilterStatus;
    }

    /// <summary>
    /// Restores filter values to their original values when the modal was opened.
    /// </summary>
    private void RestoreOriginalFilterValues()
    {
        FilterStartDate = _originalFilterStartDate;
        FilterEndDate = _originalFilterEndDate;
        FilterSupplier = _originalFilterSupplier;
        FilterStatus = _originalFilterStatus;
    }

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
        LoadFilterSupplierOptions();
        CaptureOriginalFilterValues();
        IsFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Requests to close the filter modal, showing confirmation if filters have been changed.
    /// </summary>
    [RelayCommand]
    public async Task RequestCloseFilterModalAsync()
    {
        if (HasFilterModalChanges)
        {
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Discard Changes?".Translate(),
                    Message = "You have unapplied filter changes. Are you sure you want to close?".Translate(),
                    PrimaryButtonText = "Discard".Translate(),
                    CancelButtonText = "Cancel".Translate(),
                    IsPrimaryDestructive = true
                });

                if (result != ConfirmationResult.Primary)
                    return;
            }

            RestoreOriginalFilterValues();
        }

        CloseFilterModal();
    }

    /// <summary>
    /// Applies the current filters.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        ResetFilterDefaults();
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    private void ResetFilterDefaults()
    {
        FilterStartDate = null;
        FilterEndDate = null;
        FilterSupplier = "All";
        FilterStatus = "All";
    }

    private void LoadFilterSupplierOptions()
    {
        FilterSupplierOptions.Clear();
        FilterSupplierOptions.Add("All");

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData?.Suppliers == null) return;

        foreach (var supplier in companyData.Suppliers.OrderBy(s => s.Name))
        {
            FilterSupplierOptions.Add(supplier.Name);
        }
    }

    #endregion
}

/// <summary>
/// ViewModel for a line item in the add/edit order modal.
/// </summary>
public partial class OrderLineItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _quantity = "1";

    [ObservableProperty]
    private string _unitCost = "0.00";

    [ObservableProperty]
    private bool _hasProductError;

    /// <summary>
    /// Available products for the dropdown.
    /// </summary>
    public ObservableCollection<Product> AvailableProducts => App.PurchaseOrdersModalsViewModel?.AvailableProducts ?? [];

    /// <summary>
    /// Selected product.
    /// </summary>
    public Product? SelectedProduct
    {
        get => AvailableProducts.FirstOrDefault(p => p.Id == ProductId);
        set
        {
            if (value != null)
            {
                ProductId = value.Id;
                ProductName = value.Name;
                UnitCost = value.CostPrice.ToString("F2");
                HasProductError = false;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Calculated line total.
    /// </summary>
    public decimal Total
    {
        get
        {
            if (int.TryParse(Quantity, out var qty) && decimal.TryParse(UnitCost, out var cost))
                return qty * cost;
            return 0;
        }
    }

    /// <summary>
    /// Total display string.
    /// </summary>
    public string TotalDisplay => $"${Total:N2}";

    partial void OnQuantityChanged(string value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalDisplay));
    }

    partial void OnUnitCostChanged(string value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalDisplay));
    }
}

/// <summary>
/// Display model for view modal line items.
/// </summary>
public class ViewLineItemDisplay
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Total { get; set; }
    public string UnitCostDisplay => $"${UnitCost:N2}";
    public string TotalDisplay => $"${Total:N2}";
    public string QuantityDisplay => $"{QuantityReceived}/{Quantity}";
}

/// <summary>
/// ViewModel for a line item in the receive modal.
/// </summary>
public partial class ReceiveLineItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _productId = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private int _ordered;

    [ObservableProperty]
    private int _received;

    [ObservableProperty]
    private int _remaining;

    [ObservableProperty]
    private string _receivingQuantity = "0";
}
