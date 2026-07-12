import math
from typing import Tuple

class StatePlaneProjection:
    """
    Pure-math projection for Florida State Plane zones:
      • EPSG:2236 — FL East  (Transverse Mercator, Jacksonville / Miami)
      • EPSG:2237 — FL West  (Transverse Mercator, Tampa / Fort Myers)
      • EPSG:2238 — FL North (Lambert Conformal Conic, Tallahassee / Pensacola)
    Ellipsoid: GRS80 / NAD83. Coords in US Survey Feet.
    """
    A = 6378137.0
    F = 1.0 / 298.257222101
    E2 = 2 * F - F * F
    E = math.sqrt(E2)

    # Unit conversions
    FT_TO_M = 1200.0 / 3937.0
    M_TO_FT = 3937.0 / 1200.0

    # TM Zones
    # z: (k0, lon0_rad, lat0_rad, fe_m, fn_m)
    TM_EAST = (0.999941177, -81.0 * math.pi / 180.0, (24.0 + 20.0 / 60.0) * math.pi / 180.0, 200000.0, 0.0)
    TM_WEST = (0.999940833, -82.0 * math.pi / 180.0, (24.0 + 20.0 / 60.0) * math.pi / 180.0, 200000.0, 0.0)

    # LCC Zone Florida North
    LCC_PHI1 = (29.0 + 34.0 / 60.0) * math.pi / 180.0
    LCC_PHI2 = (30.0 + 45.0 / 60.0) * math.pi / 180.0
    LCC_PHI0 = 29.0 * math.pi / 180.0
    LCC_LON0 = -84.5 * math.pi / 180.0
    LCC_FE_M = 600000.0
    LCC_FN_M = 0.0

    # Pre-calculated constants
    LCC_N = 0.0
    LCC_F = 0.0
    LCC_RHO0 = 0.0
    TM_M0_EAST = 0.0
    TM_M0_WEST = 0.0

    @classmethod
    def initialize(cls):
        # Meridional arc at TM origin latitudes
        cls.TM_M0_EAST = cls._meridional_arc(cls.TM_EAST[2])
        cls.TM_M0_WEST = cls._meridional_arc(cls.TM_WEST[2])

        # LCC Constants
        def lcc_m(phi):
            sp = math.sin(phi)
            return math.cos(phi) / math.sqrt(1.0 - cls.E2 * sp * sp)

        def lcc_t(phi):
            sp = cls.E * math.sin(phi)
            return math.tan(math.pi / 4.0 - phi / 2.0) / math.pow((1.0 - sp) / (1.0 + sp), cls.E / 2.0)

        m1 = lcc_m(cls.LCC_PHI1)
        m2 = lcc_m(cls.LCC_PHI2)
        t0 = lcc_t(cls.LCC_PHI0)
        t1 = lcc_t(cls.LCC_PHI1)
        t2 = lcc_t(cls.LCC_PHI2)

        cls.LCC_N = (math.log(m1) - math.log(m2)) / (math.log(t1) - math.log(t2))
        cls.LCC_F = m1 / (cls.LCC_N * math.pow(t1, cls.LCC_N))
        cls.LCC_RHO0 = cls.A * cls.LCC_F * math.pow(t0, cls.LCC_N)

    @classmethod
    def _meridional_arc(cls, phi: float) -> float:
        e2 = cls.E2
        e4 = e2 * e2
        e6 = e4 * e2
        return cls.A * (
            (1.0 - e2 / 4.0 - 3.0 * e4 / 64.0 - 5.0 * e6 / 256.0) * phi
            - (3.0 * e2 / 8.0 + 3.0 * e4 / 32.0 + 45.0 * e6 / 1024.0) * math.sin(2.0 * phi)
            + (15.0 * e4 / 256.0 + 45.0 * e6 / 1024.0) * math.sin(4.0 * phi)
            - (35.0 * e6 / 3072.0) * math.sin(6.0 * phi)
        )

    @classmethod
    def normalize_zone(cls, zone: str) -> str:
        if not zone:
            return "EPSG:2236"
        up = zone.upper()
        if "2237" in up or "WEST" in up:
            return "EPSG:2237"
        if "2238" in up or "NORTH" in up:
            return "EPSG:2238"
        return "EPSG:2236"

    @classmethod
    def to_lat_lon(cls, easting_ft: float, northing_ft: float, zone: str = "EPSG:2236") -> Tuple[float, float]:
        zone = cls.normalize_zone(zone)
        try:
            from pyproj import Transformer
            transformer = Transformer.from_crs(zone, "EPSG:4326", always_xy=True)
            lon, lat = transformer.transform(easting_ft, northing_ft)
            return lat, lon
        except Exception:
            if zone == "EPSG:2238":
                return cls._lcc_inverse(easting_ft, northing_ft)
            elif zone == "EPSG:2237":
                return cls._tm_inverse(easting_ft, northing_ft, cls.TM_WEST, cls.TM_M0_WEST)
            else:
                return cls._tm_inverse(easting_ft, northing_ft, cls.TM_EAST, cls.TM_M0_EAST)

    @classmethod
    def to_state_plane(cls, lat_deg: float, lon_deg: float, zone: str = "EPSG:2236") -> Tuple[float, float]:
        zone = cls.normalize_zone(zone)
        try:
            from pyproj import Transformer
            transformer = Transformer.from_crs("EPSG:4326", zone, always_xy=True)
            easting, northing = transformer.transform(lon_deg, lat_deg)
            return easting, northing
        except Exception:
            if zone == "EPSG:2238":
                return cls._lcc_forward(lat_deg, lon_deg)
            elif zone == "EPSG:2237":
                return cls._tm_forward(lat_deg, lon_deg, cls.TM_WEST, cls.TM_M0_WEST)
            else:
                return cls._tm_forward(lat_deg, lon_deg, cls.TM_EAST, cls.TM_M0_EAST)

    @classmethod
    def _tm_inverse(cls, easting_ft: float, northing_ft: float, z: Tuple, m0: float) -> Tuple[float, float]:
        k0, lon0, lat0, fe_m, fn_m = z
        e_m = easting_ft * cls.FT_TO_M - fe_m
        n_m = northing_ft * cls.FT_TO_M - fn_m

        m = m0 + n_m / k0
        mu = m / (cls.A * (1.0 - cls.E2 / 4.0 - 3.0 * cls.E2 * cls.E2 / 64.0 - 5.0 * cls.E2 * cls.E2 * cls.E2 / 256.0))

        e1 = (1.0 - math.sqrt(1.0 - cls.E2)) / (1.0 + math.sqrt(1.0 - cls.E2))
        phi1 = (mu
            + (3.0 * e1 / 2.0 - 27.0 * e1 * e1 * e1 / 32.0) * math.sin(2.0 * mu)
            + (21.0 * e1 * e1 / 16.0 - 55.0 * math.pow(e1, 4) / 32.0) * math.sin(4.0 * mu)
            + (151.0 * math.pow(e1, 3) / 96.0) * math.sin(6.0 * mu)
            + (1097.0 * math.pow(e1, 4) / 512.0) * math.sin(8.0 * mu)
        )

        sp = math.sin(phi1)
        cp = math.cos(phi1)
        tp = math.tan(phi1)
        n1 = cls.A / math.sqrt(1.0 - cls.E2 * sp * sp)
        t1 = tp * tp
        c1 = cls.E2 / (1.0 - cls.E2) * cp * cp
        r1 = cls.A * (1.0 - cls.E2) / math.pow(1.0 - cls.E2 * sp * sp, 1.5)
        d = e_m / (n1 * k0)

        lat = (phi1
            - (n1 * tp / r1) * (
                d * d / 2.0
                - (5.0 + 3.0 * t1 + 10.0 * c1 - 4.0 * c1 * c1 - 9.0 * cls.E2 / (1.0 - cls.E2)) * math.pow(d, 4) / 24.0
                + (61.0 + 90.0 * t1 + 298.0 * c1 + 45.0 * t1 * t1 - 252.0 * cls.E2 / (1.0 - cls.E2) - 3.0 * c1 * c1) * math.pow(d, 6) / 720.0
            )
        )

        lon = lon0 + (
            d
            - (1.0 + 2.0 * t1 + c1) * math.pow(d, 3) / 6.0
            + (5.0 - 2.0 * c1 + 28.0 * t1 - 3.0 * c1 * c1 + 8.0 * cls.E2 / (1.0 - cls.E2) + 24.0 * t1 * t1) * math.pow(d, 5) / 120.0
        ) / cp

        return lat * 180.0 / math.pi, lon * 180.0 / math.pi

    @classmethod
    def _tm_forward(cls, lat_deg: float, lon_deg: float, z: Tuple, m0: float) -> Tuple[float, float]:
        k0, lon0, lat0, fe_m, fn_m = z
        phi = lat_deg * math.pi / 180.0
        d_lam = lon_deg * math.pi / 180.0 - lon0

        sp = math.sin(phi)
        cp = math.cos(phi)
        tp = math.tan(phi)
        n = cls.A / math.sqrt(1.0 - cls.E2 * sp * sp)
        t = tp * tp
        c = cls.E2 / (1.0 - cls.E2) * cp * cp
        av = cp * d_lam
        m = cls._meridional_arc(phi)

        x = k0 * n * (
            av
            + (1.0 - t + c) * math.pow(av, 3) / 6.0
            + (5.0 - 18.0 * t + t * t + 72.0 * c - 58.0 * cls.E2 / (1.0 - cls.E2)) * math.pow(av, 5) / 120.0
        )

        y = k0 * (
            m - m0
            + n * tp * (
                av * av / 2.0
                + (5.0 - t + 9.0 * c + 4.0 * c * c) * math.pow(av, 4) / 24.0
                + (61.0 - 58.0 * t + t * t + 600.0 * c - 330.0 * cls.E2 / (1.0 - cls.E2)) * math.pow(av, 6) / 720.0
            )
        )

        return (x + fe_m) * cls.M_TO_FT, (y + fn_m) * cls.M_TO_FT

    @classmethod
    def _lcc_inverse(cls, easting_ft: float, northing_ft: float) -> Tuple[float, float]:
        x = easting_ft * cls.FT_TO_M - cls.LCC_FE_M
        y = northing_ft * cls.FT_TO_M - cls.LCC_FN_M

        y_diff = cls.LCC_RHO0 - y
        sign_n = 1.0 if cls.LCC_N >= 0 else -1.0
        rho = sign_n * math.sqrt(x * x + y_diff * y_diff)
        theta = math.atan2(sign_n * x, sign_n * y_diff)

        t = math.pow(rho / (cls.A * cls.LCC_F), 1.0 / cls.LCC_N)

        phi = math.pi / 2.0 - 2.0 * math.atan(t)
        for _ in range(10):
            sp = cls.E * math.sin(phi)
            phi = math.pi / 2.0 - 2.0 * math.atan(t * math.pow((1.0 - sp) / (1.0 + sp), cls.E / 2.0))

        lon = theta / cls.LCC_N + cls.LCC_LON0
        return phi * 180.0 / math.pi, lon * 180.0 / math.pi

    @classmethod
    def _lcc_forward(cls, lat_deg: float, lon_deg: float) -> Tuple[float, float]:
        phi = lat_deg * math.pi / 180.0
        lam = lon_deg * math.pi / 180.0

        sp = cls.E * math.sin(phi)
        t = math.tan(math.pi / 4.0 - phi / 2.0) / math.pow((1.0 - sp) / (1.0 + sp), cls.E / 2.0)
        rho = cls.A * cls.LCC_F * math.pow(t, cls.LCC_N)
        theta = cls.LCC_N * (lam - cls.LCC_LON0)

        x_m = rho * math.sin(theta) + cls.LCC_FE_M
        y_m = cls.LCC_RHO0 - rho * math.cos(theta) + cls.LCC_FN_M

        return x_m * cls.M_TO_FT, y_m * cls.M_TO_FT

StatePlaneProjection.initialize()
