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
                                {{#ShowSecurityDeposit}}
                                <tr>
                                    <td style="padding: 8px 0; font-size: 14px; color: #6b7280;">Security Deposit</td>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{SecurityDeposit}}</td>
                                </tr>
                                {{/ShowSecurityDeposit}}
                                <tr>
                                    <td colspan="2" style="border-top: 2px solid {{SecondaryColor}}; padding-top: 12px;"></td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; font-size: 18px; font-weight: 700; color: {{TextColor}};">Total</td>
                                    <td style="padding: 8px 0; font-size: 18px; font-weight: 700; color: {{HeaderColor}}; text-align: right;">{{Total}}</td>
                                </tr>
                                {{#AmountPaid}}
                                <tr>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{AccentColor}};">Amount Paid</td>
                                    <td style="padding: 8px 0; font-size: 14px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                                    <td style="padding: 8px 0; font-size: 16px; font-weight: 600; color: {{HeaderColor}}; text-align: right;">{{Balance}}</td>
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
                                        <p style="margin: 0; font-size: 14px; color: {{TextColor}}; line-height: 1.5;">{{Notes}}</p>
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
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: 600; color: {{HeaderColor}}; text-transform: uppercase;">Payment Instructions</p>
                                        <p style="margin: 0; font-size: 14px; color: {{TextColor}}; line-height: 1.5; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    {{#ShowPayOnline}}
                    <!-- Pay Online Button -->
                    <tr>
                        <td style="padding: 0 40px 25px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{PrimaryColor}}; border-radius: 8px;">
                                <tr>
                                    <td style="padding: 20px 30px; text-align: center;">
                                        <p style="margin: 0 0 12px 0; font-size: 16px; font-weight: 600; color: #ffffff;">Pay This Invoice Online</p>
                                        <p style="margin: 0 0 16px 0; font-size: 13px; color: rgba(255,255,255,0.85);">Securely pay with Stripe, PayPal, or Square</p>
                                        <a href="{{PayOnlineUrl}}" style="display: inline-block; background-color: #ffffff; color: {{PrimaryColor}}; padding: 12px 32px; border-radius: 6px; font-size: 15px; font-weight: 600; text-decoration: none;">Pay Now</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/ShowPayOnline}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 25px 40px; background-color: #f9fafb; border-radius: 0 0 8px 8px; text-align: center;">
                            <p style="margin: 0 0 10px 0; font-size: 14px; color: {{TextColor}};">{{FooterText}}</p>
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
                                                    <p style="margin: 0 0 4px 0; font-size: 24px; font-weight: 700; color: {{HeaderColor}}; letter-spacing: -0.5px;">{{HeaderText}}</p>
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
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1.5px; color: {{HeaderColor}}; font-weight: 600;">Bill To</p>
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
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{HeaderColor}}; font-weight: 600;">Notes</p>
                                        <p style="margin: 0; font-size: 13px; color: {{TextColor}}; line-height: 1.6;">{{Notes}}</p>
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
                                            {{#ShowSecurityDeposit}}
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Security Deposit</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{SecurityDeposit}}</td>
                                            </tr>
                                            {{/ShowSecurityDeposit}}
                                            <tr>
                                                <td colspan="2" style="padding-top: 12px; border-top: 2px solid {{PrimaryColor}};"></td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 16px; font-weight: 700; color: {{TextColor}};">Total</td>
                                                <td style="padding: 8px 0; font-size: 20px; font-weight: 700; color: {{HeaderColor}}; text-align: right;">{{Total}}</td>
                                            </tr>
                                            {{#AmountPaid}}
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}};">Paid</td>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                                                <td style="padding: 6px 0; font-size: 17px; font-weight: 700; color: {{HeaderColor}}; text-align: right;">{{Balance}}</td>
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
                                        <p style="margin: 0; font-size: 13px; color: {{TextColor}}; line-height: 1.6; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    {{#ShowPayOnline}}
                    <!-- Pay Online Button -->
                    <tr>
                        <td style="padding: 0 35px 25px 35px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 8px; background-color: {{AccentColor}};"></td>
                                    <td style="padding: 20px 25px; background-color: {{SecondaryColor}}; border-radius: 0 6px 6px 0; text-align: center;">
                                        <p style="margin: 0 0 12px 0; font-size: 15px; font-weight: 600; color: {{HeaderColor}};">Pay This Invoice Online</p>
                                        <p style="margin: 0 0 14px 0; font-size: 12px; color: #6b7280;">Securely pay with Stripe, PayPal, or Square</p>
                                        <a href="{{PayOnlineUrl}}" style="display: inline-block; background-color: {{PrimaryColor}}; color: #ffffff; padding: 12px 32px; border-radius: 6px; font-size: 14px; font-weight: 600; text-decoration: none;">Pay Now</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/ShowPayOnline}}

                    <!-- Footer -->
                    <tr>
                        <td>
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                    <td style="width: 8px; background-color: {{PrimaryColor}};"></td>
                                    <td style="padding: 20px 35px; background-color: {{SecondaryColor}}; text-align: center;">
                                        <p style="margin: 0 0 6px 0; font-size: 13px; color: {{TextColor}};">{{FooterText}}</p>
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
                                        <p style="margin: 0; font-size: 28px; font-weight: bold; color: {{HeaderColor}};">{{HeaderText}}</p>
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
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{HeaderColor}}; text-transform: uppercase;">Bill To:</p>
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
                                        <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{HeaderColor}};">NOTES:</p>
                                        <p style="margin: 0; font-size: 12px; color: {{TextColor}}; line-height: 1.5;">{{Notes}}</p>
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
                                            {{#ShowSecurityDeposit}}
                                            <tr>
                                                <td style="padding: 10px 15px; font-size: 13px; color: #666666; border-bottom: 1px solid {{SecondaryColor}};">Security Deposit</td>
                                                <td style="padding: 10px 15px; font-size: 13px; color: {{TextColor}}; text-align: right; border-bottom: 1px solid {{SecondaryColor}};">{{SecurityDeposit}}</td>
                                            </tr>
                                            {{/ShowSecurityDeposit}}
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
                            <p style="margin: 0 0 5px 0; font-size: 12px; font-weight: bold; color: {{HeaderColor}};">PAYMENT INSTRUCTIONS:</p>
                            <p style="margin: 0; font-size: 12px; color: {{TextColor}}; line-height: 1.5; white-space: pre-line;">{{PaymentInstructions}}</p>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    {{#ShowPayOnline}}
                    <!-- Pay Online Button -->
                    <tr>
                        <td style="padding: 15px; border-top: 1px solid {{SecondaryColor}}; text-align: center;">
                            <p style="margin: 0 0 10px 0; font-size: 14px; font-weight: bold; color: {{HeaderColor}};">PAY THIS INVOICE ONLINE</p>
                            <p style="margin: 0 0 12px 0; font-size: 12px; color: #666666;">Securely pay with Stripe, PayPal, or Square</p>
                            <a href="{{PayOnlineUrl}}" style="display: inline-block; background-color: {{PrimaryColor}}; color: #ffffff; padding: 12px 32px; font-size: 14px; font-weight: bold; text-decoration: none;">PAY NOW</a>
                        </td>
                    </tr>
                    {{/ShowPayOnline}}

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
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 2px; color: {{HeaderColor}}; font-weight: 600;">{{HeaderText}}</p>
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
                                        <p style="margin: 0; font-size: 13px; color: {{TextColor}}; line-height: 1.6;">{{Notes}}</p>
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
                                            {{#ShowSecurityDeposit}}
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 13px; color: #6b7280;">Security Deposit</td>
                                                <td style="padding: 8px 0; font-size: 13px; color: {{TextColor}}; text-align: right;">{{SecurityDeposit}}</td>
                                            </tr>
                                            {{/ShowSecurityDeposit}}
                                            <tr>
                                                <td colspan="2" style="padding-top: 12px; border-top: 2px solid {{PrimaryColor}};"></td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: #9ca3af; font-weight: 500;">Total Due</td>
                                                <td style="padding: 8px 0; font-size: 22px; font-weight: 600; color: {{HeaderColor}}; text-align: right;">{{Total}}</td>
                                            </tr>
                                            {{#AmountPaid}}
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}};">Paid</td>
                                                <td style="padding: 6px 0; font-size: 13px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{TextColor}};">Balance</td>
                                                <td style="padding: 6px 0; font-size: 15px; font-weight: 600; color: {{HeaderColor}}; text-align: right;">{{Balance}}</td>
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
                                        <p style="margin: 0 0 10px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{HeaderColor}}; font-weight: 600;">Payment Instructions</p>
                                        <p style="margin: 0; font-size: 13px; color: {{TextColor}}; line-height: 1.6; white-space: pre-line;">{{PaymentInstructions}}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/PaymentInstructions}}
                    {{/ShowPaymentInstructions}}

                    {{#ShowPayOnline}}
                    <!-- Pay Online Button -->
                    <tr>
                        <td style="padding: 0 40px 30px 40px;">
                            <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="background-color: {{SecondaryColor}}; border-radius: 6px; border-top: 3px solid {{PrimaryColor}};">
                                <tr>
                                    <td style="padding: 25px; text-align: center;">
                                        <p style="margin: 0 0 8px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; color: {{HeaderColor}}; font-weight: 600;">Pay Online</p>
                                        <p style="margin: 0 0 16px 0; font-size: 13px; color: #6b7280;">Securely pay with Stripe, PayPal, or Square</p>
                                        <a href="{{PayOnlineUrl}}" style="display: inline-block; background-color: {{PrimaryColor}}; color: #ffffff; padding: 12px 36px; border-radius: 24px; font-size: 14px; font-weight: 600; text-decoration: none; letter-spacing: 0.5px;">Pay Now</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {{/ShowPayOnline}}

                    <!-- Footer -->
                    <tr>
                        <td style="padding: 25px 40px; background-color: {{SecondaryColor}}; text-align: center; border-radius: 0 0 4px 4px;">
                            <p style="margin: 0; font-size: 13px; color: {{TextColor}};">{{FooterText}}</p>
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
    /// Ribbon template: Flowing design with decorative SVG wave ribbons on the left side,
    /// open layout with bold header, and terms section.
    /// </summary>
    public const string Ribbon = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{HeaderText}} {{InvoiceNumber}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: {{FontFamily}}; font-weight: 600; background-color: #f5f5f5;">
    <table cellpadding="0" cellspacing="0" border="0" width="800" align="center" style="background: {{BackgroundColor}}; font-family: {{FontFamily}}; font-weight: 600;">
        <tr>
            <!-- Side Decoration: Colored Ribbon Stripes -->
            <td width="42" valign="top" style="width: 42px;">
                <table cellpadding="0" cellspacing="0" border="0" width="42" style="width: 42px; min-height: 100%;">
                    <tr>
                        <td width="14" style="width: 14px; background-color: {{AccentColor}};">&nbsp;</td>
                        <td width="14" style="width: 14px; background-color: {{PrimaryColor}};">&nbsp;</td>
                        <td width="14" style="width: 14px; background-color: {{SecondaryColor}};">&nbsp;</td>
                    </tr>
                </table>
            </td>
            <td width="48" style="width: 48px;">&nbsp;</td>

            <!-- Main Content -->
            <td valign="top" style="padding: 50px 50px 50px 0;">
                <!-- Header Title -->
                <h1 style="font-family: 'Oswald', {{FontFamily}}; font-size: 52px; font-weight: bold; color: {{HeaderColor}}; letter-spacing: 2px; margin: 0 0 25px 0; text-transform: uppercase;">{{HeaderText}}</h1>

                <!-- Company Info -->
                <table cellpadding="0" cellspacing="0" border="0" style="margin-bottom: 30px;">
                    <tr><td>
                        {{#ShowLogo}}
                        <img src="{{LogoSrc}}" alt="Company Logo" width="{{LogoWidth}}" style="display: block; {{#LockAspectRatio}}height: auto;{{/LockAspectRatio}}{{^LockAspectRatio}}max-height: 60px;{{/LockAspectRatio}} margin-bottom: 10px;">
                        {{/ShowLogo}}
                        <div style="font-weight: bold; color: {{HeaderColor}}; font-size: 15px;">{{CompanyName}}</div>
                        {{#ShowCompanyAddress}}{{#CompanyAddress}}<div style="color: #555; font-size: 14px; line-height: 1.6;">{{CompanyAddress}}</div>{{/CompanyAddress}}{{/ShowCompanyAddress}}
                        {{#ShowCompanyCity}}{{#CompanyCity}}<div style="color: #555; font-size: 14px; line-height: 1.6;">{{CompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}, {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</div>{{/CompanyCity}}{{/ShowCompanyCity}}
                        {{^ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}}<div style="color: #555; font-size: 14px; line-height: 1.6;">{{CompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}}, {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}</div>{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{/ShowCompanyCity}}
                        {{#CompanyEmail}}<div style="color: #555; font-size: 14px; line-height: 1.6;">{{CompanyEmail}}</div>{{/CompanyEmail}}
                        {{#ShowCompanyPhone}}{{#CompanyPhone}}<div style="color: #555; font-size: 14px; line-height: 1.6;">{{CompanyPhone}}</div>{{/CompanyPhone}}{{/ShowCompanyPhone}}
                    </td></tr>
                </table>

                <!-- Info Section: Sold To + Receipt Details -->
                <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin-bottom: 35px;">
                    <tr>
                        <td valign="top" width="40%">
                            <div style="font-weight: bold; color: {{HeaderColor}}; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">Sold To</div>
                            <div style="color: {{TextColor}}; font-size: 14px; line-height: 1.6;">
                                <strong>{{CustomerName}}</strong><br>
                                {{#CustomerAddress}}{{CustomerAddress}}<br>{{/CustomerAddress}}
                                {{#CustomerEmail}}{{CustomerEmail}}{{/CustomerEmail}}
                            </div>
                        </td>
                        <td width="10%">&nbsp;</td>
                        <td valign="top" width="50%">
                            <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                <tr>
                                    <td style="font-weight: bold; color: {{HeaderColor}}; font-size: 16px; text-transform: uppercase; padding: 2px 0;">Receipt #</td>
                                    <td style="text-align: right; color: {{TextColor}}; font-size: 14px; padding: 2px 0;">{{InvoiceNumber}}</td>
                                </tr>
                                <tr>
                                    <td style="font-weight: bold; color: {{HeaderColor}}; font-size: 16px; text-transform: uppercase; padding: 2px 0;">Receipt Date</td>
                                    <td style="text-align: right; color: {{TextColor}}; font-size: 14px; padding: 2px 0;">{{IssueDate}}</td>
                                </tr>
                                <tr>
                                    <td style="font-weight: bold; color: {{HeaderColor}}; font-size: 16px; text-transform: uppercase; padding: 2px 0;">Due Date</td>
                                    <td style="text-align: right; font-size: 14px; padding: 2px 0; {{#IsOverdue}}color: #dc2626; font-weight: bold;{{/IsOverdue}}{{^IsOverdue}}color: {{TextColor}};{{/IsOverdue}}">{{DueDate}}</td>
                                </tr>
                            </table>
                            {{#ShowDueDateProminent}}
                            <div style="text-align: right; margin-top: 10px;">
                                <span style="display: inline-block; background-color: {{#IsOverdue}}#fef2f2{{/IsOverdue}}{{^IsOverdue}}#f0fdf4{{/IsOverdue}}; color: {{#IsOverdue}}#dc2626{{/IsOverdue}}{{^IsOverdue}}{{AccentColor}}{{/IsOverdue}}; padding: 6px 12px; font-size: 12px; font-weight: 600;">
                                    {{#IsOverdue}}OVERDUE{{/IsOverdue}}{{^IsOverdue}}DUE: {{DueDate}}{{/IsOverdue}}
                                </span>
                            </div>
                            {{/ShowDueDateProminent}}
                        </td>
                    </tr>
                </table>

                <!-- Items Table -->
                <table style="width: 100%; border-collapse: collapse; margin-bottom: 30px; font-family: {{FontFamily}}; font-weight: 600;">
                    <thead>
                        <tr>
                            <th style="color: {{HeaderColor}}; font-weight: bold; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; padding: 14px 15px; text-align: left; width: 60px; border-top: 2px solid {{HeaderColor}}; border-bottom: 2px solid {{HeaderColor}};">QTY</th>
                            <th style="color: {{HeaderColor}}; font-weight: bold; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; padding: 14px 15px; text-align: left; border-top: 2px solid {{HeaderColor}}; border-bottom: 2px solid {{HeaderColor}};">Description</th>
                            <th style="color: {{HeaderColor}}; font-weight: bold; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; padding: 14px 15px; text-align: right; border-top: 2px solid {{HeaderColor}}; border-bottom: 2px solid {{HeaderColor}};">Unit Price</th>
                            <th style="color: {{HeaderColor}}; font-weight: bold; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; padding: 14px 15px; text-align: right; border-top: 2px solid {{HeaderColor}}; border-bottom: 2px solid {{HeaderColor}};">Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        {{#LineItems}}
                        <tr>
                            <td style="padding: 16px 15px; border-bottom: 1px solid #e8e8e8; font-size: 14px; text-align: center; font-weight: 600; color: {{HeaderColor}};">{{Quantity}}</td>
                            <td style="padding: 16px 15px; border-bottom: 1px solid #e8e8e8; font-size: 14px; color: {{TextColor}};">
                                {{Description}}
                                {{#ShowItemDescriptions}}{{#ItemDescription}}<br><span style="font-size: 12px; color: #888;">{{ItemDescription}}</span>{{/ItemDescription}}{{/ShowItemDescriptions}}
                            </td>
                            <td style="padding: 16px 15px; border-bottom: 1px solid #e8e8e8; font-size: 14px; color: {{TextColor}}; text-align: right;">{{UnitPrice}}</td>
                            <td style="padding: 16px 15px; border-bottom: 1px solid #e8e8e8; font-size: 14px; color: {{TextColor}}; text-align: right;">{{Amount}}</td>
                        </tr>
                        {{/LineItems}}
                    </tbody>
                </table>

                <!-- Totals Section -->
                <table cellpadding="0" cellspacing="0" border="0" width="280" align="right" style="margin-bottom: 50px;">
                    <tr>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}}; border-top: 1px solid #e0e0e0;">Subtotal</td>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}}; border-top: 1px solid #e0e0e0; text-align: right;">{{Subtotal}}</td>
                    </tr>
                    {{#ShowTaxBreakdown}}
                    <tr>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}};">Tax ({{TaxRate}}%)</td>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{TaxAmount}}</td>
                    </tr>
                    {{/ShowTaxBreakdown}}
                    {{#ShowSecurityDeposit}}
                    <tr>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}};">Security Deposit</td>
                        <td style="padding: 10px 0; font-size: 14px; color: {{TextColor}}; text-align: right;">{{SecurityDeposit}}</td>
                    </tr>
                    {{/ShowSecurityDeposit}}
                    <tr>
                        <td style="padding: 15px 0 8px 0; font-size: 16px; font-weight: bold; color: {{HeaderColor}}; text-transform: uppercase; border-top: 2px solid {{HeaderColor}}; margin-top: 10px;">Total</td>
                        <td style="padding: 15px 0 8px 0; font-size: 22px; font-weight: bold; color: {{HeaderColor}}; text-align: right; border-top: 2px solid {{HeaderColor}};">{{Total}}</td>
                    </tr>
                    {{#AmountPaid}}
                    <tr>
                        <td style="padding: 6px 0; font-size: 14px; color: {{AccentColor}};">Amount Paid</td>
                        <td style="padding: 6px 0; font-size: 14px; color: {{AccentColor}}; text-align: right;">-{{AmountPaid}}</td>
                    </tr>
                    <tr>
                        <td style="padding: 6px 0; font-size: 16px; font-weight: 600; color: {{TextColor}};">Balance Due</td>
                        <td style="padding: 6px 0; font-size: 16px; font-weight: 600; color: {{HeaderColor}}; text-align: right;">{{Balance}}</td>
                    </tr>
                    {{/AmountPaid}}
                </table>
                <div style="clear: both;"></div>

                {{#ShowNotes}}
                {{#Notes}}
                <!-- Notes -->
                <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin-bottom: 30px;">
                    <tr>
                        <td style="padding: 15px 20px; background-color: #f9fafb;">
                            <div style="font-size: 12px; font-weight: 600; color: #6b7280; text-transform: uppercase; margin-bottom: 5px;">Notes</div>
                            <div style="font-size: 14px; color: {{TextColor}}; line-height: 1.5;">{{Notes}}</div>
                        </td>
                    </tr>
                </table>
                {{/Notes}}
                {{/ShowNotes}}

                {{#ShowPaymentInstructions}}
                {{#PaymentInstructions}}
                <!-- Terms & Conditions -->
                <div style="border-top: 3px solid {{HeaderColor}}; padding-top: 20px;">
                    <div style="font-weight: bold; color: {{HeaderColor}}; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 12px;">Terms &amp; Conditions</div>
                    <div style="color: {{TextColor}}; font-size: 13px; line-height: 1.8; white-space: pre-line;">{{PaymentInstructions}}</div>
                </div>
                {{/PaymentInstructions}}
                {{/ShowPaymentInstructions}}

                {{#ShowPayOnline}}
                <!-- Pay Online Button -->
                <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin: 30px 0;">
                    <tr>
                        <td align="center" style="padding: 25px; background-color: {{PrimaryColor}};">
                            <div style="font-size: 16px; font-weight: bold; color: #ffffff; margin-bottom: 8px;">Pay This Invoice Online</div>
                            <div style="font-size: 13px; color: #ffffffd9; margin-bottom: 16px;">Securely pay with Stripe, PayPal, or Square</div>
                            <table cellpadding="0" cellspacing="0" border="0">
                                <tr>
                                    <td align="center" style="background-color: #ffffff; padding: 12px 36px;">
                                        <a href="{{PayOnlineUrl}}" style="color: {{PrimaryColor}}; font-size: 15px; font-weight: bold; text-decoration: none; text-transform: uppercase; letter-spacing: 0.5px;">Pay Now</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
                {{/ShowPayOnline}}

                <!-- Footer -->
                <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin-top: 30px; border-top: 1px solid #e0e0e0;">
                    <tr>
                        <td align="center" style="padding-top: 20px;">
                            <div style="font-size: 13px; color: {{TextColor}}; margin-bottom: 6px;">{{FooterText}}</div>
                            <div style="font-size: 12px; color: #9ca3af;">
                                {{CompanyName}}{{#ShowCompanyAddress}}{{#CompanyAddress}} &bull; {{CompanyAddress}}{{/CompanyAddress}}{{/ShowCompanyAddress}}{{#ShowCompanyCity}}{{#CompanyCity}} &bull; {{CompanyCity}}{{/CompanyCity}}{{/ShowCompanyCity}}{{#ShowCompanyProvinceState}}{{#CompanyProvinceState}} &bull; {{CompanyProvinceState}}{{/CompanyProvinceState}}{{/ShowCompanyProvinceState}}{{#ShowCompanyCountry}}{{#CompanyCountry}} &bull; {{CompanyCountry}}{{/CompanyCountry}}{{/ShowCompanyCountry}}{{#CompanyEmail}} &bull; {{CompanyEmail}}{{/CompanyEmail}}{{#ShowCompanyPhone}}{{#CompanyPhone}} &bull; {{CompanyPhone}}{{/CompanyPhone}}{{/ShowCompanyPhone}}
                            </div>
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
            InvoiceTemplateType.Ribbon => Ribbon,
            _ => Professional
        };
    }
}
