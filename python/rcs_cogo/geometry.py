import math
from typing import Tuple, Optional
from .primitives import Angle, Point3D

class GeometryEngine:
    """
    Provides core coordinate geometry calculations.
    Equivalent to RCS.Cogo.Core.Maths.GeometryEngine.
    """
    @staticmethod
    def inverse(p1: Point3D, p2: Point3D) -> Tuple[float, Angle]:
        """
        Calculates the geodetic inverse (Distance and Azimuth) between two points.
        """
        dn = p2.northing - p1.northing
        de = p2.easting - p1.easting
        dist = math.sqrt(dn * dn + de * de)
        az = math.atan2(de, dn)
        
        if az < 0:
            az += 2.0 * math.pi
            
        return dist, Angle.from_radians(az)

    @staticmethod
    def forward(p: Point3D, az: Angle, dist: float) -> Point3D:
        """
        Calculates a new point given a starting point, azimuth, and distance.
        """
        return Point3D(
            p.northing + dist * math.cos(az.radians),
            p.easting + dist * math.sin(az.radians),
            p.elevation
        )

    @staticmethod
    def intersection_bearing_bearing(p1: Point3D, az1: Angle, p2: Point3D, az2: Angle) -> Optional[Point3D]:
        """
        Calculates the intersection point of two lines defined by points and bearings (Azimuths).
        Returns None if lines are parallel.
        """
        theta1 = az1.radians
        theta2 = az2.radians

        # Check for parallel lines
        if abs(math.sin(theta1 - theta2)) < 1e-9:
            return None

        dn = p2.northing - p1.northing
        de = p2.easting - p1.easting

        # Solve simultaneous equations:
        # k1 * sin(theta1) - k2 * sin(theta2) = de
        # k1 * cos(theta1) - k2 * cos(theta2) = dn
        denominator = math.sin(theta1 - theta2)
        k1 = (de * math.cos(theta2) - dn * math.sin(theta2)) / denominator

        return GeometryEngine.forward(p1, az1, k1)

    @staticmethod
    def intersection_line_line(
        l1_start: Point3D, l1_end: Point3D, off1: float,
        l2_start: Point3D, l2_end: Point3D, off2: float
    ) -> Optional[Point3D]:
        """
        Calculates the intersection of two lines defined by start/end points and offsets.
        Offset > 0 is Right, < 0 is Left.
        """
        # Calculate Azimuths of base lines
        _, res1_az = GeometryEngine.inverse(l1_start, l1_end)
        _, res2_az = GeometryEngine.inverse(l2_start, l2_end)

        # Adjust start points by offset (Perpendicular: Az + 90 degrees)
        p1_shifted = GeometryEngine.forward(l1_start, res1_az + Angle.half_pi(), off1)
        p2_shifted = GeometryEngine.forward(l2_start, res2_az + Angle.half_pi(), off2)

        # Intersect shifted points along their original azimuth vectors
        return GeometryEngine.intersection_bearing_bearing(p1_shifted, res1_az, p2_shifted, res2_az)

    @staticmethod
    def intersection_distance_distance(
        p1: Point3D, r1: float, p2: Point3D, r2: float
    ) -> Tuple[Optional[Point3D], Optional[Point3D]]:
        """
        Calculates the intersection(s) of two circles (Distance-Distance).
        Returns a tuple of (Left, Right) points relative to the P1->P2 vector.
        """
        dist12, az12 = GeometryEngine.inverse(p1, p2)

        # Check for no solution (separated, concentric, or contained)
        if dist12 > (r1 + r2) or dist12 < abs(r1 - r2) or dist12 == 0:
            return None, None

        # Law of Cosines: r2^2 = r1^2 + dist12^2 - 2*r1*dist12*cos(alpha)
        cos_alpha = (r1 * r1 + dist12 * dist12 - r2 * r2) / (2.0 * r1 * dist12)
        
        # Clamp for floating point errors
        cos_alpha = max(-1.0, min(1.0, cos_alpha))

        alpha = math.acos(cos_alpha)

        # Left Solution (Counter-Clockwise from Vector P1->P2)
        az_left = az12.radians - alpha
        # Right Solution (Clockwise)
        az_right = az12.radians + alpha

        return (
            GeometryEngine.forward(p1, Angle.from_radians(az_left), r1),
            GeometryEngine.forward(p1, Angle.from_radians(az_right), r1)
        )

    @staticmethod
    def intersection_segment_segment(p1: Point3D, p2: Point3D, p3: Point3D, p4: Point3D) -> Optional[Point3D]:
        """
        Calculates the intersection of two line segments, if they intersect within their bounds.
        """
        e1, n1 = p1.easting, p1.northing
        e2, n2 = p2.easting, p2.northing
        e3, n3 = p3.easting, p3.northing
        e4, n4 = p4.easting, p4.northing

        denom = (n4 - n3) * (e2 - e1) - (e4 - e3) * (n2 - n1)
        if abs(denom) < 1e-9:
            return None # Parallel or collinear

        ua = ((e4 - e3) * (n1 - n3) - (n4 - n3) * (e1 - e3)) / denom
        ub = ((e2 - e1) * (n1 - n3) - (n2 - n1) * (e1 - e3)) / denom

        if 0.0 <= ua <= 1.0 and 0.0 <= ub <= 1.0:
            int_e = e1 + (ua * (e2 - e1))
            int_n = n1 + (ua * (n2 - n1))
            int_z = p1.elevation + (ua * (p2.elevation - p1.elevation))
            return Point3D(int_n, int_e, int_z)

        return None

    @staticmethod
    def translate_rotate(
        p: Point3D, dn: float, de: float,
        origin: Point3D, rotation_deg: float
    ) -> Point3D:
        """
        Translates a point and rotates it clockwise around a specified origin point.
        """
        # Shift relative to origin
        n_shift = p.northing - origin.northing
        e_shift = p.easting - origin.easting
        
        # Rotate clockwise
        rad = math.radians(rotation_deg)
        cos_r = math.cos(rad)
        sin_r = math.sin(rad)
        
        n_rot = n_shift * cos_r - e_shift * sin_r
        e_rot = n_shift * sin_r + e_shift * cos_r
        
        # Add back origin and translation offsets
        return Point3D(
            origin.northing + n_rot + dn,
            origin.easting + e_rot + de,
            p.elevation
        )

    @staticmethod
    def point_station_offset(
        p: Point3D, start: Point3D, end: Point3D
    ) -> Tuple[float, float]:
        """
        Calculates the Station and Offset of a point relative to a baseline alignment.
        Station is the distance along the baseline; Offset is the perpendicular distance (Right > 0, Left < 0).
        """
        dn_base = end.northing - start.northing
        de_base = end.easting - start.easting
        l_base = math.sqrt(dn_base * dn_base + de_base * de_base)
        
        if l_base < 1e-9:
            return 0.0, 0.0

        # Unit direction vectors
        un = dn_base / l_base
        ue = de_base / l_base
        
        # Target relative to start
        dn_p = p.northing - start.northing
        de_p = p.easting - start.easting
        
        station = dn_p * un + de_p * ue
        offset = -dn_p * ue + de_p * un
        
        return station, offset

    @staticmethod
    def create_point_by_station_offset(
        start: Point3D, end: Point3D, station: float, offset: float
    ) -> Point3D:
        """
        Creates a new point at a specified Station and Offset relative to a baseline alignment.
        """
        dn_base = end.northing - start.northing
        de_base = end.easting - start.easting
        l_base = math.sqrt(dn_base * dn_base + de_base * de_base)
        
        if l_base < 1e-9:
            return Point3D(start.northing, start.easting, start.elevation)
            
        un = dn_base / l_base
        ue = de_base / l_base
        
        # N_P = N_1 + Sta * un - Offset * ue
        # E_P = E_1 + Sta * ue + Offset * un
        n_p = start.northing + station * un - offset * ue
        e_p = start.easting + station * ue + offset * un
        
        # Linear interpolation of elevation
        elev = start.elevation
        if l_base > 0:
            pct = station / l_base
            elev = start.elevation + pct * (end.elevation - start.elevation)
            
        return Point3D(n_p, e_p, elev)
