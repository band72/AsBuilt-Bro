# Pipe Script Manual Edits - TODO List

Here is a TODO list of recommended manual edits you should perform on the `water_sewer.pipe` script to finalize your network model:

### 1. Update Point Elevations (Z-Coordinates)
* **Context:** In the `NEZ` commands at the top of the file, all elevations were generated as `0.000` because the DXF only provided 2D layer geometry.
* **Action:** Replace the `0.000` values with the actual surface or invert elevations from your design plans or survey data.

### 2. Verify Pipe Diameters and Materials
* **Context:** The script creates default pipe groups using `PRUN START W DIAM 8 MAT PVC` and `PRUN START WW DIAM 8 MAT PVC`.
* **Action:** 
  * Change `8` to the actual diameter required for each specific utility run.
  * Change `PVC` to the correct material abbreviation recognized by your master database (e.g., `DIP`, `HDPE`). 
  * *Tip: If different runs require different materials or sizes, split the lists into separate `PRUN START ... PRUN END` blocks.*

### 3. Specify Correct Structure Types
* **Context:** The geometry parser assigned generic structure types based on the DXF layer: Water circles/inserts became `SS-C {id} Valve`, and Sewer circles/inserts became `SS-C {id} Manhole`.
* **Action:** Review the `SS-C` commands. Depending on the actual network, you may need to rename these descriptors to specific types defined in your application, such as `Hydrant`, `Meter`, `Cleanout`, `DropManhole`, or `LiftStation`.

### 4. Concatenate Continuous Pipe Runs
* **Context:** Because CAD files often draw lines segment-by-segment, you will see many short, disconnected runs in the script (e.g., a `W-B` begin, right into a `W-C` continue, immediately followed by `W-E` end).
* **Action:** Trace the connectivity. If multiple pairs represent one continuous sequential pipeline, combine them into a single run by chaining multiple `W-C` (Continue) commands before issuing the final `W-E` (End). This makes your network model much cleaner.

### 5. Check Flow Direction and Slopes (Important for Sewer)
* **Context:** For gravity lines (WW), the order of the connection points dictates the flow direction. Furthermore, your `PipeScriptCompiler.cs` actively checks for minimum slope violations (< 0.40%).
* **Action:** Ensure your `WW-B` to `WW-C` segment order matches the correct flow direction. When you assign elevations (either point elevations or explicit invert numeric tokens), insure you satisfy the 0.40% minimum slope requirement or the engine will throw an error upon parsing.

### 6. Verify Utility Codes
* **Context:** The script assumes the utility type codes are strictly `W` (Water) and `WW` (Wastewater/Sewer).
* **Action:** Ensure that `W` and `WW` are the exact codes referenced in your GUI's Validation Lists / Master Material Database so that the parts match to existing definitions correctly.
