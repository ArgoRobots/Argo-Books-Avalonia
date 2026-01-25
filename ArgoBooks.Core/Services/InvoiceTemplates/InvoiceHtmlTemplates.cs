using ArgoBooks.Core.Models.Invoices;

namespace ArgoBooks.Core.Services.InvoiceTemplates;

/// <summary>
/// Contains base HTML templates for invoice emails.
/// All templates use email-safe HTML (table-based layouts, inline CSS).
/// </summary>
public static class InvoiceHtmlTemplates
{
    /// <summary>
    /// Professional template: Corporate look with colored header banner.
    /// </summary>
    public const string Professional = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #f3f4f6;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f3f4f6;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="600" style="background-color: {{BackgroundColor}}; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background-color: {{PrimaryColor}}; padding: 30px 40px; border-radius: 8px 8px 0 0;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td>
                                        {{#ShowLogo}}
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; max-height: 60px;">
                                        {{/ShowLogo}}
                                        {{^ShowLogo}}
                                        <span style="font-size: 24px; font-weight: bold; color: #ffffff;">{{CompanyName}}</span>
                                        {{/ShowLogo}}
                                    </td>
                                    <td align="right" style="vertical-align: top;">
                                        <span style="font-size: 28px; font-weight: bold; color: #ffffff; letter-spacing: 1px;">{{HeaderText}}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Invoice Info -->
                    <tr>
                        <td style="padding: 30px 40px 20px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="vertical-align: top; width: 50%;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;">Bill To</p>
                                        <p style="margin: 0 0 5px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">{{CustomerName}}</p>
                                        {{#CustomerAddress}}
                                        <p style="margin: 0; font-size: 14px; color: #6b7280; line-height: 1.5;">{{CustomerAddress}}</p>
                                        {{/CustomerAddress}}
                                        {{#CustomerEmail}}
                                        <p style="margin: 5px 0 0 0; font-size: 14px; color: #6b7280;">{{CustomerEmail}}</p>
                                        {{/CustomerEmail}}
                                    </td>
                                    <td style="vertical-align: top; text-align: right;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" style="margin-left: auto;">
                                            <tr>
                                                <td style="padding: 4px 15px 4px 0; font-size: 13px; color: #6b7280;">Invoice #</td>
                                                <td style="padding: 4px 0; font-size: 13px; font-weight: 600; color: {{TextColor}};">{{InvoiceNumber}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 4px 15px 4px 0; font-size: 13px; color: #6b7280;">Issue Date</td>
                                                <td style="padding: 4px 0; font-size: 13px; color: {{TextColor}};">{{IssueDate}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 4px 15px 4px 0; font-size: 13px; color: #6b7280;">Due Date</td>
                                                <td style="padding: 4px 0; font-size: 13px; {{#IsOverdue}}color: #dc2626; font-weight: 600;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</td>
                                            </tr>
                                            {{#ShowDueDateProminent}}
                                            <tr>
                                                <td colspan="2" style="padding-top: 10px;">
                                                    <span style="display: inline-block; background-color: {{#IsOverdue}}#fef2f2{{/IsOverdue}}{{^IsOverdue}}#f0fdf4{{/IsOverdue}}; color: {{#IsOverdue}}#dc2626{{/IsOverdue}}{{^IsOverdue}}{{AccentColor}}{{/IsOverdue}}; padding: 6px 12px; border-radius: 4px; font-size: 12px; font-weight: 600;">
                                                        {{#IsOverdue}}OVERDUE{{/IsOverdue}}{{^IsOverdue}}DUE: {{DueDate}}{{/IsOverdue}}
                                                    </span>
                                                </td>
                                            </tr>
                                            {{/ShowDueDateProminent}}
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td style="padding: 0 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse: collapse;">
                                <tr style="background-color: {{SecondaryColor}};">
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; border-radius: 4px 0 0 4px;">Description</td>
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; text-align: center; width: 80px;">Qty</td>
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; text-align: right; width: 100px;">Price</td>
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: 600; color: #374151; text-transform: uppercase; text-align: right; width: 100px; border-radius: 0 4px 4px 0;">Amount</td>
                                </tr>
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 15px; font-size: 14px; color: {{TextColor}}; border-bottom: 1px solid {{SecondaryColor}};">
                                        {{Description}}
                                        {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 12px; color: #6b7280;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                    </td>
                                    <td style="padding: 15px; font-size: 14px; color: #6b7280; text-align: center; border-bottom: 1px solid {{SecondaryColor}};">{{Quantity}}</td>
                                    <td style="padding: 15px; font-size: 14px; color: #6b7280; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{UnitPrice}}</td>
                                    <td style="padding: 15px; font-size: 14px; font-weight: 500; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{Amount}}</td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Totals -->
                    <tr>
                        <td style="padding: 20px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="280" style="margin-left: auto;">
                                <tr>
                                    <td style="padding: 8px 0; font-size: 14px; color: #6b7280;">Subtotal</td>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{Subtotal}}</td>
                                </tr>
                                {{#ShowTaxBreakdown}}
                                <tr>
                                    <td style="padding: 8px 0; font-size: 14px; color: #6b7280;">Tax ({{TaxRate}}%)</td>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{TaxAmount}}</td>
                                </tr>
                                {{/ShowTaxBreakdown}}
                                <tr>
                                    <td colspan="2" style="border-top: 2px solid {{SecondaryColor}}; padding-top: 12px;"></td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; font-size: 18px; font-weight: 700; color: {{TextColor}};">Total</td>
                                    <td style="padding: 8px 0; font-size: 18px; font-weight: 700; color: {{PrimaryColor}}; text-align: right;">{{Total}}</td>
                                </tr>
                                {{#AmountPaid}}
                                <tr>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{AccentColor}};">Amount Paid</td>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                                    <td style="padding: 8px 0; font-size: 16px; font-weight: 600; color: {{PrimaryColor}}; text-align: right;">{{Balance}}</td>
                                </tr>
                                {{/AmountPaid}}
                            </table>
                        </td>
                    </tr>

                    {{#ShowNotes}}
                    {{#Notes}}
                    <!-- Notes -->
                    <tr>
                        <td style="padding: 0 40px 20px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f9fafb; border-radius: 6px;">
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: 600; color: #6b7280; text-transform: uppercase;">Notes</p>
                                        <p style="margin: 0; font-size: 14px; color: #374151; line-height: 1.5;">{{Notes}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/Notes}}
                    {{/ShowNotes}}

                    {{#ShowPaymentInstructions}}
                    {{#PaymentInstructions}}
                    <!-- Payment Instructions -->
                    <tr>
                        <td style="padding: 0 40px 20px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #eff6ff; border-radius: 6px; border-left: 4px solid {{PrimaryColor}};">
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: 600; color: {{PrimaryColor}}; text-transform: uppercase;">Payment Instructions</p>
                                        <p style="margin: 0; font-size: 14px; color: #374151; line-height: 1.5; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 25px 40px; background-color: #f9fafb; border-radius: 0 0 8px 8px; text-align: center;">
                            <p style="margin: 0 0 10px 0; font-size: 14px; color: #374151;">{{FooterText}}</p>
                            {{#ShowCompanyAddress}}
                            <p style="margin: 0; font-size: 12px; color: #9ca3af;">{{CompanyName}} • {{CompanyAddress}}</p>
                            {{#CompanyEmail}}<p style="margin: 5px 0 0 0; font-size: 12px; color: #9ca3af;">{{CompanyEmail}} {{#CompanyPhone}}• {{CompanyPhone}}{{/CompanyPhone}}</p>{{/CompanyEmail}}
                            {{/ShowCompanyAddress}}
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";

    /// <summary>
    /// Modern template: Clean, minimalist design with plenty of whitespace.
    /// </summary>
    public const string Modern = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: {{BackgroundColor}};">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{BackgroundColor}};">
        <tr>
            <td align="center" style="padding: 60px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="600">
                    <!-- Header -->
                    <tr>
                        <td style="padding-bottom: 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="vertical-align: middle;">
                                        {{#ShowLogo}}
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; max-height: 50px;">
                                        {{/ShowLogo}}
                                        {{^ShowLogo}}
                                        <span style="font-size: 22px; font-weight: 600; color: {{TextColor}};">{{CompanyName}}</span>
                                        {{/ShowLogo}}
                                    </td>
                                    <td style="text-align: right; vertical-align: middle;">
                                        <span style="font-size: 11px; text-transform: uppercase; letter-spacing: 2px; color: #9ca3af;">{{HeaderText}}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Invoice Number -->
                    <tr>
                        <td style="padding-bottom: 40px;">
                            <span style="font-size: 32px; font-weight: 300; color: {{TextColor}};">{{InvoiceNumber}}</span>
                        </td>
                    </tr>

                    <!-- Details Grid -->
                    <tr>
                        <td style="padding-bottom: 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 50%; vertical-align: top; padding-right: 20px;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Billed To</p>
                                        <p style="margin: 0; font-size: 15px; font-weight: 500; color: {{TextColor}}; line-height: 1.6;">
                                            {{CustomerName}}<br>
                                            {{#CustomerAddress}}<span style="font-weight: 400; color: #6b7280;">{{CustomerAddress}}</span>{{/CustomerAddress}}
                                        </p>
                                    </td>
                                    <td style="width: 25%; vertical-align: top;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Issued</p>
                                        <p style="margin: 0; font-size: 15px; color: {{TextColor}};">{{IssueDate}}</p>
                                    </td>
                                    <td style="width: 25%; vertical-align: top; text-align: right;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Due</p>
                                        <p style="margin: 0; font-size: 15px; {{#IsOverdue}}color: #dc2626; font-weight: 500;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td>
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-top: 1px solid {{SecondaryColor}};">
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 20px 0; border-bottom: 1px solid {{SecondaryColor}};">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td style="vertical-align: top;">
                                                    <p style="margin: 0; font-size: 15px; color: {{TextColor}};">{{Description}}</p>
                                                    {{#ShowItemDescriptions}}{{#ItemDescription}}<p style="margin: 4px 0 0 0; font-size: 13px; color: #9ca3af;">{{ItemDescription}}</p>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                                </td>
                                                <td style="width: 80px; text-align: center; vertical-align: top;">
                                                    <p style="margin: 0; font-size: 14px; color: #6b7280;">{{Quantity}}</p>
                                                </td>
                                                <td style="width: 100px; text-align: right; vertical-align: top;">
                                                    <p style="margin: 0; font-size: 14px; color: #6b7280;">{{UnitPrice}}</p>
                                                </td>
                                                <td style="width: 100px; text-align: right; vertical-align: top;">
                                                    <p style="margin: 0; font-size: 15px; font-weight: 500; color: {{TextColor}};">{{Amount}}</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Totals -->
                    <tr>
                        <td style="padding: 30px 0;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="250" style="margin-left: auto;">
                                <tr>
                                    <td style="padding: 6px 0; font-size: 14px; color: #6b7280;">Subtotal</td>
                                    <td style="padding: 6px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{Subtotal}}</td>
                                </tr>
                                {{#ShowTaxBreakdown}}
                                <tr>
                                    <td style="padding: 6px 0; font-size: 14px; color: #6b7280;">Tax ({{TaxRate}}%)</td>
                                    <td style="padding: 6px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{TaxAmount}}</td>
                                </tr>
                                {{/ShowTaxBreakdown}}
                                <tr>
                                    <td colspan="2" style="padding: 15px 0 0 0; border-top: 1px solid {{SecondaryColor}};"></td>
                                </tr>
                                <tr>
                                    <td style="font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Total Due</td>
                                    <td style="font-size: 24px; font-weight: 300; color: {{PrimaryColor}}; text-align: right;">{{Total}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    {{#ShowPaymentInstructions}}
                    {{#PaymentInstructions}}
                    <!-- Payment Instructions -->
                    <tr>
                        <td style="padding: 30px 0; border-top: 1px solid {{SecondaryColor}};">
                            <p style="margin: 0 0 10px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Payment Details</p>
                            <p style="margin: 0; font-size: 14px; color: #6b7280; line-height: 1.6; white-space: pre-line;">{{PaymentInstructions}}</p>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    {{#ShowNotes}}
                    {{#Notes}}
                    <!-- Notes -->
                    <tr>
                        <td style="padding: 30px 0; border-top: 1px solid {{SecondaryColor}};">
                            <p style="margin: 0 0 10px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af;">Notes</p>
                            <p style="margin: 0; font-size: 14px; color: #6b7280; line-height: 1.6;">{{Notes}}</p>
                        </td>
                    </tr>
                    {{/Notes}}
                    {{/ShowNotes}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding-top: 40px; text-align: center;">
                            <p style="margin: 0; font-size: 13px; color: #9ca3af;">{{FooterText}}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";

    /// <summary>
    /// Classic template: Traditional invoice layout with borders and structured sections.
    /// </summary>
    public const string Classic = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #ffffff;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #ffffff;">
        <tr>
            <td align="center" style="padding: 30px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="600" style="border: 2px solid {{PrimaryColor}};">
                    <!-- Header -->
                    <tr>
                        <td style="padding: 20px; border-bottom: 2px solid {{PrimaryColor}};">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 60%;">
                                        {{#ShowLogo}}
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; max-height: 60px; margin-bottom: 10px;">
                                        {{/ShowLogo}}
                                        <p style="margin: 0; font-size: 18px; font-weight: bold; color: {{TextColor}};">{{CompanyName}}</p>
                                        {{#ShowCompanyAddress}}
                                        <p style="margin: 5px 0 0 0; font-size: 12px; color: #666666; line-height: 1.5;">{{CompanyAddress}}</p>
                                        {{#CompanyEmail}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyEmail}}</p>{{/CompanyEmail}}
                                        {{#CompanyPhone}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyPhone}}</p>{{/CompanyPhone}}
                                        {{/ShowCompanyAddress}}
                                    </td>
                                    <td style="width: 40%; text-align: right; vertical-align: top;">
                                        <p style="margin: 0; font-size: 28px; font-weight: bold; color: {{PrimaryColor}};">{{HeaderText}}</p>
                                        <p style="margin: 10px 0 0 0; font-size: 14px; color: {{TextColor}};"><strong>Invoice #:</strong> {{InvoiceNumber}}</p>
                                        <p style="margin: 5px 0 0 0; font-size: 14px; color: {{TextColor}};"><strong>Date:</strong> {{IssueDate}}</p>
                                        <p style="margin: 5px 0 0 0; font-size: 14px; {{#IsOverdue}}color: #dc2626;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}"><strong>Due:</strong> {{DueDate}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Bill To -->
                    <tr>
                        <td style="padding: 20px; background-color: #f8f9fa; border-bottom: 1px solid {{SecondaryColor}};">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td>
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{PrimaryColor}}; text-transform: uppercase;">Bill To:</p>
                                        <p style="margin: 0; font-size: 15px; font-weight: bold; color: {{TextColor}};">{{CustomerName}}</p>
                                        {{#CustomerAddress}}<p style="margin: 3px 0 0 0; font-size: 13px; color: #666666;">{{CustomerAddress}}</p>{{/CustomerAddress}}
                                        {{#CustomerEmail}}<p style="margin: 3px 0 0 0; font-size: 13px; color: #666666;">{{CustomerEmail}}</p>{{/CustomerEmail}}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse: collapse;">
                                <tr style="background-color: {{PrimaryColor}};">
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: bold; color: #ffffff; border: 1px solid {{PrimaryColor}};">DESCRIPTION</td>
                                    <td style="padding: 12px 10px; font-size: 12px; font-weight: bold; color: #ffffff; text-align: center; width: 60px; border: 1px solid {{PrimaryColor}};">QTY</td>
                                    <td style="padding: 12px 10px; font-size: 12px; font-weight: bold; color: #ffffff; text-align: right; width: 90px; border: 1px solid {{PrimaryColor}};">RATE</td>
                                    <td style="padding: 12px 15px; font-size: 12px; font-weight: bold; color: #ffffff; text-align: right; width: 90px; border: 1px solid {{PrimaryColor}};">AMOUNT</td>
                                </tr>
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 12px 15px; font-size: 13px; color: {{TextColor}}; border: 1px solid {{SecondaryColor}};">
                                        {{Description}}
                                        {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 11px; color: #888888;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                    </td>
                                    <td style="padding: 12px 10px; font-size: 13px; color: {{TextColor}}; text-align: center; border: 1px solid {{SecondaryColor}};">{{Quantity}}</td>
                                    <td style="padding: 12px 10px; font-size: 13px; color: {{TextColor}}; text-align: right; border: 1px solid {{SecondaryColor}};">{{UnitPrice}}</td>
                                    <td style="padding: 12px 15px; font-size: 13px; font-weight: bold; color: {{TextColor}}; text-align: right; border: 1px solid {{SecondaryColor}};">{{Amount}}</td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Totals -->
                    <tr>
                        <td style="padding: 0;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 60%; padding: 15px; vertical-align: top;">
                                        {{#ShowNotes}}
                                        {{#Notes}}
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{PrimaryColor}};">NOTES:</p>
                                        <p style="margin: 0; font-size: 12px; color: #666666; line-height: 1.5;">{{Notes}}</p>
                                        {{/Notes}}
                                        {{/ShowNotes}}
                                    </td>
                                    <td style="width: 40%; border-left: 1px solid {{SecondaryColor}};">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td style="padding: 10px 15px; font-size: 13px; color: #666666; border-bottom: 1px solid {{SecondaryColor}};">Subtotal</td>
                                                <td style="padding: 10px 15px; font-size: 13px; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{Subtotal}}</td>
                                            </tr>
                                            {{#ShowTaxBreakdown}}
                                            <tr>
                                                <td style="padding: 10px 15px; font-size: 13px; color: #666666; border-bottom: 1px solid {{SecondaryColor}};">Tax ({{TaxRate}}%)</td>
                                                <td style="padding: 10px 15px; font-size: 13px; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{TaxAmount}}</td>
                                            </tr>
                                            {{/ShowTaxBreakdown}}
                                            <tr style="background-color: {{PrimaryColor}};">
                                                <td style="padding: 12px 15px; font-size: 14px; font-weight: bold; color: #ffffff;">TOTAL</td>
                                                <td style="padding: 12px 15px; font-size: 16px; font-weight: bold; color: #ffffff; text-align: right;">{{Total}}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    {{#ShowPaymentInstructions}}
                    {{#PaymentInstructions}}
                    <!-- Payment Instructions -->
                    <tr>
                        <td style="padding: 15px; border-top: 1px solid {{SecondaryColor}}; background-color: #f8f9fa;">
                            <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{PrimaryColor}};">PAYMENT INSTRUCTIONS:</p>
                            <p style="margin: 0; font-size: 12px; color: #666666; line-height: 1.5; white-space: pre-line;">{{PaymentInstructions}}</p>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 15px; background-color: {{PrimaryColor}}; text-align: center;">
                            <p style="margin: 0; font-size: 13px; color: #ffffff;">{{FooterText}}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";

    /// <summary>
    /// Minimal template: Bare essentials, very simple and clean.
    /// </summary>
    public const string Minimal = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #ffffff;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #ffffff;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="550">
                    <!-- Header -->
                    <tr>
                        <td style="padding-bottom: 30px; border-bottom: 1px solid #e5e7eb;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td>
                                        {{#ShowLogo}}
                                        <img src="{{LogoSrc}}" alt="Logo" width="{{LogoWidth}}" style="display: block; max-height: 40px;">
                                        {{/ShowLogo}}
                                        {{^ShowLogo}}
                                        <span style="font-size: 16px; font-weight: 600; color: {{TextColor}};">{{CompanyName}}</span>
                                        {{/ShowLogo}}
                                    </td>
                                    <td style="text-align: right;">
                                        <span style="font-size: 14px; color: #6b7280;">{{InvoiceNumber}}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Details -->
                    <tr>
                        <td style="padding: 25px 0;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="vertical-align: top;">
                                        <p style="margin: 0 0 3px 0; font-size: 12px; color: #9ca3af;">To</p>
                                        <p style="margin: 0; font-size: 14px; color: {{TextColor}};">{{CustomerName}}</p>
                                    </td>
                                    <td style="text-align: right; vertical-align: top;">
                                        <p style="margin: 0 0 3px 0; font-size: 12px; color: #9ca3af;">Date</p>
                                        <p style="margin: 0 0 15px 0; font-size: 14px; color: {{TextColor}};">{{IssueDate}}</p>
                                        <p style="margin: 0 0 3px 0; font-size: 12px; color: #9ca3af;">Due</p>
                                        <p style="margin: 0; font-size: 14px; {{#IsOverdue}}color: #dc2626;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td>
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 12px 0; border-bottom: 1px solid #f3f4f6;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td>
                                                    <p style="margin: 0; font-size: 14px; color: {{TextColor}};">{{Description}}</p>
                                                    <p style="margin: 3px 0 0 0; font-size: 12px; color: #9ca3af;">{{Quantity}} × {{UnitPrice}}</p>
                                                </td>
                                                <td style="text-align: right; vertical-align: top;">
                                                    <p style="margin: 0; font-size: 14px; color: {{TextColor}};">{{Amount}}</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Total -->
                    <tr>
                        <td style="padding: 20px 0; border-top: 1px solid #e5e7eb;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                {{#ShowTaxBreakdown}}
                                <tr>
                                    <td style="font-size: 13px; color: #6b7280; padding-bottom: 8px;">Subtotal</td>
                                    <td style="font-size: 13px; color: {{TextColor}}; text-align: right; padding-bottom: 8px;">{{Subtotal}}</td>
                                </tr>
                                <tr>
                                    <td style="font-size: 13px; color: #6b7280; padding-bottom: 12px;">Tax</td>
                                    <td style="font-size: 13px; color: {{TextColor}}; text-align: right; padding-bottom: 12px;">{{TaxAmount}}</td>
                                </tr>
                                {{/ShowTaxBreakdown}}
                                <tr>
                                    <td style="font-size: 16px; font-weight: 600; color: {{TextColor}};">Total</td>
                                    <td style="font-size: 18px; font-weight: 600; color: {{PrimaryColor}}; text-align: right;">{{Total}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    {{#ShowPaymentInstructions}}
                    {{#PaymentInstructions}}
                    <!-- Payment -->
                    <tr>
                        <td style="padding: 20px 0; border-top: 1px solid #e5e7eb;">
                            <p style="margin: 0 0 8px 0; font-size: 12px; color: #9ca3af;">Payment</p>
                            <p style="margin: 0; font-size: 13px; color: #6b7280; line-height: 1.5; white-space: pre-line;">{{PaymentInstructions}}</p>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding-top: 30px; text-align: center;">
                            <p style="margin: 0; font-size: 13px; color: #9ca3af;">{{FooterText}}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";

    /// <summary>
    /// Gets the HTML template for the specified template type.
    /// </summary>
    public static string GetTemplate(InvoiceTemplateType templateType)
    {
        return templateType switch
        {
            InvoiceTemplateType.Professional => Professional,
            InvoiceTemplateType.Modern => Modern,
            InvoiceTemplateType.Classic => Classic,
            InvoiceTemplateType.Minimal => Minimal,
            _ => Professional
        };
    }
}
