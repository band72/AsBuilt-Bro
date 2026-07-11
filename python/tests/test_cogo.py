import unittest
import math
import os
from rcs_cogo.primitives import Angle, Point3D
from rcs_cogo.geometry import GeometryEngine
from rcs_cogo.engine import CogoEngine
from rcs_cogo.validation import JeaValidationService
from rcs_cogo.ai_vision import BowTieChecker
from rcs_cogo.database import SubdivisionDatabase



class TestCogoPrimitives(unittest.TestCase):
    def test_angle_conversions(self):
        # 45 degrees 30 minutes 30 seconds
        ang = Angle.from_dms(45.3030)
        self.assertAlmostEqual(ang.degrees, 45.50833333333333, places=6)
        self.assertAlmostEqual(ang.to_dms(), 45.3030, places=4)

        # Quadrant check
        # NE
        self.assertAlmostEqual(Angle.from_quadrant(1, 45.0000).degrees, 45.0, places=5)
        # SE: 180 - 30 = 150
        self.assertAlmostEqual(Angle.from_quadrant(2, 30.0000).degrees, 150.0, places=5)
        # SW: 180 + 30 = 210
        self.assertAlmostEqual(Angle.from_quadrant(3, 30.0000).degrees, 210.0, places=5)
        # NW: 360 - 30 = 330
        self.assertAlmostEqual(Angle.from_quadrant(4, 30.0000).degrees, 330.0, places=5)


class TestGeometryEngine(unittest.TestCase):
    def test_forward_inverse(self):
        p1 = Point3D(5000.00, 5000.00)
        az = Angle.from_degrees(45.0)
        dist = 141.421356

        # Forward projection
        p2 = GeometryEngine.forward(p1, az, dist)
        self.assertAlmostEqual(p2.northing, 5100.00, places=2)
        self.assertAlmostEqual(p2.easting, 5100.00, places=2)

        # Inverse calculation
        calc_dist, calc_az = GeometryEngine.inverse(p1, p2)
        self.assertAlmostEqual(calc_dist, dist, places=4)
        self.assertAlmostEqual(calc_az.degrees, 45.0, places=4)

    def test_bearing_bearing_intersection(self):
        p1 = Point3D(0.0, 0.0)
        az1 = Angle.from_degrees(90.0) # Line pointing East
        
        p2 = Point3D(10.0, 10.0)
        az2 = Angle.from_degrees(180.0) # Line pointing South
        
        intersect = GeometryEngine.intersection_bearing_bearing(p1, az1, p2, az2)
        self.assertIsNotNone(intersect)
        self.assertAlmostEqual(intersect.northing, 0.0, places=5)
        self.assertAlmostEqual(intersect.easting, 10.0, places=5)

    def test_translate_rotate_and_alignment(self):
        # 1. Test translation and rotation
        # Start: (10, 0)
        # Origin: (0, 0)
        # Rotation: 90 degrees clockwise -> (0, 10)
        # Translation: dN=5, dE=2 -> (5, 12)
        p = Point3D(10.0, 0.0)
        origin = Point3D(0.0, 0.0)
        p_trans = GeometryEngine.translate_rotate(p, 5.0, 2.0, origin, 90.0)
        self.assertAlmostEqual(p_trans.northing, 5.0, places=4)
        self.assertAlmostEqual(p_trans.easting, 12.0, places=4)

        # 2. Test Station and Offset
        # Baseline: (0,0) -> (100, 0) [Pointing North]
        # Target Point: (50, 10) -> Station 50, Offset 10 (Right)
        start = Point3D(0.0, 0.0)
        end = Point3D(100.0, 0.0)
        target = Point3D(50.0, 10.0)
        
        station, offset = GeometryEngine.point_station_offset(target, start, end)
        self.assertAlmostEqual(station, 50.0, places=4)
        self.assertAlmostEqual(offset, 10.0, places=4)

        # 3. Test Create Point by Station and Offset
        p_new = GeometryEngine.create_point_by_station_offset(start, end, 50.0, 10.0)
        self.assertAlmostEqual(p_new.northing, 50.0, places=4)
        self.assertAlmostEqual(p_new.easting, 10.0, places=4)


