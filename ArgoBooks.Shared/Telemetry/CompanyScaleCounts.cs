namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// The counts behind a <see cref="CompanyScaleEvent"/>, as a value so the manager can key its
/// per-session dedupe on them directly.
/// </summary>
public readonly record struct CompanyScaleCounts(
    int Expenses,
    int Revenues,
    int Invoices,
    int Payments,
    int Customers,
    int Suppliers,
    int Products,
    int Categories,
    int Receipts,
    int Employees,
    int BankLines);
