# End-to-End Project Walkthrough
## JEA As-Built — Oakwood Subdivision Phase 1 Water Main (70498-W1A)

> **This walkthrough shows a complete project from first launch to final DXF deliverable.**
> Follow each step in order. The companion script is located at:
> `SampleScripts/JEA_Oakwood_WaterMain_70498-W1A.cogo`

---

## Project Scenario

A contractor has installed **640 linear feet of 8" PVC C-900 water main** in the Oakwood Drive right-of-way. Your job as the EOR is to process the field data, verify closure, and produce a JEA-compliant As-Built package containing:

| Deliverable | Format |
|---|---|
| Survey point map | DXF (AutoCAD) |
| Closure & area report | Embedded in DXF |
| COGO points list | CSV |
| JEA validation badge | Inside application |

**Installed infrastructure:**
- 640 LF × 8" PVC water main
- 2 gate valves (isolation)
- 1 fire hydrant (6" stub)
- 4 × 1" copper residential service laterals
- 1 hot tap connection to existing main

---

## Step 1 — Create a New Project

1. Launch **RCS Cogo Enterprise**
2. Click **File → New Project**
3. Fill out the **Project Details** form:

| Field | Value |
|---|---|
| Project Name | `Oakwood Subdivision Ph1` |
| Availability Number | `70498-W1A` |
| Utility Type | `Water` |
| Units | `USFT` |
| Revision | `1` |
| Save Location | Click **…** → choose a folder (e.g., `C:\Projects\`) |

4. Click **Save Details** → the software creates the project folder:
   ```
   C:\Projects\JEA_70498-W1A_Oakwood_Subdivision_Ph1_Water_USFT\
   ```

> **Note:** The folder name is auto-generated from your inputs in JEA naming format.

---

## Step 2 — Load the Example Script

1. Click **File → Open Script** (or paste into the Script Editor)
2. Open: `SampleScripts/JEA_Oakwood_WaterMain_70498-W1A.cogo`
3. The script editor will show the full 8-section script

### What's in the Script

```
§1   Control Points         — NGS monuments (the "known" reference)
§2   Water Main Backbone    — 9 centerline points, STA 0+00 to 5+66
§3   Appurtenances          — 2 valves, 1 hydrant, 4 service laterals
§4   Boundary Figure        — ROW disturbance footprint polygon
§5   MAPCHECK               — Closure verification
§6   Pipe Network           — Hydraulic connectivity (PRUN commands)
§7   RENUMBER               — Resequences to JEA numbering convention
§8   Export Commands        — Instructions for final file generation
```

---

## Step 3 — Run the Script

Click the **▶ Run** button (or press `F5`).

Watch the **Log** pane on the right. You should see:

```
===================================================================
 JEA AS-BUILT  |  70498-W1A  |  Oakwood Subdivision  |  Phase 1
===================================================================
--- §1: Establishing Control ---
Control established. Occupying MON-A, backsighting MON-B.
--- §2: Water Main Backbone ---
Backbone: 9 water main points defined.
--- §3: Valves, Hydrant, and Services ---
Appurtenances: 2 gate valves, 1 hydrant, 4 service laterals.
--- §4: ROW Disturbance Boundary Figure ---
Boundary figure ROW_DISTURBANCE built.
--- §5: MAPCHECK — Closure Verification ---
```

The **MAPCHECK report** is the most important output (see Step 4).

---

## Step 4 — Reading the MAPCHECK Report

After running the script, look at the MAPCHECK section in the log:

```
======================================================================
                 SURVEY MAPCHECK: ROW_DISTURBANCE
======================================================================
Start Point: 400    N: 740144.4500   E: 974518.3300

Line    Brg: N 90° 00' 00" E   Dist: 450.3600
End Point:   401    N: 740144.4500   E: 974968.6900

Line    Brg: S 00° 00' 00" E   Dist: 191.9990
End Point:   402    N: 739952.4500   E: 974980.6900

...

----------------------------------------------------------------------
Perimeter: 1,284.72 ft
Area: 77,483.20 sq.ft, 1.7786 acres
Closure Error: 0.0000, Brg: N 0° 00' 00" E
Precision: Perfect Closure
```

### What to Look For

| Metric | Acceptable | What Happens if Not Met |
|---|---|---|
| **Closure Error** | ≤ 0.05 ft | Figure turns **RED** on canvas; DXF table shows red text |
| **Precision Ratio** | ≥ 1 : 5,000 | Fails JEA minimum acceptance |
| **Area** | Should match permit | Verify against ROW permit drawings |

> **If closure FAILS:** Check the field data for the highest-error bearing. A single wrong quadrant (N vs S, E vs W) causes a bow-tie. Correct the suspect bearing in your NE command and re-run.

---

## Step 5 — Inspect the Canvas

After the script runs, the **Canvas** tab shows:

| Element | Color | Meaning |
|---|---|---|
| Blue dots | — | Survey control points (MON-A, MON-B) |
| Yellow dots | — | Water main backbone points |
| Yellow line | — | `ROW_DISTURBANCE` boundary (passes MAPCHECK → yellow) |
| Blue lines | — | 8" water main pipe runs |
| Cyan lines | — | 6" hydrant lateral |
| Gray lines | — | 1" service laterals |

Use the **Figures** panel on the left to see the QC badge:
- **✓** green — `ROW_DISTURBANCE` passed closure
- **?** gray — MAPCHECK not yet run on that figure

Hover the ✓ badge to see the full tooltip:
```
QC: Passed
Closure: 0.0000 ft
Area: 1.7786 ac
Precision: 1:∞ (Perfect)
Checked: 2026-04-11 17:09
```

---

## Step 6 — Run JEA Validation

Click **Validation → JEA As-Built Validation**

The window auto-runs checks and shows:

| Check | Status |
|---|---|
| All pipe points described | ✅ Pass |
| All gate valves present | ✅ Pass |
| Fire hydrant recorded | ✅ Pass |
| MAPCHECK run on figures | ✅ Pass |
| Save location set | ✅ Pass |

The **live badge** in the toolbar updates to show total issues.

> If any check shows ⚠️, correct the flagged item in the script and re-run before generating the DXF.

---

## Step 7 — Export the DXF

Click **File → Export → Full Project DXF**

The DXF is saved to:
```
C:\Projects\JEA_70498-W1A_Oakwood_Subdivision_Ph1_Water_USFT\
    Oakwood_Subdivision_Ph1.dxf
```

**Open the DXF in AutoCAD**. You will see:

### Layer Structure

| Layer | Content |
|---|---|
| `STRUCT_W_BACKBONE` | 8" water main centerline (Blue) |
| `STRUCT_W_LATL` | Service laterals (Blue) |
| `STRUCT_W_HYD` | Hydrant lateral (Blue) |
| `FIGURE_CLOSURE_TABLE` | Closure report table (Yellow labels / White values) |
| `POINTS` | Survey point symbols + IDs |

### Closure Table (Embedded in DXF)

The auto-generated closure table appears in the lower-right of model space:

```
╔══════════════════════════════════════════════════╗
║  BOUNDARY CLOSURE REPORT — ROW_DISTURBANCE       ║  (Yellow header)
╠══════════════════════════════════════════════════╣
║  Closure Error :  0.0000 ft                      ║  (White values)
║  Bearing       :  N 00°00'00" E                  ║
║  Area          :  77,483.20 sq ft                ║
║  Acres         :  1.7786 ac                      ║
║  Perimeter     :  1,284.72 ft                    ║
║  Precision     :  Perfect Closure                ║
╠══════════════════════════════════════════════════╣
║  ► CLOSED ◄                                      ║  (Green text)
╚══════════════════════════════════════════════════╝
```

> If the figure had a misclosure, the status line would be in **red** and show the error distance.

---

## Step 8 — Export COGO Points CSV

Click **File → Export → COGO Points CSV**

Output format:
```csv
Point, Northing, Easting, Elevation, Description
1001, 740132.450, 974518.330, 0.000, W-TIE  8in HOTAP  EXIST MAIN  STA 0+00
1002, 740132.450, 974618.420, 0.000, W-MAIN  8in PVC  STA 1+00.09
1003, 740132.450, 974718.510, 0.000, W-MAIN  8in PVC  STA 2+00.18
...
2001, 740132.450, 974528.330, 0.000, W-GV-01  8in GATE VALVE  STA 0+10
2002, 740132.450, 974908.690, 0.000, W-GV-02  8in GATE VALVE  STA 3+90
2010, 740080.200, 974950.100, 0.000, W-HYD-01  JEA STD HYDRANT  6in LATERAL
2020, 740132.450, 974660.000, 0.000, W-SVC-01  1in COPPER  LOT 1
...
```

> **Note:** Point IDs now reflect the JEA convention (1001+) applied by RENUMBER in §7 of the script.

---

## Step 9 — Final Checklist Before Submission

Use this checklist before sending deliverables to JEA:

- [ ] **MAPCHECK Closure ≤ 0.05 ft** — verified in log
- [ ] **Precision ratio ≥ 1:5,000** — verified in log
- [ ] **All appurtenances labeled correctly** — `W-GV-xx`, `W-HYD-xx`, `W-SVC-xx`
- [ ] **DXF opens correctly in AutoCAD** — spot-check layer visibility
- [ ] **Closure table visible** — confirm `FIGURE_CLOSURE_TABLE` layer is ON
- [ ] **JEA Validation badge shows 0 errors** — all checks green
- [ ] **Point numbering starts at 1001** — RENUMBER applied
- [ ] **Project folder created** — correct JEA naming format
- [ ] **All 4 deliverables saved** — `.dxf`, `.csv`, `.db`, COGO script

---

## Key Command Reference for this Project

```text
// Define a 3D survey point (Northing, Easting, Elevation, Description)
NE <ID> <N> <E> "<Description>"

// Occupy a station and set backsight
STN <ID>
BS <ID>  <Angle_DMS>

// Build a boundary figure polygon
BEG <FigureName>
FIG <PointID>           // connect point to figure
END

// Closure analysis — run before DXF export
MAPCHECK <FigureName>

// Pipe run (pressure main)
PRUN START <FromPt> <ToPt> <Code> <Diameter> <Invert>

// Pipe lateral (off main)
PRUN LATERAL <FromPt> <ToPt> <Code> <Diameter> <Invert>

// Renumber point IDs in range
RENUMBER <start> <end> <newStart>

// Print all points and figures
LIST PTS
LIST FIGS
```

---

## Common Issues & Fixes

| Symptom | Likely Cause | Fix |
|---|---|---|
| Figure turns **RED** on canvas | MAPCHECK failed — closure > 0.05 ft | Check for flipped N/S or E/W in NE command |
| Closure table shows **red** text in DXF | Same as above | Re-run MAPCHECK after fix |
| Points not renumbered | RENUMBER target IDs already exist | Use `LIST PTS` first to confirm free range |
| Pipe runs not visible | Script stopped before `PRUN` section | Check log for error above §6 |
| JEA Validation shows badge count | Missing descriptions or asset names | Update NE descriptions to match JEA naming |
| MAPCHECK shows bow-tie area | Points out of sequence | Reorder FIG commands in BEG/END block |

---

*This walkthrough corresponds to script version: `JEA_Oakwood_WaterMain_70498-W1A.cogo`*
*RCS Cogo Enterprise Modern — Version 2.2.0*