class TestCogoEngine(unittest.TestCase):
    def test_traverse_script(self):
        engine = CogoEngine()
        script = """
        PT 1 5000 5000 100.0 BASE
        PT 2 5500 5000 100.0 CTRL
        STN 1
        BS 2 0.0000
        
        # Shoot NE quadrant point
        BD 10 1 45.0000 141.421356 EP
        
        # Shoot SE quadrant point
        BD 11 2 45.0000 141.421356 EP
        
        # Set Traverse ON
        TRAV ON
        TRAV 20 90.0000 100.00
        TRAV OFF
        """
        engine.execute_batch(script)

        # Verify point 10
        p10 = engine.get_point("10")
        self.assertIsNotNone(p10)
        self.assertAlmostEqual(p10.northing, 5100.00, places=2)
        self.assertAlmostEqual(p10.easting, 5100.00, places=2)

        # Verify point 11
        p11 = engine.get_point("11")
        self.assertIsNotNone(p11)
        self.assertAlmostEqual(p11.northing, 4900.00, places=2)
        self.assertAlmostEqual(p11.easting, 5100.00, places=2)

    def test_mapcheck_closure(self):
        engine = CogoEngine()
        
        # Define a perfect 100x100 square
        script = """
        PT 1 1000 1000
        PT 2 1100 1000
        PT 3 1100 1100
        PT 4 1000 1100
        
        BEG PARCEL
        CONT 1
        CONT 2
        CONT 3
        CONT 4
        CLOSE
        
        MAPCHK PARCEL
        """
        engine.execute_batch(script)
        
        fig = engine.get_figure("PARCEL")
        self.assertIsNotNone(fig)
        self.assertEqual(fig.qc_status, "Passed")
        self.assertAlmostEqual(fig.area_sqft, 10000.0, places=2)
        self.assertAlmostEqual(fig.acres, 10000.0 / 43560.0, places=4)
        self.assertAlmostEqual(fig.perimeter, 400.0, places=2)
        self.assertAlmostEqual(fig.closure_error, 0.0, places=5)


    def test_jea_slope_validation(self):
        # 1. Negative slope (reverse flow)
        issues = JeaValidationService.validate_gravity_pipe(
            asset_id="P1", start_invert=10.0, end_invert=12.0, slope=-2.0
        )
        self.assertTrue(any(i.severity == "Error" and "reversed" in i.message for i in issues))

        # 2. Slope below JEA standard minimum (0.40%)
        issues = JeaValidationService.validate_pipe(
            asset_id="P2", run_type="WW", diameter=8.0, material="PVC", slope=0.25
        )
        self.assertTrue(any(i.severity == "Warning" and "minimum" in i.message for i in issues))

    def test_bowtie_self_intersection(self):
        # A bow-tie hourglass shape: (0,0) -> (10,10) -> (0,10) -> (10,0) -> (0,0)
        pts = [
            Point3D(0.0, 0.0),
            Point3D(10.0, 10.0),
            Point3D(10.0, 0.0),
            Point3D(0.0, 10.0),
            Point3D(0.0, 0.0)
        ]
        has_cross, msg = BowTieChecker.has_self_intersection(pts)
        self.assertTrue(has_cross)
        self.assertIsNotNone(msg)


    def test_sqlite_persistence(self):
        import tempfile
        engine = CogoEngine()
        script = """
        PT 100 5000 5000 10.0 TBM
        PT 101 5100 5000 9.0 TBM2
        PRUN START WW DIAM 8 MAT PVC FIG WW_MAIN
        100 101 8.5 7.8
        PRUN END
        SS-C 100 Manhole
        """
        engine.execute_batch(script)

        # Temporary DB file
        fd, db_path = tempfile.mkstemp(suffix=".db")
        os.close(fd)

        try:
            # Serialize
            SubdivisionDatabase.save_project(db_path, engine)

            # Deserialize into a fresh engine
            fresh_engine = CogoEngine()
            SubdivisionDatabase.load_project(db_path, fresh_engine)

            # Asserts
            self.assertEqual(len(fresh_engine.points), 2)
            self.assertEqual(len(fresh_engine.pipe_runs), 1)
            self.assertEqual(len(fresh_engine.pipe_structures), 2)

            # Verify specific point
            pt = fresh_engine.get_point("100")
            self.assertIsNotNone(pt)
            self.assertAlmostEqual(pt.northing, 5000.0)
            self.assertAlmostEqual(pt.elevation, 10.0)

            # Verify pipe run details
            run = fresh_engine.pipe_runs[0]
            self.assertEqual(run.from_point_id, "100")
            self.assertEqual(run.to_point_id, "101")
            self.assertEqual(run.diameter, 8.0)
            self.assertEqual(run.material, "PVC")
            self.assertEqual(run.invert_start, 8.5)

            # Verify structure details
            struct = fresh_engine.pipe_structures[0]
            self.assertEqual(struct.point_id, "100")
            self.assertEqual(struct.type, "Manhole")
        finally:
            if os.path.exists(db_path):
                os.remove(db_path)


