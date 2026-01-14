# Azure Document Intelligence Setup Guide

This guide walks you through setting up Azure Document Intelligence for AI-powered receipt scanning in ArgoBooks.

## Overview

Azure Document Intelligence (formerly Form Recognizer) provides highly accurate receipt scanning with:
- **98%+ field-level accuracy** for receipts
- **Line item extraction** (item descriptions, quantities, prices)
- **Multi-currency support**
- **500 free pages/month** on the free tier

## Step 1: Create an Azure Account

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Start free** or sign in with your existing Microsoft account
3. New accounts receive **$200 free credit** for 30 days

## Step 2: Create a Document Intelligence Resource

1. In Azure Portal, click **+ Create a resource**
2. Search for **"Document Intelligence"** (or "Form Recognizer")
3. Click **Create**

### Configure the Resource

| Setting | Value |
|---------|-------|
| **Subscription** | Your Azure subscription |
| **Resource group** | Create new or select existing |
| **Region** | Choose closest to your location |
| **Name** | Unique name (e.g., `argobooks-receipts`) |
| **Pricing tier** | **Free F0** (500 pages/month) or **Standard S0** |

4. Click **Review + create**, then **Create**
5. Wait for deployment to complete (usually 1-2 minutes)

## Step 3: Get Your API Keys

1. After deployment, click **Go to resource**
2. In the left menu, click **Keys and Endpoint**
3. Copy these values:
   - **Endpoint**: `https://your-resource-name.cognitiveservices.azure.com/`
   - **KEY 1** or **KEY 2**: Your API key

> **Security Note**: Never share your API key. ArgoBooks encrypts it locally on your machine.

## Step 4: Configure ArgoBooks

1. Open ArgoBooks
2. Go to **Settings** (gear icon in header)
3. Navigate to **AI Receipt Scanning** section
4. Enter your Azure credentials:
   - **Endpoint URL**: Paste the endpoint from Step 3
   - **API Key**: Paste either KEY 1 or KEY 2
5. Click **Save**

## Using AI Receipt Scanning

Once configured, you can scan receipts in two ways:

### Method 1: Click to Upload
1. Go to the **Receipts** page
2. Click the **AI Scan Receipt** button
3. Select a receipt image (JPEG, PNG, or PDF)
4. Review the extracted data
5. Click **Create Expense**

### Method 2: Drag and Drop
1. Go to the **Receipts** page
2. Drag a receipt image onto the page
3. Drop to start scanning
4. Review and create the expense

## Pricing

### Free Tier (F0)
- **500 pages/month** free
- Perfect for small businesses
- 4MB file size limit
- First 2 pages per document analyzed

### Standard Tier (S0)
- **$10 per 1,000 pages** for receipts
- 500MB file size limit
- All pages analyzed
- Higher rate limits

## Supported File Types

| Format | Extensions |
|--------|------------|
| **JPEG** | `.jpg`, `.jpeg` |
| **PNG** | `.png` |
| **PDF** | `.pdf` |
| **BMP** | `.bmp` |
| **TIFF** | `.tiff`, `.tif` |

## Best Practices for Accurate Scanning

1. **Good lighting** - Avoid shadows on the receipt
2. **Flat surface** - Lay receipt flat, no wrinkles
3. **Clear image** - Minimum 50 DPI resolution
4. **Full receipt** - Include all edges
5. **Readable text** - Avoid blurry or faded receipts

## Troubleshooting

### "Invalid API key" Error
- Double-check the API key in Settings
- Ensure you copied the full key without extra spaces
- Try using KEY 2 instead of KEY 1

### "Access denied" Error
- Verify your Azure subscription is active
- Check that the Document Intelligence resource is running
- Ensure the endpoint URL is correct

### "Rate limit exceeded" Error
- Wait a few minutes and try again
- Consider upgrading to Standard tier for higher limits

### Low Confidence Results
- Improve image quality (better lighting, higher resolution)
- Ensure the full receipt is visible
- Try flattening wrinkled receipts

## Data Privacy

- Receipt images are sent to Azure for analysis
- Azure does not store your receipt data after processing
- Your API key is encrypted and stored locally
- ArgoBooks does not have access to your Azure credentials

## Additional Resources

- [Azure Document Intelligence Documentation](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/)
- [Receipt Model Documentation](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/prebuilt/receipt)
- [Pricing Details](https://azure.microsoft.com/en-us/pricing/details/ai-document-intelligence/)
- [Azure Free Account FAQ](https://azure.microsoft.com/en-us/free/free-account-faq/)

## Support

If you encounter issues with Azure Document Intelligence:
1. Check the [Azure Status Page](https://status.azure.com/)
2. Review the [Azure AI Services troubleshooting guide](https://learn.microsoft.com/en-us/azure/ai-services/troubleshoot/)
3. Contact Azure Support through the Azure Portal

For ArgoBooks-specific issues, please open an issue on the GitHub repository.
