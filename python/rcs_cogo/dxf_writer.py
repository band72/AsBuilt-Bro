import math
from typing import List, Tuple, Optional
from .primitives import Point3D

try:
    import ezdxf
    HAS_EZDXF = True
except ImportError:
    HAS_EZDXF = False

class RawDxfWriter:
    """
    Fallback DXF writer that produces standard AutoCAD R14-compatible DXF files.
    Matches the exact implementation logic of ProfessionalDxfWriter.cs.
    """
    def __init__(self):
        self.lines = []
        self.handle_counter = 0x100

    def _next_handle(self) -> str:
        self.handle_counter += 1
        return f"{self.handle_counter:X}"

    def _sanitize_layer(self, input_str: str) -> str:
        if not input_str:
            return "DEFAULT"
        invalid_chars = ['<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', '=', '`', ' ']
        result = input_str
        for c in invalid_chars:
            result = result.replace(c, '_')
        return result[:255]

    def begin(self):
        self.lines.extend(["0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1009", "0", "ENDSEC"])
        self._define_blocks()
        self.lines.extend(["0", "SECTION", "2", "ENTITIES"])

    def _define_blocks(self):
        self.lines.extend(["0", "SECTION", "2", "BLOCKS"])
        
        # Define Manhole Block
        self._define_block("MANHOLE")
        self._add_circle_raw(0.0, 0.0, 1.0, "0")
        self._add_text_raw("M", 0.0, 0.0, 0.8, "0")
        self._end_block()

        # Define Valve Block
        self._define_block("VALVE")
        self._add_line_raw(-0.5, -0.5, 0.5, 0.5, "0")
        self._add_line_raw(-0.5, 0.5, 0.5, -0.5, "0")
        self._add_line_raw(-0.5, -0.5, -0.5, 0.5, "0")
        self._add_line_raw(0.5, -0.5, 0.5, 0.5, "0")
        self._end_block()

        # Define Hydrant Block
        self._define_block("HYDRANT")
        self._add_circle_raw(0.0, 0.0, 0.8, "0")
        self._add_line_raw(-0.8, 0.0, 0.8, 0.0, "0")
        self._add_line_raw(0.0, -0.8, 0.0, 0.8, "0")
        self._end_block()

        # Define Meter Block
        self._define_block("METER")
        self._add_line_raw(-1.0, -1.0, 1.0, -1.0, "0")
        self._add_line_raw(1.0, -1.0, 1.0, 1.0, "0")
        self._add_line_raw(1.0, 1.0, -1.0, 1.0, "0")
        self._add_line_raw(-1.0, 1.0, -1.0, -1.0, "0")
        self._add_text_raw("Mtr", 0.0, -0.3, 0.6, "0")
        self._end_block()

        # Define Box / Vault Block
        self._define_block("BOX")
        self._add_line_raw(-1.5, -1.5, 1.5, -1.5, "0")
        self._add_line_raw(1.5, -1.5, 1.5, 1.5, "0")
        self._add_line_raw(1.5, 1.5, -1.5, 1.5, "0")
        self._add_line_raw(-1.5, 1.5, -1.5, -1.5, "0")
        self._add_line_raw(-1.5, -1.5, 1.5, 1.5, "0")
        self._add_line_raw(-1.5, 1.5, 1.5, -1.5, "0")
        self._end_block()

        self.lines.extend(["0", "ENDSEC"])

    def _define_block(self, name: str):
        self.lines.extend(["0", "BLOCK", "8", "0", "2", name, "70", "0", "10", "0.0", "20", "0.0", "30", "0.0"])

    def _end_block(self):
        self.lines.extend(["0", "ENDBLK"])

    def _add_line_raw(self, x1: float, y1: float, x2: float, y2: float, layer: str, color: int = 256):
        self.lines.extend(["0", "LINE", "8", self._sanitize_layer(layer)])
        if color != 256:
            self.lines.extend(["62", str(color)])
        self.lines.extend(["10", f"{x1:.4f}", "20", f"{y1:.4f}", "11", f"{x2:.4f}", "21", f"{y2:.4f}"])

    def _add_circle_raw(self, x: float, y: float, r: float, layer: str):
        self.lines.extend(["0", "CIRCLE", "8", self._sanitize_layer(layer), "10", f"{x:.4f}", "20", f"{y:.4f}", "40", f"{r:.4f}"])

    def _add_text_raw(self, text: str, x: float, y: float, height: float, layer: str, rotation: float = 0.0, color: int = 256):
        self.lines.extend(["0", "TEXT", "8", self._sanitize_layer(layer)])
        if color != 256:
            self.lines.extend(["62", str(color)])
        self.lines.extend(["10", f"{x:.4f}", "20", f"{y:.4f}", "40", f"{height:.4f}", "1", text.replace("\n", " ")])
        if abs(rotation) > 0.001:
            self.lines.extend(["50", f"{rotation:.4f}"])

    def add_point(self, p: Point3D, layer: str = "POINTS"):
        self.lines.extend(["0", "POINT", "8", self._sanitize_layer(layer), "10", f"{p.easting:.4f}", "20", f"{p.northing:.4f}", "30", f"{p.elevation:.4f}"])

    def add_line(self, p1: Point3D, p2: Point3D, layer: str, color: int = 256):
        self._add_line_raw(p1.easting, p1.northing, p2.easting, p2.northing, layer, color)

    def add_polyline(self, pts: List[Tuple[float, float]], layer: str, color: int = 256):
        if len(pts) < 2:
            return
        for i in range(len(pts) - 1):
            self._add_line_raw(pts[i][0], pts[i][1], pts[i+1][0], pts[i+1][1], layer, color)

    def add_text(self, text: str, x: float, y: float, height: float, layer: str, rotation: float = 0.0, color: int = 256):
        self._add_text_raw(text, x, y, height, layer, rotation, color)

    def insert_block(self, block_name: str, x: float, y: float, scale: float, layer: str, color: int = 256):
        self.lines.extend(["0", "INSERT", "8", self._sanitize_layer(layer)])
        if color != 256:
            self.lines.extend(["62", str(color)])
        self.lines.extend(["2", block_name, "10", f"{x:.4f}", "20", f"{y:.4f}", "41", f"{scale:.4f}", "42", f"{scale:.4f}", "43", f"{scale:.4f}"])

    def end(self):
        self.lines.extend(["0", "ENDSEC", "0", "EOF"])

    def save(self, path: str):
        with open(path, "w") as f:
            f.write("\n".join(self.lines) + "\n")


