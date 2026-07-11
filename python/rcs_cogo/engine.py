import math
import uuid
import re
from typing import Dict, List, Tuple, Optional
from .primitives import Angle, Point3D
from .geometry import GeometryEngine
from .validation import JeaValidationService, JeaIssue


class FigureLabel:
    def __init__(self, text: str, easting: float, northing: float, rotation: float):
        self.text = text
        self.easting = easting
        self.northing = northing
        self.rotation = rotation


class Figure:
    def __init__(self, name: str):
        self.name = name
        self.point_ids: List[str] = []
        self.labels: List[FigureLabel] = []
        self.map_check_failed = False
        self.qc_status = "NotRun" # Passed, Failed
        self.closure_error = 0.0
        self.closure_bearing = 0.0
        self.area_sqft = 0.0
        self.acres = 0.0
        self.perimeter = 0.0
        self.precision_ratio = None


class PipeRun:
    def __init__(self, run_id: str, run_type: str, figure_name: str, from_pt: str, to_pt: str, diameter: float, material: str, invert_start: float, invert_end: float, slope: float, line_no: int):
        self.id = run_id
        self.type = run_type # W, WW, ST, GAS, ELEC
        self.figure_name = figure_name
        self.from_point_id = from_pt
        self.to_point_id = to_pt
        self.diameter = diameter
        self.material = material
        self.invert_start = invert_start
        self.invert_end = invert_end
        self.slope = slope
        self.part_key = f"Pipe-{run_type}-{diameter}"
        self.line_number = line_no


class PipeStructure:
    def __init__(self, struct_id: str, point_id: str, struct_type: str, line_no: int):
        self.id = struct_id
        self.point_id = point_id
        self.type = struct_type # MH, VALVE, HYDRANT, JUNCTION, VAULT
        self.line_number = line_no


class PrunContext:
    def __init__(self, util_type: str, diameter: Optional[float], material: Optional[str], feature: str):
        self.utility_type = util_type
        self.diameter = diameter
        self.material = material
        self.feature = feature
        self.is_figure_active = False
        self.start_vertex: Optional[str] = None
        self.vertices: List[str] = []


