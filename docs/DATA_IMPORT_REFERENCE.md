# RCS Cogo Enterprise — Data Import Reference Guide
## All Import Workflows, Formats, and Detailed Instructions

> This document covers every method of getting data **into** the application — from GPS field files to JEA Excel templates to hand-typed scripts.

---

## Overview — The 4 Import Pathways

| # | Method | Access | Best For |
|---|---|---|---|
| 1 | **COGO Script Commands** | Script Editor | Any workflow; most flexible |
| 2 | **PNEZD Point List Import** | `Data → Import → Points List` | Field data download from data collectors |
| 3 | **JEA As-Built Excel Template** | `Data → Import → JEA As-Built Excel` | Office-prepared JEA deliverable data |
| 4 | **Pipe Characteristics CSV** | `Data → Pipe Characteristics → Import` | Pipe spec tables exported from Civil 3D |

---

## Method 1 — COGO Script Commands (Recommended)

The most powerful and flexible method. Type commands directly in the Script Editor or paste a prepared text file.

### 1.1 — `NE` — Northing/Easting Point (Most Common)

```
NE <PointID> <Northing> <Easting> "<Description>"
```

**Rules:**
- `PointID` can be a number or a text label (`W-GV-01`, `MH01`, `1001`)
- Description is optional; wrap multi-word descriptions in quotes
- Elevation defaults to 0.00 when omitted

**Examples:**
```
NE 1    740132.45  974518.33  "W-TIE  8in HOTAP  STA 0+00"
NE 101  740132.45  974618.42  "W-MAIN  8in PVC  STA 1+00"
NE W-GV-01  740132.45  974528.33  "GATE VALVE  8in"
```

---

### 1.2 — `NEZ` — Northing/Easting/Elevation Point

```
NEZ <PointID> <Northing> <Easting> <Elevation> "<Description>"
```

Use when elevation (top-of-pipe, rim elevation, invert) is known from field data.

**Examples:**
```
NEZ 200  740132.45  974528.33  25.42  "W-GV-01  8in GATE VALVE  RIM 25.42"
NEZ MH1  740200.00  974600.00  28.75  "EXIST SSMH  RIM 28.75  INV-IN 21.50"
```

---

### 1.3 — `PNT` / `PT` — Alternate Point Definition

Same as NEZ but with different field ordering — used by some data collectors:

```
PNT <PointID> <Northing> <Easting> <Elevation>
PT  <PointID> <Northing> <Easting> <Elevation>
```

---

### 1.4 — `ST` — Structural Node (JEA Pipe Network Point)

```
ST <NodeID> <Northing> <Easting> <Elevation> "<Description>"
```

`ST` creates a pipe network node used as the `FromPoint`/`ToPoint` in `PRUN` commands. Naming convention: use JEA asset codes as the ID.

**Examples:**
```
ST mh-01   740200.00  974600.00  28.75  "SSMH-01"
ST mh-02   740300.00  974700.00  27.50  "SSMH-02"
ST wv-01   740132.45  974528.33  0.00   "W-GV-01"
```

---

### 1.5 — Auto-Point Mode (`AP`)

When enabled, the engine assigns point IDs automatically instead of requiring you to specify them.

```
AP ON          // Enable auto-numbering (starts at 1, increments)
NE 740132.45  974518.33  "W-TIE"    // ID auto-assigned = 1
NE 740132.45  974618.42  "W-MAIN"   // ID auto-assigned = 2
AP OFF         // Disable auto-numbering
```

---

### 1.6 — Mass Script Import (Paste from Excel/Data Collector)

If your field crew provides data in a spreadsheet, convert it to NE commands using this Excel formula:

**Excel formula in column A** (assumes N in B, E in C, Elev in D, Desc in E):
```excel
= "NE " & A2 & " " & TEXT(B2,"0.000") & " " & TEXT(C2,"0.000") & " " & TEXT(D2,"0.00") & " """ & E2 & """"
```

Then copy-paste all generated rows into the Script Editor and run.

---

## Method 2 — PNEZD Point List Import (`.txt` / `.csv`)

**Access:** `Data → Import → Import Points List`

This imports a flat text file with one point per line. This is the standard output format from Trimble, Leica, and most GPS data collectors.

### Required Format

```
<PointID>  <Northing>  <Easting>  <Elevation>  <Description>
```

**Delimiters accepted:** comma `,`, tab `\t`, or space (any mix, multiple spaces OK)

**Minimum required columns:** 3 (`PointID`, `Northing`, `Easting`)

### Format Examples

**Space-delimited (Trimble Digital Fieldbook):**
```
100  740132.450  974518.330  25.400  W-TIE 8in HOTAP
101  740132.450  974618.420  25.380  W-MAIN 8in PVC
200  740132.450  974528.330  25.420  W-GV-01 GATE VALVE
```