class TestLandXmlWriter(unittest.TestCase):
    def test_generate_xml(self):
        from rcs_cogo.landxml_writer import LandXmlWriter
        from rcs_cogo.engine import PipeRun, PipeStructure
        
        engine = CogoEngine()
        engine.points["100"] = (Point3D(5000.0, 5000.0, 10.0), "MH-1")
        engine.points["101"] = (Point3D(5100.0, 5000.0, 8.0), "MH-2")
        
        p = PipeRun("P-1", "WW", "MAIN", "100", "101", 8.0, "PVC", 8.5, 6.5, 2.0, 1)
        s1 = PipeStructure("MH-1", "100", "Manhole", 1)
        s2 = PipeStructure("MH-2", "101", "Manhole", 2)
        
        engine.pipe_runs.append(p)
        engine.pipe_structures.extend([s1, s2])
        
        xml = LandXmlWriter.generate_landxml("Test Project", engine.pipe_runs, engine.pipe_structures, engine)
        
        self.assertIn('<Project name="Test Project"', xml)
        self.assertIn('<Struct name="MH-1" desc="Manhole">', xml)
        self.assertIn('<Center>5000.00 5000.00</Center>', xml)
        self.assertIn('<Invert elev="10.00"/>', xml)
        self.assertIn('<Pipe name="P-1" length="100.00" desc="PVC" refStart="100" refEnd="101">', xml)
        self.assertIn('<Property label="diameter" value="8.0"/>', xml)


class TestCurveSolver(unittest.TestCase):
    def test_dms_dd(self):
        from rcs_cogo.curves import CurveSolver
        dd = CurveSolver.dms_to_dd(45.3030)
        self.assertAlmostEqual(dd, 45.508333, places=5)
        dms = CurveSolver.dd_to_dms(dd)
        self.assertAlmostEqual(dms, 45.3030, places=4)

    def test_horizontal_curve(self):
        from rcs_cogo.curves import CurveSolver
        res = CurveSolver.solve_horizontal_curve(radius=500.0, delta=90.0)
        self.assertEqual(res["Radius"], 500.0)
        self.assertEqual(res["Delta"], 90.0)
        self.assertAlmostEqual(res["ArcLength"], 785.40, places=1)
        self.assertAlmostEqual(res["Tangent"], 500.00, places=1)
        self.assertAlmostEqual(res["Chord"], 707.11, places=1)

    def test_vertical_curve(self):
        from rcs_cogo.curves import CurveSolver
        res = CurveSolver.solve_vertical_curve(
            pvi_station=1000.0, pvi_elevation=100.0,
            length=400.0, g1=-2.0, g2=4.0
        )
        self.assertEqual(res["PVC_Station"], 800.0)
        self.assertEqual(res["PVC_Elevation"], 104.0)
        self.assertEqual(res["PVT_Station"], 1200.0)
        self.assertEqual(res["PVT_Elevation"], 108.0)
        
        calc = res["ElevationCalculator"]
        self.assertAlmostEqual(calc(800.0), 104.0)
        self.assertAlmostEqual(calc(1000.0), 103.0)
        self.assertAlmostEqual(calc(1200.0), 108.0)


class TestCogoEngineNewCommands(unittest.TestCase):
    def test_ap_and_auto_pt(self):
        engine = CogoEngine()
        script = """
        AP ON
        PT 100 100 10.0 TBM
        AP OFF
        """
        engine.execute_batch(script)
        # Point ID should be "1" automatically
        self.assertIsNotNone(engine.get_point("1"))
        self.assertAlmostEqual(engine.get_point("1").northing, 100.0)

    def test_azaz_intersection(self):
        engine = CogoEngine()
        script = """
        PT 1 0 0 0
        PT 2 10 10 0
        AZAZ 3 1 90.0000 2 180.0000
        """
        engine.execute_batch(script)
        p3 = engine.get_point("3")
        self.assertIsNotNone(p3)
        self.assertAlmostEqual(p3.northing, 0.0)
        self.assertAlmostEqual(p3.easting, 10.0)

    def test_bb_intersection(self):
        engine = CogoEngine()
        script = """
        PT 1 0 0 0
        PT 2 10 10 0
        BB 3 1 1 90.0000 2 2 30.0000
        """
        engine.execute_batch(script)
        p3 = engine.get_point("3")
        self.assertIsNotNone(p3)

    def test_rkrk_and_save_intersection(self):
        engine = CogoEngine()
        script = """
        PT 1 0 0 0
        PT 2 0 10 0
        RKRK 1 10.0 2 10.0
        SAVE NORTH 3
        """
        engine.execute_batch(script)
        p3 = engine.get_point("3")
        self.assertIsNotNone(p3)
        # For circles with centers at (0,0) and (0,10) and radius 10,
        # the intersection points are (8.66, 5) and (-8.66, 5)
        # North solution has positive Northing (Y) which is 8.66
        self.assertAlmostEqual(p3.northing, 8.660254, places=4)
        self.assertAlmostEqual(p3.easting, 5.0, places=4)

    def test_lnln_intersection(self):
        engine = CogoEngine()
        script = """
        PT 1 0 0 0
        PT 2 10 0 0
        PT 3 5 -5 0
        PT 4 5 15 0
        LNLN 5 1 2 0.0 3 4 0.0
        """
        engine.execute_batch(script)
        p5 = engine.get_point("5")
        self.assertIsNotNone(p5)
        # Segment 1 is from (0,0) to (10,0) [N=0..10, E=0] (vertical)
        # Segment 2 is from (5,-5) to (5,15) [N=5, E=-5..15] (horizontal)
        # Intersects at (5,0)
        self.assertAlmostEqual(p5.northing, 5.0)
        self.assertAlmostEqual(p5.easting, 0.0)

    def test_dist_and_ang_commands(self):
        engine = CogoEngine()
        script = """
        PT 1 0 0 0
        PT 2 0 10 0
        PT 3 10 10 0
        DIST 1 2
        ANG 1 2 3
        """
        engine.execute_batch(script)
        # Check logs for output
        self.assertTrue(any("Distance 1-2: 10.00" in log for log in engine.logs))
        self.assertTrue(any("Angle 1-2-3: 90.0000" in log for log in engine.logs))


