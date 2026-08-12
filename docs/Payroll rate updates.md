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

Better still, read the numbers straight out of T4127 rather than asking anything to recall
them. Chapter 8 carries the whole lot in two tables: **Table 8.1** has every jurisdiction's
thresholds `A`, rates `V` and constants `KP`, and **Table 8.2** has the basic amounts, the
Canada Employment Amounts, the tax reduction amounts `S2` and Ontario's surtax. That is the
source, and it is faster than checking a recollection of it against the source anyway.

Note that the July edition only reproduces the sections that CHANGED. Formulas that did not
change, and Ontario's health premium bands, are in the January edition instead.

### Reading Table 8.1 into the rate file

`A` is the LOWER bound of each bracket. Our `upTo` is the TOP of each bracket, which is the
next `A`, with `null` for the last one. So a row reading

    A   0       61,200   154,259
    V   0.0800  0.1000   0.1200
    KP  0       1,224    4,309

becomes `upTo 61200 / rate .08 / k 0`, then `upTo 154259 / rate .10 / k 1224`, then
`upTo 185111 / rate .12 / k 4309`. Getting this off by one shifts every bracket and does not
look wrong in the output, so check the first row against the previous edition.


---

## Quebec is a separate update, from a separate source

Quebec administers its own income tax, pension plan and parental insurance plan, so none of
its figures appear in T4127 and none of the PDOC fixtures say anything about it. It is a
second gathering exercise on its own schedule.

**Sources**

