namespace ArgoBooks.Core.Enums;

/// <summary>
/// Payment method used for transactions.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Cash payment.</summary>
    Cash,

    /// <summary>Credit card payment.</summary>
    CreditCard,

    /// <summary>Debit card payment.</summary>
    DebitCard,

    /// <summary>Bank transfer payment.</summary>
    BankTransfer,

    /// <summary>Check payment.</summary>
    Check,

    /// <summary>PayPal payment.</summary>
    PayPal,

    /// <summary>Square payment.</summary>
    Square,

    /// <summary>Other payment method.</summary>
    Other
}
