# Reports

Argo Books generates PDF and image reports from customizable templates. Built-in templates cover both day-to-day operational reports (Revenue, Expenses, Customer Analysis, etc.) and formal accounting reports (Income Statement, Balance Sheet, Cash Flow, etc.).

See [Calculations](Calculations.md) for the rules governing how the numbers in each report are computed.

## Built-in templates

- Revenue Overview
- Financial Overview
- Performance Analysis
- Returns Analysis
- Losses Analysis
- Geographic Analysis
- Customer Analysis
- Expense Breakdown
- Custom Report (blank starting point)

**Accounting reports** follow accrual rules by default, with an optional cash-basis toggle on each report:

- Income Statement
- Balance Sheet
- Cash Flow Statement
- General Ledger
- Accounts Receivable Aging
- Tax Summary

## Creating a report

The report editor is a three-step wizard:

1. **Template & Settings**: pick a built-in template or a previously saved custom template; choose date range and report-level options.
2. **Layout Designer**: drag and resize elements on the canvas, edit per-element properties (font, color, sorting, etc.).
3. **Preview & Export**: preview each page and export as PDF, PNG, or JPEG.

A report is composed of elements placed on one or more pages: charts, tables, labels, images, summary boxes, date-range labels, and formal accounting tables.

## Saving custom templates

Edits to a built-in template can be saved as a new custom template that is reusable across companies. Custom templates are stored as `.argotemplate` files in the app's local data directory. They are global to the user, not stored inside any one `.argo` file.

Built-in templates themselves are read-only. Saving an edited built-in creates a new custom entry rather than overwriting.

## Pagination

Each template has a fixed page count, but the report can grow at render time:

- If a transaction table doesn't fit on its page, it overflows to a continuation page automatically.
- Continuation pages re-render only the overflowing table. The other elements on that page appear once, on the first effective page.
- Headers and footers redraw on every page, including continuation pages.
- Charts, labels, and images are not split across pages. If they don't fit, they are clipped.

## Charts inside reports

Charts in reports are drawn directly into the PDF rather than embedding the on-screen chart control. Bar, line, area, scatter, pie, and world-map charts all render with the same look as the analytics tab.
