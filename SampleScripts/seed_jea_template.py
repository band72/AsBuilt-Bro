import os
import sys
import sqlite3
import pandas as pd
import uuid
from datetime import datetime

# ---------------------------------------------------------
# RCS COGO Enterprise Modern - JEA Excel -> SQLite Importer
# ---------------------------------------------------------

def connect_db():
    local_app_data = os.environ.get('LOCALAPPDATA', '')
    db_path = os.path.join(local_app_data, 'rcs_installed_assets.db')
    
    # Check if we can find it, otherwise assume local execution environment paths
    if not os.path.exists(db_path):
        print(f"ERROR: Cannot locate enterprise database at {db_path}")
        sys.exit(1)
        
    return sqlite3.connect(db_path)

def seed_project(excel_path):
    print(f"Loading data from: {excel_path}...")
    try:
        xls = pd.ExcelFile(excel_path)
    except Exception as e:
        print(f"Failed to read Excel file: {e}")
        return

    # Create a new Project Guid and Insert the Profile
    project_id = str(uuid.uuid4()).upper()
    proj_name = f"JEA Reverse Engineered - {datetime.now().strftime('%Y-%m-%d %H:%M')}"
    
    conn = connect_db()
    cursor = conn.cursor()
    
    print(f"Generating New Project: {proj_name}")
    print(f"Project ID: {project_id}")
    
    try:
        # 1. Establish the Project Record
        cursor.execute("""
            INSERT INTO Projects (Id, ProjectName, EngineerOfRecord, DataSourcedFrom)
            VALUES (?, ?, ?, ?)
        """, (project_id, proj_name, "Admin Python Seed", excel_path))
        
        # 2. Extract Sewer Manholes -> Manholes table
        if 'Sewer Manhole' in xls.sheet_names:
            df = pd.read_excel(xls, sheet_name='Sewer Manhole')
            print(f"Processing {len(df)} Sewer Manholes...")
            for _, row in df.iterrows():
                # Avoid inserting blank rows
                if pd.isna(row.get('Subtype')): continue
                
                cursor.execute("""
                    INSERT INTO Manholes (
                        Id, ProjectId, PartKey, Subtype, FacilityOwner, 
                        ManholeType, DropType, Size, LiningMaterial, 
                        RimElevation, Depth,
                        Northing, Easting, Latitude, Longitude
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    str(uuid.uuid4()).upper(), project_id,
                    str(row.get('Manhole Number', '')),
                    str(row.get('Subtype', 'Sewer Manhole')),
                    str(row.get('Facility Owner', 'JEA')),
                    str(row.get('Manhole Type', '')),
                    str(row.get('Manhole Drop Type', '')),
                    str(row.get('Manhole Size (Feet)', '')),
                    str(row.get('Lining Material', '')),
                    row.get('Rim Elevation (Feet)', 0.0) if pd.notna(row.get('Rim Elevation (Feet)')) else 0.0,
                    row.get('Depth (Feet)', 0.0) if pd.notna(row.get('Depth (Feet)')) else 0.0,
                    row.get('GPS Y Coord', 0.0) if pd.notna(row.get('GPS Y Coord')) else 0.0,
                    row.get('GPS X Coord', 0.0) if pd.notna(row.get('GPS X Coord')) else 0.0,
                    row.get('GPS Latitude', 0.0) if pd.notna(row.get('GPS Latitude')) else 0.0,
                    row.get('GPS Longitude', 0.0) if pd.notna(row.get('GPS Longitude')) else 0.0
                ))

        # 3. Extract Water Fittings -> WaterFittings table
        if 'Water Fitting' in xls.sheet_names:
            df = pd.read_excel(xls, sheet_name='Water Fitting')
            print(f"Processing {len(df)} Water Fittings...")
            for _, row in df.iterrows():
                if pd.isna(row.get('Subtype')): continue
                cursor.execute("""
                    INSERT INTO WaterFittings (
                        Id, ProjectId, PartKey, Subtype, FacilityOwner, 
                        Size, SizeSecondary, Material, TopElevation,
                        Northing, Easting, Latitude, Longitude
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    str(uuid.uuid4()).upper(), project_id,
                    str(row.get('Fitting Number', '')),
                    str(row.get('Subtype', 'Water Fitting')),
                    str(row.get('Facility Owner', 'JEA')),
                    str(row.get('Fitting Size Primary (Inches)', '')),
                    str(row.get('Fitting Size Reducer (Inches)', '')),
                    str(row.get('Material', '')),
                    row.get('Elevation (Feet)', 0.0) if pd.notna(row.get('Elevation (Feet)')) else 0.0,
                    row.get('GPS Y Coord', 0.0) if pd.notna(row.get('GPS Y Coord')) else 0.0,
                    row.get('GPS X Coord', 0.0) if pd.notna(row.get('GPS X Coord')) else 0.0,
                    row.get('GPS Latitude', 0.0) if pd.notna(row.get('GPS Latitude')) else 0.0,
                    row.get('GPS Longitude', 0.0) if pd.notna(row.get('GPS Longitude')) else 0.0
                ))

        # 4. Extract Sewer Fittings -> WWFittings table
        if 'Sewer Fitting' in xls.sheet_names:
            df = pd.read_excel(xls, sheet_name='Sewer Fitting')
            print(f"Processing {len(df)} Sewer Fittings...")
            for _, row in df.iterrows():
                if pd.isna(row.get('Subtype')): continue
                cursor.execute("""
                    INSERT INTO WWFittings (
                        Id, ProjectId, PartKey, Subtype, FacilityOwner, 
                        Size, SizeSecondary, Material, TopElevation,
                        Northing, Easting, Latitude, Longitude
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    str(uuid.uuid4()).upper(), project_id,
                    str(row.get('Fitting Number', '')),
                    str(row.get('Subtype', 'Sewer Fitting')),
                    str(row.get('Facility Owner', 'JEA')),
                    str(row.get('Fitting Size Primary (Inches)', '')),
                    str(row.get('Fitting Size Reducer (Inches)', '')),
                    str(row.get('Material', '')),
                    row.get('Elevation (Feet)', 0.0) if pd.notna(row.get('Elevation (Feet)')) else 0.0,
                    row.get('GPS Y Coord', 0.0) if pd.notna(row.get('GPS Y Coord')) else 0.0,
                    row.get('GPS X Coord', 0.0) if pd.notna(row.get('GPS X Coord')) else 0.0,
                    row.get('GPS Latitude', 0.0) if pd.notna(row.get('GPS Latitude')) else 0.0,
                    row.get('GPS Longitude', 0.0) if pd.notna(row.get('GPS Longitude')) else 0.0
                ))
        
        # 5. Hydrants
        if 'Water Hydrant' in xls.sheet_names:
            df = pd.read_excel(xls, sheet_name='Water Hydrant')
            print(f"Processing {len(df)} Water Hydrants...")
            for _, row in df.iterrows():
                if pd.isna(row.get('Subtype')): continue
                cursor.execute("""
                    INSERT INTO WaterHydrants (
                        Id, ProjectId, PartKey, Subtype, FacilityOwner, 
                        Northing, Easting, Latitude, Longitude
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    str(uuid.uuid4()).upper(), project_id,
                    str(row.get('Hydrant Number', '')),
                    str(row.get('Subtype', 'Fire Hydrant')),
                    str(row.get('Facility Owner', 'JEA')),
                    row.get('GPS Y Coord', 0.0) if pd.notna(row.get('GPS Y Coord')) else 0.0,
                    row.get('GPS X Coord', 0.0) if pd.notna(row.get('GPS X Coord')) else 0.0,
                    row.get('GPS Latitude', 0.0) if pd.notna(row.get('GPS Latitude')) else 0.0,
                    row.get('GPS Longitude', 0.0) if pd.notna(row.get('GPS Longitude')) else 0.0
                ))
                
        # Commit the transaction
        conn.commit()
        print("\nSUCCESS! JEA Asset Template fully reverse engineered into the backend.")
        print("Please open 'RCS COGO Enterprise', go to Project -> Load, select the new project,")
        print("and then navigate to the 'Tables' menu to view the Extracted Assets and export DXF tables.")
        
    except Exception as e:
        print(f"Database error occurred: {e}")
        conn.rollback()
    finally:
        conn.close()

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: py seed_jea_template.py <path_to_excel_file>")
        sys.exit(1)
        
    seed_project(sys.argv[1])
