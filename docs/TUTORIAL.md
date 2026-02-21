# Learn COGO Scripting - A Developer's Tutorial

Welcome to the **RCS COGO Scripting Engine Developer's Tutorial**.
By completing these lessons, you will learn how to initialize point targets, route observation models, and verify math closure parameters. Let's begin building some virtual topology!

---

## Lesson 1: A Fundamental Plot (Basic Shapes)
Every operation starts with a coordinate location. The `NE` command dictates fixed points. Let's define the corners of a 100x100 square. Open a new script file and type:

```text
// Reset the operational database before starting
RESET

// Draw the corners of the plot
NE 1 5000.000 5000.000 "SW_CORNER"
NE 2 5100.000 5000.000 "NW_CORNER"
NE 3 5100.000 5100.000 "NE_CORNER"
NE 4 5000.000 5100.000 "SE_CORNER"

ECHO "Plot Corners Defined"
```

To visibly link those points into a layer line:

```text
// Begin drawing a new shape boundary
BEG PLOT_BOUNDARY

// Order matters here!
FIG 1
FIG 2
FIG 3
FIG 4
FIG 1 

// Conclude the drawing boundary layer
END

// Validate the geometry
MAPCHK
```
*Tip:* Run your script. The log will output a `Perfect Closure` MAPCHK report, confirming all sides mathematically close identically out of the loop!

---

## Lesson 2: Traverse Shooting
Real surveying involves a station tripod turning angles at targets, calculating the distances into new coordinates. The `TRAV` command tracks this movement chaining.

```text
RESET
ECHO "--- Initiating Line Traverse ---"

// Establish our baseline known reference target
NE 100 0.000 0.000 "Base_Origin"

// Set up the equipment tripod at Point 100
STN 100

// Aim the lens North
BS 0.0000

// Activate automatic station moving
TRAV ON

// Turn 90 degress CW, distance 50 ft. 
AD 101 90.0000 50.0 "First Shot"

// Because TRAV ON is active: 
// Our station just moved to Point 101.
// Our instrument is aimed back at Point 100!
// So, turn 180 degrees (direct ahead), distance 75 ft.
AD 102 180.0000 75.0 "Second Shot"

ECHO "--- Final Points ---"
LIST PTS
```

---

## Lesson 3: System Logging and List Summaries
As your projects scale into thousands of targets and intricate pipeline figures, tracking specific IDs in log files becomes tedious.
Here are effective macro scripts to handle noise filtering:

```text
// Dump hundreds of temporary computational intersection targets silently
LOG OFF
RKRK 1 25.0 2 30.0 RKRK_Temporary_Target   
CURVE 1 2 50.0
OFFSETLINE 1 2 50.0

// Computation complete. Turn logs back on.
LOG ON

// Let's print out what actually matters
ECHO "Execution Completed."
SHOW RKRK_Temporary_Target_L 
LIST FIGS
REPORT STATUS
```

With `LOG OFF`, you essentially suppress the detailed mathematical verification dumps, generating extremely clean textual output. And `LIST` operations bypass the suppressed state to summarize the raw counts for diagnostics!

---
## Lesson 4: Smooth Curve Visualization
Generating arc lines off a central tangent vertex employs the `XC` command line.

```text
NE 5 100 100 "Start"
BEG TANGENT_ARC
FIG 5 // Trace starts here

// Sweep 12 simulated curve fragments right, along with a specified Azimuth chord limit
XC ZD (BULB) 150.0 45.0000 50.0

// Now sweep them back left! (S-Curve)
XC ZD (BULB) -150.0 90.0000 50.0

END
CLOSURE
```
The engine parses the `(BULB)` macro to render `XC_*` synthesized targets behind the scenes—building visual arc sweeps dynamically without overwriting your standard numerical targets (`#5` etc.). You don't have to define a circle polygon manually; the macro algorithm handles it automatically!
