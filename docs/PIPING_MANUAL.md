# RCS Piping Engine - User Manual

Welcome to the RCS Piping Script Engine guide! The pipeline generation engine operates strictly off recognized JEA Master Database values to ensure compliant, enterprise-grade water and sewer network modeling.

## 1. Engine Toggles

The pipeline scripts are designed to coexist alongside baseline Coordinate Geometry (COGO) scripts using explicit toggles:

*   `pipe-engine-on` - Engages the pipeline compiler to read commands.
*   `pipe-engine-off` - Disables the pipeline compiler for the following lines.
*   `cogo-engine-on` / `cogo-engine-off` - Toggles the COGO engine respectively.

When laying out pipelines, surround your `PRUN` blocks with `pipe-engine-on` to ensure the COGO engine ignores the specialized syntax!

---

## 2. Pipe Runs (`PRUN`)

All pipelines must be declared inside a `PRUN...END` block. This block defines the active Utility, Diameter, Material, and tracking Feature.

### Syntax
`PRUN START <UtilityCode> DIAM <Size> MAT <MaterialName> FIG <FeatureName>`
...
`PRUN END`

### Valid Utility Codes
*   `W` - Water (Potable)
*   `WW` - Wastewater / Sanitary Sewer 
*   `S` - Storm Drainage
*   `R` - Reclaimed Water

### Strict Material Validation
The engine will validate the `MAT` argument against the Master Database. Only recognized strings (e.g. `DIP`, `PVC`, `RCP`, `Iron`, `Brass`, `Conc`, `SS`, `ERCP`) are valid. Providing an unrecognizable string will halt execution and generate an `[ERROR]`.

---

## 3. Connecting Pipe Vertices

Inside an active `PRUN` block, you tell the engine exactly how to trace the pipe from Point ID to Point ID by prepending the active Utility Code (e.g. `W-`, `WW-`).

*   `<Utility>-B <PtId>` (Begin)
    *   *Example:* `W-B 10` Starts the water main at Point 10.
*   `<Utility>-C <PtId>` (Continue)
    *   *Example:* `W-C 11` Draws a generic, straight segment connecting the previous node to Point 11.
*   `<Utility>-CLS` (Close)
    *   Forms a geometric closed loop out of the current path back to the Beginning Point automatically. 
*   `<Utility>-E` (End)
    *   Disengages the active trace tracking mechanism explicitly.

---

## 4. Attaching Structural Features (`SS-C`)

To install explicit objects onto your pipeline networks (Manholes, Valves, etc.), use the **Store Structure - Code** command.

### Syntax
`SS-C <PointId> <FeatureType>`

### Strict FeatureType Validation
Similar to materials, specifying a FeatureType that is not tracked in the Master catalog will emit a warning flag. Use appropriate types such as:
*   `Manhole`
*   `Valve`
*   `Hydrant`
*   `Cleanout`
*   `Meter`
*   `Service`
*   `Inlet`
*   `Fitting`
*   `Pipe`
*   `ARV`
*   `Backflow`

*Example:* `SS-C 5 Manhole` converts Node 5 tracking info into an official Manhole structural mapping.

---