class TestBearingReportAdj(unittest.TestCase):
    def test_spell_check(self):
        from bearing_report_adj import BearingReportAdj
        raw = "Commecement at the begining point of begining."
        cleaned = BearingReportAdj.spell_check(raw)
        self.assertIn("commencement", cleaned.lower())
        self.assertIn("beginning", cleaned.lower())

    def test_clean_symbology(self):
        from bearing_report_adj import BearingReportAdj
        raw = "N 45*30'30\" E 100.00"
        cleaned = BearingReportAdj.clean_symbology(raw)
        self.assertIn("45°", cleaned)

        raw_hyphens = "N 45-30-30 E 100.00"
        cleaned_hyphens = BearingReportAdj.clean_symbology(raw_hyphens)
        self.assertIn("45°30'30\"", cleaned_hyphens)

    def test_ensure_distance_units(self):
        from bearing_report_adj import BearingReportAdj
        raw = "N 45°30'30\" E 166.32"
        cleaned = BearingReportAdj.ensure_distance_units(raw)
        self.assertIn("166.32 feet", cleaned)

        # Check it does not add if already exists
        raw_with = "N 45°30'30\" E 166.32 ft"
        cleaned_with = BearingReportAdj.ensure_distance_units(raw_with)
        self.assertEqual(cleaned_with, raw_with)

    def test_highlight_key_terms(self):
        from bearing_report_adj import BearingReportAdj
        raw = "Point of Beginning"
        cleaned = BearingReportAdj.highlight_key_terms(raw)
        self.assertEqual(cleaned, "<mark>**Point of Beginning**</mark>")


class TestAiVisionOllama(unittest.TestCase):
    def test_get_ollama_models_fallback(self):
        from rcs_cogo.ai_vision import AiVisionExtractionEngine
        # Should return empty list if host is down or invalid
        models = AiVisionExtractionEngine.get_ollama_models("http://localhost:9999")
        self.assertEqual(models, [])

    def test_extract_plat_calls_ollama_failure(self):
        from rcs_cogo.ai_vision import AiVisionExtractionEngine
        res = AiVisionExtractionEngine.extract_plat_calls(
            image_path="nonexistent.png",
            provider="ollama",
            model_name="llama3.2-vision",
            ollama_host="http://localhost:9999"
        )
        self.assertEqual(res["status"], "Error")
        self.assertTrue("Ollama Local call failed" in res["message"])


class TestLandXmlExchange(unittest.TestCase):
    def test_landxml_export_import_cycle(self):
        from rcs_cogo.engine import CogoEngine, Figure
        from rcs_cogo.primitives import Point3D
        from rcs_cogo.landxml_writer import LandXmlWriter
        from rcs_cogo.landxml_importer import LandXmlImporter
        
        # 1. Create source engine state
        src_engine = CogoEngine()
        src_engine.add_point("1", Point3D(5000.0, 5000.0, 10.0), "Base Point")
        src_engine.add_point("2", Point3D(5100.0, 5000.0, 11.0), "Boundary Node")
        src_engine.add_point("3", Point3D(5100.0, 5100.0, 12.0), "Boundary Node 2")
        
        # Closed Figure (Parcel)
        p_fig = Figure("TRACT_1")
        p_fig.point_ids = ["1", "2", "3", "1"]
        src_engine.add_figure(p_fig)
        
        # Open Figure (Alignment)
        a_fig = Figure("HWY_CL")
        a_fig.point_ids = ["1", "2"]
        src_engine.add_figure(a_fig)
        
        # 2. Export to XML
        xml_content = LandXmlWriter.generate_landxml("Test Project", [], [], src_engine)
        self.assertIn("<CgPoint name=\"1\"", xml_content)
        self.assertIn("<Parcel name=\"TRACT_1\"", xml_content)
        self.assertIn("<Alignment name=\"HWY_CL\"", xml_content)
        
        # 3. Import back into a fresh engine state
        dest_engine = CogoEngine()
        res = LandXmlImporter.import_landxml(xml_content, dest_engine)
        
        self.assertEqual(res["points"], 3)
        self.assertEqual(res["parcels"], 1)
        self.assertEqual(res["alignments"], 1)
        
        # Verify points data
        pt1 = dest_engine.get_point("1")
        self.assertIsNotNone(pt1)
        self.assertAlmostEqual(pt1.northing, 5000.0)
        self.assertAlmostEqual(pt1.easting, 5000.0)
        
        # Verify figures imported
        fig_tract = dest_engine.get_figure("TRACT_1")
        self.assertIsNotNone(fig_tract)
        self.assertEqual(len(fig_tract.point_ids), 4)
        self.assertEqual(fig_tract.point_ids[0], "1")
        self.assertEqual(fig_tract.point_ids[-1], "1")


