# Rentals

The Rentals module manages equipment and asset rentals, tracking inventory availability and rental transactions.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Rental System Overview (Rentals.svg)                           │
│                                                                 │
│  ┌──────────────────┐         ┌──────────────────┐             │
│  │  Rental Items    │◀───────▶│  Rental Records  │             │
│  │  (Equipment)     │         │  (Transactions)  │             │
│  └──────────────────┘         └──────────────────┘             │
│           │                            │                        │
│           ▼                            ▼                        │
│  ┌──────────────────┐         ┌──────────────────┐             │
│  │   Availability   │         │    Customer      │             │
│  │    Calendar      │         │    History       │             │
│  └──────────────────┘         └──────────────────┘             │
│                                                                 │
│  Show: Relationship between rental inventory and records        │
│  Include: Customer association                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Rental Inventory

Equipment and assets available for rental.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/RentalItem.cs` |
| ViewModel | `ArgoBooks/ViewModels/RentalInventoryPageViewModel.cs` |
| View | `ArgoBooks/Views/RentalInventoryPage.axaml` |
| Modals | `ArgoBooks/Modals/RentalInventoryModals/` |

### Features
- Equipment catalog management
- Rental pricing (hourly, daily, weekly rates)
- Availability tracking
- Condition tracking
- Maintenance scheduling

## Rental Records

Individual rental transactions and bookings.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/RentalRecord.cs` |
| ViewModel | `ArgoBooks/ViewModels/RentalRecordsPageViewModel.cs` |
| View | `ArgoBooks/Views/RentalRecordsPage.axaml` |
| Modals | `ArgoBooks/Modals/RentalRecordsModals/` |

### Features
- Rental booking and reservations
- Duration tracking (start/end dates)
- Return management
- Late fee calculation
- Damage assessment on return

## Rental Status Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Rental Status Flow (RentalStatus.svg)                          │
│                                                                 │
│  [Reserved] ──▶ [Active/Rented] ──▶ [Returned]                 │
│       │               │                  │                      │
│       ▼               ▼                  ▼                      │
│  [Cancelled]     [Overdue]         [Completed]                  │
│                       │                                         │
│                       ▼                                         │
│                  [Returned]                                     │
│                                                                 │
│  Show: RentalStatus enum values and transitions                 │
│  Include: Trigger conditions for each transition                │
└─────────────────────────────────────────────────────────────────┘
```

## Rental Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Rental Lifecycle (RentalLifecycle.svg)                         │
│                                                                 │
│  1. Customer Request                                            │
│         │                                                       │
│         ▼                                                       │
│  2. Check Availability ──▶ [Not Available] ──▶ Waitlist        │
│         │                                                       │
│         ▼                                                       │
│  3. Create Reservation                                          │
│         │                                                       │
│         ▼                                                       │
│  4. Equipment Pickup ──▶ Record Condition                       │
│         │                                                       │
│         ▼                                                       │
│  5. Rental Period                                               │
│         │                                                       │
│         ▼                                                       │
│  6. Equipment Return ──▶ Inspect Condition                      │
│         │                                                       │
│         ▼                                                       │
│  7. Generate Invoice (if damage/late fees)                      │
│         │                                                       │
│         ▼                                                       │
│  8. Complete & Archive                                          │
│                                                                 │
│  Show: Full rental workflow from request to completion          │
│  Include: Decision points and alternate paths                   │
└─────────────────────────────────────────────────────────────────┘
```

## Rate Types

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Rate Types (RateTypes.svg)                                     │
│                                                                 │
│  RateType enum:                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │  Hourly  │  │  Daily   │  │  Weekly  │  │ Monthly  │       │
│  │   $X/hr  │  │  $X/day  │  │  $X/wk   │  │  $X/mo   │       │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘       │
│                                                                 │
│  Show: Different rate type options with pricing structure       │
└─────────────────────────────────────────────────────────────────┘
```

## Integration Points

- **Customers** - Rental records link to customer profiles
- **Invoicing** - Late fees and damage charges generate invoices
- **Inventory** - Rental items are separate from sale inventory
- **Calendar** - Availability visualization (future enhancement)
