# RCS COGO Enterprise Modern - User Guide

Welcome to the RCS COGO Enterprise Modern User Guide! This document provides a comprehensive overview of the commands and capabilities of the advanced scripting engine.

## 1. Introduction to COGO Scripting

COGO (Coordinate Geometry) scripting allows you to define points, lines, curves, and figures through text-based commands. The system engine interprets these scripts line-by-line to build an interactive, geometric mapping environment. 

### Basic Syntax Rules
- **Formatting:** Each command goes on a new line. Multiple parameters should be separated by spaces.
- **Comments:** Lines starting with `//`, `!`, or `;` are ignored by the parser. Use these to document your workflows!
- **Case-Insensitive:** Commands like `NE` and `ne` execute the same function, though UPPERCASE commands are often preferred for readability.

---

## 2. Core Point Definitions

Points represent raw coordinate data mapping (Northing, Easting, Elevation) within the environment.

*   `NE <PointId> <Northing> <Easting> [Description]`
    *   Defines a basic Northing/Easting point.
    *   *Example:* `NE 100 5000.000 5020.000 "Iron Pipe"`

*   `NEZ <PointId> <Northing> <Easting> <Elevation> [Description]`
    *   Defines a 3D target with Elevation.
    *   *Example:* `NEZ 101 5000 5020 120.45 "Fire Hydrant"`

*   `PNT <PointId> <Northing> <Easting> <Elevation>`
    *   Shorthand for setting standard points.

*   `DEL <PointId>`
    *   Wipes a point from the operational database.
    *   *Example:* `DEL 100`

---

## 3. Traverse and Observations

Establish "occupied" logic stations to chain commands directionally. 

*   `STN <PointId>`
    *   Occupies a specific known point.
*   `BS <PointId> or BS <Azimuth>`
    *   Establishes a Backsight reference orientation.
*   `TRAV <ON/OFF>`
    *   Enables automatic traverse advancing (current station jumps to the target point after an observation calculation).

*   `ZD <PointId> <Azimuth> <Distance> [Description]`
    *   Calculates a new coordinate by shooting a Zenith/Distance from the occupied Station.
*   `AD <PointId> <TurnedAngle> <Distance> [Description]`
    *   Calculates relative to the currently active Backsight referencing point via Angle Right/Left.
*   `TURN <Angle>`
    *   Sets a new rotational origin without moving.
    *   *Example:* `TURN 90.00` shifts the scope 90 degrees right.

---

## 4. Constructing Figures and Polylines

Points can be connected visually using Figures.

*   `BEG <FigureName>` or `FIGURE <FigureName>`
    *   Initializes a new figure layer.
*   `FIG <PointId>`
    *   Connects the specified point to the end of the currently active figure trace.
*   `END`
    *   Ends the drawing state of the current figure boundary.
*   `CLOSURE` / `MAPCHK`
    *   Performs an internal tolerance check tracking Closure errors against traversing loops.

---

## 5. Curve and Intersection Solving

For modeling realistic right-of-ways or compound radii loops.

*   `XC BD (BULB) <Radius> <Azimuth> <Chord-Distance>`
    *   **Extend Curve**: Generates high-fidelity visual curve sweeps extending from a `FIG` boundary.
    *   Positive Radius indicates curvature bulging Right. Negative Radius bulges Left.
*   `CP <PointId> <Radius> <CentralAngle>`
    *   Extracts a computed geometric center point from delta parameters.
*   `PTOFFSET <LineStartPoint> <LineEndPoint> <Distance>`
    *   Sets points perpendicular laterally off an established bearing reference line.
*   `OFFSETLINE <StartPt> <EndPt> <WidthSpacing>`
    *   Draws parallel geometries tracking original alignments.
*   `RKRK <Pt1> <R1> <Pt2> <R2> <TargetBaseId>`
    *   Solves complex intersecting distance ranges (Circle-Circle intersecting computations).

---

## 6. System Macro Commands

Useful utility functions for debugging, configuring, and verifying scripts.

*   `RESET` - Instantly wipes all environment configurations, parameters, and loaded geometric map points. Clean slates the execution state.
*   `ABOUT` - Yields versioning block info to the terminal.
*   `ECHO <Message...>` - Prints custom string messages back to the logger trace for tracking sequential operations.
*   `LOG <ON/OFF>` - Temporarily silences log trace output. Very useful for hiding output dumps in massive script blocks.
*   `LIST <PTS/FIGS>` - Spits out a tabular list summarizing the counts and IDs of loaded targets.
*   `SHOW <PointId>` - Inspects a single target to provide immediate metadata mapping tracing.
*   `SET <Variable> <Value>` - Manipulate operational runtime modes (e.g., `SET UNITS METRIC`).
*   `REPORT <Type>` - Renders diagnostic summary outputs. 

---

## 7. Shared Scripting Execution Toggles

With dual-engine capabilities, scripts can contain both COGO mapping parameters and Pipenetwork models securely. You can actively toggle which engine listens to the current script block to prevent commands from throwing unknown token errors.

*   `cogo-engine-off` - Disables the COGO Engine. All processing is handed entirely to the active Pipe engine.
*   `cogo-engine-on`  - Restores the COGO Engine back to parsing standard commands.
*   `pipe-engine-off` - Suspends Pipe Network Scripting. Extremely useful during intensive geometric intersections and loops.
*   `pipe-engine-on`  - Activates Pipe Network construction commands (e.g. `PRUN`) to observe the script lines again.