**Comma-delimited CSV (data collector download):**
```
100,740132.450,974518.330,25.400,W-TIE 8in HOTAP
101,740132.450,974618.420,25.380,W-MAIN 8in PVC
```

**Tab-delimited (Excel export):**
```
100	740132.450	974518.330	25.400	W-TIE 8in HOTAP
```

**No-elevation CSV (2D GPS, 3-column minimum):**
```
100,740132.450,974518.330
101,740132.450,974618.420
```

### Rules & Behavior

| Rule | Detail |
|---|---|
| Header row | Skipped automatically if first field is non-numeric |
| Blank lines | Skipped silently |
| Description | Everything after column 4 is joined as the description |
| Duplicate IDs | **Overwrite** existing point's N/E/Z in both the project and database |
| Point ID type | Must be a string (numbers or alphanumeric both work) |
| Elevation missing | Defaults to `0.00` |
| Comments | Lines starting with `//` or `!` are **not** skipped — use blank lines instead |

### Column Order Reference

| Col | Field | Required |
|---|---|---|
| 1 | Point ID | ✅ Yes |
| 2 | Northing | ✅ Yes |
| 3 | Easting | ✅ Yes |
| 4 | Elevation | Optional (defaults 0) |
| 5+ | Description | Optional |

> **Note:** The parser does NOT support swapped N/E. Northing must be column 2. If your data collector uses `P, E, N, Z, D` format (Easting before Northing), swap the columns in Excel before importing.

---

## Method 3 — JEA As-Built Excel Template (`.xlsx`)

**Access:** `Data → Import → 🚀 New Project from Segment1A Excel (.xlsx)...`

This is the heavyweight import — it reads the official JEA As-Built Excel template and populates all utility asset tables in the project database in a single pass.

### Supported Sheets (All Disciplines)

| Sheet Name | Asset Type | Key Columns |
|---|---|---|
| `Sewer Manhole` | Sewer manholes | A=MH#, K=GPS_Y(N), L=GPS_X(E) |
| `Sewer Pipe` | Gravity sewer pipe | A=Pipe#, F=UpMH, G=DnMH, J=Length |
| `Sewer Fitting` | Sewer fittings | A=Fitting#, H=GPS_Y(N), I=GPS_X(E) |
| `Sewer Valve` | Sewer valves | A=Valve#, F=GPS_Y(N), G=GPS_X(E) |
| `Sewer Meter` | Sewer meters | A=Meter#, E=GPS_Y(N), F=GPS_X(E) |
| `Water Pipe` | Water main pipe | A=Pipe#, G=StartN, H=StartE, I=EndN, J=EndE |
| `Water Fitting` | Water fittings | A=Fitting#, H=GPS_Y(N), I=GPS_X(E) |
| `Water Valve` | Water valves | A=Valve#, H=GPS_Y(N), I=GPS_X(E) |
| `Water Hydrant` | Hydrants | A=Hydrant#, F=GPS_Y(N), G=GPS_X(E) |
| `Water Meter` | Water meters/services | A=Meter#, E=GPS_Y(N), F=GPS_X(E) |
| `Reclaimed Pipe` | Reclaimed water pipe | Same as Water Pipe |
| `Reclaimed Fitting` | Reclaimed fittings | Same as Water Fitting |
| `Reclaimed Valve` | Reclaimed valves | Same as Water Valve |
| `Reclaimed Hydrant` | Reclaimed hydrants | Same as Water Hydrant |
| `Reclaimed Meter` | Reclaimed meters | Same as Water Meter |
| `Water_Main_Fittings` | S1A water fittings | A=Fitting#, M=X(E), N=Y(N) |
| `Water_Wire_Box` | Locate boxes | A=Box#, C=X(E), D=Y(N) |
| `Water_Main_Valves` | S1A water valves | A=Valve#, M=X(E), N=Y(N) |
| `Water_Main_Top_Of_Pipe` | Top-of-pipe survey | A=Point#, O=X(E), P=Y(N) |
| `Pipe_Crossings` | Pipe crossings | A=Crossing#, M=X(E), N=Y(N) |
| `Validus_Bore_Log` | Bore log points | A=Index, B=N, C=E, D=Elev |
| `Burnt_Mill_Bore_Log` | Bore log points | A=Index, B=N, C=E, D=Elev |

### Import Rules

| Rule | Behavior |
|---|---|
| Row 1 | **Always treated as header — skipped** |
| Blank ID (Col A) | Row skipped entirely |
| `nan` values | Treated as null/empty |
| Duplicate Part Key | **Skipped with warning** (existing data preserved) |
| Missing sheet | Silently skipped (not an error) |
| Coordinates | GPS_Y = Northing, GPS_X = Easting throughout ALL sheets |
| Missing coords | Record is still imported; N/E default to 0 |

