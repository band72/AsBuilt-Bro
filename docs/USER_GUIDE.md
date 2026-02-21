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
