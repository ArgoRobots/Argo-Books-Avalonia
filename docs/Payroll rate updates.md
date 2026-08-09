# Payroll rate updates

CRA publishes new payroll deduction tables **twice a year**, effective **January 1** and
**July 1**. Argo Books ships no tax numbers in the app itself, so each new edition is a data
file that has to be prepared, checked and uploaded.

> This has a hard deadline and no grace period. A pay run calculated on or after a changeover
> either refuses to run, or uses the previous edition and produces deductions that look
> correct and are wrong. The app cannot detect that on its own.

A reminder email arrives in mid-December and mid-June. It is one of the alerts that cannot be
switched off, and it comes from `cron/payroll_rate_reminder.php` in the website repository.

---

## When

| Edition | Effective | Prepare during |
|---|---|---|
| `YYYY-01` | January 1 | December |
| `YYYY-07` | July 1 | June |

Aim to have the file uploaded a week before it takes effect.

---

## Why there are two editions a year

The July edition is not just a refresh. When a province changes a rate part way through the
year, CRA publishes a **prorated** figure for the second half that offsets what was used in
the first half. In 2026 that affected British Columbia, Newfoundland and Labrador, and Prince
Edward Island.

This is why a pay run picks its table by **pay date** rather than by year, and why the
January edition must be kept alongside the July one rather than replaced by it. A February
pay run and an August pay run in the same year legitimately use different numbers.

---

## What to collect

Everything below comes from CRA, except Quebec which comes from Revenu Québec.

**Federal**
- Income tax brackets: thresholds, rates, and the constant `K` for each
- Basic personal amount `BPAF`, including its phase-out range
- Canada Employment Amount
- The lowest rate, used to convert credits

**Contributions**
- CPP: employee and employer rates, basic exemption, YMPE ceiling, annual maximum
- CPP2: rates, YAMPE ceiling, annual maximum
- EI: employee rate, employer multiplier, maximum insurable earnings, annual maximum, and
  the separate Quebec rate

**Each province and territory**
- Brackets, rates and constants
- Basic personal amount, which for Manitoba and Yukon is a formula rather than a number
- Ontario only: surtax thresholds and rates, the health premium bands, the tax reduction
- British Columbia only: the tax reduction

### Sources

- [T4127 Payroll Deductions Formulas](https://www.canada.ca/en/revenue-agency/services/forms-publications/payroll/t4127-payroll-deductions-formulas.html), the authoritative one
- [CPP contribution rates, maximums and exemptions](https://www.canada.ca/en/revenue-agency/services/tax/businesses/topics/payroll/payroll-deductions-contributions/canada-pension-plan-cpp/cpp-contribution-rates-maximums-exemptions.html)
- [CPP2 rates and maximums](https://www.canada.ca/en/revenue-agency/services/tax/businesses/topics/payroll/calculating-deductions/making-deductions/second-additional-cpp-contribution-rates-maximums.html)
- [EI premium rates and maximums](https://www.canada.ca/en/revenue-agency/services/tax/businesses/topics/payroll/payroll-deductions-contributions/employment-insurance-ei/ei-premium-rates-maximums.html)
- Revenu Québec guide TP-1015.F-V, for Quebec

`C:\Users\evand\Downloads\payroll-numbers-prompt-combined.txt` is a prompt that collects all
of this in one pass, in the right shape. Run it in two different AI tools and compare the
answers rather than trusting one.

---

## How to check the numbers before trusting them

Never take these on faith. Three checks catch almost everything, and all are quick.

**1. Derived maximums must reproduce from their own rates.** These cannot survive a
fabricated number:

```
CPP  max = (YMPE - basic exemption) x rate
CPP2 max = (YAMPE - YMPE)           x rate
EI   max = maximum insurable earnings x rate
```

For 2026: `(74600 - 3500) x 0.0595 = 4230.45`, `(85000 - 74600) x 0.04 = 416.00`,
`68900 x 0.0163 = 1123.07`. All three match the published maximums exactly.

**2. Bracket constants must be continuous.** At each boundary, tax computed from the bracket
below and the bracket above must agree:

```
rate_below x boundary - K_below  ==  rate_above x boundary - K_above
```

They will differ by under a dollar, because `K` is published rounded to whole dollars.
Anything larger means a wrong rate or a wrong constant.

**3. Two independent retrievals must agree.** Ask two different tools, then diff the results
field by field. In the 2026 round this caught nothing wrong but did reveal that each tool had
silently omitted a different set of provinces, which is the more likely failure than a bad
number.

---

## Building the file

1. Copy the previous edition from `ArgoBooks.Core/Resources/Payroll/` and rename it to the
   new edition, for example `2027-01.json`.
2. Update `editionId`, `effectiveFrom` and `effectiveTo`. The January edition runs to June 30,
   the July edition to December 31.
3. Replace the figures.
4. Update the `_source` and `_verification` notes at the top so the next person can see where
   the numbers came from and what was checked.
5. Run the test suite. `PayrollCalculatorTests` checks bracket continuity and the annual
   maximums against whatever table is loaded, so a transcription error usually fails there.

---

## Verifying against CRA's calculator

The tests prove the file is internally consistent. They do not prove it agrees with CRA.

Run a handful of cases through
[PDOC](https://www.canada.ca/en/revenue-agency/services/e-services/digital-services-businesses/payroll-deductions-online-calculator.html)
and compare against the app. Cover each pay frequency, one income below the CPP ceiling, one
between the ceilings, and one employee already at their annual maximum. Any disagreement is
the file, not the engine, unless the engine changed too.

---

## Shipping it

The rate file is delivered the same way language files are, so existing installs pick it up
without an app update. `PayrollRateService` looks in the cache directory first and falls back
to the copy embedded in the assembly, so:

- **Upload** the new edition for everyone already running the app
- **Also commit** it to `ArgoBooks.Core/Resources/Payroll/` so fresh installs have it offline

Do both. Uploading alone leaves a new install unable to calculate until it syncs; committing
alone means nobody gets it until they update the app.

---

## If a deadline is missed

The app refuses to calculate rather than guessing, so nothing incorrect is produced. Users on
the affected dates will see that payroll cannot be run and will contact support.

Recovering is just the normal process above. Any pay run already approved under the previous
edition keeps its stored figures and is not retroactively changed, which is intended: it
matches the stub the employee was given.
