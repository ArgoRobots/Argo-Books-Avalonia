# Inventory

The Inventory module manages products, stock levels, adjustments, and procurement.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Inventory System Overview (Inventory.svg)                      │
│                                                                 │
│  ┌──────────┐    ┌──────────────┐    ┌─────────────────┐       │
│  │ Products │◀──▶│ Stock Levels │◀──▶│ Stock Adjustments│      │
│  └──────────┘    └──────────────┘    └─────────────────┘       │
│        │                │                                       │
│        ▼                ▼                                       │
│  ┌──────────┐    ┌──────────────┐                              │
│  │ Suppliers│◀───│Purchase Orders│                              │
│  └──────────┘    └──────────────┘                              │
│                                                                 │
│  Show: Relationships between inventory components               │
│  Include: Data flow for stock updates                           │
└─────────────────────────────────────────────────────────────────┘
```

## Products

Products represent items available for sale or use.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Product.cs` |
| ViewModel | `ArgoBooks/ViewModels/ProductsPageViewModel.cs` |
| View | `ArgoBooks/Views/ProductsPage.axaml` |
| Modals | `ArgoBooks/Modals/ProductModals/` |

### Features
- Product catalog management
- Pricing and cost tracking
- Supplier association
- Category assignment
- SKU/barcode support

## Stock Levels

Real-time inventory tracking across locations.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/InventoryItem.cs` |
| ViewModel | `ArgoBooks/ViewModels/StockLevelsPageViewModel.cs` |
| View | `ArgoBooks/Views/StockLevelsPage.axaml` |
| Modals | `ArgoBooks/Modals/StockLevelsModals/` |

### Features
- Current stock quantity per location
- Reorder point management
- Low-stock alerts
- Stock availability visibility
- Multi-location support

### Inventory Status

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Inventory Status States (InventoryStatus.svg)                  │
│                                                                 │
│  [In Stock] ──▶ [Low Stock] ──▶ [Out of Stock]                 │
│       ▲              │                  │                       │
│       └──────────────┴──────────────────┘                       │
│                   (Restock)                                     │
│                                                                 │
│  Show: InventoryStatus enum values                              │
│  Include: Threshold triggers for transitions                    │
└─────────────────────────────────────────────────────────────────┘
```

## Stock Adjustments

Manual corrections and reconciliations to inventory counts.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/StockAdjustment.cs` |
| ViewModel | `ArgoBooks/ViewModels/StockAdjustmentsPageViewModel.cs` |
| View | `ArgoBooks/Views/StockAdjustmentsPage.axaml` |
| Modals | `ArgoBooks/Modals/StockAdjustmentsModals/` |

### Features
- Manual stock corrections
- Adjustment reason tracking
- Audit trail for changes
- Bulk adjustments

### Adjustment Types

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Adjustment Types (AdjustmentTypes.svg)                         │
│                                                                 │
│  AdjustmentType enum:                                           │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │
│  │   Correction   │  │   Shrinkage    │  │   Found Stock  │    │
│  └────────────────┘  └────────────────┘  └────────────────┘    │
│  ┌────────────────┐  ┌────────────────┐                        │
│  │    Damaged     │  │   Write-Off    │                        │
│  └────────────────┘  └────────────────┘                        │
│                                                                 │
│  Show: All adjustment type options with +/- indicators          │
└─────────────────────────────────────────────────────────────────┘
```

## Purchase Orders

Procurement workflow for ordering from suppliers.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/PurchaseOrder.cs` |
| ViewModel | `ArgoBooks/ViewModels/PurchaseOrdersPageViewModel.cs` |
| View | `ArgoBooks/Views/PurchaseOrdersPage.axaml` |
| Modals | `ArgoBooks/Modals/PurchaseOrdersModals/` |

### Features
- Create purchase orders for suppliers
- Line item management
- Delivery tracking
- PO status workflow
- Automatic stock updates on receipt

### Purchase Order Status Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Purchase Order Status Flow (PurchaseOrderStatus.svg)           │
│                                                                 │
│  [Draft] ──▶ [Submitted] ──▶ [Approved] ──▶ [Ordered]          │
│                                                │                │
│                                                ▼                │
│                              [Partially Received] ──▶ [Received]│
│                                                │                │
│                                                ▼                │
│                                           [Cancelled]           │
│                                                                 │
│  Show: PurchaseOrderStatus enum transitions                     │
│  Include: Actions that trigger each transition                  │
└─────────────────────────────────────────────────────────────────┘
```

## Stock Update Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Stock Update Sources (StockUpdates.svg)                        │
│                                                                 │
│           ┌─────────────────┐                                   │
│           │   Stock Level   │                                   │
│           └────────▲────────┘                                   │
│                    │                                            │
│      ┌─────────────┼─────────────┐                             │
│      │             │             │                              │
│  ┌───┴───┐    ┌────┴────┐   ┌───┴────┐                         │
│  │ Sales │    │   PO    │   │ Adjust │                         │
│  │  (-)  │    │Receipt  │   │  (+/-) │                         │
│  └───────┘    │  (+)    │   └────────┘                         │
│               └─────────┘                                       │
│                                                                 │
│  Show: All sources that modify stock levels                     │
│  Include: Direction of stock change (+/-)                       │
└─────────────────────────────────────────────────────────────────┘
```