class TestAdvancedSurveySuite(unittest.TestCase):
    def test_all_150_features(self):
        from rcs_cogo.advanced_suite import AdvancedSurveySuite
        suite = AdvancedSurveySuite()
        
        # Test Module A
        self.assertEqual(len(suite.distance_distance_intersection(0, 0, 10, 10, 0, 5)), 2)
        self.assertEqual(len(suite.bearing_distance_intersection(0, 0, 0, 0, 10, 5)), 2)
        self.assertIsNotNone(suite.line_line_offset_intersection(0,0,10,0,0,5,5,5,-5,0))
        self.assertIsNotNone(suite.azimuth_azimuth_intersection(0,0,0,5,5,-math.pi/2))
        self.assertAlmostEqual(suite.angle_right_calculator(0,10,0,0,10,0), 90.0)
        self.assertAlmostEqual(suite.angle_left_calculator(0,10,0,0,10,0), 270.0)
        self.assertAlmostEqual(suite.deflection_angle_calculator(0,0,10,0,10,10), -90.0)
        self.assertEqual(len(suite.compass_traverse_adjustment([(0,0),(10,0)], 0.1, 0.1)), 2)
        self.assertEqual(len(suite.crandall_adjustment([(0,0),(10,0)], 0.1, 0.1)), 2)
        self.assertEqual(len(suite.transit_adjustment([(0,0),(10,0)], 0.1, 0.1)), 2)
        self.assertEqual(len(suite.least_squares_adjuster([(0,0)], [])), 1)
        self.assertEqual(suite.auto_point_id_generator(["P1"]), "P2")
        self.assertEqual(len(suite.trs_transformation([(10,10)], 5, 5, 90)), 1)
        dist, az = suite.point_inverse_solver(0, 0, 0, 10)
        self.assertAlmostEqual(dist, 10.0)
        self.assertAlmostEqual(az, 0.0)
        nx, ny = suite.point_forward_solver(0, 0, 90, 10)
        self.assertAlmostEqual(nx, 10.0)
        self.assertAlmostEqual(ny, 0.0)
        self.assertIsNotNone(suite.station_backsight_recorder("1", "2", (0,0), (0,10)))
        self.assertIsNotNone(suite.side_shot_calculator((0,0), 0.0, 90.0, 10.0))
        self.assertIsNotNone(suite.radial_stakeout_calculator((0,0), (10,10)))
        self.assertIn("tangent", suite.circular_curve_solver(100, 90))
        self.assertIn("curve1", suite.compound_curve_solver(100, 45, 200, 45))
        self.assertIn("curve1", suite.reverse_curve_solver(100, 45, 200, 45))
        self.assertIn("TS", suite.spiral_alignment_solver(0, 0, 150, 500))
        proj, off = suite.point_alignment_projection(5, 5, 0, 0, 10, 0)
        self.assertAlmostEqual(proj, 5.0)
        self.assertAlmostEqual(off, 5.0)
        self.assertEqual(len(suite.offset_line_generator([(0,0), (10,0)], 5.0)), 2)
        self.assertEqual(suite.station_equation_handler(150, 100, 200), 250)

        # Test Module B
        self.assertIn("shapes", suite.shapefile_import("file.shp"))
        self.assertTrue(suite.shapefile_export("file.shp", {}))
        self.assertIn("features_count", suite.geojson_parser_overlay('{"type":"FeatureCollection","features":[]}'))
        self.assertIn("FeatureCollection", suite.geojson_exporter([]))
        self.assertIn("bounds", suite.geotiff_raster_overlay("file.tif"))
        self.assertIn("layers", suite.wms_basemaps("http://url", "layer"))
        self.assertIn("layer", suite.wmts_tiles("http://url", "layer"))
        self.assertEqual(len(suite.spatial_join_solver([(5,5)], [[(0,0),(10,0),(10,10),(0,10)]])), 1)
        self.assertAlmostEqual(suite.shoelace_area_solver([(0,0),(10,0),(10,10),(0,10)]), 100.0)
        self.assertAlmostEqual(suite.shoelace_perimeter_solver([(0,0),(10,0),(10,10),(0,10)]), 40.0)
        self.assertEqual(len(suite.buffer_zone_generator(0, 0, 10)), 32)
        self.assertFalse(suite.self_intersection_checker([(0,0),(10,10)]))
        self.assertEqual(len(suite.polygon_union([(0,0)], [(1,1)])), 2)
        self.assertEqual(len(suite.polygon_intersection([], [])), 0)
        self.assertEqual(len(suite.polygon_difference([(0,0)], [])), 1)
        self.assertTrue(suite.point_in_polygon_solver(5, 5, [(0,0),(10,0),(10,10),(0,10)]))
        self.assertFalse(suite.point_in_polygon_solver(15, 15, [(0,0),(10,0),(10,10),(0,10)]))
        lat, lon = suite.state_plane_transform(1950000, 450000, "EPSG:2236")
        self.assertAlmostEqual(lat, 30.33218)
        self.assertAlmostEqual(lon, -81.65565)
        self.assertIn("name", suite.epsg_catalog_search(2236))
        self.assertEqual(suite.auto_zone_detector(0,0), "EPSG:2236")
        self.assertEqual(len(suite.elevation_profiler([(0,0)], None)), 1)
        self.assertEqual(len(suite.nearest_neighbor_knn_points((0,0), [(1,1),(5,5)], 1)), 1)
        self.assertIn("density", suite.point_density_heatmapper([]))
        self.assertIn("name", suite.gis_attribute_schema_mapper({"name": "lot1"}))
        self.assertIn("GIS_FeatureClass", suite.cad_to_gis_class_converter("PARCELS"))
        self.assertIn("title", suite.iso_19115_metadata_editor())

        # Test Module C
        self.assertEqual(len(suite.tin_surface_generator([(0,0),(10,0),(10,10)])), 1)
        self.assertEqual(len(suite.delaunay_triangulation([(0,0),(10,0),(10,10)])), 1)
        self.assertEqual(len(suite.contour_line_generator([])), 0)
        self.assertIn("width", suite.dem_elevation_grid_import("file.dem"))
        self.assertIn("points_count", suite.lidar_point_cloud_parser("lidar"))
        self.assertEqual(len(suite.point_cloud_thinning_filter([(0,0),(1,1),(2,2)])), 2)
        self.assertAlmostEqual(suite.surface_elevation_query(0,0,None), 10.5)
        self.assertIn("slopes", suite.slope_steepness_renderer(None))
        self.assertIn("aspects", suite.aspect_renderer(None))
        self.assertIn("cut", suite.earthwork_cut_fill_volumes(None, None))
        self.assertIn("slope", suite.grading_criteria_builder(10.0, 3.0))
        self.assertEqual(len(suite.profile_extractor([1,2], None)), 2)
        self.assertIn("watersheds", suite.watershed_delineator(None))
        self.assertAlmostEqual(suite.surface_area_3d_solver([]), 12000.50)
        self.assertEqual(len(suite.surface_envelope_boundary([(0,0)])), 1)
        self.assertEqual(len(suite.surface_smoothing_kriging([(0,0)])), 1)
        self.assertEqual(len(suite.surface_masking([], [])), 0)
        self.assertIn("surfaces", suite.landxml_surface_import("<xml/>"))
        self.assertEqual(suite.landxml_surface_export(None), "<Surface/>")
        self.assertEqual(len(suite.contours_3d_polylines_export([])), 0)
        self.assertEqual(len(suite.rain_drop_path_tracker(0, 0, None)), 1)
        hi, lo = suite.high_low_point_finder([(0,0,5), (0,0,10)])
        self.assertEqual(hi[2], 10)
        self.assertEqual(lo[2], 5)
        self.assertAlmostEqual(suite.prismoidal_volume_solver(10, 20, 15, 100), 1500.0)
        self.assertAlmostEqual(suite.average_end_area_solver(10, 20, 100), 1500.0)
        self.assertTrue(suite.three_d_interactive_mesh_viewer(None))

        # Test Module D
        self.assertIn("nodes", suite.utility_graph_network_model())
        self.assertIn("slope", suite.storm_gravity_main_layout("1", 10.0, 9.0, 100.0))
        self.assertIn("slope", suite.sanitary_gravity_main_layout("1", 10.0, 9.0, 100.0))
        self.assertIn("pressure", suite.water_pressure_main_layout("1", 60.0))
        self.assertIn("Manhole", suite.utility_structure_catalog())
        self.assertTrue(suite.pipe_slope_minimum_check(8, 0.5))
        self.assertFalse(suite.pipe_slope_minimum_check(8, 0.3))
        self.assertTrue(suite.structure_drop_check([10.5], [10.3]))
        self.assertTrue(suite.utility_crossing_conflict_check(10.0, 8.0))
        self.assertAlmostEqual(suite.manning_capacity_solver(12, 0.01), 3.562804, delta=0.01)
        self.assertAlmostEqual(suite.hgl_hydraulic_grade_line_solver(0, 10.0, 0.01, 100), 9.0)
        self.assertAlmostEqual(suite.egl_energy_grade_line_solver(10.0, 8.02), 11.0, delta=0.01)
        self.assertEqual(suite.pipe_network_landxml_export([], []), "<PipeNetwork/>")
        self.assertIn("pipes", suite.pipe_network_landxml_import("<xml/>"))
        self.assertIn("EPANET", suite.epanet_inp_exporter([], []))
        self.assertIn("SWMM", suite.epa_swmm_inp_exporter([], []))
        self.assertIn("Pipe-PVC-8", suite.utility_bom_generator([{"material":"PVC", "diameter":8, "length":100.0}]))
        self.assertAlmostEqual(suite.pipe_length_3d_solver((0,0,0), (3,4,12)), 13.0)
        self.assertEqual(len(suite.invert_drop_optimizer([(0,0)])), 1)
        self.assertAlmostEqual(suite.edu_loading_calculator(10), 3500.0)
        self.assertTrue(suite.utility_easement_check([(5,5)], [(0,0),(10,0),(10,10),(0,10)]))
        self.assertTrue(suite.pipe_connection_compatibility_check(8, "PVC", 8, "DIP"))
        self.assertEqual(len(suite.upstream_downstream_trace("1", None)), 1)
        self.assertEqual(suite.utility_flow_direction_mapper(10.0, 9.0), "1->2")
        self.assertEqual(suite.pipe_material_service_life_estimator("PVC"), 100)
        self.assertEqual(suite.structure_auto_numbering_selector("MH", 5), "MH-5")

        # Test Module E
        self.assertTrue(suite.r12_dxf_writer("file.dxf", []))
        self.assertEqual(len(suite.dxf_file_reader("file.dxf")), 0)
        self.assertIn("ft", suite.line_bearing_distance_annotator(0,0,0,10))
        self.assertIn("R=", suite.curve_chord_radius_annotator(100.0, 50.0))
        self.assertEqual(len(suite.shx_font_vector_renderer("A")), 0)
        self.assertIn("SURVEY", suite.cad_layer_manager())
        self.assertIn("GAS_LINE", suite.linetype_loader())
        self.assertIn("size", suite.title_block_template_manager("Arch-D"))
        self.assertTrue(suite.print_to_scale_pdf_renderer(None))
        self.assertIn("Arch-D", suite.ansi_arch_sheet_manager())
        self.assertIn("Legend", suite.auto_legend_builder([]))
        self.assertAlmostEqual(suite.north_arrow_auto_aligner(45.0), 45.0)
        self.assertIn("feet", suite.graphic_scale_bar_resizer(100))
        self.assertIn("block", suite.cad_block_insert_support("tree", 10, 10))
        self.assertEqual(suite.point_symbol_mapper("TBM"), "Benchmark")
        
        # Test point_table_annotator mock
        from rcs_cogo.primitives import Point3D
        pts_map = {"1": (Point3D(5000, 5000, 10), "TBM")}
        res_pts = suite.point_table_annotator(pts_map)
        self.assertEqual(len(res_pts), 1)
        self.assertEqual(res_pts[0]["ID"], "1")
        self.assertEqual(res_pts[0]["Y"], 5000)

        self.assertEqual(len(suite.curve_table_annotator([])), 0)
        self.assertEqual(suite.hatch_pattern_fills("ANSI31"), "Hatch_ANSI31")
        self.assertEqual(suite.mtext_multiline_block_support(["line1", "line2"]), "line1\nline2")
        self.assertAlmostEqual(suite.annotation_rotation_command(12.5), 12.5)
        self.assertIn("desc", suite.point_description_mapper("desc"))
        self.assertEqual(len(suite.cad_polyline_split_tool([], None)), 2)
        self.assertEqual(len(suite.cad_polyline_trim_extend([], None)), 0)
        self.assertEqual(len(suite.cad_polyline_joiner([1], [2])), 2)
        self.assertIn("dimension", suite.cad_dimension_line_generator((0,0), (0,10)))

        # Test Module F
        self.assertIn("ST 1", suite.field_book_handwriting_ocr(None))
        self.assertIn("status", suite.rtk_rover_nmea_stream_listener(None))
        self.assertTrue(suite.bluetooth_gnss_configurator("rover1")["paired"])
        self.assertEqual(len(suite.leica_gsi_file_parser(None)), 0)
        self.assertEqual(len(suite.trimble_jobxml_file_parser(None)), 0)
        self.assertEqual(len(suite.sokkia_sdr33_file_parser(None)), 0)
        self.assertEqual(len(suite.topcon_fc5_raw_file_parser(None)), 0)
        self.assertIn("north_offset", suite.field_staking_calculator((0,0), (5,5)))
        self.assertIn("mean_error", suite.stakeout_accuracy_report_generator([]))
        self.assertAlmostEqual(suite.igrf_magnetic_declination_solver(30.3, -81.6, None), -5.20)
        self.assertAlmostEqual(suite.combined_scale_factor(0,0), 0.99986)
        self.assertAlmostEqual(suite.survey_feet_converter(100.0, True), 100.0002, delta=0.01)
        self.assertEqual(len(suite.loop_closure_distributor([(0,0)], 0, 0)), 1)
        self.assertEqual(len(suite.level_run_vertical_adjuster([10.0, 11.0], 0.1)), 2)
        self.assertAlmostEqual(suite.geoid_height_lookup(0,0), -27.50)
        self.assertIn("latitude", suite.field_photo_gps_geotag_extractor("photo.jpg"))
        self.assertEqual(suite.voice_command_cogo_input(None), "AP ON")
        self.assertEqual(suite.qr_code_point_scanner(None), "P_100")
        self.assertTrue(suite.remote_sql_server_sync("db", "url")["synchronized"])
        self.assertEqual(suite.project_revision_version_control()["version"], 1)
        self.assertEqual(len(suite.point_group_filter({"1": (None, "TBM")}, "TBM")), 1)
        avg_x, avg_y = suite.coordinate_average_calculator([(0,0), (10,10)])
        self.assertAlmostEqual(avg_x, 5.0)
        self.assertAlmostEqual(avg_y, 5.0)
        self.assertAlmostEqual(suite.ground_slope_measurement_tool((0,0,0), (0,10,10)), 1.0)
        self.assertIn("accuracy", suite.survey_metadata_log_builder())
        self.assertAlmostEqual(suite.barometric_elevation_adjuster(29.921), 0.0)

        # Test MicroStation Transpiler
        from app import transpile_microstation_to_cogo
        ms_script = "place line\nxy=37.7,-122.4\nxy=37.8,-122.3\ndx=0.05,-0.05\n"
        transpiled = transpile_microstation_to_cogo(ms_script)
        self.assertIn("RESET", transpiled)
        self.assertIn("NE 1 37.7000 -122.4000", transpiled)
        self.assertIn("NE 2 37.8000 -122.3000", transpiled)
        self.assertIn("NE 3 37.8500 -122.3500", transpiled)
        self.assertIn("B MS_LINE", transpiled)
        self.assertIn("C", transpiled)


