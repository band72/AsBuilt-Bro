import math
from typing import Dict, Optional, Tuple

class CurveSolver:
    """
    Solves Horizontal Circular and Vertical Parabolic Curves for land surveying.
    """

    # ── Part 1: DMS and Decimal Degrees conversions ────────────────────────────
    @staticmethod
    def dms_to_dd(dms: float) -> float:
        """
        Converts DMS format (DDD.MMSS) to Decimal Degrees.
        e.g., 45.3030 -> 45.508333
        """
        sign = -1.0 if dms < 0 else 1.0
        abs_dms = abs(dms)
        
        degrees = int(abs_dms)
        fractional = abs_dms - degrees
        
        minutes = int(fractional * 100 + 1e-9)
        seconds = (fractional * 100 - minutes) * 100
        
        return sign * (degrees + (minutes / 60.0) + (seconds / 3600.0))

    @staticmethod
    def dd_to_dms(dd: float) -> float:
        """
        Converts Decimal Degrees to DMS format (DDD.MMSS).
        e.g., 45.508333 -> 45.3030
        """
        sign = -1.0 if dd < 0 else 1.0
        abs_dd = abs(dd)
        
        degrees = int(abs_dd)
        remainder = abs_dd - degrees
        
        total_minutes = remainder * 60.0
        minutes = int(total_minutes)
        seconds = (total_minutes - minutes) * 60.0
        
        # Round seconds to 2 decimal places to avoid float precision creep
        seconds = round(seconds, 2)
        if seconds >= 60.0:
            seconds -= 60.0
            minutes += 1
        if minutes >= 60:
            minutes -= 60
            degrees += 1
            
        return sign * (degrees + (minutes / 100.0) + (seconds / 10000.0))

    # ── Part 2: Horizontal Circular Curves Solver ──────────────────────────────
    @staticmethod
    def solve_horizontal_curve(
        radius: Optional[float] = None,
        delta: Optional[float] = None, # central angle in DD
        arc_length: Optional[float] = None,
        tangent: Optional[float] = None,
        chord: Optional[float] = None
    ) -> Dict[str, float]:
        """
        Solves a horizontal circular curve given any two parameters.
        Returns a dictionary containing: Radius, Delta (DD), ArcLength, Tangent,
        Chord, External, MiddleOrdinate, and DegreeOfCurve (arc definition).
        """
        # Determine R and Delta first
        if radius is not None and delta is not None:
            r = radius
            d = delta
        elif radius is not None and arc_length is not None:
            r = radius
            d = (arc_length / r) * (180.0 / math.pi)
        elif radius is not None and tangent is not None:
            r = radius
            d = 2.0 * math.atan(tangent / r) * (180.0 / math.pi)
        elif radius is not None and chord is not None:
            r = radius
            # Clamp chord/2R to [-1, 1] to avoid domain error
            d = 2.0 * math.asin(max(-1.0, min(1.0, chord / (2.0 * r)))) * (180.0 / math.pi)
        elif delta is not None and arc_length is not None:
            d = delta
            r = arc_length / (d * (math.pi / 180.0))
        elif delta is not None and tangent is not None:
            d = delta
            r = tangent / math.tan((d / 2.0) * (math.pi / 180.0))
        elif delta is not None and chord is not None:
            d = delta
            r = chord / (2.0 * math.sin((d / 2.0) * (math.pi / 180.0)))
        else:
            raise ValueError("Insufficient parameters. Please provide at least two parameters (e.g., Radius and Delta).")

        # Compute all derived parameters
        rad_half_d = (d / 2.0) * (math.pi / 180.0)
        
        arc = r * (d * (math.pi / 180.0))
        tan = r * math.tan(rad_half_d)
        chd = 2.0 * r * math.sin(rad_half_d)
        
        # External Distance: E = R * (sec(D/2) - 1)
        cos_half = math.cos(rad_half_d)
        ext = r * ((1.0 / cos_half) - 1.0) if abs(cos_half) > 1e-9 else 0.0
        
        # Middle Ordinate: M = R * (1 - cos(D/2))
        mid_ord = r * (1.0 - cos_half)
        
        # Degree of Curve (Arc Definition): Da = 5729.58 / R
        deg_curve = 5729.58 / r if r > 0.1 else 0.0

        return {
            "Radius": round(r, 2),
            "Delta": round(d, 6),
            "ArcLength": round(arc, 2),
            "Tangent": round(tan, 2),
            "Chord": round(chd, 2),
            "External": round(ext, 2),
            "MiddleOrdinate": round(mid_ord, 2),
            "DegreeOfCurve": round(deg_curve, 4)
        }

    # ── Part 3: Vertical Curve Solver (Parabolic) ──────────────────────────────
    @staticmethod
    def solve_vertical_curve(
        pvi_station: float,
        pvi_elevation: float,
        length: float, # Curve length in feet
        g1: float, # Grade in (%)
        g2: float # Grade out (%)
    ) -> Dict[str, any]:
        """
        Solves an equal-tangent vertical parabolic curve.
        Returns:
            PVC/PVT Stations & Elevations, Rate of Change (r),
            and a function to compute elevation at any station.
        """
        pvc_station = pvi_station - (length / 2.0)
        pvc_elevation = pvi_elevation - (g1 / 100.0) * (length / 2.0)
        
        pvt_station = pvi_station + (length / 2.0)
        pvt_elevation = pvi_elevation + (g2 / 100.0) * (length / 2.0)
        
        # Rate of change of grade per foot
        r = (g2 - g1) / length

        def get_elevation_at(station: float) -> float:
            x = station - pvc_station
            if x < 0 or x > length:
                # Extrapolate outside curve
                if x < 0:
                    return pvc_elevation + (g1 / 100.0) * x
                else:
                    return pvt_elevation + (g2 / 100.0) * (station - pvt_station)
            # Parabolic equation: Y = Y_pvc + (g1/100)*x + (r/200)*x^2
            # since r = (g2 - g1)/L
            return pvc_elevation + (g1 / 100.0) * x + (r / 200.0) * (x ** 2)

        return {
            "PVC_Station": round(pvc_station, 2),
            "PVC_Elevation": round(pvc_elevation, 2),
            "PVT_Station": round(pvt_station, 2),
            "PVT_Elevation": round(pvt_elevation, 2),
            "RateOfChange": round(r, 6),
            "ElevationCalculator": get_elevation_at
        }
