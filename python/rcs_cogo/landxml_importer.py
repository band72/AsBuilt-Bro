import xml.etree.ElementTree as ET
import math
import uuid
from .primitives import Point3D
from .engine import Figure

class LandXmlImporter:
    @staticmethod
    def import_landxml(xml_content: str, engine) -> dict:
        """
        Imports points, parcels, and alignments from a LandXML version 1.2 compliant string
        into the CogoEngine workspace database.
        """
        # Parse XML tree
        root = ET.fromstring(xml_content)
        
        # Remove namespace prefixes to make parsing simple
        for elem in root.iter():
            if elem.tag.startswith('{'):
                elem.tag = elem.tag.split('}', 1)[1]
        
        points_imported = 0
        parcels_imported = 0
        alignments_imported = 0
        
        next_pt_idx = [1]
        
        def find_or_create_point(n_str, e_str):
            n = float(n_str)
            e = float(e_str)
            
            # Check existing within 0.01 tolerance
            for pt_id, (pt, desc) in engine.points.items():
                if abs(pt.northing - n) < 0.01 and abs(pt.easting - e) < 0.01:
                    return pt_id
            
            # Create new point
            while f"P_{next_pt_idx[0]}" in engine.points:
                next_pt_idx[0] += 1
            pt_id = f"P_{next_pt_idx[0]}"
            engine.add_point(pt_id, Point3D(n, e, 0.0), "Imported")
            return pt_id

        def interpolate_arc(start_n, start_e, center_n, center_e, end_n, end_e, rot):
            r = math.sqrt((start_n - center_n)**2 + (start_e - center_e)**2)
            theta_s = math.atan2(start_e - center_e, start_n - center_n)
            theta_e = math.atan2(end_e - center_e, end_n - center_n)
            
            delta = theta_e - theta_s
            is_cw = (rot == 'cw')
            if is_cw:
                if delta > 0:
                    delta -= 2 * math.pi
            else:
                if delta < 0:
                    delta += 2 * math.pi
            
            segments = 12
            pts = []
            for i in range(segments + 1):
                frac = float(i) / segments
                theta = theta_s + frac * delta
                y = center_n + r * math.cos(theta)
                x = center_e + r * math.sin(theta)
                pts.append((y, x))
            return pts

        # 1. Parse CgPoints
        for cg_points in root.findall('CgPoints'):
            for cg_pt in cg_points.findall('CgPoint'):
                pt_id = cg_pt.get('name')
                desc = cg_pt.get('desc') or "CgPoint"
                if not pt_id:
                    continue
                coords_str = cg_pt.text.strip().split() if cg_pt.text else []
                if len(coords_str) >= 2:
                    try:
                        n = float(coords_str[0])
                        e = float(coords_str[1])
                        z = float(coords_str[2]) if len(coords_str) >= 3 else 0.0
                        engine.add_point(pt_id, Point3D(n, e, z), desc)
                        points_imported += 1
                    except ValueError:
                        continue

        # 2. Parse Parcels
        for parcels_elem in root.findall('Parcels'):
            for parcel in parcels_elem.findall('Parcel'):
                name = parcel.get('name') or f"Parcel_{uuid.uuid4().hex[:6]}"
                fig = Figure(name)
                
                coord_geom = parcel.find('CoordGeom')
                if coord_geom is not None:
                    # Process lines
                    for line in coord_geom.findall('Line'):
                        start = line.find('Start')
                        end = line.find('End')
                        if start is not None and end is not None:
                            s_coords = start.text.strip().split() if start.text else []
                            e_coords = end.text.strip().split() if end.text else []
                            if len(s_coords) >= 2 and len(e_coords) >= 2:
                                s_id = find_or_create_point(s_coords[0], s_coords[1])
                                e_id = find_or_create_point(e_coords[0], e_coords[1])
                                if not fig.point_ids or fig.point_ids[-1] != s_id:
                                    fig.point_ids.append(s_id)
                                fig.point_ids.append(e_id)
                    
                    # Process curves
                    for curve in coord_geom.findall('Curve'):
                        start = curve.find('Start')
                        center = curve.find('Center')
                        end = curve.find('End')
                        rot = curve.get('rot') or 'cw'
                        if start is not None and center is not None and end is not None:
                            s_c = start.text.strip().split() if start.text else []
                            c_c = center.text.strip().split() if center.text else []
                            e_c = end.text.strip().split() if end.text else []
                            if len(s_c) >= 2 and len(c_c) >= 2 and len(e_c) >= 2:
                                try:
                                    arc_pts = interpolate_arc(
                                        float(s_c[0]), float(s_c[1]),
                                        float(c_c[0]), float(c_c[1]),
                                        float(e_c[0]), float(e_c[1]),
                                        rot
                                    )
                                    for pt_y, pt_x in arc_pts:
                                        pt_id = find_or_create_point(str(pt_y), str(pt_x))
                                        if not fig.point_ids or fig.point_ids[-1] != pt_id:
                                            fig.point_ids.append(pt_id)
                                except ValueError:
                                    continue
                
                if fig.point_ids:
                    engine.add_figure(fig)
                    parcels_imported += 1

        # 3. Parse Alignments
        for alignments_elem in root.findall('Alignments'):
            for alignment in alignments_elem.findall('Alignment'):
                name = alignment.get('name') or f"Alignment_{uuid.uuid4().hex[:6]}"
                fig = Figure(name)
                
                coord_geom = alignment.find('CoordGeom')
                if coord_geom is not None:
                    # Process lines
                    for line in coord_geom.findall('Line'):
                        start = line.find('Start')
                        end = line.find('End')
                        if start is not None and end is not None:
                            s_coords = start.text.strip().split() if start.text else []
                            e_coords = end.text.strip().split() if end.text else []
                            if len(s_coords) >= 2 and len(e_coords) >= 2:
                                s_id = find_or_create_point(s_coords[0], s_coords[1])
                                e_id = find_or_create_point(e_coords[0], e_coords[1])
                                if not fig.point_ids or fig.point_ids[-1] != s_id:
                                    fig.point_ids.append(s_id)
                                fig.point_ids.append(e_id)
                    
                    # Process curves
                    for curve in coord_geom.findall('Curve'):
                        start = curve.find('Start')
                        center = curve.find('Center')
                        end = curve.find('End')
                        rot = curve.get('rot') or 'cw'
                        if start is not None and center is not None and end is not None:
                            s_c = start.text.strip().split() if start.text else []
                            c_c = center.text.strip().split() if center.text else []
                            e_c = end.text.strip().split() if end.text else []
                            if len(s_c) >= 2 and len(c_c) >= 2 and len(e_c) >= 2:
                                try:
                                    arc_pts = interpolate_arc(
                                        float(s_c[0]), float(s_c[1]),
                                        float(c_c[0]), float(c_c[1]),
                                        float(e_c[0]), float(e_c[1]),
                                        rot
                                    )
                                    for pt_y, pt_x in arc_pts:
                                        pt_id = find_or_create_point(str(pt_y), str(pt_x))
                                        if not fig.point_ids or fig.point_ids[-1] != pt_id:
                                            fig.point_ids.append(pt_id)
                                except ValueError:
                                    continue
                
                if fig.point_ids:
                    engine.add_figure(fig)
                    alignments_imported += 1

        return {
            "points": points_imported,
            "parcels": parcels_imported,
            "alignments": alignments_imported
        }
