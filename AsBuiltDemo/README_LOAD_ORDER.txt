=====================================================================
  RCS COGO ENTERPRISE — As-Built Project Load Instructions
  Project:  JEA W-04471  |  Oakridge Subdivision Phase 1B
  Rev:      1   |   Date: 04/12/2025
=====================================================================

STEP-BY-STEP OPERATOR GUIDE
─────────────────────────────────────────────────────────────────────

1. OPEN APPLICATION
   Launch RCS Cogo Enterprise.
   At the Welcome screen, click:
   → "🏗 New As-Built Job…"

2. NEW JOB WIZARD
   Fill in the following when prompted:
     Project Name   : Oakridge Subdivision Phase 1B Utility Extension
     Availability # : W-04471
     Job Number     : JEA-2024-0412
     Contractor     : Coastal Utility Contractors Inc.
     Utility Type   : Water / Sewer / Storm
     Revision       : Rev 1
     Save Location  : (browse to your output folder)

3. INTAKE — TAB 1: IMPORT POINTS LIST  ← Start Here
   File : W-04471_PNEZD.csv
   Type : PNEZD / CSV
   → Click "Import Points List"
   → Expect: "46 point(s) loaded"

4. INTAKE — TAB 2: IMPORT BATCH / COGO SCRIPT
   File : W-04471_COGO.cogo
   Type : COGO Script
   → Click "Import Batch Script"
   → Expect: "17 run(s), 12 structure(s)"
   
   NOTE: Points MUST be loaded (Step 3) before the script
   because the compiler resolves point IDs dynamically.

5. INTAKE — OPTIONAL: DXF DESIGN LINEWORK
   File : W-04471_DESIGN.dxf
   Type : DXF
   → Click "Import DXF"
   → Merges design linework vertices as reference points.

6. INTAKE — OPTIONAL: JEA TEMPLATE IMPORT
   File : W-04471_JEA_TEMPLATE.csv
   Type : JEA Excel / CSV
   → Click "Import JEA Template"
   → Reconciles against field-collected PNEZD data.

7. PARTS MAPPING (Tab: Parts Mapping)
   Review auto-seeded part assignments.
   For each run / structure, confirm or update the Part Key:
     - WM runs           → PVC-C900-12IN (or similar)
     - WS laterals       → COPPER-1IN-SERVICE
     - FH laterals       → DI-CL51-6IN
     - SS mainline runs  → PVC-SDR35-8IN
     - SS laterals       → PVC-SDR35-4IN
     - SD trunk runs     → RCP-CL3-18IN
     - Manholes          → SS-MH-48IN
     - Junction Boxes    → SD-JB-STD
     - Catch Basins      → SD-CB-STD

8. VALIDATION (Tab: Validation)
   Click "Run Validation".
   All 8 checks should PASS (shown in step summary above).
   Do NOT proceed to Export until this tab shows ✔ PASSED.

9. BUILD DELIVERABLES PACKAGE (Tab: Deliverables)
   Click "Build Package".
   Choose output folder — a Rev1_MMDDYYYY subfolder is
   created automatically containing:
     • _ASBUILT.dxf          CAD drawing
     • _EXPORT_PNEZD.csv     Civil 3D / Carlson points
     • _REPORT.txt           Certification report

─────────────────────────────────────────────────────────────────────
FILE MANIFEST (this folder)
─────────────────────────────────────────────────────────────────────
W-04471_PNEZD.csv          INPUT  — 46 survey points (PNEZD format)
W-04471_COGO.cogo          INPUT  — Pipe network COGO script
W-04471_DESIGN.dxf         INPUT  — Design linework (DXF R2000)
W-04471_JEA_TEMPLATE.csv   INPUT  — JEA field data template
W-04471_ASBUILT.dxf        OUTPUT — Final as-built DXF
W-04471_EXPORT_PNEZD.csv   OUTPUT — Civil 3D/Carlson delivery CSV
W-04471_REPORT.txt         OUTPUT — Certification report
README_LOAD_ORDER.txt      ← This file
=====================================================================
