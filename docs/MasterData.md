# Master Data

Master Data entities are the foundational reference data used across all modules.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Master Data Relationships (MasterData.svg)                     │
│                                                                 │
│                    ┌────────────┐                               │
│                    │ Categories │                               │
│                    └─────┬──────┘                               │
│                          │                                      │
│          ┌───────────────┼───────────────┐                     │
│          ▼               ▼               ▼                      │
│    ┌──────────┐   ┌──────────┐   ┌──────────┐                  │
│    │ Products │   │ Expenses │   │ Revenue  │                  │
│    └────┬─────┘   └──────────┘   └──────────┘                  │
│         │                                                       │
│    ┌────┴─────┐                                                │
│    ▼          ▼                                                │
│ ┌──────────┐ ┌──────────┐    ┌──────────┐   ┌──────────┐      │
│ │Suppliers │ │Locations │    │Customers │   │Departments│      │
│ └──────────┘ └──────────┘    └──────────┘   └──────────┘      │
│                                                                 │
│  Show: How master data entities relate to transactions          │
│  Include: Cardinality of relationships                          │
└─────────────────────────────────────────────────────────────────┘
```

## Customers

Customer relationship management.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Customer.cs` |
| ViewModel | `ArgoBooks/ViewModels/CustomersPageViewModel.cs` |
| View | `ArgoBooks/Views/CustomersPage.axaml` |
| Modals | `ArgoBooks/Modals/CustomerModals/` |

### Features
- Contact information management
- Address with `Address` value object
- Purchase history tracking
- Customer preferences
- Invoice and payment association
- Rental history

## Suppliers

Vendor and supplier management.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Supplier.cs` |
| ViewModel | `ArgoBooks/ViewModels/SuppliersPageViewModel.cs` |
| View | `ArgoBooks/Views/SuppliersPage.axaml` |
| Modals | `ArgoBooks/Modals/SupplierModals/` |

### Features
- Supplier contact details
- Payment terms configuration
- Product association
- Purchase order history
- Pricing agreements

## Categories

Hierarchical categorization for products and expenses.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Category.cs` |
| ViewModel | `ArgoBooks/ViewModels/CategoriesPageViewModel.cs` |
| View | `ArgoBooks/Views/CategoriesPage.axaml` |
| Modals | `ArgoBooks/Modals/CategoryModals/` |

### Features
- Multi-level category hierarchy
- Category types (`CategoryType` enum)
- Color coding for visual identification
- Category-based reporting

### Category Types

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Category Types (CategoryTypes.svg)                             │
│                                                                 │
│  CategoryType enum:                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Product    │  │   Expense    │  │   Revenue    │          │
│  │  Categories  │  │  Categories  │  │  Categories  │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│                                                                 │
│  Show: Different category type purposes                         │
└─────────────────────────────────────────────────────────────────┘
```

## Departments

Organizational structure and cost centers.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Department.cs` |
| ViewModel | `ArgoBooks/ViewModels/DepartmentsPageViewModel.cs` |
| View | `ArgoBooks/Views/DepartmentsPage.axaml` |
| Modals | `ArgoBooks/Modals/DepartmentModals/` |

### Features
- Department hierarchy
- Cost center assignment
- Department-based expense tracking
- Employee association

## Locations

Physical locations for multi-site operations.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Location.cs` |
| ViewModel | `ArgoBooks/ViewModels/LocationsPageViewModel.cs` |
| View | `ArgoBooks/Views/LocationsPage.axaml` |
| Modals | `ArgoBooks/Modals/LocationsModals/` |

### Features
- Warehouse/store/branch management
- Address information
- Location-based inventory tracking
- Stock transfer between locations

## Employees

Employee records and information.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Employee.cs`, `ArgoBooks.Core/Models/Accountant.cs` |

### Features
- Employee contact information
- Role assignment
- Department association
- Status tracking (`EmployeeStatus` enum)
- Emergency contact (`EmergencyContact` value object)

### Employee Status

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Employee Status (EmployeeStatus.svg)                           │
│                                                                 │
│  EmployeeStatus enum:                                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │  Active  │  │ On Leave │  │ Inactive │  │Terminated│       │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘       │
│                                                                 │
│  Show: Employee status lifecycle                                │
└─────────────────────────────────────────────────────────────────┘
```

## Common Value Objects

### Address
Reusable address structure:
- Street lines (1 & 2)
- City
- State/Province
- Postal code
- Country

### EmergencyContact
Contact information for emergencies:
- Name
- Relationship
- Phone number

## Entity Status

All master data entities share a common status pattern:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Entity Status (EntityStatus.svg)                               │
│                                                                 │
│  EntityStatus enum:                                             │
│  ┌──────────┐         ┌──────────┐                             │
│  │  Active  │ ◀─────▶ │ Inactive │                             │
│  └──────────┘         └──────────┘                             │
│                             │                                   │
│                             ▼                                   │
│                       ┌──────────┐                             │
│                       │ Archived │                             │
│                       └──────────┘                             │
│                                                                 │
│  Show: Common status values for all entities                    │
│  Include: Soft delete pattern (Archived)                        │
└─────────────────────────────────────────────────────────────────┘
```