---

### Command Environment Best Practices
Always group your logic flows comprehensively! Standardize by initiating your baseline bounds (`NE`), building your Station networks (`STN/BS/TRAV`), drawing your visual output models (`FIG`), checking mathematical tolerances (`CLOSURE`), and cleaning up diagnostics (`LIST / ECHO`).

---

## 8. Recent Modernization Features (Version 2.0+)

The Version 2.0 update introduces heavy modernization tailored to streamline utility reporting and compliance natively inside the software:

### 8.1 Project Details Window
**Access:** Main Menu `File` -> `Project Details...`
**Function:** Locks in standard JEA AS-BUILT reporting metadata such as *Engineer of Record (EOR)*, *Project Title*, and *Dates*. This persists to the SQLite backbone to ensure all exports auto-populate correct headers.

### 8.2 Live installed Asset Tables & DXF Output
**Access:** Main Menu -> `Tables`
**Function:** A new dark-themed dashboard providing an immediate spreadsheet interface into the database for 7 critical utility classes (Force Mains, Sanitary Manholes, Water Fittings, Valves, Hydrants, Services, and Locate Boxes).
**Export:** Easily generate compliant JEA template field tables. Navigating to any tab and selecting **"📐 Export Tab → DXF"** will draw native AutoCAD `LINE` and `TEXT` entities to produce a strict, styled ACI magenta and cyan deliverable ready for formal plans.

### 8.3 Points Along Pipe (PAP)
**Access:** Main Menu -> `Data` -> `Points Along Pipe`
**Function:** Operates on active pressure mains or gravity networks mapping the pipeline alignments mathematically and extracting inverted vertices exactly on 50 FT stationed intervals automatically. 

### 8.4 Automated Civil 3D Batch Plotting
The plugin features robust batch plotting integrations (when running the accompanying native Civil 3D extension module). It reads window selections to slice up complex geometric plans into pristine multi-page PDF documents unconditionally centered on standard 0.25-inch margins.

---

## 9. Version 2.2.0 Features

### 9.1 AI Streaming Chat (SSE Pipeline)
**Access:** `Tools` -> `AI Script Assistant`
**Function:** The AI chat window now uses a Server-Sent Events (SSE) streaming connection, rendering each token as it arrives. A placeholder bubble appears immediately, eliminating perceived latency. Supports `.jpg` / `.png` attachments for automatic Plat-to-DXF extraction.

### 9.2 MAPCHECK Persistence & QC Badges
**Command:** `MAPCHECK <FigureName>`
**Function:** Running MAPCHECK now permanently stores results on the Figure for the entire session:
- **QC Status** — Passed / Failed / Unknown
- **Closure Error** — distance in feet from last point back to Point of Beginning
- **Area** — square feet and acres (Shoelace formula)
- **Perimeter** — total boundary length in feet
- **Precision Ratio** — 1:N conventional surveying measure
- **Last Checked** — UTC timestamp

**QC Badge on canvas:** checkmark (passed), red X (failed), gray ? (not yet run). Hover for full tooltip.

### 9.3 RENUMBER Command
**Syntax:** `RENUMBER <StartPt> <EndPt> <NewStart>`
**Function:** Renumbers a contiguous range of point IDs. All Figure references are updated in place. A pre-flight collision check aborts the operation if any target ID already exists outside the rename range.

**Examples:**
`
RENUMBER 1 50 1001       // renames pts 1-50 to 1001-1050
RENUMBER 100 105 200     // renames pts 100-105 to 200-205
`

### 9.4 Script Auto-Save
Every batch execution automatically saves a timestamped copy to `[ProjectDir]\Scripts\MMDDYYYY.fff\CogoScript.cogo`. Fallback is AppData when no project is active.

### 9.5 DXF Closure Tables with Color Coding
Every DXF export appends an AutoCAD-style closure/area table below figure geometry. Closure status renders in **Green** (closed) or **Red** (misclosure). Labels are Yellow (ACI 2).

### 9.6 JEA Validation Live Badge
**Access:** `Validation` menu -> `JEA As-Built Validation`
Validation runs automatically on window open and pushes the total issue count and error count to the main toolbar badge in real time.

---

## 10. DXF Layer Color Reference (ACI Standard)

| Layer | Discipline | ACI | Color |
|---|---|---|---|
| STRUCT_WW_* | Sanitary Sewer | 3 | Green |
| STRUCT_W_* | Water | 5 | Blue |
| STRUCT_R_* | Reclaimed Water | 6 | Magenta |
| STRUCT_ST_* | Storm Drain | 4 | Cyan |
| STRUCT_E_* | Electric | 1 | Red |
| STRUCT_G_* | Gas | 30 | Orange |
| STRUCT_CHIL_* | Chilled Water | 141 | Light Blue* |
| XS_EG | Cross-Section EG | 5 | Blue |
| XS_FG | Cross-Section FG | 2 | Yellow |
| XS_CUT | Cross-Section Cut | 1 | Red |
| XS_FILL | Cross-Section Fill | 3 | Green |
| XS_GRID | Cross-Section Grid | 8 | Dark Gray |
| XS_BORDER / XS_LABEL | Cross-Section Frame | 7 | White/Black |
| FIGURE_CLOSURE_TABLE | Closure Report | 2/7/3/1 | Yellow/White/Green/Red |
| FIGURES / POINTS | Survey Geometry | 256 | ByLayer |

*ACI 141 may appear faint on white paper-space backgrounds.