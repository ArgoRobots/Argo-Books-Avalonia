# Receipt Scanning

Argo Books extracts structured data from receipts using Google Gemini's vision model. Take a photo or drop in a PDF; the AI returns line items, totals, supplier name, date, and currency. A second AI pass then matches the supplier and category against your existing records before you review and save as an expense or revenue.

All AI calls route through the Argo Books server. Your device never holds a Google API key. The server is authenticated by your license key and enforces a monthly scan quota. See [LicenseKey](LicenseKey.md) for the quota mechanics.

## Supported formats

JPEG, PNG, PDF, BMP, TIFF.

Images are pre-processed before scanning: EXIF orientation is corrected, contrast is boosted slightly, and the image is sharpened to help with faded thermal receipts. Receipts over 4 MB are progressively downscaled until they fit.

For multi-page PDFs, the preview shows page 1, but the AI sees every page.

## Scanning a receipt

1. Click **AI Scan** on the Receipts page.
2. Select the receipt file.
3. The image is processed and sent to the AI for extraction.
4. The result modal opens with the extracted fields pre-filled: supplier, date, totals, currency, payment method, and a line-item table.
5. A second AI pass runs in the background to suggest a supplier and category match against your existing records.
6. Review the result, edit anything the AI got wrong, and save it.

A transaction is automatically created and the receipt is attached to it. The receipt is also added to the Receipts page.

## Bulk scanning

Select multiple receipts to queue a bulk scan. Three receipts are scanned in parallel; the rest wait their turn. When the batch finishes, a review carousel walks you through approving or skipping each result.

## Supplier and category matching

After OCR returns, a second AI call compares the extracted supplier and line items against your existing supplier and category lists.

**Supplier matching**: recognises exact names, common variations ("Walmart" ≈ "Walmart Inc."), and known abbreviations. If no confident match exists, the modal suggests creating a new supplier with the cleaned-up name.

**Category matching**: chosen based on the line items and supplier type. Vague names like "General", "Expenses", or "Miscellaneous" are explicitly rejected. The AI is pushed toward specific categories or new-category creation.

You can override either suggestion in the modal before saving.

## Multi-currency receipts

The AI infers the currency from address, language, currency symbol, and tax labels (GST → CAD, VAT → EUR/GBP, etc.) and returns an ISO 4217 code. If detection is ambiguous, USD is used as the fallback. You can override the currency in the modal before saving.

## Quota

Each scan counts against your monthly limit (the limit depends on your subscription tier). The quota is checked inside the scan modal before each scan and incremented on success. The dashboard's Quick Scan widget also pre-checks the quota before opening the file picker.

If you're offline, scans are allowed when cached quota data shows remaining capacity. If the quota is exhausted, scans are blocked until the network returns to confirm.

## Notable limitations

- **No offline OCR.** Scanning requires an internet connection.
- **No automatic retries.** Failed scans surface in the modal and you retry manually.
- **Receipt images live in the `.argo` file.** Large receipt libraries grow the file proportionally.
