using ArgoBooks.Core.Models.AI;
using ArgoBooks.Localization;

namespace ArgoBooks.Services;

/// <summary>
/// Vetted, localized user-facing copy for each rescue rejection reason. The AI only ever chooses a
/// reason code; this is the single place that turns a code into text the user sees, so a hallucinating
/// model can never leak bad copy. An unrecognized code defaults to the UnsupportedStructure message.
/// </summary>
public static class ImportRescueMessages
{
    public static string ForReason(ImportRescueRejectionReason reason) => reason switch
    {
        ImportRescueRejectionReason.SummaryOrReport =>
            "This file looks like a summary or report (category totals and subtotals) rather than a list of individual records. Argo Books imports records like transactions, customers, or products, one per row.".Translate(),
        ImportRescueRejectionReason.NotArgoData =>
            "We couldn't find anything in this file that matches the kind of data Argo Books tracks.".Translate(),
        ImportRescueRejectionReason.EmptyOrUnreadable =>
            "This file didn't contain any readable data to import.".Translate(),
        ImportRescueRejectionReason.TooLarge =>
            "This file is too large to organize automatically. Try splitting it into smaller files and importing them one at a time.".Translate(),
        _ =>
            "This file's layout couldn't be matched to any Argo Books record. Files import best as a simple table with one record per row and a header row.".Translate(),
    };
}