- [TP-1015.F-V, Formulas to Calculate Source Deductions](https://www.revenuquebec.ca/en/online-services/forms-and-publications/current-details/tp-1015-f-v/),
  which carries the brackets, the constants, the personal amounts and the formula itself.
  Appendix 1 has a worked example that is the Quebec equivalent of T4127's, and is pinned as
  a test.
- [QPP maximums and contribution rate](https://www.revenuquebec.ca/en/businesses/source-deductions-and-employer-contributions/calculating-source-deductions-and-contributions/qpp-contributions/maximum-pensionable-earnings-and-contribution-rate/)
- [QPIP maximums and premium rates](https://www.revenuquebec.ca/en/businesses/source-deductions-and-employer-contributions/calculating-source-deductions-and-contributions/qpip-premiums/maximum-insurable-earnings-and-premium-rate/)
- The Quebec EI maximum, which is lower than the rest of Canada because QPIP covers parental
  benefits. The RATE is in T4127; the maximum is announced with the EI rate each autumn.
- The federal abatement, 16.5%, which is in T4127's Table 8.2 against QC.

**The oracle is not WebRAS.** This is the opposite of the rest of Canada. Revenu Quebec states
in the guide and again inside WebRAS itself: "In the event of a discrepancy between the
calculations using the formulas and those using WebRAS, the calculations using the formulas
prevail." So TP-1015.F is authoritative and WebRAS is a sanity check. Do not "fix" the
calculator to match WebRAS.

**A search summary got QPIP wrong.** It reported 0.455% and 0.636% for 2026. The published
table says 0.430% and 0.602%, and 103,000 x 0.00430 = 442.90 confirms it. Read the tables.

---

## The RL-1

A Quebec employer files an **RL-1 slip and RL-1 Summary with Revenu Quebec**, in addition to
the T4 and T4 Summary with CRA. That is why the T4 Summary's CPP totals deliberately exclude
Quebec's QPP: those figures belong on the RL-1, and CRA has no field for them.

Built in `Rl1Service`, `Rl1PdfRenderer` and `Models/Payroll/Rl1Slip.cs`, and reachable from the
Quebec section of the year end modal, which only appears for an employer who actually has a
Quebec employee.

### What was settled about building it

**The slip and summary are PDFs, not XML.** Revenu Quebec requires online XML filing only
from employers sending MORE than 5 RL slips of the same type. Anyone filing fewer than 6 may
use the Transmitting RL Slips online service or send paper. This app's employer has two or
three staff, so they are under the threshold and will key the figures in, exactly as they
would with CRA's Web Forms rather than T4 XML.

**The RL-1 XML specification is not public anyway.** Unlike CRA, which publishes the T4 layout
as a web page, Revenu Quebec routes RL-slip specifications through its Division de
l'acquisition des donnees electroniques for registered product developers. Building the XML
would mean registering first.

Above five slips the app says so and stops, rather than handing over paper that Revenu Quebec
will send back. See `Rl1Service.Validate`.

### The boxes

From the [Guide to Filing the RL-1 Slip](https://www.revenuquebec.ca/en/online-services/forms-and-publications/current-details/rl-1.g-v/):

| Box | Meaning | Where it comes from |
|---|---|---|
| A | Employment income | gross pay for the year |
| B.A | QPP contribution | PayRunLine.CppEmployee for a QC employee |
| B.B | Additional QPP contribution | PayRunLine.Cpp2Employee |
| C | Employment Insurance premium | PayRunLine.EiEmployee |
| D | RPP contribution | not collected, always nil |
| E | Quebec income tax withheld | PayRunLine.ProvincialTax |
| F | Union dues | not collected, always nil |
| G | Pensionable salary or wages under the QPP | gross, or nil if QPP exempt |
| H | QPIP premium | PayRunLine.QpipEmployee |
| I | Eligible salary or wages under the QPIP | gross, or nil if exempt |

H and I were confirmed against Revenu Quebec's own box-by-box pages
([box H](https://www.revenuquebec.ca/en/businesses/source-deductions-and-employer-contributions/filing-rl-slips-and-the-rl-1-summary-general-rules/rl-1-slip-employment-and-other-income/how-to-complete-the-rl-1-slip-box-by-box-instructions/box-h/),
[box I](https://www.revenuquebec.ca/en/businesses/source-deductions-and-employer-contributions/filing-rl-slips-and-the-rl-1-summary-general-rules/rl-1-slip-employment-and-other-income/how-to-complete-the-rl-1-slip-box-by-box-instructions/box-i/))
rather than inferred, because a wrong box letter puts a real number in the wrong place on a
government slip and nothing downstream would catch it.

Three things about those boxes are easy to get wrong:

- **Box I takes `0` rather than being left blank** when there is no eligible salary. Revenu
  Quebec states this explicitly, which is why the renderer always prints it.
- **Box E is the Quebec tax alone.** The federal tax withheld in the same pay run goes to CRA
  and appears in box 22 of the T4. Printing the combined figure would credit the employee with
  Quebec tax that Quebec never received. Pinned by a test.
- **Box I is capped at the QPIP maximum insurable earnings** ($103,000 for 2026, $98,000 for
  2025), which comes from the rate file rather than from the slip.

### What the RL-1 Summary does NOT cover

The summary's remittance total is QPP, QPIP and Quebec income tax. It excludes the
**contribution to the health services fund**, the **contribution related to labour standards**,
and the **workforce skills development contribution**, none of which this app calculates. The
PDF says so on its face, because an employer comparing the total against what they actually
remitted would otherwise conclude they had overpaid.

Employment insurance is federal and is deliberately absent: it is on the T4 Summary.

---

## Prorated figures, the easiest thing here to get wrong

When a province changes its rates part way through the year, CRA does not print the annual
figure in the July edition. It prints a **prorated** one that, combined with what was already
withheld over the first six months, lands the year on the right total.

Three jurisdictions are prorated in the July 2026 edition:

| | Annual figure | July edition figure |
|---|---|---|
| BC, lowest rate | 5.60% | **6.14%** |
| BC, basic reduction | $690 | **$805** |
| Newfoundland and Labrador, basic personal amount | $13,094 | **$15,000** |
| PEI, top bracket rate | 20% | **21%** |

Two things follow. Putting the annual figure into a July edition is wrong in a way that looks
right, because the number matches what the province itself announced. And carrying a July
figure forward into the next January edition is wrong the other way, because the proration has
done its job and expired.

Whenever the "What's new" section of T4127 names a province, assume its figures are prorated
until you have checked otherwise.

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
