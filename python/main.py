import sys
import os
from rcs_cogo.engine import CogoEngine
from rcs_cogo.validation import JeaValidationService
from rcs_cogo.dxf_writer import ProfessionalDxfWriter
from rcs_cogo.geometry import GeometryEngine

def get_pipe_layer_and_color(run_type: str) -> tuple[str, int]:
    r = run_type.upper()
    if r in ("WATER", "POTABLE", "W"):
        return "W-MAIN", 5 # Blue
    elif r in ("WASTEWATER", "SEWER", "WW"):
        return "WW-MAIN", 3 # Green
    elif r in ("FORCE MAIN", "FM"):
        return "WW-FORCE-MAIN", 3 # Green
    elif r in ("RECLAIM", "R"):
        return "RCL-MAIN", 6 # Magenta
    elif r in ("STORM", "ST", "D", "DRAINAGE"):
        return "ST-MAIN", 6 # Magenta
    elif r in ("ELECTRIC", "E", "ELEC"):
        return "E-CONDUIT", 2 # Yellow
    elif r in ("GAS", "G"):
        return "G-MAIN", 1 # Red
    elif r in ("TELECOM", "TEL"):
        return "TEL-MAIN", 4 # Cyan
    else:
        return "AS-BUILT-PIPES", 7 # White/Black

def get_struct_layer_and_block(struct_type: str) -> tuple[str, str]:
    t = struct_type.upper()
    if "MANHOLE" in t or "MH" in t or t == "WW":
        return "WW-MH", "MANHOLE"
    elif "VALVE" in t or "VLV" in t or t == "W":
        return "W-VALVE", "VALVE"
    elif "HYDRANT" in t or "FH" in t:
        return "W-HYDRANT", "HYDRANT"
    elif "JUNCTION" in t or "JB" in t or t == "ST" or t == "D":
        return "ST-MH", "BOX"
    elif "VAULT" in t or "VLT" in t or t == "E":
        return "E-VAULT", "BOX"
    elif "METER" in t:
        return "W-METER", "METER"
    else:
        return "AS-BUILT-STRUCTURES", "MANHOLE"

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 main.py <script_file.cogo> [output_file.dxf]")
        return

    script_path = sys.argv[1]
    dxf_path = sys.argv[2] if len(sys.argv) >= 3 else "output.dxf"

    if not os.path.exists(script_path):
        print(f"Error: Script file '{script_path}' not found.")
        return

    print(f"Reading COGO script: {script_path}")
    with open(script_path, "r") as f:
        script_text = f.read()

    # 1. Initialize and run COGO Engine
    engine = CogoEngine()
    engine.execute_batch(script_text)

    # 2. Output Execution Logs
    print("\n--- COGO EXECUTION LOGS ---")
    for log in engine.logs:
        print(log)
    print("---------------------------\n")

    # 3. Perform JEA Validation Checks on generated points and pipe runs
    print("--- JEA AS-BUILT VALIDATION CHECKS ---")
    validation_issues = []
    
    # Coordinates check
    for pt_id, (pt, desc) in engine.points.items():
        from rcs_cogo.projection import StatePlaneProjection
        lat, lon = None, None
        if pt.easting != 0 and pt.northing != 0:
            try:
                lat, lon = StatePlaneProjection.to_lat_lon(pt.easting, pt.northing, "EPSG:2236")
            except Exception:
                pass
        issues = JeaValidationService.check_coords("Points Table", pt_id, pt.easting, pt.northing, lat, lon)
        validation_issues.extend(issues)

    # Piping slope and catalog check
    for idx, run in enumerate(engine.pipe_runs):
        issues = JeaValidationService.validate_pipe(
            asset_id=f"{run.from_point_id}->{run.to_point_id}",
            run_type=run.type,
            diameter=run.diameter,
            material=run.material,
            slope=run.slope
        )
        validation_issues.extend(issues)

    if not validation_issues:
        print("All points and pipe segments passed JEA QC criteria!")
    else:
        for issue in validation_issues:
            print(issue)
    print("--------------------------------------\n")

    # 4. Generate DXF Drawing Output
    print(f"Generating DXF Drawing -> {dxf_path}")
    dxf = ProfessionalDxfWriter()

    # Draw all stored points
    for pt_id, (pt, desc) in engine.points.items():
        dxf.add_point(pt, "POINTS")
        dxf.add_text(f"{pt_id}: {desc}", pt.easting + 1.0, pt.northing + 1.0, 0.8, "POINTS_LABELS")

    # Draw all boundary figures
    for fig_name, figure in engine.figures.items():
        pts = []
        for pid in figure.point_ids:
            pt = engine.get_point(pid)
            if pt:
                pts.append((pt.easting, pt.northing))
        dxf.add_polyline(pts, "FIGURES", 7) # Default boundary white/black
        for label in figure.labels:
            dxf.add_text(label.text, label.easting, label.northing, 0.8, "FIGURES_LABELS", label.rotation, 7)

    # Draw all pipe runs
    for run in engine.pipe_runs:
        p1 = engine.get_point(run.from_point_id)
        p2 = engine.get_point(run.to_point_id)
        if p1 and p2:
            layer, color = get_pipe_layer_and_color(run.type)
            dxf.add_line(p1, p2, layer, color)
            # Label invert and diameter
            mid_x = (p1.easting + p2.easting) / 2.0
            mid_y = (p1.northing + p2.northing) / 2.0
            _, az = GeometryEngine.inverse(p1, p2)
            rot = -az.degrees + 90.0
            if rot < -90: rot += 180.0
            if rot > 90: rot -= 180.0
            label = f"{run.diameter:.0f}\" {run.material} (S={run.slope:.2f}%)"
            dxf.add_text(label, mid_x, mid_y + 1.0, 0.6, f"{layer}_LABELS", rot, color)

    # Insert all structures
    for struct in engine.pipe_structures:
        pt = engine.get_point(struct.point_id)
        if pt:
            layer, block = get_struct_layer_and_block(struct.type)
            # Find the color for layer
            _, color = get_pipe_layer_and_color(struct.type)
            dxf.insert_block(block, pt.easting, pt.northing, 1.5, layer, color)

    dxf.save(dxf_path)
    print("DXF generation complete.")

if __name__ == "__main__":
    main()
