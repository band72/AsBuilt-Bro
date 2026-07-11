import math

class Angle:
    """
    Represents an angle, providing conversions between Radians, Decimal Degrees, and DMS (DDD.MMSS).
    Equivalent to RCS.Cogo.Core.Primitives.Angle.
    """
    TOLERANCE = 1e-9

    def __init__(self, radians: float):
        self.radians = float(radians)

    @property
    def degrees(self) -> float:
        return self.radians * (180.0 / math.pi)

    @classmethod
    def from_radians(cls, radians: float) -> "Angle":
        return cls(radians)

    @classmethod
    def from_degrees(cls, degrees: float) -> "Angle":
        return cls(degrees * (math.pi / 180.0))

    @classmethod
    def from_dms(cls, dms: float) -> "Angle":
        """
        Creates an angle from a DMS value in the format DDD.MMSS
        Example: 45.3030 => 45° 30' 30"
        """
        sign = -1.0 if dms < 0 else 1.0
        abs_dms = abs(dms)
        
        degrees = int(abs_dms)
        fractional = abs_dms - degrees
        
        # Extract minutes (first two decimal places)
        # e.g., 0.3030 * 100 = 30.30 => int 30
        minutes = int(fractional * 100 + 1e-9)
        
        # Extract seconds (remainder)
        # e.g., (30.30 - 30) = 0.30 * 100 = 30
        seconds = (fractional * 100 - minutes) * 100
        
        decimal_degrees = degrees + (minutes / 60.0) + (seconds / 3600.0)
        return cls.from_degrees(sign * decimal_degrees)

    @classmethod
    def from_quadrant(cls, quadrant: int, bearing_dms: float) -> "Angle":
        """
        Creates an Azimuth Angle from a Quadrant and Bearing (DMS).
        Quadrants: 1=NE, 2=SE, 3=SW, 4=NW
        """
        bearing = cls.from_dms(bearing_dms)
        dec_deg = bearing.degrees
        
        if quadrant == 1: # NE
            return cls.from_degrees(dec_deg)
        elif quadrant == 2: # SE
            return cls.from_degrees(180.0 - dec_deg)
        elif quadrant == 3: # SW
            return cls.from_degrees(180.0 + dec_deg)
        elif quadrant == 4: # NW
            return cls.from_degrees(360.0 - dec_deg)
        else:
            raise ValueError("Quadrant must be 1-4")

    def to_dms(self) -> float:
        """
        Returns the angle in DMS format (DDD.MMSS).
        """
        total_degrees = self.degrees
        sign = -1.0 if total_degrees < 0 else 1.0
        abs_deg = abs(total_degrees)
        
        d = int(abs_deg)
        remainder = abs_deg - d
        
        total_minutes = remainder * 60.0
        m = int(total_minutes)
        
        s = (total_minutes - m) * 60.0
        
        # Format as d.mmss with rounding check
        dms = d + (m / 100.0) + (s / 10000.0)
        return sign * dms

    # Common survey directions
    @classmethod
    def zero(cls) -> "Angle":
        return cls(0.0)

    @classmethod
    def half_pi(cls) -> "Angle":
        return cls(math.pi / 2.0)

    @classmethod
    def pi(cls) -> "Angle":
        return cls(math.pi)

    @classmethod
    def two_pi(cls) -> "Angle":
        return cls(2.0 * math.pi)

    # Arithmetic operators
    def __add__(self, other: "Angle") -> "Angle":
        return Angle(self.radians + other.radians)

    def __sub__(self, other: "Angle") -> "Angle":
        return Angle(self.radians - other.radians)

    def __mul__(self, scalar: float) -> "Angle":
        return Angle(self.radians * scalar)

    def __truediv__(self, scalar: float) -> "Angle":
        return Angle(self.radians / scalar)

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, Angle):
            return False
        return abs(self.radians - other.radians) < self.TOLERANCE

    def __lt__(self, other: "Angle") -> bool:
        return self.radians < other.radians

    def __le__(self, other: "Angle") -> bool:
        return self.radians <= other.radians or self == other

    def __gt__(self, other: "Angle") -> bool:
        return self.radians > other.radians

    def __ge__(self, other: "Angle") -> bool:
        return self.radians >= other.radians or self == other

    def __repr__(self) -> str:
        return f"Angle({self.radians:.9f} rad / {self.degrees:.4f}°)"

    def __str__(self) -> str:
        return f"{self.to_dms():.4f}"


class Point3D:
    """
    Represents a 3D coordinate point (Northing, Easting, Elevation).
    Equivalent to RCS.Cogo.Core.Primitives.Point3D.
    """
    def __init__(self, northing: float, easting: float, elevation: float = 0.0):
        self.northing = float(northing)
        self.easting = float(easting)
        self.elevation = float(elevation)

    def __repr__(self) -> str:
        return f"Point3D(N={self.northing:.4f}, E={self.easting:.4f}, Z={self.elevation:.4f})"
