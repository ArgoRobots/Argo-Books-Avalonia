# Bank Matching test data

Sample bank statement exports for the Bank Matching feature. These are reference data
(imported via the smart importer, never committed as book transactions).

Formats covered:

- `signed_amount_statement.csv` - single signed `Amount` column (negative = money out).
- `debit_credit_statement.csv` - separate `Debit`/`Credit` columns plus `Balance` and `Reference`.
- `messy_bank_export.csv` - non-standard headers (Txn Date, Withdrawal Amt, Deposit Amt, ...) to
  exercise the AI column mapping.
- `mixed_expenses_and_payments.csv` - a QuickBooks-style export with mixed rows.

The importer maps columns to: Date, Description, Amount, Debit, Credit, Balance, Reference.
