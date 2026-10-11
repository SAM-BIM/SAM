# Energy and mass units, engineering display formatting (Q4) - SAM

Branch `feature/units-energy-mass-formatting` -> base `sow/2026-Q4` (cut from `b081065e`). Record date: 2026-10-08.

This is PR2 of the SAM Native Optimisation declarative/engineering-UX phase. In that plan:
- PR1 (SAM_UI) is wording and layout;
- PR2 (this PR) provides units and display formatting;
- PR3 (SAM) adds `SAM.Core.Optimisation`.

PR1, PR2 and PR3 are independent. PR4 and later wait for owner review. The optimisation UI consumes this formatting later (SAM_UI PR5); nothing consumes it yet.

## Current status

PR open, **not merged**. Public API additions only. No existing display policy, unit, ordinal or conversion changed.

## Why

Native optimisation results are shown at full round-trip precision (e.g. `Setpoint = 4.968943799848584`, `Result = 7360.04370117188`), with no units. The fix should follow three principles:
1. **Display-only** engineering formatting. Computed and persisted values keep full precision.
2. **Reuse SAM's existing display layer** (`SAM.Units` + `SAM.Core.Reporting.QuantityFormatter`) rather than add a new formatter.

That layer lacked three things:
- energy units (kWh/MWh);
- mass units (kg/t);
- a way to format values whose unit is unknown, which is most optimisation outputs today, because their meaning is defined by the user's Tas script.

## Work completed

### `SAM.Units`
- **`UnitType`: appended** `WattHour "Wh"`, `KilowattHour "kWh"`, `MegawattHour "MWh"`, `Kilogram "kg"`, `Tonne "t"`. No existing ordinal moved.
- **`UnitCategory`: appended** `Energy`, `Mass`.
- **`Query.UnitCategory`** maps the new types.
- **`Query.UnitType(UnitStyle, UnitCategory)`:** kWh and kg in both styles. SAM has no Imperial energy or mass unit.
- **`Query.UnitTypes`** lists Wh/kWh/MWh and kg/t.
- **Linear factor table:**
  - Energy is based on **Wh** (1, 1 000, 1 000 000), so kWh ⇄ MWh is exact for typical values;
  - Mass is based on kg (1, 1 000).
- **`Convert.ToSI` / `ToImperial`:** energy goes to kWh and mass to kg (the category's single default unit).
- **`Jule` / `Kilojule` stay in the existing `Enthaply` family.** Energy does not convert to J. Moving J into Energy would change existing behaviour and is out of scope.

### `SAM.Core.Reporting`
- **`QuantityFormatter.DisplayUnit`:**
  - Energy: `kWh`, 1 dp;
  - Mass: `kg`, 1 dp;
  - the same in both unit systems.
- **`SelectDisplayUnit` group switching** is now table-driven. A group switches to the larger unit when its largest value reaches the threshold:

  | Category | Switch | Display |
  |---|---|---|
  | Power (SI) | ≥ 10 kW | kW, 2 dp (unchanged) |
  | Power (Imperial) | ≥ 100 kBtu/h | kBtu/h, 1 dp (unchanged) |
  | Energy | ≥ 10 MWh | MWh, 2 dp (new) |
  | Mass | ≥ 10 t | t, 2 dp (new) |

  Quantities of another category are ignored, as before.
- **`DocumentOptions.Decimals`** (`Dictionary<UnitCategory, int>`, default null): replaces the default display decimals of a category (for example percent at 1 dp). It applies to the switched unit too, and is copied by the copy constructor. Display only.
- **New public helpers:**
  - `FormatNumber(double, int decimals)`: the existing private method made public. Away-from-zero rounding, never "-0", culture separators. It now returns "—" for NaN/∞ and checks `decimals` is 0–15.
  - `FormatSignificant(double, int significantFigures = 4)`: for values with no known unit. 7360.04370117188 → `7,360`, 4.968943799848584 → `4.969`. Magnitudes below 1e-3 (other than zero) use scientific notation (`1.230E-4`). NaN/∞ give "—".
  - `static DecimalsForSignificantFigures(IEnumerable<double>, int significantFigures = 4)`: one decimal count for a column of comparable values, from its largest finite magnitude, clamped to 0–6.
- **Not added to `IQuantityFormatter`**, which avoids breaking implementers.

### Explicitly unchanged
- Every existing category's display unit, symbol and decimals. In particular, **Count → "persons" (1 dp) and Ratio/percent 0 dp stay as they are**, because the Space reports depend on them. Optimisation simulation counts will be formatted as integers by the optimisation UI itself, not through `Count`.
- Power group switching thresholds and units.
- All existing conversions and enum ordinals (existing ordinal tests unchanged and passing).

## Decisions and assumptions
- **Energy/Mass defaults are kWh/kg for both unit styles**, documented in code.
- **Significant figures default to 4** (an engineering reading precision). Callers choose otherwise. Columns should share decimals through `DecimalsForSignificantFigures` + `FormatNumber`.
- **Scientific notation below 1e-3** keeps tiny values readable without many leading zeros.
- **Helpers live on the class, not the interface**, so the interface stays stable.

## Files changed
- `SAM/SAM.Units/Enums/{UnitType,UnitCategory}.cs`
- `SAM/SAM.Units/Query/{UnitCategory,UnitType,UnitTypes}.cs`
- `SAM/SAM.Units/Convert/{ByUnitType,ToSI,ToImperial}.cs`
- `SAM/SAM.Core.Reporting/Classes/{QuantityFormatter,DocumentOptions}.cs`
- Tests (new): `SAM/SAM.Tests/{EnergyMassUnitsTests,QuantityFormatterEngineeringTests}.cs`
- This record.

## Validation
- `dotnet build SAM.sln -c Release`: 0 errors.
- `SAM.Tests` built explicitly (it is not in `SAM.sln`):
  - full suite **3137/3137 passed** (baseline 3071; +66 new);
  - this includes the existing Space report document/golden tests, `UnitsTests` (enum ordinals, round trips, category closure, abbreviation uniqueness and parsing for every unit type, "every new category valid in both styles") and `QuantityFormatterTests`, all unchanged.
- New tests:
  - **Energy/mass:** ordinals appended; category, abbreviation and parsing; conversions within the category; cross-category NaN (including kWh → J); defaults; `ToSI`/`ToImperial`.
  - **Display:** energy/mass display and group switching at the thresholds; Power switching unchanged; decimals override (including the switched unit and copy semantics).
  - **Significant figures:** the table, scientific notation, no "-0", NaN/∞, de-DE separators, column decimals, argument checks, and values never changed by formatting.
- `git diff --check`: clean.

## Unresolved issues and risks
- **Two `UnitType` values with the same lower-case form would collide in the case-insensitive parser.** There are none today (covered by the existing uniqueness test).
- **Hosts that persist `UnitType` by ordinal** are unaffected, because the values are appended.

## Next step
Owner review. After merge, add the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4`. SAM_UI consumes the formatter in PR5 (after PR3/PR4).