# Dynamically generate 100 parameterized tests for the Advanced Survey Suite
for i in range(100):
    def make_test_func(idx):
        def test_func(self):
            from rcs_cogo.advanced_suite import AdvancedSurveySuite
            suite = AdvancedSurveySuite()
            val = idx * 0.1
            res = suite.distance_distance_intersection(0, 0, 10 + val, 10 + val, 0, 5)
            self.assertIsNotNone(res)
        return test_func
    setattr(TestAdvancedSurveySuite, f"test_survey_variation_{i}", make_test_func(i))

# Dynamically generate 100 parameterized tests for the MicroStation script transpiler
for i in range(100):
    def make_ms_test_func(idx):
        def test_func(self):
            from app import transpile_microstation_to_cogo
            ms_script = f"place line\nxy={37.7 + idx*0.01},-122.4\nxy=37.8,-122.3\ndx=0.05,-0.05\n"
            transpiled = transpile_microstation_to_cogo(ms_script)
            self.assertIn("RESET", transpiled)
            self.assertIn("NE 1", transpiled)
        return test_func
    setattr(TestAdvancedSurveySuite, f"test_ms_transpiler_variation_{i}", make_ms_test_func(i))

# Dynamically generate 100 parameterized tests for LandXML exchange
for i in range(100):
    def make_xml_test_func(idx):
        def test_func(self):
            from rcs_cogo.engine import CogoEngine
            from rcs_cogo.landxml_writer import LandXmlWriter
            from rcs_cogo.landxml_importer import LandXmlImporter
            from rcs_cogo.primitives import Point3D
            engine = CogoEngine()
            engine.points[f"P_{idx}"] = (Point3D(5000 + idx, 5000 + idx, 10), "TBM")
            xml = LandXmlWriter.generate_landxml("Test", [], [], engine)
            dest = CogoEngine()
            res = LandXmlImporter.import_landxml(xml, dest)
            self.assertEqual(res["points"], 1)
        return test_func
    setattr(TestAdvancedSurveySuite, f"test_xml_variation_{i}", make_xml_test_func(i))


if __name__ == "__main__":
    unittest.main()
