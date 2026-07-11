import sqlite3
import os
from typing import Optional
from .engine import CogoEngine, PipeRun, PipeStructure
from .primitives import Point3D

class SubdivisionDatabase:
    @staticmethod
    def initialize_db(db_path: str):
        """
        Creates all required tables if they do not exist.
        """
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # 1. Core Coordinate points table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS Points (
                PointId TEXT PRIMARY KEY,
                Northing REAL NOT NULL,
                Easting REAL NOT NULL,
                Elevation REAL DEFAULT 0.0,
                Description TEXT
            )
        """)
        
        # 2. Pipe Runs table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS PipeRuns (
                Id TEXT PRIMARY KEY,
                FromPointId TEXT NOT NULL,
                ToPointId TEXT NOT NULL,
                Diameter REAL,
                Material TEXT,
                InvertStart REAL,
                InvertEnd REAL,
                Slope REAL,
                Type TEXT
            )
        """)
        
        # 3. Pipe Structures table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS PipeStructures (
                Id TEXT PRIMARY KEY,
                PointId TEXT NOT NULL,
                Type TEXT NOT NULL
            )
        """)
        
        # 4. JEA Seeds tables
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS CogoCodes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LocalCode TEXT NOT NULL,
                SystemCode TEXT,
                Description TEXT
            )
        """)
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS Materials (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PartKey TEXT,
                Discipline TEXT,
                FeatureType TEXT,
                Size TEXT,
                Material TEXT
            )
        """)
        
        conn.commit()
        conn.close()

    @staticmethod
    def save_project(db_path: str, engine: CogoEngine):
        """
        Saves all points, pipe runs, and structures in the current workspace session.
        """
        SubdivisionDatabase.initialize_db(db_path)
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Clear existing workspace entries
        cursor.execute("DELETE FROM Points")
        cursor.execute("DELETE FROM PipeRuns")
        cursor.execute("DELETE FROM PipeStructures")
        
        # Save points
        for pt_id, (pt, desc) in engine.points.items():
            cursor.execute("INSERT INTO Points (PointId, Northing, Easting, Elevation, Description) VALUES (?, ?, ?, ?, ?)",
                           (pt_id, pt.northing, pt.easting, pt.elevation, desc))
            
        # Save pipe runs
        for r in engine.pipe_runs:
            cursor.execute("INSERT INTO PipeRuns (Id, FromPointId, ToPointId, Diameter, Material, InvertStart, InvertEnd, Slope, Type) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                           (r.id, r.from_point_id, r.to_point_id, r.diameter, r.material, r.invert_start, r.invert_end, r.slope, r.type))
            
        # Save structures
        for s in engine.pipe_structures:
            cursor.execute("INSERT INTO PipeStructures (Id, PointId, Type) VALUES (?, ?, ?)",
                           (s.id, s.point_id, s.type))
            
        conn.commit()
        conn.close()

    @staticmethod
    def load_project(db_path: str, engine: CogoEngine):
        """
        Loads points, pipe runs, and structures from SQLite database.
        """
        if not os.path.exists(db_path):
            raise FileNotFoundError(f"SQLite project database '{db_path}' not found.")
            
        SubdivisionDatabase.initialize_db(db_path)
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Reset current workspace state
        engine.points.clear()
        engine.pipe_runs.clear()
        engine.pipe_structures.clear()
        engine.figures.clear()
        engine.active_figure = None
        
        # Load Points
        cursor.execute("SELECT PointId, Northing, Easting, Elevation, Description FROM Points")
        for row in cursor.fetchall():
            pt_id, n, e, z, desc = row
            engine.add_point(pt_id, Point3D(n, e, z), desc)
            
        # Load Pipe Runs
        cursor.execute("SELECT Id, FromPointId, ToPointId, Diameter, Material, InvertStart, InvertEnd, Slope, Type FROM PipeRuns")
        for row in cursor.fetchall():
            r_id, from_pt, to_pt, diam, mat, inv_s, inv_e, slope, r_type = row
            run = PipeRun(
                run_id=r_id,
                run_type=r_type,
                figure_name=f"{r_type}-L0",
                from_pt=from_pt,
                to_pt=to_pt,
                diameter=diam,
                material=mat,
                invert_start=inv_s,
                invert_end=inv_e,
                slope=slope,
                line_no=0
            )
            engine.pipe_runs.append(run)
            
        # Load Structures
        cursor.execute("SELECT Id, PointId, Type FROM PipeStructures")
        for row in cursor.fetchall():
            s_id, pt_id, s_type = row
            engine.pipe_structures.append(PipeStructure(s_id, pt_id, s_type, 0))
            
        conn.close()
