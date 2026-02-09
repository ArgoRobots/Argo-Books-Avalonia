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
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 60px;{{/LockAspectRatio}}">
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
                            <p style="margin: 0; font-size: 12px; color: #9ca3af;">
                                {{CompanyName}}{{#ShowCompanyAddress}}{{#CompanyAddress}} • {{CompanyAddress}}{{/CompanyAddress}}{{/ShowCompanyAddress}}{{#ShowCompanyCity}}{{#CompanyCity}} • {{CompanyCity}}{{/CompanyCity}}{{/ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}} • {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}} • {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}
                            </p>
                            {{#CompanyEmail}}<p style="margin: 5px 0 0 0; font-size: 12px; color: #9ca3af;">{{CompanyEmail}}{{#ShowCompanyPhone}}{{#CompanyPhone}} • {{CompanyPhone}}{{/CompanyPhone}}{{/ShowCompanyPhone}}</p>{{/CompanyEmail}}
                            {{^CompanyEmail}}{{#ShowCompanyPhone}}{{#CompanyPhone}}<p style="margin: 5px 0 0 0; font-size: 12px; color: #9ca3af;">{{CompanyPhone}}</p>{{/CompanyPhone}}{{/ShowCompanyPhone}}{{/CompanyEmail}}
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
    /// Modern template: Contemporary design with sidebar accent and clean layout.
    /// </summary>
    public const string Modern = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #f0f4f8;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f0f4f8;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="600" style="background-color: {{BackgroundColor}}; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);">
                    <!-- Header with accent bar -->
                    <tr>
                        <td>
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 8px; background-color: {{PrimaryColor}};"></td>
                                    <td style="padding: 30px 35px; background-color: {{SecondaryColor}};">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td style="vertical-align: middle;">
                                                    {{#ShowLogo}}
                                                    <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 55px;{{/LockAspectRatio}}">
                                                    {{/ShowLogo}}
                                                    {{^ShowLogo}}
                                                    <span style="font-size: 22px; font-weight: 700; color: {{TextColor}};">{{CompanyName}}</span>
                                                    {{/ShowLogo}}
                                                </td>
                                                <td style="text-align: right; vertical-align: middle;">
                                                    <p style="margin: 0 0 4px 0; font-size: 24px; font-weight: 700; color: {{PrimaryColor}}; letter-spacing: -0.5px;">{{HeaderText}}</p>
                                                    <p style="margin: 0; font-size: 14px; color: #6b7280;">{{InvoiceNumber}}</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Invoice Details -->
                    <tr>
                        <td style="padding: 30px 35px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 55%; vertical-align: top; padding-right: 20px;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1.5px; color: {{PrimaryColor}}; font-weight: 600;">Bill To</p>
                                        <p style="margin: 0 0 4px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">{{CustomerName}}</p>
                                        {{#CustomerAddress}}<p style="margin: 0 0 2px 0; font-size: 13px; color: #6b7280; line-height: 1.5;">{{CustomerAddress}}</p>{{/CustomerAddress}}
                                        {{#CustomerEmail}}<p style="margin: 4px 0 0 0; font-size: 13px; color: #6b7280;">{{CustomerEmail}}</p>{{/CustomerEmail}}
                                    </td>
                                    <td style="width: 45%; vertical-align: top;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{SecondaryColor}}; border-radius: 6px;">
                                            <tr>
                                                <td style="padding: 15px;">
                                                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                                        <tr>
                                                            <td style="padding: 4px 0; font-size: 12px; color: #6b7280;">Issue Date</td>
                                                            <td style="padding: 4px 0; font-size: 13px; font-weight: 500; color: {{TextColor}}; text-align: right;">{{IssueDate}}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style="padding: 4px 0; font-size: 12px; color: #6b7280;">Due Date</td>
                                                            <td style="padding: 4px 0; font-size: 13px; font-weight: 500; text-align: right; {{#IsOverdue}}color: #dc2626;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</td>
                                                        </tr>
                                                        {{#ShowDueDateProminent}}
                                                        <tr>
                                                            <td colspan="2" style="padding-top: 10px;">
                                                                <span style="display: inline-block; width: 100%; text-align: center; background-color: {{#IsOverdue}}#fef2f2{{/IsOverdue}}{{^IsOverdue}}{{BackgroundColor}}{{/IsOverdue}}; color: {{#IsOverdue}}#dc2626{{/IsOverdue}}{{^IsOverdue}}{{PrimaryColor}}{{/IsOverdue}}; padding: 8px 0; border-radius: 4px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;">
                                                                    {{#IsOverdue}}⚠ Overdue{{/IsOverdue}}{{^IsOverdue}}Due: {{DueDate}}{{/IsOverdue}}
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
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td style="padding: 0 35px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse: collapse;">
                                <tr style="background-color: {{PrimaryColor}};">
                                    <td style="padding: 12px 15px; font-size: 11px; font-weight: 600; color: #ffffff; text-transform: uppercase; letter-spacing: 0.5px; border-radius: 6px 0 0 6px;">Description</td>
                                    <td style="padding: 12px 10px; font-size: 11px; font-weight: 600; color: #ffffff; text-transform: uppercase; letter-spacing: 0.5px; text-align: center; width: 70px;">Qty</td>
                                    <td style="padding: 12px 10px; font-size: 11px; font-weight: 600; color: #ffffff; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 90px;">Rate</td>
                                    <td style="padding: 12px 15px; font-size: 11px; font-weight: 600; color: #ffffff; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 100px; border-radius: 0 6px 6px 0;">Amount</td>
                                </tr>
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 16px 15px; font-size: 14px; color: {{TextColor}}; border-bottom: 1px solid {{SecondaryColor}};">
                                        {{Description}}
                                        {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 12px; color: #9ca3af;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                    </td>
                                    <td style="padding: 16px 10px; font-size: 14px; color: #6b7280; text-align: center; border-bottom: 1px solid {{SecondaryColor}};">{{Quantity}}</td>
                                    <td style="padding: 16px 10px; font-size: 14px; color: #6b7280; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{UnitPrice}}</td>
                                    <td style="padding: 16px 15px; font-size: 14px; font-weight: 600; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{Amount}}</td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Totals and Notes Row -->
                    <tr>
                        <td style="padding: 25px 35px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 55%; vertical-align: top; padding-right: 30px;">
                                        {{#ShowNotes}}
                                        {{#Notes}}
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{PrimaryColor}}; font-weight: 600;">Notes</p>
                                        <p style="margin: 0; font-size: 13px; color: #6b7280; line-height: 1.6;">{{Notes}}</p>
                                        {{/Notes}}
                                        {{/ShowNotes}}
                                    </td>
                                    <td style="width: 45%;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Subtotal</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{Subtotal}}</td>
                                            </tr>
                                            {{#ShowTaxBreakdown}}
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Tax ({{TaxRate}}%)</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{TaxAmount}}</td>
                                            </tr>
                                            {{/ShowTaxBreakdown}}
                                            <tr>
                                                <td colspan="2" style="padding-top: 12px; border-top: 2px solid {{PrimaryColor}};"></td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 16px; font-weight: 700; color: {{TextColor}};">Total</td>
                                                <td style="padding: 8px 0; font-size: 20px; font-weight: 700; color: {{PrimaryColor}}; text-align: right;">{{Total}}</td>
                                            </tr>
                                            {{#AmountPaid}}
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}};">Paid</td>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                                                <td style="padding: 6px 0; font-size: 17px; font-weight: 700; color: {{PrimaryColor}}; text-align: right;">{{Balance}}</td>
                                            </tr>
                                            {{/AmountPaid}}
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
                        <td style="padding: 0 35px 25px 35px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{SecondaryColor}}; border-radius: 6px; border-left: 4px solid {{AccentColor}};">
                                <tr>
                                    <td style="padding: 18px 20px;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{AccentColor}}; font-weight: 600;">Payment Instructions</p>
                                        <p style="margin: 0; font-size: 13px; color: #4b5563; line-height: 1.6; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    <!-- Footer -->
                    <tr>
                        <td>
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 8px; background-color: {{PrimaryColor}};"></td>
                                    <td style="padding: 20px 35px; background-color: {{SecondaryColor}}; text-align: center;">
                                        <p style="margin: 0 0 6px 0; font-size: 13px; color: #4b5563;">{{FooterText}}</p>
                                        <p style="margin: 0; font-size: 11px; color: #9ca3af;">
                                            {{CompanyName}}{{#ShowCompanyAddress}}{{#CompanyAddress}} • {{CompanyAddress}}{{/CompanyAddress}}{{/ShowCompanyAddress}}{{#ShowCompanyCity}}{{#CompanyCity}} • {{CompanyCity}}{{/CompanyCity}}{{/ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}} • {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}} • {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}{{#ShowCompanyPhone}}{{#CompanyPhone}} • {{CompanyPhone}}{{/CompanyPhone}}{{/ShowCompanyPhone}}
                                        </p>
                                    </td>
                                </tr>
                            </table>
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
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 60px;{{/LockAspectRatio}} margin-bottom: 10px;">
                                        {{/ShowLogo}}
                                        <p style="margin: 0; font-size: 18px; font-weight: bold; color: {{TextColor}};">{{CompanyName}}</p>
                                        {{#ShowCompanyAddress}}{{#CompanyAddress}}<p style="margin: 5px 0 0 0; font-size: 12px; color: #666666; line-height: 1.5;">{{CompanyAddress}}</p>{{/CompanyAddress}}{{/ShowCompanyAddress}}
                                        {{#ShowCompanyCity}}{{#CompanyCity}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}, {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyCity}}{{/ShowCompanyCity}}
                                        {{^ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{^ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyCountry}}</p>{{/CompanyCountry}}{{/ShowCompanyCountry}}{{/ShowCompanyProvinceState}}{{/ShowCompanyCity}}
                                        {{#CompanyEmail}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyEmail}}</p>{{/CompanyEmail}}
                                        {{#ShowCompanyPhone}}{{#CompanyPhone}}<p style="margin: 2px 0 0 0; font-size: 12px; color: #666666;">{{CompanyPhone}}</p>{{/CompanyPhone}}{{/ShowCompanyPhone}}
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
    /// Elegant template: Sophisticated design with accent borders and refined typography.
    /// </summary>
    public const string Elegant = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #f8f9fa;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f8f9fa;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="600" style="background-color: {{BackgroundColor}}; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);">
                    <!-- Accent Top Border -->
                    <tr>
                        <td style="height: 4px; background: linear-gradient(90deg, {{PrimaryColor}}, {{AccentColor}});"></td>
                    </tr>

                    <!-- Header -->
                    <tr>
                        <td style="padding: 35px 40px 25px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 60%; vertical-align: top;">
                                        {{#ShowLogo}}
                                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 55px;{{/LockAspectRatio}} margin-bottom: 15px;">
                                        {{/ShowLogo}}
                                        {{^ShowLogo}}
                                        <p style="margin: 0 0 15px 0; font-size: 20px; font-weight: 600; color: {{TextColor}}; letter-spacing: -0.5px;">{{CompanyName}}</p>
                                        {{/ShowLogo}}
                                        {{#ShowCompanyAddress}}{{#CompanyAddress}}<p style="margin: 0 0 3px 0; font-size: 13px; color: #6b7280; line-height: 1.5;">{{CompanyAddress}}</p>{{/CompanyAddress}}{{/ShowCompanyAddress}}
                                        {{#ShowCompanyCity}}{{#CompanyCity}}<p style="margin: 0 0 3px 0; font-size: 13px; color: #6b7280;">{{CompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}, {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyCity}}{{/ShowCompanyCity}}
                                        {{^ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}<p style="margin: 0 0 3px 0; font-size: 13px; color: #6b7280;">{{CompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{^ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}<p style="margin: 0 0 3px 0; font-size: 13px; color: #6b7280;">{{CompanyCountry}}</p>{{/CompanyCountry}}{{/ShowCompanyCountry}}{{/ShowCompanyProvinceState}}{{/ShowCompanyCity}}
                                        {{#CompanyEmail}}<p style="margin: 0 0 3px 0; font-size: 13px; color: #6b7280;">{{CompanyEmail}}</p>{{/CompanyEmail}}
                                        {{#ShowCompanyPhone}}{{#CompanyPhone}}<p style="margin: 0; font-size: 13px; color: #6b7280;">{{CompanyPhone}}</p>{{/CompanyPhone}}{{/ShowCompanyPhone}}
                                    </td>
                                    <td style="width: 40%; text-align: right; vertical-align: top;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 2px; color: {{PrimaryColor}}; font-weight: 600;">{{HeaderText}}</p>
                                        <p style="margin: 0 0 20px 0; font-size: 22px; font-weight: 300; color: {{TextColor}};">{{InvoiceNumber}}</p>
                                        <table role="presentation" cellpadding="0" cellspacing="0" style="margin-left: auto;">
                                            <tr>
                                                <td style="padding: 3px 12px 3px 0; font-size: 12px; color: #9ca3af; text-align: right;">Issued</td>
                                                <td style="padding: 3px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{IssueDate}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 3px 12px 3px 0; font-size: 12px; color: #9ca3af; text-align: right;">Due</td>
                                                <td style="padding: 3px 0; font-size: 13px; text-align: right; {{#IsOverdue}}color: #dc2626; font-weight: 600;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</td>
                                            </tr>
                                        </table>
                                        {{#ShowDueDateProminent}}
                                        <p style="margin: 15px 0 0 0;">
                                            <span style="display: inline-block; background-color: {{#IsOverdue}}#fef2f2{{/IsOverdue}}{{^IsOverdue}}{{SecondaryColor}}{{/IsOverdue}}; color: {{#IsOverdue}}#dc2626{{/IsOverdue}}{{^IsOverdue}}{{PrimaryColor}}{{/IsOverdue}}; padding: 6px 14px; border-radius: 20px; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;">
                                                {{#IsOverdue}}Overdue{{/IsOverdue}}{{^IsOverdue}}Due {{DueDate}}{{/IsOverdue}}
                                            </span>
                                        </p>
                                        {{/ShowDueDateProminent}}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Bill To Section -->
                    <tr>
                        <td style="padding: 0 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-left: 3px solid {{PrimaryColor}}; padding-left: 15px;">
                                <tr>
                                    <td>
                                        <p style="margin: 0 0 5px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af; font-weight: 500;">Bill To</p>
                                        <p style="margin: 0 0 3px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">{{CustomerName}}</p>
                                        {{#CustomerAddress}}<p style="margin: 0 0 2px 0; font-size: 13px; color: #6b7280; line-height: 1.5;">{{CustomerAddress}}</p>{{/CustomerAddress}}
                                        {{#CustomerEmail}}<p style="margin: 0; font-size: 13px; color: #6b7280;">{{CustomerEmail}}</p>{{/CustomerEmail}}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Line Items -->
                    <tr>
                        <td style="padding: 30px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse: collapse;">
                                <tr>
                                    <td style="padding: 12px 0; font-size: 11px; font-weight: 600; color: #9ca3af; text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 2px solid {{SecondaryColor}};">Description</td>
                                    <td style="padding: 12px 0; font-size: 11px; font-weight: 600; color: #9ca3af; text-transform: uppercase; letter-spacing: 0.5px; text-align: center; width: 70px; border-bottom: 2px solid {{SecondaryColor}};">Qty</td>
                                    <td style="padding: 12px 0; font-size: 11px; font-weight: 600; color: #9ca3af; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 90px; border-bottom: 2px solid {{SecondaryColor}};">Rate</td>
                                    <td style="padding: 12px 0; font-size: 11px; font-weight: 600; color: #9ca3af; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 100px; border-bottom: 2px solid {{SecondaryColor}};">Amount</td>
                                </tr>
                                {{#LineItems}}
                                <tr>
                                    <td style="padding: 16px 0; font-size: 14px; color: {{TextColor}}; border-bottom: 1px solid {{SecondaryColor}};">
                                        {{Description}}
                                        {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 12px; color: #9ca3af; font-style: italic;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                    </td>
                                    <td style="padding: 16px 0; font-size: 14px; color: #6b7280; text-align: center; border-bottom: 1px solid {{SecondaryColor}};">{{Quantity}}</td>
                                    <td style="padding: 16px 0; font-size: 14px; color: #6b7280; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{UnitPrice}}</td>
                                    <td style="padding: 16px 0; font-size: 14px; font-weight: 500; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{Amount}}</td>
                                </tr>
                                {{/LineItems}}
                            </table>
                        </td>
                    </tr>

                    <!-- Totals -->
                    <tr>
                        <td style="padding: 0 40px 30px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 55%; vertical-align: top; padding-right: 30px;">
                                        {{#ShowNotes}}
                                        {{#Notes}}
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af; font-weight: 500;">Notes</p>
                                        <p style="margin: 0; font-size: 13px; color: #6b7280; line-height: 1.6;">{{Notes}}</p>
                                        {{/Notes}}
                                        {{/ShowNotes}}
                                    </td>
                                    <td style="width: 45%;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Subtotal</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{Subtotal}}</td>
                                            </tr>
                                            {{#ShowTaxBreakdown}}
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Tax ({{TaxRate}}%)</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{TaxAmount}}</td>
                                            </tr>
                                            {{/ShowTaxBreakdown}}
                                            <tr>
                                                <td colspan="2" style="padding-top: 12px; border-top: 2px solid {{PrimaryColor}};"></td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af; font-weight: 500;">Total Due</td>
                                                <td style="padding: 8px 0; font-size: 22px; font-weight: 600; color: {{PrimaryColor}}; text-align: right;">{{Total}}</td>
                                            </tr>
                                            {{#AmountPaid}}
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}};">Paid</td>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{TextColor}};">Balance</td>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{PrimaryColor}}; text-align: right;">{{Balance}}</td>
                                            </tr>
                                            {{/AmountPaid}}
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
                        <td style="padding: 0 40px 30px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{SecondaryColor}}; border-radius: 6px;">
                                <tr>
                                    <td style="padding: 20px 25px;">
                                        <p style="margin: 0 0 10px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{PrimaryColor}}; font-weight: 600;">Payment Instructions</p>
                                        <p style="margin: 0; font-size: 13px; color: #4b5563; line-height: 1.6; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 25px 40px; background-color: {{SecondaryColor}}; text-align: center; border-radius: 0 0 4px 4px;">
                            <p style="margin: 0; font-size: 13px; color: #6b7280;">{{FooterText}}</p>
                            <p style="margin: 8px 0 0 0; font-size: 12px; color: #9ca3af;">
                                {{CompanyName}}{{#ShowCompanyAddress}}{{#CompanyAddress}} • {{CompanyAddress}}{{/CompanyAddress}}{{/ShowCompanyAddress}}{{#ShowCompanyCity}}{{#CompanyCity}} • {{CompanyCity}}{{/CompanyCity}}{{/ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}} • {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}} • {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}{{#CompanyEmail}} • {{CompanyEmail}}{{/CompanyEmail}}{{#ShowCompanyPhone}}{{#CompanyPhone}} • {{CompanyPhone}}{{/CompanyPhone}}{{/ShowCompanyPhone}}
                            </p>
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
    /// Sales Receipt template: Receipt-style layout with decorative side wave ribbons,
    /// signature area, and terms section. Inspired by a classic sales receipt design.
    /// </summary>
    public const string SalesReceipt = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{HeaderText}} {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; background-color: #f5f5f5;">
    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f5f5f5;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" cellpadding="0" cellspacing="0" width="700" style="background-color: {{BackgroundColor}}; box-shadow: 0 25px 80px rgba(0,0,0,0.15); position: relative;">
                    <tr>
                        <td>
                            <!-- Outer wrapper for side decoration + content -->
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <!-- Side Decoration Column -->
                                    <td style="width: 120px; vertical-align: top; background: linear-gradient(180deg, {{AccentColor}}33 0%, {{AccentColor}}22 25%, {{AccentColor}}11 50%, {{PrimaryColor}}22 75%, {{PrimaryColor}}33 100%); position: relative;" width="120">
                                        <!-- Decorative wave ribbons using background -->
                                        <div style="width: 120px; min-height: 100%; opacity: 0.3; background: repeating-linear-gradient(180deg, {{AccentColor}}44 0px, transparent 200px, {{PrimaryColor}}44 400px, transparent 600px);"></div>
                                    </td>

                                    <!-- Main Content Column -->
                                    <td style="vertical-align: top; padding: 50px 50px 50px 30px;">
                                        <!-- Receipt Title -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                            <tr>
                                                <td>
                                                    <p style="margin: 0 0 25px 0; font-size: 42px; font-weight: bold; color: {{PrimaryColor}}; letter-spacing: 2px; text-transform: uppercase;">{{HeaderText}}</p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Company Info -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-bottom: 30px;">
                                            <tr>
                                                <td>
                                                    {{#ShowLogo}}
                                                    <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 60px;{{/LockAspectRatio}} margin-bottom: 10px;">
                                                    {{/ShowLogo}}
                                                    <p style="margin: 0 0 2px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 15px;">{{CompanyName}}</p>
                                                    {{#ShowCompanyAddress}}{{#CompanyAddress}}<p style="margin: 0 0 2px 0; color: #555555; font-size: 14px; line-height: 1.6;">{{CompanyAddress}}</p>{{/CompanyAddress}}{{/ShowCompanyAddress}}
                                                    {{#ShowCompanyCity}}{{#CompanyCity}}<p style="margin: 0 0 2px 0; color: #555555; font-size: 14px;">{{CompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}, {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyCity}}{{/ShowCompanyCity}}
                                                    {{^ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}<p style="margin: 0 0 2px 0; color: #555555; font-size: 14px;">{{CompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</p>{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{/ShowCompanyCity}}
                                                    {{#CompanyEmail}}<p style="margin: 0 0 2px 0; color: #555555; font-size: 14px;">{{CompanyEmail}}</p>{{/CompanyEmail}}
                                                    {{#ShowCompanyPhone}}{{#CompanyPhone}}<p style="margin: 0; color: #555555; font-size: 14px;">{{CompanyPhone}}</p>{{/CompanyPhone}}{{/ShowCompanyPhone}}
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Info Section: Bill To / Ship To / Receipt Details -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-bottom: 35px;">
                                            <tr>
                                                <td style="width: 33%; vertical-align: top; padding-right: 15px;">
                                                    <p style="margin: 0 0 8px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px;">Bill To</p>
                                                    <p style="margin: 0 0 4px 0; font-size: 14px; font-weight: bold; color: {{TextColor}};">{{CustomerName}}</p>
                                                    {{#CustomerAddress}}<p style="margin: 0 0 2px 0; color: #555555; font-size: 14px; line-height: 1.6;">{{CustomerAddress}}</p>{{/CustomerAddress}}
                                                    {{#CustomerEmail}}<p style="margin: 4px 0 0 0; color: #555555; font-size: 14px;">{{CustomerEmail}}</p>{{/CustomerEmail}}
                                                </td>
                                                <td style="width: 33%; vertical-align: top; padding-right: 15px;">
                                                </td>
                                                <td style="width: 34%; vertical-align: top;">
                                                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                                        <tr>
                                                            <td style="padding: 2px 10px 2px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase;">Receipt #</td>
                                                            <td style="padding: 2px 0; text-align: right; color: {{TextColor}}; font-size: 14px;">{{InvoiceNumber}}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style="padding: 2px 10px 2px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase;">Date</td>
                                                            <td style="padding: 2px 0; text-align: right; color: {{TextColor}}; font-size: 14px;">{{IssueDate}}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style="padding: 2px 10px 2px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase;">Due Date</td>
                                                            <td style="padding: 2px 0; text-align: right; font-size: 14px; {{#IsOverdue}}color: #dc2626; font-weight: bold;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</td>
                                                        </tr>
                                                    </table>
                                                    {{#ShowDueDateProminent}}
                                                    <p style="margin: 10px 0 0 0; text-align: right;">
                                                        <span style="display: inline-block; background-color: {{#IsOverdue}}#fef2f2{{/IsOverdue}}{{^IsOverdue}}#f0fdf4{{/IsOverdue}}; color: {{#IsOverdue}}#dc2626{{/IsOverdue}}{{^IsOverdue}}{{AccentColor}}{{/IsOverdue}}; padding: 6px 12px; border-radius: 4px; font-size: 12px; font-weight: 600;">
                                                            {{#IsOverdue}}OVERDUE{{/IsOverdue}}{{^IsOverdue}}DUE: {{DueDate}}{{/IsOverdue}}
                                                        </span>
                                                    </p>
                                                    {{/ShowDueDateProminent}}
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Items Table -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-collapse: collapse; margin-bottom: 30px;">
                                            <tr style="border-top: 2px solid {{PrimaryColor}}; border-bottom: 2px solid {{PrimaryColor}};">
                                                <td style="padding: 14px 15px; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px; width: 60px; text-align: center;">Qty</td>
                                                <td style="padding: 14px 15px; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px;">Description</td>
                                                <td style="padding: 14px 15px; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 120px;">Unit Price</td>
                                                <td style="padding: 14px 15px; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px; text-align: right; width: 100px;">Amount</td>
                                            </tr>
                                            {{#LineItems}}
                                            <tr>
                                                <td style="padding: 16px 15px; font-size: 14px; font-weight: 600; color: {{PrimaryColor}}; text-align: center; border-bottom: 1px solid #e8e8e8;">{{Quantity}}</td>
                                                <td style="padding: 16px 15px; font-size: 14px; color: #444444; border-bottom: 1px solid #e8e8e8;">
                                                    {{Description}}
                                                    {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 12px; color: #888888;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                                                </td>
                                                <td style="padding: 16px 15px; font-size: 14px; color: #444444; text-align: right; border-bottom: 1px solid #e8e8e8;">{{UnitPrice}}</td>
                                                <td style="padding: 16px 15px; font-size: 14px; color: #444444; text-align: right; border-bottom: 1px solid #e8e8e8;">{{Amount}}</td>
                                            </tr>
                                            {{/LineItems}}
                                        </table>

                                        <!-- Totals Section -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-bottom: 50px;">
                                            <tr>
                                                <td style="width: 55%;"></td>
                                                <td style="width: 45%;">
                                                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                                        <tr>
                                                            <td style="padding: 10px 0; font-size: 14px; color: #555555; border-top: 1px solid #e0e0e0;">Subtotal</td>
                                                            <td style="padding: 10px 0; font-size: 14px; color: #555555; text-align: right; border-top: 1px solid #e0e0e0;">{{Subtotal}}</td>
                                                        </tr>
                                                        {{#ShowTaxBreakdown}}
                                                        <tr>
                                                            <td style="padding: 10px 0; font-size: 14px; color: #555555;">Tax ({{TaxRate}}%)</td>
                                                            <td style="padding: 10px 0; font-size: 14px; color: #555555; text-align: right;">{{TaxAmount}}</td>
                                                        </tr>
                                                        {{/ShowTaxBreakdown}}
                                                        <tr>
                                                            <td colspan="2" style="border-top: 2px solid {{PrimaryColor}}; padding-top: 15px;"></td>
                                                        </tr>
                                                        <tr>
                                                            <td style="padding: 8px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 16px; text-transform: uppercase;">Total</td>
                                                            <td style="padding: 8px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 22px; text-align: right;">{{Total}}</td>
                                                        </tr>
                                                        {{#AmountPaid}}
                                                        <tr>
                                                            <td style="padding: 6px 0; font-size: 14px; color: {{AccentColor}};">Amount Paid</td>
                                                            <td style="padding: 6px 0; font-size: 14px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                                        </tr>
                                                        <tr>
                                                            <td style="padding: 6px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                                                            <td style="padding: 6px 0; font-size: 16px; font-weight: 600; color: {{PrimaryColor}}; text-align: right;">{{Balance}}</td>
                                                        </tr>
                                                        {{/AmountPaid}}
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>

                                        {{#ShowNotes}}
                                        {{#Notes}}
                                        <!-- Notes -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-bottom: 30px;">
                                            <tr>
                                                <td style="padding: 15px 20px; background-color: #f9fafb; border-radius: 6px;">
                                                    <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: 600; color: #6b7280; text-transform: uppercase;">Notes</p>
                                                    <p style="margin: 0; font-size: 14px; color: #374151; line-height: 1.5;">{{Notes}}</p>
                                                </td>
                                            </tr>
                                        </table>
                                        {{/Notes}}
                                        {{/ShowNotes}}

                                        {{#ShowPaymentInstructions}}
                                        {{#PaymentInstructions}}
                                        <!-- Payment Instructions / Terms -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="border-top: 3px solid {{PrimaryColor}}; padding-top: 20px;">
                                            <tr>
                                                <td>
                                                    <p style="margin: 0 0 12px 0; font-weight: bold; color: {{PrimaryColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px;">Terms &amp; Conditions</p>
                                                    <p style="margin: 0; color: #555555; font-size: 13px; line-height: 1.8; white-space: pre-line;">{{PaymentInstructions}}</p>
                                                </td>
                                            </tr>
                                        </table>
                                        {{/PaymentInstructions}}
                                        {{/ShowPaymentInstructions}}

                                        <!-- Footer -->
                                        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="margin-top: 30px;">
                                            <tr>
                                                <td style="text-align: center; padding-top: 20px; border-top: 1px solid #e0e0e0;">
                                                    <p style="margin: 0 0 6px 0; font-size: 13px; color: #374151;">{{FooterText}}</p>
                                                    <p style="margin: 0; font-size: 12px; color: #9ca3af;">
                                                        {{CompanyName}}{{#ShowCompanyAddress}}{{#CompanyAddress}} • {{CompanyAddress}}{{/CompanyAddress}}{{/ShowCompanyAddress}}{{#ShowCompanyCity}}{{#CompanyCity}} • {{CompanyCity}}{{/CompanyCity}}{{/ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}} • {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}} • {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}{{#CompanyEmail}} • {{CompanyEmail}}{{/CompanyEmail}}{{#ShowCompanyPhone}}{{#CompanyPhone}} • {{CompanyPhone}}{{/CompanyPhone}}{{/ShowCompanyPhone}}
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
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
            InvoiceTemplateType.Elegant => Elegant,
            InvoiceTemplateType.SalesReceipt => SalesReceipt,
            _ => Professional
        };
    }
}