### Step-by-Step Instructions

1. Open the JEA As-Built Template (`Simulated_JEA_AsBuilt_Template.xlsx`)
2. Fill in each applicable discipline tab with field data
3. Save the `.xlsx` file
4. In the app: `Data → Import → 🚀 New Project from Segment1A Excel (.xlsx)...`
5. Browse to your filled template
6. Click **Open**
7. A summary dialog shows: `Imported X records across Y sheets. Z blank rows skipped.`

> **Critical:** Sheet names must match exactly (case-sensitive). Do not rename tabs. If a tab doesn't apply to your project, leave it empty — blank sheets are skipped automatically.

---

## Method 4 — Pipe Characteristics CSV

**Access:** `Data → Pipe Characteristics → Import`

Imports pipe material/specification data (not geometry) for the Pipe Characteristics table. This is typically exported from Civil 3D or provided by the contractor.

### Required Format (CSV, comma-delimited)

```
PipeID, MaterialCode, Diameter, WallThickness, Class, Manufacturer, JointType, Lining
```

**Example:**
```csv
PipeID,MaterialCode,Diameter,WallThickness,Class,Manufacturer,JointType,Lining
WP-001,PVC,8,0.375,C900,JM Eagle,Push-On,None
WP-002,DI,12,0.350,CL52,US Pipe,Mechanical,Cement Mortar
SS-001,VCP,8,0.500,Ex-Strength,Pacific Coast,Push-On,None
```

**Rules:**
- Row 1 = header (skipped)
- Comma delimited only (no tabs/spaces)
- Extra columns are ignored
- Blank rows skipped

---

## Formats at a Glance

```
┌─────────────────────────────────────────────────────────────────────┐
│ COGO SCRIPT                                                         │
│  NE  <ID> <N> <E> ["Desc"]           → Simple 2D point             │
│  NEZ <ID> <N> <E> <Z> ["Desc"]       → 3D point with elevation     │
│  PNT <ID> <N> <E> <Z>                → Alternate 3D format         │
│  ST  <NodeID> <N> <E> <Z> ["Desc"]   → Pipe network node           │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ PNEZD TEXT FILE  (.txt / .csv)                                      │
│  <ID>  <N>  <E>              → Minimum 3 cols (no elevation)        │
│  <ID>  <N>  <E>  <Z>         → 4 cols with elevation               │
│  <ID>  <N>  <E>  <Z>  <Desc> → Full PNEZD (5 cols)                 │
│  Delimiters: comma, tab, space (any)                                │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ JEA EXCEL TEMPLATE  (.xlsx)                                         │
│  Row 1: Header (skipped)                                            │
│  Col A: Asset ID (blank = skip row)                                 │
│  GPS_Y = Northing   GPS_X = Easting   (on all sheets)              │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Common Import Errors & Fixes

| Error | Cause | Fix |
|---|---|---|
| Points appear at 0,0 | N and E columns swapped | Swap cols 2 and 3 in your file |
| Import count = 0 | Header detected on row 1 but col 1 is numeric | Add a text header row, or delete header |
| Description truncated | Unquoted description has spaces parsed as delimiters | Put description in quotes in COGO script; for PNEZD files all text after col 4 is joined |
| Excel sheet not found | Sheet name doesn't match exactly | Check spelling and case of tab name |
| Duplicate skipped warning | PartKey already exists from prior import | Expected behavior — data is preserved; run again after deleting old records if needed |
| `nan` values in Excel | Python-generated file used NaN for empty | Import service handles this automatically |
| Points off by 10,000 ft | Different datum/coordinate zone | Verify your GPS unit is set to FL State Plane East (NAD83), not Lat/Lon |

---

## Best Practices

1. **Always use COGO Script for survey points** — it gives you full control, is auditable, and produces a re-runnable record of your data entry.

2. **Use PNEZD import for data collector downloads** — GPS total station data from Trimble, Leica, or Topcon comes out in this format natively.

3. **Use the JEA Excel template for asset tables** — this is what JEA expects; fill it in the field, import once, done.

4. **Never mix coordinate systems in one project** — all coordinates must be in the same system (FL State Plane East is the JEA standard).

5. **Run `MAPCHECK` after any import** to verify the imported points form a geometrically valid boundary before generating the DXF.

6. **Use `LIST PTS` after import** to verify point count and spot-check IDs:
   ```
   LIST PTS
   ```
   Outputs a count and first/last IDs — quick sanity check.

7. **If in doubt, test with 3 points first** — run a small subset through your import method and verify on the canvas before committing the full dataset.
