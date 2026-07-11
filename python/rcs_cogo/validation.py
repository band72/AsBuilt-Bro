from typing import List, Tuple, Optional
from .projection import StatePlaneProjection

# Coordinate limits from JeaValidationService.cs
EAST_MIN, EAST_MAX = 320_000, 590_000
NORTH_MIN, NORTH_MAX = 1_920_000, 2_370_000
LAT_MIN, LAT_MAX = 29.0, 31.0
LON_MIN, LON_MAX = -83.0, -80.0

class JeaIssue:
    def __init__(self, table: str, asset_id: str, field: str, message: str, severity: str = "Error"):
        self.table = table
        self.asset_id = asset_id
        self.field = field
        self.message = message
        self.severity = severity # Error, Warning, Info

    def __repr__(self) -> str:
        return f"[{self.severity}] Table: {self.table} | Asset: {self.asset_id} | Field: {self.field} | {self.message}"


class JeaValidationService:
    """
    Validates assets and points against JEA As-Built criteria.
    """
    @staticmethod
    def check_coords(
        table: str, asset_id: str, easting: Optional[float], northing: Optional[float],
        lat: Optional[float], lon: Optional[float]
    ) -> List[JeaIssue]:
        issues = []

        has_state_plane = easting is not None and northing is not None and easting != 0 and northing != 0
        has_lat_lon = lat is not None and lon is not None and lat != 0 and lon != 0

        if not has_state_plane:
            issues.append(JeaIssue(table, asset_id, "State Plane Coordinates", "Easting/Northing coordinates are missing.", "Error"))
        else:
            if not (EAST_MIN <= easting <= EAST_MAX):
                issues.append(JeaIssue(table, asset_id, "Easting (X)", f"Easting {easting:,.2f} is outside JEA bounds ({EAST_MIN:,}–{EAST_MAX:,} ft).", "Error"))
            if not (NORTH_MIN <= northing <= NORTH_MAX):
                issues.append(JeaIssue(table, asset_id, "Northing (Y)", f"Northing {northing:,.2f} is outside JEA bounds ({NORTH_MIN:,}–{NORTH_MAX:,} ft).", "Error"))

        if not has_lat_lon:
            if has_state_plane:
                issues.append(JeaIssue(table, asset_id, "Latitude / Longitude", "Lat/Lon is empty — will be auto-computed from State Plane on export.", "Warning"))
            else:
                issues.append(JeaIssue(table, asset_id, "Latitude / Longitude", "Lat/Lon coordinates are missing and no State Plane coordinates to derive from.", "Error"))
        else:
            if not (LAT_MIN <= lat <= LAT_MAX):
                issues.append(JeaIssue(table, asset_id, "Latitude", f"Latitude {lat:.4f}° is outside JEA bounds ({LAT_MIN}–{LAT_MAX}°).", "Error"))
            if not (LON_MIN <= lon <= LON_MAX):
                issues.append(JeaIssue(table, asset_id, "Longitude", f"Longitude {lon:.4f}° is outside JEA bounds ({LON_MIN}–{LON_MAX}°).", "Error"))

        return issues

    @staticmethod
    def validate_gravity_pipe(
        asset_id: str, start_invert: Optional[float], end_invert: Optional[float], slope: Optional[float]
    ) -> List[JeaIssue]:
        issues = []
        if start_invert is None or end_invert is None:
            issues.append(JeaIssue("Pipe Run", asset_id, "Inverts", "Inverts are required for wastewater gravity runs.", "Error"))
        else:
            if end_invert > start_invert:
                issues.append(JeaIssue("Pipe Run", asset_id, "Invert Slope", f"Gravity flow is reversed (End {end_invert:.2f} > Start {start_invert:.2f}).", "Error"))
            
        if slope is not None:
            if slope <= 0:
                issues.append(JeaIssue("Pipe Run", asset_id, "Slope", f"Slope must be positive ({slope:.3f}%).", "Error"))
            elif slope < 0.40:
                issues.append(JeaIssue("Pipe Run", asset_id, "Slope", f"Slope warning: Gravity Pipe is {slope:.2f}%. Standard minimum allowable slope is 0.40%.", "Warning"))
            
        return issues

    @staticmethod
    def validate_pipe(
        asset_id: str, run_type: str, diameter: float, material: str, slope: Optional[float]
    ) -> List[JeaIssue]:
        issues = []
        valid_materials = {"PVC", "DIP", "HDPE", "RCP", "DI", "STEEL"}
        valid_diameters = {2, 3, 4, 6, 8, 10, 12, 14, 16, 18, 20, 24, 30, 36, 42, 48}

        r_type = run_type.upper()
        
        # Slope check for gravity sewers/storm runs
        if r_type in ("WW", "SEWER", "WASTEWATER", "ST", "STORM", "D", "DRAINAGE"):
            if slope is not None and slope < 0.40:
                issues.append(JeaIssue("Pipe Run", asset_id, "Slope", f"Slope warning: Gravity Pipe is {slope:.2f}%. Standard minimum allowable slope is 0.40%.", "Warning"))

        if material and material.upper() not in valid_materials:
            issues.append(JeaIssue("Pipe Run", asset_id, "Material", f"Material '{material}' is not recognized in the Master Database catalog.", "Warning"))

        if diameter and int(diameter) not in valid_diameters:
            issues.append(JeaIssue("Pipe Run", asset_id, "Diameter", f"Diameter {diameter}\" is non-standard for utility specifications.", "Warning"))

        return issues