class EzdxfWriter:
    """
    Professional DXF writer using the standard 'ezdxf' Python package.
    Used when the ezdxf package is installed locally.
    """
    def __init__(self):
        self.doc = ezdxf.new('R2010')
        self.msp = self.doc.modelspace()
        self._define_blocks()

    def _define_blocks(self):
        # 1. Manhole
        mh_blk = self.doc.blocks.new(name='MANHOLE')
        mh_blk.add_circle((0, 0), radius=1.0, dxfattribs={'layer': '0'})
        mh_blk.add_text('M', dxfattribs={'layer': '0', 'height': 0.8, 'halign': 1, 'valign': 2}).set_placement((0, 0))

        # 2. Valve
        val_blk = self.doc.blocks.new(name='VALVE')
        val_blk.add_line((-0.5, -0.5), (0.5, 0.5), dxfattribs={'layer': '0'})
        val_blk.add_line((-0.5, 0.5), (0.5, -0.5), dxfattribs={'layer': '0'})
        val_blk.add_line((-0.5, -0.5), (-0.5, 0.5), dxfattribs={'layer': '0'})
        val_blk.add_line((0.5, -0.5), (0.5, 0.5), dxfattribs={'layer': '0'})

        # 3. Hydrant
        hyd_blk = self.doc.blocks.new(name='HYDRANT')
        hyd_blk.add_circle((0, 0), radius=0.8, dxfattribs={'layer': '0'})
        hyd_blk.add_line((-0.8, 0), (0.8, 0), dxfattribs={'layer': '0'})
        hyd_blk.add_line((0, -0.8), (0, 0.8), dxfattribs={'layer': '0'})

        # 4. Meter Box
        mtr_blk = self.doc.blocks.new(name='METER')
        mtr_blk.add_line((-1, -1), (1, -1), dxfattribs={'layer': '0'})
        mtr_blk.add_line((1, -1), (1, 1), dxfattribs={'layer': '0'})
        mtr_blk.add_line((1, 1), (-1, 1), dxfattribs={'layer': '0'})
        mtr_blk.add_line((-1, 1), (-1, -1), dxfattribs={'layer': '0'})
        mtr_blk.add_text('Mtr', dxfattribs={'layer': '0', 'height': 0.6, 'halign': 1, 'valign': 2}).set_placement((0, -0.3))

        # 5. Box/Vault
        box_blk = self.doc.blocks.new(name='BOX')
        box_blk.add_line((-1.5, -1.5), (1.5, -1.5), dxfattribs={'layer': '0'})
        box_blk.add_line((1.5, -1.5), (1.5, 1.5), dxfattribs={'layer': '0'})
        box_blk.add_line((1.5, 1.5), (-1.5, 1.5), dxfattribs={'layer': '0'})
        box_blk.add_line((-1.5, 1.5), (-1.5, -1.5), dxfattribs={'layer': '0'})
        box_blk.add_line((-1.5, -1.5), (1.5, 1.5), dxfattribs={'layer': '0'})
        box_blk.add_line((-1.5, 1.5), (1.5, -1.5), dxfattribs={'layer': '0'})

    def add_point(self, p: Point3D, layer: str = "POINTS"):
        self.msp.add_point((p.easting, p.northing, p.elevation), dxfattribs={'layer': layer})

    def add_line(self, p1: Point3D, p2: Point3D, layer: str, color: int = 256):
        attribs = {'layer': layer}
        if color != 256:
            attribs['color'] = color
        self.msp.add_line((p1.easting, p1.northing), (p2.easting, p2.northing), dxfattribs=attribs)

    def add_polyline(self, pts: List[Tuple[float, float]], layer: str, color: int = 256):
        attribs = {'layer': layer}
        if color != 256:
            attribs['color'] = color
        self.msp.add_lwpolyline(pts, dxfattribs=attribs)

    def add_text(self, text: str, x: float, y: float, height: float, layer: str, rotation: float = 0.0, color: int = 256):
        attribs = {'layer': layer, 'height': height, 'rotation': rotation}
        if color != 256:
            attribs['color'] = color
        self.msp.add_text(text.replace("\n", " "), dxfattribs=attribs).set_placement((x, y))

    def insert_block(self, block_name: str, x: float, y: float, scale: float, layer: str, color: int = 256):
        attribs = {'layer': layer, 'xscale': scale, 'yscale': scale, 'zscale': scale}
        if color != 256:
            attribs['color'] = color
        self.msp.add_blockref(block_name, (x, y), dxfattribs=attribs)

    def save(self, path: str):
        self.doc.saveas(path)


# Unified Factory Writer class
class ProfessionalDxfWriter:
    def __init__(self):
        if HAS_EZDXF:
            self.writer = EzdxfWriter()
        else:
            self.writer = RawDxfWriter()
            self.writer.begin()

    def add_point(self, p: Point3D, layer: str = "POINTS"):
        self.writer.add_point(p, layer)

    def add_line(self, p1: Point3D, p2: Point3D, layer: str, color: int = 256):
        self.writer.add_line(p1, p2, layer, color)

    def add_polyline(self, pts: List[Tuple[float, float]], layer: str, color: int = 256):
        self.writer.add_polyline(pts, layer, color)

    def add_text(self, text: str, x: float, y: float, height: float, layer: str, rotation: float = 0.0, color: int = 256):
        self.writer.add_text(text, x, y, height, layer, rotation, color)

    def insert_block(self, block_name: str, x: float, y: float, scale: float, layer: str, color: int = 256):
        self.writer.insert_block(block_name, x, y, scale, layer, color)

    def save(self, path: str):
        if not HAS_EZDXF:
            self.writer.end()
        self.writer.save(path)
