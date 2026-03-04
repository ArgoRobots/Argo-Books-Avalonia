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

/// <summary>
/// Extension methods for PaymentMethod.
/// </summary>
public static class PaymentMethodExtensions
{
    /// <summary>
    /// Gets the display name for a payment method.
    /// </summary>
    public static string GetDisplayName(this PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.CreditCard => "Credit Card",
            PaymentMethod.DebitCard => "Debit Card",
            _ => method.ToString()
        };
    }

    /// <summary>
    /// Gets the common payment method options for transaction forms.
    /// </summary>
    public static string[] GetCommonOptions()
    {
        return
        [
            PaymentMethod.Cash.GetDisplayName(),
            "Bank Card",
            PaymentMethod.BankTransfer.GetDisplayName(),
            PaymentMethod.Check.GetDisplayName(),
            PaymentMethod.PayPal.GetDisplayName(),
            PaymentMethod.Other.GetDisplayName()
        ];
    }
}
