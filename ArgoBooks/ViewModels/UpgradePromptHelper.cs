using ArgoBooks.Core.Enums;
using ArgoBooks.Localization;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Helper for showing compelling upgrade prompt dialogs when free-tier limits are reached.
/// Instead of plain error messages, these prompts highlight Premium benefits and offer
/// a direct path to upgrade.
/// </summary>
public static class UpgradePromptHelper
{
    /// <summary>
    /// Shows a compelling upgrade prompt when the invoice send limit is reached.
    /// </summary>
    /// <param name="limit">The monthly invoice limit on the free plan.</param>
    public static async Task ShowInvoiceLimitPromptAsync(int limit)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Invoice Limit Reached".Translate(),
            Message = string.Format(
                "You've sent all {0} invoices included in your free plan this month.\n\nUpgrade to Premium for unlimited invoices, online payment collection, and priority support — all for just $10 CAD/month.".Translate(),
                limit),
            PrimaryButtonText = "Upgrade Now".Translate(),
            CancelButtonText = "Maybe Later".Translate(),
            SecondaryButtonText = null
        });

        if (result == ConfirmationResult.Primary)
        {
            App.OpenUpgradeModal();
        }
    }

    /// <summary>
    /// Shows a compelling upgrade prompt when the AI import limit is reached.
    /// </summary>
    /// <param name="importCount">Number of imports used this month.</param>
    /// <param name="monthlyLimit">The monthly import limit.</param>
    /// <param name="resetsAt">When the limit resets.</param>
    public static async Task ShowAiImportLimitPromptAsync(int importCount, int monthlyLimit, string? resetsAt)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var resetDate = resetsAt ?? "the 1st of next month".Translate();
        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "AI Import Limit Reached".Translate(),
            Message = string.Format(
                "You've used all {0} of your {1} AI imports this month. Your limit resets on {2}.\n\nUpgrade to Premium for a higher import allowance and unlock AI receipt scanning, predictive analytics, and more.".Translate(),
                importCount, monthlyLimit, resetDate),
            PrimaryButtonText = "Upgrade Now".Translate(),
            CancelButtonText = "Maybe Later".Translate(),
            SecondaryButtonText = null
        });

        if (result == ConfirmationResult.Primary)
        {
            App.OpenUpgradeModal();
        }
    }

    /// <summary>
    /// Shows a compelling upgrade prompt when the receipt scan limit is reached.
    /// </summary>
    /// <param name="scanCount">Number of scans used this month.</param>
    /// <param name="monthlyLimit">The monthly scan limit.</param>
    /// <param name="resetsAt">When the limit resets.</param>
    public static async Task ShowReceiptScanLimitPromptAsync(int scanCount, int monthlyLimit, string? resetsAt)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var resetDate = resetsAt ?? "the 1st of next month".Translate();
        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Scan Limit Reached".Translate(),
            Message = string.Format(
                "You've used all {0} of your {1} receipt scans this month. Your limit resets on {2}.\n\nUpgrade to Premium for 500 scans per month, unlimited invoices, and predictive analytics.".Translate(),
                scanCount, monthlyLimit, resetDate),
            PrimaryButtonText = "Upgrade Now".Translate(),
            CancelButtonText = "Maybe Later".Translate(),
            SecondaryButtonText = null
        });

        if (result == ConfirmationResult.Primary)
        {
            App.OpenUpgradeModal();
        }
    }

    /// <summary>
    /// Shows a compelling upgrade prompt when the product limit is reached.
    /// </summary>
    /// <param name="limit">The product limit per category on the free plan.</param>
    public static async Task ShowProductLimitPromptAsync(int limit)
    {
        var dialog = App.ConfirmationDialog;
        if (dialog == null) return;

        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Product Limit Reached".Translate(),
            Message = string.Format(
                "You've added all {0} products included in your free plan.\n\nUpgrade to Premium for unlimited products, AI receipt scanning, predictive analytics, and priority support.".Translate(),
                limit),
            PrimaryButtonText = "Upgrade Now".Translate(),
            CancelButtonText = "Maybe Later".Translate(),
            SecondaryButtonText = null
        });

        if (result == ConfirmationResult.Primary)
        {
            App.OpenUpgradeModal();
        }
    }
}
