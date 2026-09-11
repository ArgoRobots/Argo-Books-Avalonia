using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Export writes the stored receipt file. A PDF used to be exported as its rendered page-1 JPEG
/// under a .pdf name, which PDF readers can't open and which dropped every other page.
/// </summary>
public class ReceiptExportTests
{
    [Fact]
    public void ExportFile_OfAPdfReceipt_IsTheOriginalPdf()
    {
        var pdf = "%PDF-1.4 two pages"u8.ToArray();
        var receipt = new Receipt
        {
            Id = "RCP-2026-00001",
            FileName = "Scan.pdf",
            FileType = "application/pdf",
            FileData = Convert.ToBase64String(pdf)
        };
        var folder = Directory.CreateTempSubdirectory().FullName;

        var path = ReceiptsPageViewModel.WriteExportFile(receipt, folder, "Receipt_RCP-2026-00001");

        Assert.Equal(".pdf", Path.GetExtension(path));
        Assert.Equal(pdf, File.ReadAllBytes(path!));
    }
}