class CogoEngine:
    """
    Core COGO execution engine. Parses scripts and manages points, figure states,
    and survey instrument configurations.
    Equivalent to RCS.Cogo.App.Scripting.ScriptEngine and RCS.Piping.Core.Scripting.PipeScriptCompiler.
    """
    def __init__(self):
        self.points: Dict[str, Tuple[Point3D, str]] = {} # point_id -> (Point3D, description)
        self.figures: Dict[str, Figure] = {}
        self.pipe_runs: List[PipeRun] = []
        self.pipe_structures: List[PipeStructure] = []
        
        self.active_figure: Optional[Figure] = None
        self.current_station: Optional[Point3D] = None
        self.current_station_id: Optional[str] = None
        self.current_backsight: Optional[Point3D] = None
        self.current_backsight_id: Optional[str] = None
        
        self.traverse_mode = False
        self.logs: List[str] = []
        self.map_check_closure_tolerance = 0.02 # feet
        self.auto_point = False
        self._next_point_id_counter = 1
        self.last_intersections: Tuple[Optional[Point3D], Optional[Point3D]] = (None, None)
        
        # Piping context state
        self.prun_context: Optional[PrunContext] = None
        self.pipe_engine_off = False
        self._line_number_counter = 0

    def log(self, message: str):
        self.logs.append(message)

    def get_point(self, point_id: str) -> Optional[Point3D]:
        val = self.points.get(point_id)
        return val[0] if val else None

    def add_point(self, point_id: str, point: Point3D, description: str = ""):
        self.points[point_id] = (point, description)

    def get_figure(self, name: str) -> Optional[Figure]:
        return self.figures.get(name)

    def add_figure(self, figure: Figure):
        self.figures[figure.name] = figure

    def get_next_point_id(self) -> int:
        while str(self._next_point_id_counter) in self.points:
            self._next_point_id_counter += 1
        return self._next_point_id_counter

    def add_structure(self, pt_id: str, struct_type: str, line_no: int):
        existing = next((s for s in self.pipe_structures if s.point_id == pt_id), None)
        if not existing:
            s_id = str(uuid.uuid4())
            self.pipe_structures.append(PipeStructure(s_id, pt_id, struct_type, line_no))
        else:
            if struct_type and len(struct_type) > 2:
                existing.type = struct_type

    def execute_line(self, line: str):
        self._line_number_counter += 1
        line_no = self._line_number_counter

        # Strip inline comments
        line = self._strip_inline_comment(line)
        if not line:
            return

        trimmed = line.lstrip()
        if not trimmed:
            return

        # Skip whole-line comments
        if any(trimmed.startswith(comment) for comment in ["!", "//", "/", ";", "--", "#"]):
            return

        args = self._tokenize(line)
        if not args:
            return

        cmd = args[0].upper()

        # Engine toggles
        if cmd == "PIPE-ENGINE-OFF":
            self.pipe_engine_off = True
            self.log("Pipe Engine Scripting PAUSED.")
            return
        if cmd == "PIPE-ENGINE-ON":
            self.pipe_engine_off = False
            self.log("Pipe Engine Scripting RESUMED.")
            return

        # Handle implicit point/cont numeric-first lines
        try:
            float(cmd)
            is_numeric = True
        except ValueError:
            is_numeric = False

        if is_numeric:
            if self.prun_context is not None and not self.pipe_engine_off:
                # Handle numeric pipe segment in active PRUN context
                self._handle_prun_segment(args, line_no)
                return
            else:
                if len(args) >= 3:
                    # Implicit PT: PT ID N E Z DESC
                    args = ["PT"] + args
                    cmd = "PT"
                else:
                    # Implicit CONT
                    args = ["CONT"] + args
                    cmd = "CONT"

        # Check for Feature-Directive commands (e.g. W-B, WW-C)
        if "-" in args[0] and not self.pipe_engine_off:
            parts = args[0].split("-")
            if len(parts) == 2:
                feature, directive = parts[0], parts[1].upper()
                if self.prun_context is not None:
                    self._handle_feature_directive(feature, directive, args, line_no)
                    return

        self._run_command(cmd, args, line_no)

    def execute_batch(self, script_text: str):
        lines = script_text.splitlines()
        for line in lines:
            self.execute_line(line)
            
        # Warning if PRUN context is still open
        if self.prun_context is not None:
            self.log("[WARN] PRUN context was not closed with 'PRUN END'.")

    def _strip_inline_comment(self, line: str) -> str:
        in_quotes = False
        for i in range(len(line)):
            c = line[i]
            if c == '"':
                in_quotes = not in_quotes
                continue
            if in_quotes:
                continue
            # Double slash or semicolon comments
            if c == ';' or (c == '/' and i + 1 < len(line) and line[i+1] == '/'):
                return line[:i].rstrip()
        return line

    def _tokenize(self, input_str: str) -> List[str]:
        tokens = []
        sb = []
        in_quotes = False
        for c in input_str:
            if c == '"':
                in_quotes = not in_quotes
                continue
            if c.isspace() and not in_quotes:
                if sb:
                    tokens.append("".join(sb))
                    sb = []
            else:
                sb.append(c)
        if sb:
            tokens.append("".join(sb))
        return tokens

    def _handle_prun_segment(self, tokens: List[str], line_no: int):
        if not self.prun_context:
            return
        if len(tokens) < 2:
            self.log(f"Error: Pipe segment on line {line_no} requires at least From and To points.")
            return

        from_pt = tokens[0]
        to_pt = tokens[1]

        p1 = self.get_point(from_pt)
        p2 = self.get_point(to_pt)

        if not p1: self.log(f"Error: Point {from_pt} not found on line {line_no}.")
        if not p2: self.log(f"Error: Point {to_pt} not found on line {line_no}.")

        inv_start = float(tokens[2]) if len(tokens) > 2 and self._is_float(tokens[2]) else (p1.elevation if p1 else 0.0)
        inv_end = float(tokens[3]) if len(tokens) > 3 and self._is_float(tokens[3]) else (p2.elevation if p2 else 0.0)

        # Slope calculation
        slope = 0.0
        if p1 and p2:
            dx = p2.easting - p1.easting
            dy = p2.northing - p1.northing
            length = math.sqrt(dx * dx + dy * dy)
            if length > 0:
                drop = inv_start - inv_end
                slope = (drop / length) * 100.0
                
                # Minimum gravity slope warn
                util_upper = self.prun_context.utility_type.upper()
                if util_upper in ("WW", "SEWER", "ST", "STORM", "D", "DRAINAGE") and slope < 0.40:
                    self.log(f"Slope warning: Gravity Pipe {from_pt}-{to_pt} is {slope:.2f}%. Standard minimum allowable slope is 0.40%.")

        run_id = str(uuid.uuid4())
        run = PipeRun(
            run_id=run_id,
            run_type=self.prun_context.utility_type,
            figure_name=self.prun_context.feature,
            from_pt=from_pt,
            to_pt=to_pt,
            diameter=self.prun_context.diameter or 0.0,
            material=self.prun_context.material or "",
            invert_start=inv_start,
            invert_end=inv_end,
            slope=slope,
            line_no=line_no
        )
        self.pipe_runs.append(run)
        
        # Auto-create generic structures
        self.add_structure(from_pt, self.prun_context.utility_type, line_no)
        self.add_structure(to_pt, self.prun_context.utility_type, line_no)
        self.log(f"Added Pipe {from_pt}-{to_pt} on line {line_no}.")

    def _handle_feature_directive(self, feature: str, directive: str, tokens: List[str], line_no: int):
        prun = self.prun_context
        if not prun:
            return

        # Check feature mismatch
        if prun.feature.upper() != feature.upper() and prun.utility_type.upper() != feature.upper():
            self.log(f"Feature '{feature}' mismatch. Expected '{prun.feature}' or '{prun.utility_type}'.")
            return

        if directive == "B":
            if len(tokens) < 2:
                self.log(f"Error: {feature}-B requires a point number/ID.")
                return
            pt_id = tokens[1]
            if not self.get_point(pt_id):
                self.log(f"Error: Point {pt_id} not found for {feature}-B.")
                return

            prun.vertices = [pt_id]
            prun.start_vertex = pt_id
            prun.is_figure_active = True
            
            # Start structure
            self.add_structure(pt_id, prun.utility_type, line_no)
            self.log(f"{feature} begin at point {pt_id}.")

        elif directive == "C":
            if not prun.is_figure_active:
                self.log(f"Error: {feature}-C used before {feature}-B.")
                return
            if len(tokens) < 2:
                self.log(f"Error: {feature}-C requires a point number/ID.")
                return
            pt_id = tokens[1]
            if not self.get_point(pt_id):
                self.log(f"Error: Point {pt_id} not found for {feature}-C.")
                return

            prev = prun.vertices[-1]
            explicit_type = prun.utility_type
            if len(tokens) > 2:
                explicit_type = tokens[2]

            if prev == pt_id:
                # Update structure details on the same node
                self.add_structure(pt_id, explicit_type, line_no)
                return

            prun.vertices.append(pt_id)
            
            # Add PipeRun
            p1 = self.get_point(prev)
            p2 = self.get_point(pt_id)
            slope = 0.0
            if p1 and p2:
                dx = p2.easting - p1.easting
                dy = p2.northing - p1.northing
                length = math.sqrt(dx * dx + dy * dy)
                if length > 0:
                    slope = ((p1.elevation - p2.elevation) / length) * 100.0

            run_id = str(uuid.uuid4())
            run = PipeRun(
                run_id=run_id,
                run_type=prun.utility_type,
                figure_name=prun.feature,
                from_pt=prev,
                to_pt=pt_id,
                diameter=prun.diameter or 0.0,
                material=prun.material or "",
                invert_start=p1.elevation if p1 else 0.0,
                invert_end=p2.elevation if p2 else 0.0,
                slope=slope,
                line_no=line_no
            )
            self.pipe_runs.append(run)
            
            # Add structure on endpoints
            self.add_structure(pt_id, explicit_type, line_no)

        elif directive == "CLS":
            if not prun.is_figure_active:
                self.log(f"Error: {feature}-CLS used before {feature}-B.")
                return
            if len(prun.vertices) < 2 or not prun.start_vertex:
                return

            last = prun.vertices[-1]
            first = prun.start_vertex
            if last != first:
                p1 = self.get_point(last)
                p2 = self.get_point(first)
                slope = 0.0
                if p1 and p2:
                    dx = p2.easting - p1.easting
                    dy = p2.northing - p1.northing
                    length = math.sqrt(dx * dx + dy * dy)
                    if length > 0:
                        slope = ((p1.elevation - p2.elevation) / length) * 100.0

                run_id = str(uuid.uuid4())
                run = PipeRun(
                    run_id=run_id,
                    run_type=prun.utility_type,
                    figure_name=prun.feature,
                    from_pt=last,
                    to_pt=first,
                    diameter=prun.diameter or 0.0,
                    material=prun.material or "",
                    invert_start=p1.elevation if p1 else 0.0,
                    invert_end=p2.elevation if p2 else 0.0,
                    slope=slope,
                    line_no=line_no
                )
                self.pipe_runs.append(run)
            
            self.log(f"{feature} closed.")

        elif directive == "E":
            prun.is_figure_active = False
            self.log(f"{feature} ended.")

    def _run_command(self, cmd: str, args: List[str], line_no: int):
        try:
            if cmd == "CLEAR":
                self.points.clear()
                self.figures.clear()
                self.pipe_runs.clear()
                self.pipe_structures.clear()
                self.active_figure = None
                self.current_station = None
                self.current_station_id = None
                self.current_backsight = None
                self.current_backsight_id = None
                self.traverse_mode = False
                self.prun_context = None
                self.log("Workspace cleared.")

            elif cmd in ("PT", "NEZ", "POINT"):
                # PT <id> <N> <E> [Z] [Desc] or if AP is enabled PT <N> <E> [Z] [Desc]
                if not self.auto_point and len(args) < 4:
                    self.log(f"Error: {cmd} usage: {cmd} <id> <N> <E> [Z] [Desc] or enable AP to omit <id>")
                    return
                
                if self.auto_point:
                    pt_id = str(self.get_next_point_id())
                    start_idx = 1
                else:
                    pt_id = args[1]
                    start_idx = 2

                # Parse the remaining arguments for numbers and description
                numbers = []
                desc_parts = []
                for i in range(start_idx, len(args)):
                    val_str = args[i]
                    if self._is_float(val_str):
                        numbers.append(float(val_str))
                    else:
                        desc_parts.append(val_str.strip('"'))
                
                if len(numbers) < 2:
                    self.log("Error: Invalid coordinates. Need N, E, [Z].")
                    return
                
                n = numbers[0]
                e = numbers[1]
                z = numbers[2] if len(numbers) >= 3 else 0.0
                desc = " ".join(desc_parts)

                pt = Point3D(n, e, z)
                self.add_point(pt_id, pt, desc)
                self.log(f"Point {pt_id} created at {pt}")
                
                # Auto set station if none exists
                if self.current_station is None:
                    self.current_station = pt
                    self.current_station_id = pt_id
                    self.log(f"Auto-set Station to {pt_id}")

            elif cmd in ("STN", "OC"):
                # STN <PointId> [N E Z] [Desc]
                if len(args) < 2:
                    self.log("Error: STN requires a Point ID")
                    return
                pt_id = args[1]
                
                # Inline definition
                if len(args) >= 5 and self._is_float(args[2]) and self._is_float(args[3]):
                    try:
                        n = float(args[2])
                        e = float(args[3])
                        z = float(args[4]) if self._is_float(args[4]) else 0.0
                        desc_idx = 5 if self._is_float(args[4]) else 4
                        desc = " ".join(args[desc_idx:]) if len(args) > desc_idx else ""
                        pt = Point3D(n, e, z)
                        self.add_point(pt_id, pt, desc)
                        self.current_station = pt
                        self.current_station_id = pt_id
                        self.log(f"Stored and Occupied Point {pt_id}: {pt} ({desc})")
                        return
                    except ValueError:
                        pass
                
                pt = self.get_point(pt_id)
                if pt:
                    self.current_station = pt
                    self.current_station_id = pt_id
                    self.log(f"Station occupied: Point {pt_id}")
                else:
                    self.log(f"Error: Occupy point {pt_id} not found.")

            elif cmd == "BS":
                # BS <PointId> [Azimuth_or_Bearing]
                if len(args) < 2:
                    self.log("Error: BS requires backsight Point ID or Azimuth.")
                    return
                bs_id = args[1]
                
                # Check if second arg is explicit azimuth
                if len(args) >= 3 and self._is_float(args[2]):
                    try:
                        az_val = float(args[2])
                        self.current_backsight_id = bs_id
                        self.current_backsight = GeometryEngine.forward(self.current_station, Angle.from_dms(az_val), 100.0)
                        self.log(f"Backsight set to {bs_id} with explicit direction: {az_val:.4f}")
                        return
                    except ValueError:
                        pass

                pt = self.get_point(bs_id)
                if pt:
                    self.current_backsight = pt
                    self.current_backsight_id = bs_id
                    if self.current_station:
                        dist, az = GeometryEngine.inverse(self.current_station, pt)
                        self.log(f"Backsight set to Point {bs_id} (Calculated Azimuth: {az.to_dms():.4f})")
                    else:
                        self.log(f"Backsight set to Point {bs_id} (Warning: Station not occupied yet)")
                else:
                    self.log(f"Error: Backsight point {bs_id} not found.")

            elif cmd == "BD":
                # BD <NewPt> <Quad 1-4> <Bearing DMS> <Distance> [Desc]
                if len(args) < 5:
                    self.log("Error: Usage: BD <NewPt> <Quad 1-4> <Bearing DMS> <Distance> [Desc]")
                    return
                if not self.current_station:
                    self.log("Error: Station must be occupied.")
                    return
                
                pt_id = args[1]
                try:
                    quad = int(args[2])
                    brg = float(args[3])
                    dist = float(args[4])
                    desc = " ".join(args[5:]) if len(args) > 5 else ""
                except ValueError:
                    self.log("Error: Invalid numbers in BD command.")
                    return

                az = Angle.from_quadrant(quad, brg)
                new_pt = GeometryEngine.forward(self.current_station, az, dist)
                self.add_point(pt_id, new_pt, desc)
                self.log(f"Point {pt_id} created by BD: {new_pt} (Brg: Q{quad}-{brg:.4f}, Dist: {dist:.2f})")

                if self.traverse_mode:
                    self.current_backsight = self.current_station
                    self.current_backsight_id = self.current_station_id
                    self.current_station = new_pt
                    self.current_station_id = pt_id
                    self.log(f"Auto-Traverse to {pt_id}")

            elif cmd in ("FS", "AD"):
                # FS <Pt> <Angle> <Dist> [Desc]
                if len(args) < 4:
                    self.log(f"Error: Usage: {cmd} <Pt> <Angle DMS> <Dist> [Desc]")
                    return
                if not self.current_station:
                    self.log("Error: Station setup required.")
                    return
                if not self.current_backsight:
                    self.log("Error: Backsight required for FS/AD.")
                    return

                pt_id = args[1]
                try:
                    ang_val = float(args[2])
                    dist = float(args[3])
                    desc = " ".join(args[4:]) if len(args) > 4 else ""
                except ValueError:
                    self.log("Error: Invalid numbers in FS/AD command.")
                    return

                # Calculate Backsight Azimuth
                _, bs_az = GeometryEngine.inverse(self.current_station, self.current_backsight)
                turn_ang = Angle.from_dms(ang_val)
                final_az = bs_az + turn_ang
                new_pt = GeometryEngine.forward(self.current_station, final_az, dist)

                self.add_point(pt_id, new_pt, desc)
                self.log(f"Point {pt_id} created by {cmd}: {new_pt} (Angle Right: {ang_val:.4f}, Dist: {dist:.2f})")

                # If traverse or command is TRAV (which forces traverse mode)
                if self.traverse_mode or cmd == "TRAV":
                    self.current_backsight = self.current_station
                    self.current_backsight_id = self.current_station_id
                    self.current_station = new_pt
                    self.current_station_id = pt_id
                    self.log(f"Auto-Traverse to {pt_id}")

            elif cmd == "TRAV":
                # TRAV ON/OFF or TRAV <Pt> <Angle> <Dist> [Desc]
                if len(args) < 2:
                    self.log(f"Traverse Mode is {'ON' if self.traverse_mode else 'OFF'}")
                    return
                
                sub = args[1].upper()
                if sub == "ON":
                    self.traverse_mode = True
                    self.log("Traverse Mode: ON")
                elif sub == "OFF":
                    self.traverse_mode = False
                    self.log("Traverse Mode: OFF")
                else:
                    # Execute as a single traverse step
                    old_trav = self.traverse_mode
                    self.traverse_mode = True
                    # Re-run as AD command
                    self._run_command("AD", args, line_no)
                    self.traverse_mode = old_trav

            elif cmd == "DD":
                # DD <Pt> <Defl DMS> <Dist> [Desc]
                if len(args) < 4:
                    self.log("Error: Usage: DD <Pt> <Defl DMS> <Dist> [Desc]")
                    return
                if not self.current_station or not self.current_backsight:
                    self.log("Error: Station & Backsight setup required.")
                    return

                pt_id = args[1]
                try:
                    defl_val = float(args[2])
                    dist = float(args[3])
                    desc = " ".join(args[4:]) if len(args) > 4 else ""
                except ValueError:
                    self.log("Error: Invalid numbers in DD command.")
                    return

                # Forward azimuth is from Backsight to Station
                _, fwd_az = GeometryEngine.inverse(self.current_backsight, self.current_station)
                defl_ang = Angle.from_dms(defl_val)
                final_az = fwd_az + defl_ang
                new_pt = GeometryEngine.forward(self.current_station, final_az, dist)

                self.add_point(pt_id, new_pt, desc)
                self.log(f"Point {pt_id} by Deflection created: {new_pt}")

                if self.traverse_mode:
                    self.current_backsight = self.current_station
                    self.current_backsight_id = self.current_station_id
                    self.current_station = new_pt
                    self.current_station_id = pt_id
                    self.log(f"Auto-Traverse to {pt_id}")

            elif cmd in ("BEG", "B", "START", "FIG"):
                # BEG <Name>
                if len(args) < 2:
                    self.log("Error: BEG requires a figure name.")
                    return
                fig_name = args[1]
                if len(args) >= 3 and fig_name.upper() == "FIG":
                    fig_name = args[2]

                base_name = fig_name
                if self.get_figure(fig_name) is not None:
                    seg = 2
                    while self.get_figure(f"{base_name}_{seg}") is not None:
                        seg += 1
                    fig_name = f"{base_name}_{seg}"
                    self.log(f"Figure '{base_name}' already exists. Started new segment '{fig_name}'.")

                fig = Figure(fig_name)
                self.add_figure(fig)
                self.active_figure = fig
                self.log(f"Figure '{fig_name}' started.")

            elif cmd in ("CONT", "L", "POINT", "PNT"):
                # CONT <Pt>
                if not self.active_figure:
                    self.log("Error: No active figure. Use BEG <Name> to start a figure.")
                    return
                if len(args) < 2:
                    self.log("Error: CONT requires Point ID.")
                    return
                
                pt_id = args[1]
                pt = self.get_point(pt_id)
                if pt is None:
                    self.log(f"[WARN] Point {pt_id} not computed yet — will resolve when point is created.")

                self.active_figure.point_ids.append(pt_id)
                self.log(f"Point {pt_id} added to figure {self.active_figure.name}.")

            elif cmd in ("CLOSE", "C"):
                if not self.active_figure:
                    self.log("Error: No active figure.")
                    return
                if self.active_figure.point_ids:
                    first = self.active_figure.point_ids[0]
                    last = self.active_figure.point_ids[-1]
                    if first != last:
                        self.active_figure.point_ids.append(first)
                        self.log(f"Point {first} added to close figure {self.active_figure.name}.")
                self._run_command("END", [], line_no)

            elif cmd == "END":
                if self.active_figure:
                    self.log(f"Figure {self.active_figure.name} ended.")
                    self.active_figure = None
                else:
                    self.log("No active figure to end.")

            elif cmd in ("INV", "INVERSE"):
                # INV <Pt1> <Pt2>
                if len(args) < 3:
                    self.log("Error: INV requires two point IDs.")
                    return
                p1_id, p2_id = args[1], args[2]
                p1, p2 = self.get_point(p1_id), self.get_point(p2_id)
                if not p1 or not p2:
                    self.log(f"Error: One or both points not found ({p1_id}, {p2_id}).")
                    return

                dist, az = GeometryEngine.inverse(p1, p2)
                brg_str = self._format_bearing(az.degrees)
                self.log(f"INV: {p1_id} -> {p2_id}: Dist: {dist:.3f} ft | Azimuth: {az.to_dms():.4f} ({brg_str})")

            elif cmd in ("AZ", "A"):
                if len(args) < 3:
                    self.log("Error: AZ requires two point IDs.")
                    return
                p1_id, p2_id = args[1], args[2]
                p1, p2 = self.get_point(p1_id), self.get_point(p2_id)
                if p1 and p2:
                    _, az = GeometryEngine.inverse(p1, p2)
                    self.log(f"AZ: {p1_id} -> {p2_id}: Azimuth: {az.to_dms():.4f} ({self._format_bearing(az.degrees)})")

            elif cmd in ("COPYPT", "COPY-PT"):
                # COPYPT <src> <dest> [desc]
                if len(args) < 3:
                    self.log("Error: COPYPT requires Src and Dest point IDs.")
                    return
                src, dest = args[1], args[2]
                pt_val = self.points.get(src)
                if pt_val:
                    desc = " ".join(args[3:]) if len(args) > 3 else pt_val[1]
                    self.add_point(dest, Point3D(pt_val[0].northing, pt_val[0].easting, pt_val[0].elevation), desc)
                    self.log(f"Point {src} copied to {dest} ({desc})")
                else:
                    self.log(f"Error: Source point {src} not found.")

            elif cmd in ("DELPT", "DEL"):
                # DELPT <PtId>
                if len(args) < 2:
                    self.log("Error: DELPT requires a point ID.")
                    return
                pt_id = args[1]
                if pt_id in self.points:
                    del self.points[pt_id]
                    self.log(f"Point {pt_id} deleted.")
                else:
                    self.log(f"Error: Point {pt_id} not found.")

            elif cmd in ("MAPCHK", "MAPCHECK"):
                # MAPCHK <FigName>
                if len(args) < 2:
                    self.log("Error: MAPCHK requires figure name.")
                    return
                fig_name = args[1]
                self._execute_mapcheck(fig_name)

            elif cmd == "XC":
                # XC <ZD/BD> <Radius> <SubArgs...>
                if len(args) < 5:
                    self.log("Error: XC requires SubCmd <Radius> <Args...>")
                    return
                
                # Check for active figure
                if not self.active_figure:
                    self.log("Error: XC requires an active figure.")
                    return
                
                last_pt_id = self.active_figure.point_ids[-1] if self.active_figure.point_ids else None
                if not last_pt_id:
                    self.log("Error: Figure has no start point.")
                    return
                
                start_pt = self.get_point(last_pt_id)
                if not start_pt:
                    self.log(f"Error: Start point {last_pt_id} not found.")
                    return

                clean_args = [a for a in args if a.lower() != "(bulb)"]
                sub_cmd = clean_args[1].upper()

                try:
                    radius = float(clean_args[2])
                    dist = float(clean_args[-1])
                except ValueError:
                    self.log("Error: Invalid radius/distance in XC command.")
                    return

                az = Angle.zero()
                target_id = None

                if sub_cmd == "BD":
                    if len(clean_args) >= 6:
                        try:
                            brg = float(clean_args[-2])
                            quad = int(clean_args[-3])
                            az = Angle.from_quadrant(quad, brg)
                            if len(clean_args) >= 7:
                                target_id = clean_args[3]
                        except ValueError:
                            self.log("Error: Invalid quadrant bearing in XC BD.")
                            return
                elif sub_cmd == "ZD":
                    if len(clean_args) >= 5:
                        try:
                            az_dms = float(clean_args[-2])
                            az = Angle.from_dms(az_dms)
                            if len(clean_args) >= 6:
                                target_id = clean_args[3]
                        except ValueError:
                            self.log("Error: Invalid azimuth in XC ZD.")
                            return
                else:
                    self.log(f"Error: XC sub-command '{sub_cmd}' not implemented.")
                    return

                forward_pt = GeometryEngine.forward(start_pt, az, dist)
                abs_r = abs(radius)
                final_pt = forward_pt

                if abs_r > 0 and abs_r >= (dist / 2.0) * 0.999:
                    if abs_r < dist / 2.0:
                        abs_r = dist / 2.0
                    
                    delta = 2.0 * math.asin(dist / (2.0 * abs_r))
                    m_pt = Point3D((start_pt.northing + forward_pt.northing) / 2.0, (start_pt.easting + forward_pt.easting) / 2.0, start_pt.elevation)
                    d_center = abs_r * math.cos(delta / 2.0)
                    dir_to_center = az.radians + (math.pi / 2.0 if radius > 0 else -math.pi / 2.0)
                    center_pt = GeometryEngine.forward(m_pt, Angle.from_radians(dir_to_center), d_center)

                    _, inv_center = GeometryEngine.inverse(center_pt, start_pt)
                    az_origin = inv_center.radians
                    sweep = delta if radius > 0 else -delta

                    segments = 12
                    for i in range(1, segments + 1):
                        fraction = float(i) / segments
                        current_az = az_origin + fraction * sweep
                        p_curve = GeometryEngine.forward(center_pt, Angle.from_radians(current_az), abs_r)

                        if i == segments:
                            p_curve = forward_pt
                            t_id = target_id if target_id else f"XC_{uuid.uuid4().hex[:6]}"
                            self.add_point(t_id, p_curve, "XC Curve Tie" if target_id else "XC Curve End")
                            self.active_figure.point_ids.append(t_id)
                            final_pt = p_curve
                        else:
                            s_id = f"XC_{uuid.uuid4().hex[:6]}"
                            self.add_point(s_id, p_curve, "XC Curve Segment")
                            self.active_figure.point_ids.append(s_id)
                    self.log(f"Assimilated {segments}-segment curve to figure.")
                else:
                    t_id = target_id if target_id else f"XC_{uuid.uuid4().hex[:6]}"
                    self.add_point(t_id, forward_pt, "XC Computed Linear End")
                    self.active_figure.point_ids.append(t_id)
                    self.log(f"Radius omitted/invalid, added straight chord line to {t_id}")

                if self.traverse_mode:
                    self.current_backsight = self.current_station
                    self.current_backsight_id = self.current_station_id
                    self.current_station = final_pt
                    self.current_station_id = target_id if target_id else None

            elif cmd == "PRUN":
                if len(args) < 2:
                    self.log("Error: PRUN missing action. Use START or END.")
                    return
                sub = args[1].upper()
                if sub == "START":
                    # PRUN START <UTIL> DIAM <diam> MAT <mat> [FIG <FEATURE>]
                    if len(args) < 6:
                        self.log("Error: PRUN START syntax: PRUN START <UTIL> DIAM <diam> MAT <material> [FIG <FEATURE>]")
                        return
                    
                    util = args[2]
                    
                    diam = 0.0
                    mat = ""
                    fig = f"{util}-L{line_no}"
                    
                    for i in range(3, len(args)):
                        if args[i].upper() == "DIAM" and i + 1 < len(args):
                            try:
                                diam = float(args[i+1])
                            except ValueError:
                                pass
                        elif args[i].upper() == "MAT" and i + 1 < len(args):
                            mat = args[i+1]
                        elif args[i].upper() == "FIG" and i + 1 < len(args):
                            fig = args[i+1]

                    self.prun_context = PrunContext(util_type=util, diameter=diam, material=mat, feature=fig)
                    self.log(f"PRUN started: {util} DIAM={diam} MAT={mat} FIG={fig}")

                elif sub == "END":
                    if self.prun_context is None:
                        self.log("Warning: PRUN END encountered but no active PRUN block exists.")
                        return
                    self.prun_context = None
                    self.log("PRUN ended.")

            elif cmd == "SS-C":
                # SS-C <PointID> <Code>
                if len(args) < 3:
                    self.log("Error: SS-C requires PointID and Code.")
                    return
                pt_id = args[1]
                code = args[2]
                if not self.get_point(pt_id):
                    self.log(f"Error: Point {pt_id} not found for SS-C.")
                    return
                self.add_structure(pt_id, code, line_no)
                self.log(f"Added/Updated Structure {code} at {pt_id}.")

            elif cmd == "AP":
                # AP ON/OFF
                if len(args) > 1:
                    self.auto_point = args[1].upper() == "ON"
                else:
                    self.auto_point = not self.auto_point
                self.log(f"AP set to {'ON' if self.auto_point else 'OFF'} (Stored)")

            elif cmd == "AZAZ":
                # AZAZ <PtNew> <Pt1> <Az1> <Pt2> <Az2> [Desc]
                if len(args) < 6:
                    self.log("Error: Usage: AZAZ <PtNew> <Pt1> <Az1> <Pt2> <Az2> [Desc]")
                    return
                new_pt = args[1]
                id1 = args[2]
                id2 = args[4]
                p1 = self.get_point(id1)
                p2 = self.get_point(id2)
                if not p1 or not p2:
                    self.log("Error: One or both reference points not found.")
                    return
                try:
                    az1_dms = float(args[3])
                    az2_dms = float(args[5])
                except ValueError:
                    self.log("Error: Invalid Azimuth format.")
                    return
                desc = args[6] if len(args) > 6 else ""
                az1 = Angle.from_dms(az1_dms)
                az2 = Angle.from_dms(az2_dms)
                result = GeometryEngine.intersection_bearing_bearing(p1, az1, p2, az2)
                if result is not None:
                    self.add_point(new_pt, result, desc)
                    self.log(f"Point {new_pt} created at intersection: {result}")
                else:
                    self.log("Error: Parallel lines, no intersection.")

            elif cmd == "BB":
                # BB <PtNew> <Pt1> <Quad1> <Brg1> <Pt2> <Quad2> <Brg2> [Desc]
                if len(args) < 8:
                    self.log("Error: Usage: BB <PtNew> <Pt1> <Q1> <B1> <Pt2> <Q2> <B2> [Desc]")
                    return
                new_pt = args[1]
                id1 = args[2]
                id2 = args[5]
                p1 = self.get_point(id1)
                p2 = self.get_point(id2)
                if not p1 or not p2:
                    self.log("Error: One or both reference points not found.")
                    return
                try:
                    q1 = int(args[3])
                    b1 = float(args[4])
                    q2 = int(args[6])
                    b2 = float(args[7])
                except ValueError:
                    self.log("Error: Invalid Quadrant/Bearing format.")
                    return
                desc = args[8] if len(args) > 8 else ""
                try:
                    az1 = Angle.from_quadrant(q1, b1)
                    az2 = Angle.from_quadrant(q2, b2)
                    result = GeometryEngine.intersection_bearing_bearing(p1, az1, p2, az2)
                    if result is not None:
                        self.add_point(new_pt, result, desc)
                        self.log(f"Point {new_pt} created at intersection: {result}")
                    else:
                        self.log("Error: Parallel lines, no intersection.")
                except Exception as ex:
                    self.log(f"Error: {str(ex)}")

            elif cmd in ("RKRK", "ARCARC"):
                # RKRK <P1> <Dist1> <P2> <Dist2>
                if len(args) < 5:
                    self.log("Usage: RKRK <P1> <Dist1> <P2> <Dist2>")
                    return
                p1_id = args[1]
                dist1_str = args[2]
                p2_id = args[3]
                dist2_str = args[4]
                p1 = self.get_point(p1_id)
                p2 = self.get_point(p2_id)
                if not p1:
                    self.log(f"Error: Point '{p1_id}' not found.")
                    return
                if not p2:
                    self.log(f"Error: Point '{p2_id}' not found.")
                    return
                try:
                    r1 = float(dist1_str)
                    r2 = float(dist2_str)
                except ValueError:
                    self.log("Error: Invalid distance values.")
                    return
                try:
                    left, right = GeometryEngine.intersection_distance_distance(p1, r1, p2, r2)
                    if left is None or right is None:
                        self.log("Error: No intersection found (Circles do not meet, are contained, or concentric).")
                        return
                    self.last_intersections = (left, right)
                    self.log("Found two intersections.")
                    self.log(f"L: N={left.northing:.4f} E={left.easting:.4f}")
                    self.log(f"R: N={right.northing:.4f} E={right.easting:.4f}")
                    self.log("Use 'SAVE <NORTH|SOUTH|EAST|WEST> <PtNew>' to select and store a point.")
                except Exception as ex:
                    self.log(f"Error executing RKRK: {str(ex)}")

            elif cmd == "LNLN":
                # LNLN <PtNew> <P1> <P2> [Off1] <P3> <P4> [Off2] [Desc]
                if len(args) < 6:
                    self.log("Error: Usage: LNLN <PtNew> <P1> <P2> [Off1] <P3> <P4> [Off2] [Desc]")
                    return
                pt_id = args[1]
                p1_id = args[2]
                p2_id = args[3]
                
                curr_idx = 4
                off1 = 0.0
                try:
                    off1 = float(args[curr_idx])
                    curr_idx += 1
                except ValueError:
                    pass
                
                if curr_idx + 1 >= len(args):
                    self.log("Error: Missing second line points.")
                    return
                p3_id = args[curr_idx]
                p4_id = args[curr_idx+1]
                curr_idx += 2
                
                off2 = 0.0
                if curr_idx < len(args):
                    try:
                        off2 = float(args[curr_idx])
                        curr_idx += 1
                    except ValueError:
                        pass
                
                desc = args[curr_idx] if curr_idx < len(args) else ""
                p1 = self.get_point(p1_id)
                p2 = self.get_point(p2_id)
                p3 = self.get_point(p3_id)
                p4 = self.get_point(p4_id)
                
                if not p1 or not p2 or not p3 or not p4:
                    self.log("Error: One or more reference points not found.")
                    return
                
                result = GeometryEngine.intersection_line_line(p1, p2, off1, p3, p4, off2)
                if result is not None:
                    self.add_point(pt_id, result, desc)
                    self.log(f"Point {pt_id} created at intersection: {result}")
                else:
                    self.log("Error: Parallel lines, no intersection.")

            elif cmd == "SAVE":
                # SAVE <NORTH/SOUTH/EAST/WEST/Filename> [PtNew]
                if len(args) >= 2:
                    param1 = args[1].upper()
                    if param1 in ("NORTH", "SOUTH", "EAST", "WEST"):
                        if self.last_intersections[0] is None or self.last_intersections[1] is None:
                            self.log("Error: No active intersections stored. Run RKRK or other intersection command first.")
                            return
                        pt_id = str(self.get_next_point_id()) if self.auto_point else (args[2] if len(args) >= 3 else "")
                        if not pt_id:
                            self.log("Error: Usage: SAVE <Direction> <PtNew> or enable AP.")
                            return
                        left, right = self.last_intersections
                        if param1 == "NORTH":
                            selected = left if left.northing > right.northing else right
                        elif param1 == "SOUTH":
                            selected = left if left.northing < right.northing else right
                        elif param1 == "EAST":
                            selected = left if left.easting > right.easting else right
                        else: # WEST
                            selected = left if left.easting < right.easting else right
                        
                        self.add_point(pt_id, selected, f"RKRK Intersection ({param1})")
                        self.log(f"Point {pt_id} created: N:{selected.northing:.4f}, E:{selected.easting:.4f} (Saved {param1})")
                        self.last_intersections = (None, None)
                        return
                    
                    filename = args[1]
                else:
                    filename = f"AutoSave_{uuid.uuid4().hex[:8]}.cogo"
                
                if not filename.endswith(".cogo"):
                    filename += ".cogo"
                
                sb = []
                sb.append("// RCS COGO Project Save File")
                # Sort points by ID if possible
                sorted_pts = sorted(self.points.items(), key=lambda x: int(x[0]) if x[0].isdigit() else x[0])
                for pt_id, (pt, desc) in sorted_pts:
                    desc_str = f' "{desc}"' if desc else ""
                    sb.append(f"NE {pt_id} {pt.northing:.4f} {pt.easting:.4f}{desc_str}")
                
                for fig_name, fig in self.figures.items():
                    sb.append(f"BEG {fig_name}")
                    for pid in fig.point_ids:
                        sb.append(f"CONT {pid}")
                    sb.append("END")
                
                if self.current_station_id:
                    sb.append(f"STN {self.current_station_id}")
                if self.current_backsight_id:
                    sb.append(f"BS {self.current_backsight_id}")
                
                try:
                    with open(filename, "w") as f:
                        f.write("\n".join(sb) + "\n")
                    self.log(f"Project saved to {os.path.abspath(filename)}")
                except Exception as ex:
                    self.log(f"Error saving file: {str(ex)}")

            elif cmd in ("DIST", "D"):
                # DIST <Pt1> <Pt2>
                if len(args) < 3:
                    self.log("Error: DIST requires two point IDs.")
                    return
                p1_id, p2_id = args[1], args[2]
                p1 = self.get_point(p1_id)
                p2 = self.get_point(p2_id)
                if not p1 or not p2:
                    self.log("Point not found.")
                    return
                dist, _ = GeometryEngine.inverse(p1, p2)
                self.log(f"Distance {p1_id}-{p2_id}: {dist:.4f}")

            elif cmd == "ANG":
                # ANG <BackPt> <AtPt> <ForePt>
                if len(args) < 4:
                    self.log("Usage: ANG <BackPt> <AtPt> <ForePt>")
                    return
                p1_id, p2_id, p3_id = args[1], args[2], args[3]
                p1 = self.get_point(p1_id)
                p2 = self.get_point(p2_id)
                p3 = self.get_point(p3_id)
                if not p1 or not p2 or not p3:
                    self.log("Point not found.")
                    return
                
                _, inv1 = GeometryEngine.inverse(p2, p1)
                _, inv2 = GeometryEngine.inverse(p2, p3)
                
                ang_rad = inv2.radians - inv1.radians
                if ang_rad < 0:
                    ang_rad += 2.0 * math.pi
                
                angle = Angle.from_radians(ang_rad)
                self.log(f"Angle {p1_id}-{p2_id}-{p3_id}: {angle.to_dms():.4f}")

        except Exception as ex:
            self.log(f"[ERROR] command {cmd} failed: {str(ex)}")

    def _execute_mapcheck(self, fig_name: str):
        figure = self.get_figure(fig_name)
        if not figure:
            self.log(f"Error: Figure {fig_name} not found.")
            return

        if len(figure.point_ids) < 3:
            self.log("Error: Figure must have at least 3 points for MapCheck.")
            return

        points_list = []
        for pid in figure.point_ids:
            pt = self.get_point(pid)
            if not pt:
                self.log(f"Error: Point {pid} in figure {fig_name} not found.")
                return
            points_list.append(pt)

        self.log("======================================================================")
        self.log(f"                 SURVEY MAPCHECK: {fig_name.upper()}")
        self.log("======================================================================")
        self.log(f"Start Point: {figure.point_ids[0]}    \tN: {points_list[0].northing:.4f}   \tE: {points_list[0].easting:.4f}")

        figure.labels.clear()
        i = 0
        while i < len(points_list) - 1:
            p1 = points_list[i]
            pid1 = figure.point_ids[i]
            next_idx = i + 1
            next_pid = figure.point_ids[next_idx]

            if next_pid.startswith("XC_"):
                end_curve_idx = next_idx
                while end_curve_idx < len(points_list) and figure.point_ids[end_curve_idx].startswith("XC_"):
                    end_curve_idx += 1

                if end_curve_idx < len(points_list):
                    p_end = points_list[end_curve_idx]
                    
                    arc_len = 0.0
                    for j in range(i, end_curve_idx):
                        seg_dist, _ = GeometryEngine.inverse(points_list[j], points_list[j+1])
                        arc_len += seg_dist

                    chord_dist, chord_az = GeometryEngine.inverse(p1, p_end)

                    mid_idx = i + (end_curve_idx - i) // 2
                    p_mid = points_list[mid_idx]
                    a, _ = GeometryEngine.inverse(p1, p_mid)
                    b, _ = GeometryEngine.inverse(p_mid, p_end)
                    c = chord_dist
                    s = (a + b + c) / 2.0
                    area_t = math.sqrt(max(0.0, s * (s - a) * (s - b) * (s - c)))
                    r = 0.0 if area_t < 0.001 else (a * b * c) / (4.0 * area_t)

                    rot = -chord_az.degrees + 90.0
                    if rot < -90: rot += 180.0
                    if rot > 90: rot -= 180.0
                    mid_x = (p1.easting + p_end.easting) / 2.0
                    mid_y = (p1.northing + p_end.northing) / 2.0

                    label_text = f"C: L={arc_len:.2f} R={r:.2f}\nChd: {self._format_bearing(chord_az.degrees)} {chord_dist:.2f}"
                    figure.labels.append(FigureLabel(label_text, mid_x, mid_y, rot))

                    self.log(f"Curve \tChd: {self._format_bearing(chord_az.degrees)} \tDist: {chord_dist:.4f}")
                    self.log(f"      \tRadius: {r:.4f} \tLength: {arc_len:.4f}")
                    self.log(f"End Point:   {figure.point_ids[end_curve_idx]}    \tN: {p_end.northing:.4f}   \tE: {p_end.easting:.4f}")

                    i = end_curve_idx
                else:
                    i += 1
            else:
                p2 = points_list[next_idx]
                dist, az = GeometryEngine.inverse(p1, p2)

                mid_x = (p1.easting + p2.easting) / 2.0
                mid_y = (p1.northing + p2.northing) / 2.0
                rot = -az.degrees + 90.0
                if rot < -90: rot += 180.0
                if rot > 90: rot -= 180.0

                label_text = f"{self._format_bearing(az.degrees)}\n{dist:.2f}"
                figure.labels.append(FigureLabel(label_text, mid_x, mid_y, rot))

                self.log(f"Line  \tBrg: {self._format_bearing(az.degrees)} \tDist: {dist:.4f}")
                self.log(f"End Point:   {next_pid}    \tN: {p2.northing:.4f}   \tE: {p2.easting:.4f}")
                i += 1

        perimeter = 0.0
        for i in range(len(points_list) - 1):
            seg_dist, _ = GeometryEngine.inverse(points_list[i], points_list[i+1])
            perimeter += seg_dist

        last_pt = points_list[-1]
        first_pt = points_list[0]
        closure_dist, closure_az = GeometryEngine.inverse(last_pt, first_pt)

        is_closed = closure_dist <= self.map_check_closure_tolerance
        figure.map_check_failed = not is_closed

        if not is_closed:
            perimeter += closure_dist

        area_sum = 0.0
        for i in range(len(points_list)):
            p_curr = points_list[i]
            p_next = points_list[(i + 1) % len(points_list)]
            area_sum += (p_curr.easting * p_next.northing) - (p_next.easting * p_curr.northing)
        area = abs(area_sum) * 0.5
        acres = area / 43560.0

        precision = 0.0
        if closure_dist > 1e-9:
            precision = perimeter / closure_dist

        self.log("----------------------------------------------------------------------")
        self.log(f"Perimeter: {perimeter:.3f}")
        self.log(f"Area: {area:.2f} sq.ft, {acres:.4f} acres")
        self.log(f"Closure Error: {closure_dist:.4f}, Brg: {self._format_bearing(closure_az.degrees)}")
        if closure_dist > 1e-9:
            self.log(f"Precision: 1:{int(round(precision))}")
        else:
            self.log("Precision: Perfect Closure")

        figure.qc_status = "Passed" if is_closed else "Failed"
        figure.closure_error = closure_dist
        figure.closure_bearing = closure_az.degrees
        figure.area_sqft = area
        figure.acres = acres
        figure.perimeter = perimeter
        figure.precision_ratio = precision if precision > 1e-9 else None

    def _format_bearing(self, azimuth_deg: float) -> str:
        az = azimuth_deg % 360.0
        if az < 0:
            az += 360.0

        def format_dms(deg):
            d = int(deg)
            min_raw = (deg - d) * 60.0
            m = int(min_raw)
            s = round((min_raw - m) * 60.0)
            if s >= 60:
                s -= 60
                m += 1
            if m >= 60:
                m -= 60
                d += 1
            return f"{d}° {m:02d}' {s:02d}\""

        if az < 90.0:
            return f"N {format_dms(az)} E"
        elif az <= 180.0:
            return f"S {format_dms(180.0 - az)} E"
        elif az < 270.0:
            return f"S {format_dms(az - 180.0)} W"
        else:
            return f"N {format_dms(360.0 - az)} W"

    def _is_float(self, val: str) -> bool:
        try:
            float(val)
            return True
        except ValueError:
            return False
