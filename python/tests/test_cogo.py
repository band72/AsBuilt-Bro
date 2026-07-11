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


if __name__ == "__main__":
    unittest.main()
