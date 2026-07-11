import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import math
import os
from typing import Tuple
from rcs_cogo.engine import CogoEngine, PipeRun, PipeStructure
from rcs_cogo.validation import JeaValidationService, JeaIssue
from rcs_cogo.projection import StatePlaneProjection
from rcs_cogo.ai_vision import AiVisionExtractionEngine, BowTieChecker
from rcs_cogo.primitives import Point3D
from rcs_cogo.geometry import GeometryEngine
from rcs_cogo.dxf_writer import ProfessionalDxfWriter
from rcs_cogo.database import SubdivisionDatabase

# Visual design constants
COLOR_BG = "#1e1e1e"
COLOR_SIDEBAR = "#252526"
COLOR_ACCENT = "#007acc"
COLOR_TEXT = "#d4d4d4"
COLOR_LOG_BG = "#1e1e1e"
COLOR_CANVAS_BG = "#151515"
import random

def transpile_microstation_to_cogo(ms_script):
    lines = ms_script.split('\n')
    cogo_lines = [
        '// ====================================================================',
        '//  TRANSPILED FROM BENTLEY MICROSTATION KEY-IN SCRIPT',
        '// ====================================================================',
        'RESET'
    ]
    pt_counter = 1
    active_figure = None
    current_occupied = None
    occupied_coords = None

    for line in lines:
        cleaned = line.strip()
        if not cleaned or cleaned.startswith('#') or cleaned.startswith('//') or cleaned.startswith(';'):
            continue

        lower = cleaned.lower()

        if lower.startswith('active color') or lower.startswith('co='):
            cogo_lines.append(f'// Color style changed: {cleaned}')
            continue

        if lower.startswith('place line') or lower.startswith('place smartline'):
            if active_figure:
                cogo_lines.append('C')
            suffix = "".join(random.choices("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", k=4))
            active_figure = f'MS_LINE_{suffix}'
            cogo_lines.append(f'B {active_figure}')
            continue

        if lower.startswith('xy='):
            coords_part = cleaned[3:].strip()
            coords = coords_part.split(',')
            if len(coords) >= 2:
                try:
                    x = float(coords[0])
                    y = float(coords[1])
                    z = float(coords[2]) if len(coords) > 2 else 0.0
                    pt_id = pt_counter
                    pt_counter += 1
                    cogo_lines.append(f'NE {pt_id} {x:.4f} {y:.4f} {z:.2f} "MicroStation Point"')
                    current_occupied = pt_id
                    occupied_coords = (x, y, z)
                    if active_figure:
                        cogo_lines.append(f'L {pt_id}')
                except ValueError:
                    pass
            continue

        if lower.startswith('dx='):
            coords_part = cleaned[3:].strip()
            coords = coords_part.split(',')
            if len(coords) >= 2 and current_occupied is not None and occupied_coords is not None:
                try:
                    dx = float(coords[0])
                    dy = float(coords[1])
                    pt_id = pt_counter
                    pt_counter += 1
                    nx = occupied_coords[0] + dx
                    ny = occupied_coords[1] + dy
                    nz = occupied_coords[2]
                    cogo_lines.append(f'NE {pt_id} {nx:.4f} {ny:.4f} {nz:.2f} "Offset point"')
                    current_occupied = pt_id
                    occupied_coords = (nx, ny, nz)
                    if active_figure:
                        cogo_lines.append(f'L {pt_id}')
                except ValueError:
                    pass
            continue

        if lower.startswith('active angle') or lower.startswith('aa='):
            parts = cleaned.split('=')
            val = parts[1] if len(parts) > 1 else cleaned.replace('active angle', '').strip()
            cogo_lines.append(f'// Active rotation angle: {val}')
            continue

        cogo_lines.append(f'// MS_KEYIN: {cleaned}')

    if active_figure:
        cogo_lines.append('C')

    return '\n'.join(cogo_lines)

class CogoGuiApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("RCS AsBuilt-Bro Utility COGO & Vision Engine")
        self.root.geometry("1200x800")
        
        self.style = ttk.Style()
        self.style.theme_use("clam")
        
        self.style.configure(".", background=COLOR_BG, foreground=COLOR_TEXT)
        self.style.configure("TFrame", background=COLOR_BG)
        self.style.configure("Sidebar.TFrame", background=COLOR_SIDEBAR)
        
        self.style.configure("TLabel", background=COLOR_BG, foreground=COLOR_TEXT, font=("Segoe UI", 10))
        self.style.configure("Header.TLabel", font=("Segoe UI", 13, "bold"), background=COLOR_BG, foreground=COLOR_TEXT)
        self.style.configure("SidebarHeader.TLabel", font=("Segoe UI", 11, "bold"), background=COLOR_SIDEBAR, foreground=COLOR_TEXT)
        
        self.style.configure("TButton", background=COLOR_ACCENT, foreground="#ffffff", font=("Segoe UI", 9, "bold"), borderwidth=0)
        self.style.map("TButton", background=[("active", "#0062a3")])
        
        self.style.configure("TNotebook", background=COLOR_BG, tabmargins=[2, 5, 2, 0])
        self.style.configure("TNotebook.Tab", background=COLOR_SIDEBAR, foreground=COLOR_TEXT, padding=[12, 4], font=("Segoe UI", 9))
        self.style.map("TNotebook.Tab", background=[("selected", COLOR_BG)], foreground=[("selected", COLOR_TEXT)])
        
        # Sleek Dark Mode Entry Style to prevent white-on-white contrast issues
        self.style.configure("TEntry", fieldbackground="#2d2d2d", foreground="#ffffff", insertcolor="#ffffff")
        
        # Style Treeview tables to have highly legible black text on white cells
        self.style.configure("Treeview", foreground="#000000", fieldbackground="#ffffff", background="#ffffff")
        self.style.configure("Treeview.Heading", foreground="#000000")
        
        self.engine = CogoEngine()
        self.api_key_var = tk.StringVar(value=os.environ.get("GEMINI_API_KEY", ""))
        self.ai_provider_var = tk.StringVar(value="gemini")
        self.gemini_model_var = tk.StringVar(value="gemini-2.0-flash")
        self.ollama_host_var = tk.StringVar(value="http://localhost:11434")
        self.ollama_model_var = tk.StringVar(value="llama3.2-vision")
        self.zoom_level = 1.0
        self.pan_dx = 0.0
        self.pan_dy = 0.0
        self.drag_start_x = 0
        self.drag_start_y = 0

        
        self._build_ui()
        self._load_sample_script()

    def _build_ui(self):
        # Paned Window Splitter
        self.paned = tk.PanedWindow(self.root, orient=tk.HORIZONTAL, bg="#333333", bd=0, sashwidth=4)
        self.paned.pack(fill=tk.BOTH, expand=True)

        # Left Panel (Editor + File Operations)
        self.left_frame = ttk.Frame(self.paned, style="Sidebar.TFrame")
        self.paned.add(self.left_frame, minsize=380, stretch="always")

        lbl_editor = ttk.Label(self.left_frame, text="Utility Run Script Compiler", style="SidebarHeader.TLabel")
        lbl_editor.pack(anchor=tk.W, padx=10, pady=8)

        # Script Format Radio Buttons
        self.script_format = tk.StringVar(value="cogo")
        f_format = ttk.Frame(self.left_frame, style="Sidebar.TFrame")
        f_format.pack(anchor=tk.W, padx=10, pady=2)
        
        lbl_fmt = ttk.Label(f_format, text="Format:", font=("Segoe UI", 9, "bold"), foreground="#bbbbbb")
        lbl_fmt.pack(side=tk.LEFT, padx=(0, 6))
        
        rb_cogo = ttk.Radiobutton(f_format, text="JEA COGO", variable=self.script_format, value="cogo", command=self.handle_format_toggle)
        rb_cogo.pack(side=tk.LEFT, padx=4)
        
        rb_ms = ttk.Radiobutton(f_format, text="MicroStation", variable=self.script_format, value="microstation", command=self.handle_format_toggle)
        rb_ms.pack(side=tk.LEFT, padx=4)

        # Text area
        self.editor = tk.Text(
            self.left_frame, bg="#2d2d2d", fg="#e3e3e3", insertbackground="white",
            font=("Consolas", 10), wrap=tk.NONE, bd=0, highlightthickness=1, highlightcolor=COLOR_ACCENT
        )
        self.editor.pack(fill=tk.BOTH, expand=True, padx=10, pady=2)
        
        ed_hscroll = ttk.Scrollbar(self.left_frame, orient=tk.HORIZONTAL, command=self.editor.xview)
        self.editor.configure(xscrollcommand=ed_hscroll.set)
        ed_hscroll.pack(fill=tk.X, padx=10, pady=2)

        # Action Buttons Row (Compile & Map, Export DXF, Export LandXML on one line)
        f_actions = ttk.Frame(self.left_frame, style="Sidebar.TFrame")
        f_actions.pack(fill=tk.X, padx=10, pady=6)

        self.btn_run = ttk.Button(f_actions, text="⚡ Compile & Map", command=self.run_script)
        self.btn_run.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        self.btn_dxf = ttk.Button(f_actions, text="📐 Export DXF", command=self.export_dxf_drawing)
        self.btn_dxf.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        # Script File Operations Frame
        f_script = ttk.LabelFrame(self.left_frame, text="COGO Script Files", style="Sidebar.TFrame", padding=8)
        f_script.pack(fill=tk.X, padx=10, pady=5)
        
        btn_open_cogo = ttk.Button(f_script, text="📂 Open Script", command=self.open_cogo_script)
        btn_open_cogo.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        btn_save_cogo = ttk.Button(f_script, text="💾 Save Script", command=self.save_cogo_script)
        btn_save_cogo.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        # CSV Point Operations Frame
        f_csv = ttk.LabelFrame(self.left_frame, text="PNEZD Point Exchange (CSV)", style="Sidebar.TFrame", padding=8)
        f_csv.pack(fill=tk.X, padx=10, pady=5)
        
        btn_import_csv = ttk.Button(f_csv, text="📥 Import CSV", command=self.import_csv_points)
        btn_import_csv.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        btn_export_csv = ttk.Button(f_csv, text="📤 Export CSV", command=self.export_csv_points)
        btn_export_csv.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        # LandXML Operations Frame
        f_xml = ttk.LabelFrame(self.left_frame, text="LandXML Data Exchange", style="Sidebar.TFrame", padding=8)
        f_xml.pack(fill=tk.X, padx=10, pady=5)

        btn_import_xml = ttk.Button(f_xml, text="📥 Import LandXML", command=self.import_landxml_file)
        btn_import_xml.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        self.btn_xml = ttk.Button(f_xml, text="💾 Export LandXML", command=self.export_landxml_network)
        self.btn_xml.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        # Database Operations Frame
        f_db = ttk.LabelFrame(self.left_frame, text="Subdivision SQLite DB", style="Sidebar.TFrame", padding=8)
        f_db.pack(fill=tk.X, padx=10, pady=5)
        
        btn_load_db = ttk.Button(f_db, text="📥 Load Database", command=self.load_sqlite_db)
        btn_load_db.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)

        btn_save_db = ttk.Button(f_db, text="💾 Save Database", command=self.save_sqlite_db)
        btn_save_db.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=2)


        # Right Panel (Tabs)
        self.right_frame = ttk.Frame(self.paned)
        self.paned.add(self.right_frame, minsize=650, stretch="always")

        self.notebook = ttk.Notebook(self.right_frame)
        self.notebook.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Tab 1: Live CAD View
        self.tab_cad = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_cad, text="📐 Live CAD Viewer")
        self.canvas = tk.Canvas(self.tab_cad, bg=COLOR_CANVAS_BG, bd=0, highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)
        
        # Navigation bindings
        self.canvas.bind("<Configure>", lambda e: self.draw_cad())
        self.canvas.bind("<ButtonPress-3>", self.on_drag_start) # Right-click drag to pan
        self.canvas.bind("<B3-Motion>", self.on_dragging)
        self.canvas.bind("<MouseWheel>", self.on_zoom)
        self.canvas.bind("<Button-4>", self.on_zoom_in_linux)
        self.canvas.bind("<Button-5>", self.on_zoom_out_linux)
        self.canvas.bind("<Double-Button-1>", self.on_reset_view)


        # Tab 2: Points List
        self.tab_points = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_points, text="📍 Points List")
        self._build_points_list_tab()

        # Tab 3: Utility Pipes & Structures
        self.tab_utility = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_utility, text="🚰 Utility Networks")
        self._build_utility_networks_tab()

        # Tab 4: AI Blueprint Intake
        self.tab_ai = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_ai, text="👁️ AI Blueprint Intake")
        self._build_ai_intake_tab()

        # Tab 5: Logs / Console
        self.tab_log = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_log, text="📄 Console logs")
        self.log_text = tk.Text(self.tab_log, bg=COLOR_LOG_BG, fg="#89d4f5", font=("Consolas", 10), wrap=tk.WORD, bd=0)
        self.log_text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Tab 6: Validation Errors
        self.tab_val = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_val, text="⚠️ JEA validation")
        self.val_text = tk.Text(self.tab_val, bg=COLOR_LOG_BG, fg="#f44336", font=("Segoe UI", 10), wrap=tk.WORD, bd=0)
        self.val_text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Tab 7: COGO Calculator
        self.tab_cogo = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_cogo, text="🧮 COGO Calculator")
        self._build_cogo_calculator_tab()

        # Tab 8: Advanced Geoprocessing Suite
        self.tab_advanced = ttk.Frame(self.notebook)
        self.notebook.add(self.tab_advanced, text="🛠️ Advanced Suite")
        self._build_advanced_suite_tab()

    def _build_points_list_tab(self):
        columns = ("id", "northing", "easting", "elevation", "desc")
        self.tree_pts = ttk.Treeview(self.tab_points, columns=columns, show="headings")
        self.tree_pts.heading("id", text="Point ID")
        self.tree_pts.heading("northing", text="Northing (Y)")
        self.tree_pts.heading("easting", text="Easting (X)")
        self.tree_pts.heading("elevation", text="Elevation (Z)")
        self.tree_pts.heading("desc", text="Description")
        
        self.tree_pts.column("id", width=80, anchor=tk.CENTER)
        self.tree_pts.column("northing", width=120, anchor=tk.E)
        self.tree_pts.column("easting", width=120, anchor=tk.E)
        self.tree_pts.column("elevation", width=100, anchor=tk.E)
        self.tree_pts.column("desc", width=220, anchor=tk.W)
        self.tree_pts.pack(fill=tk.BOTH, expand=True)

    def _build_utility_networks_tab(self):
        # Vertical split: Pipe Runs (top), Structures (bottom)
        paned_util = tk.PanedWindow(self.tab_utility, orient=tk.VERTICAL, bg="#333333", bd=0, sashwidth=4)
        paned_util.pack(fill=tk.BOTH, expand=True)

        # Top frame for Pipe Runs
        fr_runs = ttk.Frame(paned_util)
        paned_util.add(fr_runs, minsize=200, stretch="always")
        
        lbl_runs = ttk.Label(fr_runs, text="Pipe Runs", style="Header.TLabel")
        lbl_runs.pack(anchor=tk.W, padx=5, pady=5)

        cols_runs = ("from", "to", "type", "diam", "mat", "inv_start", "inv_end", "slope")
        self.tree_runs = ttk.Treeview(fr_runs, columns=cols_runs, show="headings")
        self.tree_runs.heading("from", text="From")
        self.tree_runs.heading("to", text="To")
        self.tree_runs.heading("type", text="Type")
        self.tree_runs.heading("diam", text="Diameter")
        self.tree_runs.heading("mat", text="Material")
        self.tree_runs.heading("inv_start", text="Invert Start")
        self.tree_runs.heading("inv_end", text="Invert End")
        self.tree_runs.heading("slope", text="Slope (%)")
        
        for c in cols_runs:
            self.tree_runs.column(c, width=80, anchor=tk.CENTER)
        self.tree_runs.pack(fill=tk.BOTH, expand=True)

        # Bottom frame for Structures
        fr_structs = ttk.Frame(paned_util)
        paned_util.add(fr_structs, minsize=200, stretch="always")
        
        lbl_structs = ttk.Label(fr_structs, text="Structures", style="Header.TLabel")
        lbl_structs.pack(anchor=tk.W, padx=5, pady=5)

        cols_structs = ("pt_id", "type")
        self.tree_structs = ttk.Treeview(fr_structs, columns=cols_structs, show="headings")
        self.tree_structs.heading("pt_id", text="Point ID")
        self.tree_structs.heading("type", text="Structure Type")
        self.tree_structs.column("pt_id", width=120, anchor=tk.CENTER)
        self.tree_structs.column("type", width=250, anchor=tk.W)
        self.tree_structs.pack(fill=tk.BOTH, expand=True)

    def _build_ai_intake_tab(self):
        f_top = ttk.Frame(self.tab_ai, padding=10)
        f_top.pack(fill=tk.X)

        # Provider Selector Row
        lbl_prov = ttk.Label(f_top, text="AI Provider:", font=("Segoe UI", 9, "bold"))
        lbl_prov.pack(anchor=tk.W, pady=2)

        f_prov = ttk.Frame(f_top)
        f_prov.pack(fill=tk.X, pady=2)

        rb_gemini = ttk.Radiobutton(f_prov, text="Gemini Cloud", variable=self.ai_provider_var, value="gemini", command=self._toggle_ai_provider)
        rb_gemini.pack(side=tk.LEFT, padx=(0, 10))

        rb_ollama = ttk.Radiobutton(f_prov, text="Ollama Local", variable=self.ai_provider_var, value="ollama", command=self._toggle_ai_provider)
        rb_ollama.pack(side=tk.LEFT)

        # Dynamic Settings Panel
        self.f_ai_settings = ttk.Frame(f_top)
        self.f_ai_settings.pack(fill=tk.X, pady=5)

        lbl_img = ttk.Label(f_top, text="Deed Blueprint Image / Plat Map PDF:")
        lbl_img.pack(anchor=tk.W, pady=4)

        f_picker = ttk.Frame(f_top)
        f_picker.pack(fill=tk.X)

        self.ent_img_path = ttk.Entry(f_picker, width=50)
        self.ent_img_path.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 5))

        btn_browse = ttk.Button(f_picker, text="📂 Browse", command=self.browse_blueprint_image)
        btn_browse.pack(side=tk.RIGHT)

        f_ai_actions = ttk.Frame(f_top)
        f_ai_actions.pack(fill=tk.X, pady=10)

        btn_extract = ttk.Button(f_ai_actions, text="🤖 Run OCR & Extract", command=self.run_ai_extraction)
        btn_extract.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 2))

        btn_clean = ttk.Button(f_ai_actions, text="🧹 Clean & Correct Deed Text", command=self.clean_deed_text)
        btn_clean.pack(side=tk.RIGHT, fill=tk.X, expand=True, padx=(2, 0))

        # Extraction output / warning box
        lbl_ai_res = ttk.Label(self.tab_ai, text="AI OCR Results & Self-Intersection Analysis", style="Header.TLabel")
        lbl_ai_res.pack(anchor=tk.W, padx=10, pady=5)

        self.ai_res_text = tk.Text(self.tab_ai, bg=COLOR_LOG_BG, fg="#89d4f5", font=("Consolas", 10), wrap=tk.WORD, bd=0)
        self.ai_res_text.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        # Draw settings panel initially
        self._toggle_ai_provider()

    def _toggle_ai_provider(self):
        for child in self.f_ai_settings.winfo_children():
            child.destroy()
        
        provider = self.ai_provider_var.get()
        if provider == "gemini":
            lbl_api = ttk.Label(self.f_ai_settings, text="Gemini API Key:")
            lbl_api.pack(anchor=tk.W, pady=2)
            ent_api = ttk.Entry(self.f_ai_settings, textvariable=self.api_key_var, show="*", width=50)
            ent_api.pack(fill=tk.X, pady=2)

            lbl_model = ttk.Label(self.f_ai_settings, text="Model Name:")
            lbl_model.pack(anchor=tk.W, pady=2)
            ent_model = ttk.Entry(self.f_ai_settings, textvariable=self.gemini_model_var, width=50)
            ent_model.pack(fill=tk.X, pady=2)
        else:
            lbl_host = ttk.Label(self.f_ai_settings, text="Ollama Server URL:")
            lbl_host.pack(anchor=tk.W, pady=2)
            ent_host = ttk.Entry(self.f_ai_settings, textvariable=self.ollama_host_var, width=50)
            ent_host.pack(fill=tk.X, pady=2)

            lbl_model = ttk.Label(self.f_ai_settings, text="Model Name:")
            lbl_model.pack(anchor=tk.W, pady=2)
            
            f_model_row = ttk.Frame(self.f_ai_settings)
            f_model_row.pack(fill=tk.X, pady=2)
            
            self.cb_ollama_model = ttk.Combobox(f_model_row, textvariable=self.ollama_model_var, width=40)
            self.cb_ollama_model.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 5))
            
            btn_refresh = ttk.Button(f_model_row, text="🔄 Scan", width=8, command=self._refresh_ollama_models)
            btn_refresh.pack(side=tk.RIGHT)
            
            self._refresh_ollama_models()

    def _refresh_ollama_models(self):
        host = self.ollama_host_var.get()
        from rcs_cogo.ai_vision import AiVisionExtractionEngine
        models = AiVisionExtractionEngine.get_ollama_models(host)
        if models:
            self.cb_ollama_model['values'] = models
            if "llama3.2-vision" in models:
                self.ollama_model_var.set("llama3.2-vision")
            else:
                self.ollama_model_var.set(models[0])
        else:
            self.cb_ollama_model['values'] = ["llama3.2-vision", "llava"]
    def handle_format_toggle(self):
        fmt = self.script_format.get()
        self.editor.delete("1.0", tk.END)
        if fmt == "microstation":
            demo = (
                "# ====================================================================\n"
                "#  BENTLEY MICROSTATION KEY-IN SCRIPT DEMO\n"
                "# ====================================================================\n"
                "# Configure styling\n"
                "active color 3\n"
                "active angle=45\n\n"
                "# Draw first segment\n"
                "place line\n"
                "xy=37.7749,-122.4194\n"
                "xy=37.7800,-122.4100\n"
                "dx=0.0050,-0.0050\n\n"
                "# Draw second segment\n"
                "place line\n"
                "xy=37.7850,-122.4050\n"
                "xy=37.7900,-122.4000\n"
            )
            self.editor.insert(tk.END, demo)
        else:
            self._load_sample_script()

    def _load_sample_script(self):
        sample = """// RCS AsBuilt-Bro Utility Network Simulation
CLEAR

// Setup Duval County JEA control points
PT 1 1950000 450000 12.5 TBM_START
PT 2 1950500 450000 13.0 TBM_ALIGN

STN 1
BS 2 0.0000

// Capture survey points for utility layout
BD 10 1 45.0000 100.00 SAN_MH1
BD 11 1 45.0000 200.00 SAN_MH2
BD 12 1 90.0000 150.00 SAN_MH3
BD 13 1 90.0000 250.00 VAL_1

// Compile a Wastewater (Sewer) Gravity Line main run
PRUN START WW DIAM 8 MAT PVC FIG WW_MAIN_LINE
10 11 10.50 9.80
11 12 9.80 8.50
PRUN END

// Store explicit structures at specific points
SS-C 10 Manhole
SS-C 11 Manhole
SS-C 12 Manhole
SS-C 13 Valve

// Setup boundary figure around the work site
BEG WORK_SITE
CONT 10
CONT 11
CONT 12
CONT 13
CLOSE
MAPCHK WORK_SITE
"""
        self.editor.insert(tk.END, sample)

    def run_script(self):
        script = self.editor.get("1.0", tk.END)
        if self.script_format.get() == "microstation":
            script = transpile_microstation_to_cogo(script)
        self.engine = CogoEngine()
        
        self.engine.execute_batch(script)
        self._refresh_gui_state()

    def _refresh_gui_state(self):
        # 1. Update Logs Console
        self.log_text.delete("1.0", tk.END)
        self.log_text.insert(tk.END, "\n".join(self.engine.logs))
        
        # 2. Update Points
        for row in self.tree_pts.get_children():
            self.tree_pts.delete(row)
        for pt_id, (pt, desc) in self.engine.points.items():
            self.tree_pts.insert("", tk.END, values=(pt_id, f"{pt.northing:.3f}", f"{pt.easting:.3f}", f"{pt.elevation:.2f}", desc))

        # 3. Update Utility Runs & Structures
        for row in self.tree_runs.get_children():
            self.tree_runs.delete(row)
        for r in self.engine.pipe_runs:
            self.tree_runs.insert("", tk.END, values=(r.from_point_id, r.to_point_id, r.type, f"{r.diameter:.0f}\"", r.material, f"{r.invert_start:.2f}", f"{r.invert_end:.2f}", f"{r.slope:.2f}%"))

        for row in self.tree_structs.get_children():
            self.tree_structs.delete(row)
        for s in self.engine.pipe_structures:
            self.tree_structs.insert("", tk.END, values=(s.point_id, s.type))

        # 4. Update JEA Validation
        self.val_text.delete("1.0", tk.END)
        validation_issues = []
        for pt_id, (pt, desc) in self.engine.points.items():
            lat, lon = None, None
            if pt.easting != 0 and pt.northing != 0:
                try:
                    lat, lon = StatePlaneProjection.to_lat_lon(pt.easting, pt.northing, "EPSG:2236")
                except Exception:
                    pass
            issues = JeaValidationService.check_coords("Points Table", pt_id, pt.easting, pt.northing, lat, lon)
            validation_issues.extend(issues)

        for run in self.engine.pipe_runs:
            issues = JeaValidationService.validate_pipe(
                asset_id=f"{run.from_point_id}->{run.to_point_id}",
                run_type=run.type,
                diameter=run.diameter,
                material=run.material,
                slope=run.slope
            )
            validation_issues.extend(issues)

        # Check self-intersection on boundary figures
        for fig_name, fig in self.figures_list():
            coords = []
            for pid in fig.point_ids:
                pt = self.engine.get_point(pid)
                if pt: coords.append(pt)
            intersected, warn_msg = BowTieChecker.has_self_intersection(coords)
            if intersected:
                validation_issues.append(JeaIssue("Boundary Figure", fig_name, "Topology", f"Bow-Tie warning: {warn_msg}", "Error"))

        if not validation_issues:
            self.val_text.insert(tk.END, "ALL JEA QC VALIDATIONS PASSED: Grid layout is compliant.")
            self.val_text.configure(fg="#4caf50")
        else:
            self.val_text.configure(fg="#f44336")
            report_text = f"JEA VALIDATION FAILURE: {len(validation_issues)} QC issue(s) detected.\n\n"
            for issue in validation_issues:
                report_text += f"{str(issue)}\n"
            self.val_text.insert(tk.END, report_text)

        # Redraw
        self.draw_cad()
        self.notebook.select(0)

    def figures_list(self):
        return self.engine.figures.items()

    def browse_blueprint_image(self):
        path = filedialog.askopenfilename(filetypes=[("Images & PDFs", "*.png;*.jpg;*.jpeg;*.pdf"), ("All Files", "*.*")])
        if path:
            self.ent_img_path.delete(0, tk.END)
            self.ent_img_path.insert(0, path)

    def run_ai_extraction(self):
        path = self.ent_img_path.get()
        if not path:
            messagebox.showwarning("Warning", "Please select a blueprint image or plat PDF to run OCR.")
            return

        self.ai_res_text.delete("1.0", tk.END)
        provider = self.ai_provider_var.get()
        
        if provider == "gemini":
            api_key = self.api_key_var.get()
            model_name = self.gemini_model_var.get()
            self.ai_res_text.insert(tk.END, f"Analyzing image with Gemini Cloud OCR ({model_name})...\n")
            self.ai_res_text.insert(tk.END, "Please wait, contacting Gemini API in background...\n")
        else:
            api_key = None
            host = self.ollama_host_var.get()
            model_name = self.ollama_model_var.get()
            self.ai_res_text.insert(tk.END, f"Analyzing image with Local Ollama ({model_name}) at {host}...\n")
            self.ai_res_text.insert(tk.END, "Please wait, contacting local Ollama server in background...\n")
            
        self.root.update_idletasks()

        import threading

        def bg_task():
            try:
                if provider == "gemini":
                    res = AiVisionExtractionEngine.extract_plat_calls(
                        image_path=path,
                        api_key=api_key,
                        provider="gemini",
                        model_name=model_name
                    )
                else:
                    res = AiVisionExtractionEngine.extract_plat_calls(
                        image_path=path,
                        provider="ollama",
                        model_name=model_name,
                        ollama_host=host
                    )
                self.root.after(0, self._on_ai_extraction_complete, res)
            except Exception as e:
                self.root.after(0, self._on_ai_extraction_error, str(e))

        threading.Thread(target=bg_task, daemon=True).start()

    def _on_ai_extraction_complete(self, res):
        self.ai_res_text.delete("1.0", tk.END)
        self.ai_res_text.insert(tk.END, f"Status: {res.get('status')}\n")
        if "message" in res:
            self.ai_res_text.insert(tk.END, f"Details: {res['message']}\n\n")

        # Reconstruct COGO script blocks from extracted metes/bounds calls
        cogo_script = ["// EXTRACTED COGO FROM DEED DECODING", "CLEAR", "PT 100 10000 10000 0.0 POB", "STN 100", "ST 101 10000 10100 0.0 BS_ALIGN", "BS 101 0.0"]
        
        last_pid = 100
        calls = res.get("calls", [])
        
        for idx, call in enumerate(calls):
            next_pid = last_pid + 1
            ctype = call.get("type", "line")
            desc = call.get("desc", f"Shot_{idx}")

            if ctype == "line":
                brg_str = call.get("bearing", "n00.0000e")
                dist = call.get("distance", 100.0)
                # Parse quadrant bearing string
                quad, deg = self._parse_bearing_to_quad(brg_str)
                cogo_script.append(f"BD {next_pid} {quad} {deg:.4f} {dist:.2f} {desc}")
                last_pid = next_pid
            elif ctype == "curve":
                dir_c = call.get("direction", "right")
                r = call.get("radius", 50.0)
                cb = call.get("chord_bearing", "n45.0000w")
                cd = call.get("chord_distance", 70.7)
                
                # XC BD syntax: XC BD <Radius> <targetId> <Quad> <Bearing> <ChordDist>
                quad, deg = self._parse_bearing_to_quad(cb)
                rad_sign = r if dir_c == "right" else -r
                cogo_script.append(f"BEG CURVE_{idx}")
                cogo_script.append(f"CONT {last_pid}")
                cogo_script.append(f"XC BD {rad_sign} {next_pid} {quad} {deg:.4f} {cd:.2f}")
                cogo_script.append("END")
                last_pid = next_pid

        cogo_script.append("\n// Site Figure Boundary")
        cogo_script.append("BEG PROPERTY_DEED")
        for i in range(100, last_pid + 1):
            cogo_script.append(f"CONT {i}")
        cogo_script.append("CLOSE")
        cogo_script.append("MAPCHK PROPERTY_DEED")

        # Update editor contents
        self.editor.delete("1.0", tk.END)
        self.editor.insert(tk.END, "\n".join(cogo_script))
        
        self.ai_res_text.insert(tk.END, "Successfully generated survey script based on metes and bounds parameters!\n")
        self.ai_res_text.insert(tk.END, "Click 'Compile & Map Network' to load points and run self-intersection checks.")

    def clean_deed_text(self):
        text = self.ai_res_text.get("1.0", tk.END).strip()
        if not text:
            messagebox.showwarning("Warning", "Deed text area is empty. Please enter or extract text first.")
            return
        
        from bearing_report_adj import BearingReportAdj
        cleaned = BearingReportAdj.clean_text(text)
        
        self.ai_res_text.delete("1.0", tk.END)
        self.ai_res_text.insert(tk.END, cleaned)

    def _on_ai_extraction_error(self, error_msg):
        self.ai_res_text.delete("1.0", tk.END)
        self.ai_res_text.insert(tk.END, f"Error: Failed to contact Gemini API:\n{error_msg}\n")

    def _parse_bearing_to_quad(self, brg: str) -> Tuple[int, float]:
        brg_clean = brg.lower().strip()
        q = 1
        # Extract quadrant
        if brg_clean.startswith("n") and brg_clean.endswith("e"): q = 1
        elif brg_clean.startswith("s") and brg_clean.endswith("e"): q = 2
        elif brg_clean.startswith("s") and brg_clean.endswith("w"): q = 3
        elif brg_clean.startswith("n") and brg_clean.endswith("w"): q = 4
        
        # Extract digits
        digits = "".join([c for c in brg_clean if c.isdigit() or c == "."])
        try:
            return q, float(digits)
        except ValueError:
            return q, 0.0

    def import_csv_points(self):
        path = filedialog.askopenfilename(filetypes=[("CSV coordinate files", "*.csv"), ("Text files", "*.txt"), ("All Files", "*.*")])
        if not path:
            return
        
        try:
            count = 0
            with open(path, "r") as f:
                for line in f:
                    parts = [p.strip() for p in line.split(",") if p.strip()]
                    if len(parts) >= 3:
                        pt_id = parts[0]
                        try:
                            n = float(parts[1])
                            e = float(parts[2])
                            z = float(parts[3]) if len(parts) >= 4 and self.engine._is_float(parts[3]) else 0.0
                            desc_idx = 4 if len(parts) >= 4 and self.engine._is_float(parts[3]) else 3
                            desc = " ".join(parts[desc_idx:]) if len(parts) > desc_idx else ""
                            
                            self.engine.add_point(pt_id, Point3D(n, e, z), desc)
                            count += 1
                        except ValueError:
                            pass
            
            messagebox.showinfo("Success", f"Successfully imported {count} points from CSV database.")
            self.run_script() # Trigger workspace reload
        except Exception as ex:
            messagebox.showerror("Error", f"Could not read CSV file: {str(ex)}")

    def export_csv_points(self):
        path = filedialog.asksaveasfilename(defaultextension=".csv", filetypes=[("CSV coordinate files", "*.csv")])
        if not path:
            return
        
        try:
            with open(path, "w") as f:
                for pt_id, (pt, desc) in self.engine.points.items():
                    f.write(f"{pt_id},{pt.northing:.4f},{pt.easting:.4f},{pt.elevation:.4f},{desc}\n")
            messagebox.showinfo("Success", f"Exported {len(self.engine.points)} points to CSV.")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to save CSV file: {str(ex)}")

    def open_cogo_script(self):
        path = filedialog.askopenfilename(filetypes=[("COGO Scripts", "*.cogo;*.txt;*.key"), ("All Files", "*.*")])
        if not path:
            return
        try:
            with open(path, "r") as f:
                text = f.read()
            if self.script_format.get() == "microstation":
                text = transpile_microstation_to_cogo(text)
            self.editor.delete("1.0", tk.END)
            self.editor.insert(tk.END, text)
            messagebox.showinfo("Success", f"Loaded script: {os.path.basename(path)}")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to open script: {str(ex)}")

    def save_cogo_script(self):
        path = filedialog.asksaveasfilename(defaultextension=".cogo", filetypes=[("COGO Scripts", "*.cogo;*.txt;*.key")])
        if not path:
            return
        try:
            with open(path, "w") as f:
                f.write(self.editor.get("1.0", tk.END))
            messagebox.showinfo("Success", f"Saved script to {os.path.basename(path)}")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to save script: {str(ex)}")

    def export_dxf_drawing(self):
        path = filedialog.asksaveasfilename(defaultextension=".dxf", filetypes=[("AutoCAD DXF Drawing", "*.dxf")])
        if not path:
            return
        
        try:
            dxf = ProfessionalDxfWriter()

            # Draw points
            for pt_id, (pt, desc) in self.engine.points.items():
                dxf.add_point(pt, "POINTS")
                dxf.add_text(f"{pt_id}: {desc}", pt.easting + 1.0, pt.northing + 1.0, 0.8, "POINTS_LABELS")

            # Draw boundary figures
            for fig_name, figure in self.engine.figures.items():
                pts = []
                for pid in figure.point_ids:
                    pt = self.engine.get_point(pid)
                    if pt:
                        pts.append((pt.easting, pt.northing))
                dxf.add_polyline(pts, "FIGURES", 7)
                for label in figure.labels:
                    dxf.add_text(label.text, label.easting, label.northing, 0.8, "FIGURES_LABELS", label.rotation, 7)

            # Draw pipe runs
            from main import get_pipe_layer_and_color, get_struct_layer_and_block
            for run in self.engine.pipe_runs:
                p1 = self.engine.get_point(run.from_point_id)
                p2 = self.engine.get_point(run.to_point_id)
                if p1 and p2:
                    layer, color = get_pipe_layer_and_color(run.type)
                    dxf.add_line(p1, p2, layer, color)
                    
                    mid_x = (p1.easting + p2.easting) / 2.0
                    mid_y = (p1.northing + p2.northing) / 2.0
                    _, az = GeometryEngine.inverse(p1, p2)
                    rot = -az.degrees + 90.0
                    if rot < -90: rot += 180.0
                    if rot > 90: rot -= 180.0
                    label = f"{run.diameter:.0f}\" {run.material} (S={run.slope:.2f}%)"
                    dxf.add_text(label, mid_x, mid_y + 1.0, 0.6, f"{layer}_LABELS", rot, color)

            # Insert structures
            for struct in self.engine.pipe_structures:
                pt = self.engine.get_point(struct.point_id)
                if pt:
                    layer, block = get_struct_layer_and_block(struct.type)
                    _, color = get_pipe_layer_and_color(struct.type)
                    dxf.insert_block(block, pt.easting, pt.northing, 1.5, layer, color)

            dxf.save(path)
            messagebox.showinfo("Success", f"DXF exported successfully to {os.path.basename(path)}")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to export DXF: {str(ex)}")

    def export_landxml_network(self):
        if not self.engine.points and not self.engine.figures and not self.engine.pipe_structures:
            messagebox.showinfo("Empty Workspace", "No data to export. Compile a COGO script or import points first.")
            return
            
        path = filedialog.asksaveasfilename(
            defaultextension=".xml",
            filetypes=[("LandXML Files", "*.xml"), ("All Files", "*.*")],
            title="Save LandXML Workspace"
        )
        if not path:
            return
            
        try:
            from rcs_cogo.landxml_writer import LandXmlWriter
            xml = LandXmlWriter.generate_landxml(
                "Python AsBuilt Subdivision",
                self.engine.pipe_runs,
                self.engine.pipe_structures,
                self.engine
            )
            with open(path, "w", encoding="utf-8") as f:
                f.write(xml)
            messagebox.showinfo("Export Success", f"LandXML exported successfully to:\n{os.path.basename(path)}")
        except Exception as e:
            messagebox.showerror("Export Error", f"Failed to save LandXML: {str(e)}")

    def import_landxml_file(self):
        path = filedialog.askopenfilename(
            filetypes=[("LandXML Files", "*.xml"), ("All Files", "*.*")],
            title="Import LandXML Data"
        )
        if not path:
            return
            
        try:
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            from rcs_cogo.landxml_importer import LandXmlImporter
            res = LandXmlImporter.import_landxml(content, self.engine)
            
            # Log results and refresh GUI
            self.engine.log(f"Successfully imported LandXML from {os.path.basename(path)}:")
            self.engine.log(f"  - Points   : {res['points']}")
            self.engine.log(f"  - Parcels  : {res['parcels']}")
            self.engine.log(f"  - Alignments: {res['alignments']}")
            
            self._refresh_gui_state()
            self.draw_cad()
            
            messagebox.showinfo("Import Success", f"LandXML imported successfully:\n"
                                                 f"- Points   : {res['points']}\n"
                                                 f"- Parcels  : {res['parcels']}\n"
                                                 f"- Alignments: {res['alignments']}")
        except Exception as e:
            messagebox.showerror("Import Error", f"Failed to import LandXML: {str(e)}")

    def load_sqlite_db(self):
        path = filedialog.askopenfilename(filetypes=[("SQLite Subdivision DB", "*.db"), ("All Files", "*.*")])
        if not path:
            return
        try:
            SubdivisionDatabase.load_project(path, self.engine)
            # Re-generate script from database values to allow quick compilation
            script = ["// RESTORED COGO WORKSPACE FROM DATABASE"]
            for pt_id, (pt, desc) in self.engine.points.items():
                script.append(f"PT {pt_id} {pt.northing:.4f} {pt.easting:.4f} {pt.elevation:.4f} {desc}")
            for run in self.engine.pipe_runs:
                script.append(f"PRUN START {run.type} DIAM {run.diameter:.0f} MAT {run.material} FIG {run.figure_name}")
                script.append(f"{run.from_point_id} {run.to_point_id} {run.invert_start:.2f} {run.invert_end:.2f}")
                script.append("PRUN END")
            for struct in self.engine.pipe_structures:
                script.append(f"SS-C {struct.point_id} {struct.type}")
            
            self.editor.delete("1.0", tk.END)
            self.editor.insert(tk.END, "\n".join(script))
            self.run_script()
            messagebox.showinfo("Success", f"Loaded subdivision project from {os.path.basename(path)}")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to load SQLite project: {str(ex)}")

    def save_sqlite_db(self):
        path = filedialog.asksaveasfilename(defaultextension=".db", filetypes=[("SQLite Subdivision DB", "*.db")])
        if not path:
            return
        try:
            SubdivisionDatabase.save_project(path, self.engine)
            messagebox.showinfo("Success", f"Subdivision project saved to {os.path.basename(path)}")
        except Exception as ex:
            messagebox.showerror("Error", f"Failed to save SQLite project: {str(ex)}")

    def on_drag_start(self, event):
        self.drag_start_x = event.x
        self.drag_start_y = event.y

    def on_dragging(self, event):
        dx = event.x - self.drag_start_x
        dy = event.y - self.drag_start_y
        self.pan_dx += dx
        self.pan_dy += dy
        self.drag_start_x = event.x
        self.drag_start_y = event.y
        self.draw_cad()

    def on_zoom(self, event):
        if event.delta > 0:
            self.zoom_level *= 1.15
        else:
            self.zoom_level /= 1.15
        self.draw_cad()

    def on_zoom_in_linux(self, event):
        self.zoom_level *= 1.15
        self.draw_cad()

    def on_zoom_out_linux(self, event):
        self.zoom_level /= 1.15
        self.draw_cad()

    def on_reset_view(self, event):
        self.zoom_level = 1.0
        self.pan_dx = 0.0
        self.pan_dy = 0.0
        self.draw_cad()



    def draw_cad(self):
        self.canvas.delete("all")
        
        if not self.engine.points:
            self.canvas.create_text(
                self.canvas.winfo_width() / 2, self.canvas.winfo_height() / 2,
                text="Compile a survey script or import coordinates to view drawing.", fill="#555555", font=("Segoe UI", 11)
            )
            return

        # Calculate bounding box
        eastings = [pt.easting for pt, _ in self.engine.points.values()]
        northings = [pt.northing for pt, _ in self.engine.points.values()]
        
        min_e, max_e = min(eastings), max(eastings)
        min_n, max_n = min(northings), max(northings)
        
        span_e = max_e - min_e
        span_n = max_n - min_n
        
        pad = 50
        w = self.canvas.winfo_width()
        h = self.canvas.winfo_height()
        
        if w < 10 or h < 10:
            return

        scale_e = (w - 2 * pad) / span_e if span_e > 1e-4 else 1.0
        scale_n = (h - 2 * pad) / span_n if span_n > 1e-4 else 1.0
        scale = min(scale_e, scale_n)

        mid_e = (min_e + max_e) / 2.0
        mid_n = (min_n + max_n) / 2.0
        center_x = w / 2.0
        center_y = h / 2.0

        def to_canvas(northing, easting):
            # Centering offset scaled and zoom-factored
            x = center_x + self.pan_dx + (easting - mid_e) * scale * self.zoom_level
            y = center_y + self.pan_dy - (northing - mid_n) * scale * self.zoom_level
            return x, y


        # 1. Draw boundary figures
        for fig_name, figure in self.engine.figures.items():
            pts_canvas = []
            for pid in figure.point_ids:
                pt = self.engine.get_point(pid)
                if pt:
                    pts_canvas.append(to_canvas(pt.northing, pt.easting))
            
            if len(pts_canvas) >= 2:
                # Close the polygon loop for visual check
                pts_canvas_loop = list(pts_canvas)
                if figure.point_ids[0] != figure.point_ids[-1]:
                    first_pt = self.engine.get_point(figure.point_ids[0])
                    if first_pt:
                        pts_canvas_loop.append(to_canvas(first_pt.northing, first_pt.easting))

                self.canvas.create_line(pts_canvas_loop, fill="#ff9800", width=2, dash=(3, 3))
                
                # Draw bearing & distance labels on midpoints
                n_points = len(figure.point_ids)
                for i in range(n_points):
                    p1_id = figure.point_ids[i]
                    # Loop back to first point if closed figure
                    p2_id = figure.point_ids[(i + 1) % n_points]
                    p1 = self.engine.get_point(p1_id)
                    p2 = self.engine.get_point(p2_id)
                    
                    if p1 and p2:
                        cx1, cy1 = to_canvas(p1.northing, p1.easting)
                        cx2, cy2 = to_canvas(p2.northing, p2.easting)
                        
                        dn = p2.northing - p1.northing
                        de = p2.easting - p1.easting
                        dist = math.sqrt(dn*dn + de*de)
                        if dist > 0.1:
                            bearing_deg = math.degrees(math.atan2(abs(de), abs(dn)))
                            degs = int(bearing_deg)
                            mins = int((bearing_deg - degs) * 60)
                            secs = round(((bearing_deg - degs) * 60 - mins) * 60)
                            dms = f"{degs}°{mins:02d}'{secs:02d}\""
                            
                            if dn >= 0 and de >= 0: quad = f"N {dms} E"
                            elif dn < 0 and de >= 0: quad = f"S {dms} E"
                            elif dn < 0 and de < 0: quad = f"S {dms} W"
                            else: quad = f"N {dms} W"
                            
                            # Calculate segment angle and perpendicular unit offset vector
                            dx_px = cx2 - cx1
                            dy_px = cy2 - cy1
                            len_px = math.sqrt(dx_px * dx_px + dy_px * dy_px)
                            
                            vx, vy = 0.0, 0.0
                            if len_px > 0.1:
                                vx = -dy_px / len_px
                                vy = dx_px / len_px

                            # Shift label 8px perpendicular to the line to prevent occlusion
                            mid_x = (cx1 + cx2) / 2.0 + vx * 8.0
                            mid_y = (cy1 + cy2) / 2.0 + vy * 8.0
                            
                            angle_deg = -math.degrees(math.atan2(cy2 - cy1, cx2 - cx1))
                            if angle_deg > 90:
                                angle_deg -= 180
                            elif angle_deg < -90:
                                angle_deg += 180

                            self.canvas.create_text(
                                mid_x, mid_y, text=f"{quad} | {dist:.2f}ft",
                                fill="#ffeb3b", font=("Consolas", 8, "bold"),
                                angle=angle_deg, justify=tk.CENTER
                            )

        # 2. Draw pipe runs
        for run in self.engine.pipe_runs:
            p1 = self.engine.get_point(run.from_point_id)
            p2 = self.engine.get_point(run.to_point_id)
            if p1 and p2:
                cx1, cy1 = to_canvas(p1.northing, p1.easting)
                cx2, cy2 = to_canvas(p2.northing, p2.easting)
                
                # JEA Validations
                issues = JeaValidationService.validate_pipe(
                    asset_id=f"{run.from_point_id}->{run.to_point_id}",
                    run_type=run.type,
                    diameter=run.diameter,
                    material=run.material,
                    slope=run.slope
                )
                has_warning = any(i.severity in ("Error", "Warning") for i in issues)
                
                # Layer colors
                color = "#ffffff"
                r = run.type.upper()
                if r in ("W", "WATER"): color = "#2196f3" # Blue
                elif r in ("WW", "SEWER"): color = "#4caf50" # Green
                elif r in ("ST", "STORM", "D"): color = "#e040fb" # Magenta
                elif r in ("G", "GAS"): color = "#f44336" # Red
                elif r in ("E", "ELEC"): color = "#ffeb3b" # Yellow
                
                if has_warning:
                    # Draw a blinking-style red dashed line and warning badge
                    self.canvas.create_line(cx1, cy1, cx2, cy2, fill="#f44336", width=4, dash=(4, 4))
                    mid_x = (cx1 + cx2) / 2.0
                    mid_y = (cy1 + cy2) / 2.0
                    self.canvas.create_text(mid_x, mid_y - 12, text="⚠️ JEA ALERT", fill="#f44336", font=("Segoe UI", 7, "bold"))
                else:
                    self.canvas.create_line(cx1, cy1, cx2, cy2, fill=color, width=3)
                
                # Draw invert texts
                self.canvas.create_text(cx1, cy1 + 10, text=f"Inv: {run.invert_start:.2f}", fill="#888888", font=("Consolas", 7))
                self.canvas.create_text(cx2, cy2 + 10, text=f"Inv: {run.invert_end:.2f}", fill="#888888", font=("Consolas", 7))

        # 3. Draw structures
        for s in self.engine.pipe_structures:
            pt = self.engine.get_point(s.point_id)
            if pt:
                cx, cy = to_canvas(pt.northing, pt.easting)
                t = s.type.upper()
                
                # Circle for Manhole
                if "MANHOLE" in t or "MH" in t or t == "WW":
                    self.canvas.create_oval(cx - 8, cy - 8, cx + 8, cy + 8, outline="#4caf50", fill="#1e1e1e", width=2)
                    self.canvas.create_text(cx, cy, text="MH", fill="#4caf50", font=("Segoe UI", 7, "bold"))
                # Bowtie for Valve
                elif "VALVE" in t or "VLV" in t or t == "W":
                    self.canvas.create_polygon(cx-6, cy-6, cx+6, cy+6, cx+6, cy-6, cx-6, cy+6, outline="#2196f3", fill="#2196f3")
                # Circle with cross for Hydrant
                elif "HYDRANT" in t or "FH" in t:
                    self.canvas.create_oval(cx - 7, cy - 7, cx + 7, cy + 7, outline="#ff9800", width=2)
                    self.canvas.create_line(cx - 7, cy, cx + 7, cy, fill="#ff9800")
                    self.canvas.create_line(cx, cy - 7, cx, cy + 7, fill="#ff9800")
                # Box for junction / box vaults
                else:
                    self.canvas.create_rectangle(cx - 7, cy - 7, cx + 7, cy + 7, outline="#ffffff", width=2)

        # 4. Draw standard points
        for pt_id, (pt, desc) in self.engine.points.items():
            cx, cy = to_canvas(pt.northing, pt.easting)
            self.canvas.create_oval(cx - 2.5, cy - 2.5, cx + 2.5, cy + 2.5, fill="#ffffff", outline=COLOR_ACCENT, width=1)
            self.canvas.create_text(cx + 8, cy - 8, text=f"{pt_id} ({desc})" if desc else pt_id, fill="#bbbbbb", font=("Consolas", 8), anchor=tk.W)

        # scale indicator
        self.canvas.create_text(15, h - 15, text=f"Min Coordinates: ({min_n:.1f}, {min_e:.1f}) | Scale: 1 ft = {scale:.4f} px", fill="#666666", font=("Consolas", 8), anchor=tk.SW)

    def _build_cogo_calculator_tab(self):
        # Two column pane
        fr_paned = ttk.Frame(self.tab_cogo, padding=10)
        fr_paned.pack(fill=tk.BOTH, expand=True)

        # Column 1: Bearing & Distance Inverse/Forward
        col1 = ttk.LabelFrame(fr_paned, text="Compass Bearing & Traverse Calculator", padding=10)
        col1.grid(row=0, column=0, sticky="nsew", padx=5, pady=5)

        # Inputs for Pt A
        ttk.Label(col1, text="Point A Northing (Y):").grid(row=0, column=0, sticky=tk.W, pady=2)
        self.ent_na = ttk.Entry(col1)
        self.ent_na.insert(0, "5000.00")
        self.ent_na.grid(row=0, column=1, sticky=tk.EW, pady=2)

        ttk.Label(col1, text="Point A Easting (X):").grid(row=1, column=0, sticky=tk.W, pady=2)
        self.ent_ea = ttk.Entry(col1)
        self.ent_ea.insert(0, "5000.00")
        self.ent_ea.grid(row=1, column=1, sticky=tk.EW, pady=2)

        # Inputs for Pt B
        ttk.Label(col1, text="Point B Northing (Y):").grid(row=2, column=0, sticky=tk.W, pady=2)
        self.ent_nb = ttk.Entry(col1)
        self.ent_nb.insert(0, "5100.00")
        self.ent_nb.grid(row=2, column=1, sticky=tk.EW, pady=2)

        ttk.Label(col1, text="Point B Easting (X):").grid(row=3, column=0, sticky=tk.W, pady=2)
        self.ent_eb = ttk.Entry(col1)
        self.ent_eb.insert(0, "5100.00")
        self.ent_eb.grid(row=3, column=1, sticky=tk.EW, pady=2)

        btn_inv = ttk.Button(col1, text="⇄ Calculate Inverse (A to B)", command=self.calc_bearing_inverse)
        btn_inv.grid(row=4, column=0, columnspan=2, sticky="ew", pady=6)

        ttk.Separator(col1, orient=tk.HORIZONTAL).grid(row=5, column=0, columnspan=2, sticky=tk.EW, pady=8)

        # Forward Inputs
        ttk.Label(col1, text="Traverse Dist (ft):").grid(row=6, column=0, sticky=tk.W, pady=2)
        self.ent_trav_dist = ttk.Entry(col1)
        self.ent_trav_dist.insert(0, "100.00")
        self.ent_trav_dist.grid(row=6, column=1, sticky=tk.EW, pady=2)

        ttk.Label(col1, text="Bearing DMS (DDD.MMSS):").grid(row=7, column=0, sticky=tk.W, pady=2)
        self.ent_trav_brg = ttk.Entry(col1)
        self.ent_trav_brg.insert(0, "45.0000")
        self.ent_trav_brg.grid(row=7, column=1, sticky=tk.EW, pady=2)

        ttk.Label(col1, text="Quadrant (1-4):").grid(row=8, column=0, sticky=tk.W, pady=2)
        self.ent_trav_quad = ttk.Entry(col1)
        self.ent_trav_quad.insert(0, "1")
        self.ent_trav_quad.grid(row=8, column=1, sticky=tk.EW, pady=2)
        
        # Info quadrant note
        ttk.Label(col1, text="* Quadrants: 1=NE, 2=SE, 3=SW, 4=NW", font=("Segoe UI", 8, "italic")).grid(row=9, column=0, columnspan=2, sticky=tk.W)

        btn_fwd = ttk.Button(col1, text="➔ Calculate Forward from A", command=self.calc_bearing_forward)
        btn_fwd.grid(row=10, column=0, columnspan=2, sticky="ew", pady=6)

        # Results Inverse/Forward
        self.txt_bearing_res = tk.Text(col1, height=8, width=40, bg="#2d2d2d", fg="#a6e22e", font=("Consolas", 9))
        self.txt_bearing_res.grid(row=11, column=0, columnspan=2, sticky="nsew", pady=5)

        # Column 2: Horizontal & Vertical Curves Solver
        col2 = ttk.LabelFrame(fr_paned, text="Horizontal & Vertical Curve Solver", padding=10)
        col2.grid(row=0, column=1, sticky="nsew", padx=5, pady=5)

        # Subframe 2.1: Horizontal Curve Solver
        f_horiz = ttk.LabelFrame(col2, text="Horizontal circular curve inputs (fill any 2)", padding=8)
        f_horiz.pack(fill=tk.X, pady=4)

        ttk.Label(f_horiz, text="Radius (R):").grid(row=0, column=0, sticky=tk.W, pady=1)
        self.ent_h_rad = ttk.Entry(f_horiz, width=15)
        self.ent_h_rad.insert(0, "500.00")
        self.ent_h_rad.grid(row=0, column=1, sticky=tk.W, pady=1)

        ttk.Label(f_horiz, text="Delta Angle (°):").grid(row=0, column=2, sticky=tk.W, pady=1)
        self.ent_h_delta = ttk.Entry(f_horiz, width=15)
        self.ent_h_delta.insert(0, "90.00")
        self.ent_h_delta.grid(row=0, column=3, sticky=tk.W, pady=1)

        ttk.Label(f_horiz, text="Arc Length (L):").grid(row=1, column=0, sticky=tk.W, pady=1)
        self.ent_h_arc = ttk.Entry(f_horiz, width=15)
        self.ent_h_arc.grid(row=1, column=1, sticky=tk.W, pady=1)

        ttk.Label(f_horiz, text="Tangent (T):").grid(row=1, column=2, sticky=tk.W, pady=1)
        self.ent_h_tan = ttk.Entry(f_horiz, width=15)
        self.ent_h_tan.grid(row=1, column=3, sticky=tk.W, pady=1)

        ttk.Label(f_horiz, text="Chord (LC):").grid(row=2, column=0, sticky=tk.W, pady=1)
        self.ent_h_chord = ttk.Entry(f_horiz, width=15)
        self.ent_h_chord.grid(row=2, column=1, sticky=tk.W, pady=1)

        btn_h_solve = ttk.Button(f_horiz, text="📈 Solve Horizontal Curve", command=self.calc_horizontal_curve)
        btn_h_solve.grid(row=2, column=2, columnspan=2, sticky="ew", pady=1, padx=2)

        # Subframe 2.2: Vertical Curve Solver
        f_vert = ttk.LabelFrame(col2, text="Vertical parabolic curve inputs", padding=8)
        f_vert.pack(fill=tk.X, pady=4)

        ttk.Label(f_vert, text="PVI Station:").grid(row=0, column=0, sticky=tk.W, pady=1)
        self.ent_v_pvi_sta = ttk.Entry(f_vert, width=15)
        self.ent_v_pvi_sta.insert(0, "1000.00")
        self.ent_v_pvi_sta.grid(row=0, column=1, sticky=tk.W, pady=1)

        ttk.Label(f_vert, text="PVI Elev (ft):").grid(row=0, column=2, sticky=tk.W, pady=1)
        self.ent_v_pvi_el = ttk.Entry(f_vert, width=15)
        self.ent_v_pvi_el.insert(0, "100.00")
        self.ent_v_pvi_el.grid(row=0, column=3, sticky=tk.W, pady=1)

        ttk.Label(f_vert, text="Length (ft):").grid(row=1, column=0, sticky=tk.W, pady=1)
        self.ent_v_len = ttk.Entry(f_vert, width=15)
        self.ent_v_len.insert(0, "400.00")
        self.ent_v_len.grid(row=1, column=1, sticky=tk.W, pady=1)

        ttk.Label(f_vert, text="Grade In (%):").grid(row=1, column=2, sticky=tk.W, pady=1)
        self.ent_v_g1 = ttk.Entry(f_vert, width=15)
        self.ent_v_g1.insert(0, "-2.0")
        self.ent_v_g1.grid(row=1, column=3, sticky=tk.W, pady=1)

        ttk.Label(f_vert, text="Grade Out (%):").grid(row=2, column=0, sticky=tk.W, pady=1)
        self.ent_v_g2 = ttk.Entry(f_vert, width=15)
        self.ent_v_g2.insert(0, "4.0")
        self.ent_v_g2.grid(row=2, column=1, sticky=tk.W, pady=1)

        btn_vc = ttk.Button(f_vert, text="📈 Solve Vertical Profile", command=self.calc_vertical_curve)
        btn_vc.grid(row=2, column=2, columnspan=2, sticky="ew", pady=1, padx=2)

        # Subframe 2.3: Results Output Box
        self.txt_curve_res = tk.Text(col2, height=10, width=40, bg="#2d2d2d", fg="#66d9ef", font=("Consolas", 9))
        self.txt_curve_res.pack(fill=tk.BOTH, expand=True, pady=4)

        fr_paned.columnconfigure(0, weight=1)
        fr_paned.columnconfigure(1, weight=1)
        fr_paned.rowconfigure(0, weight=1)

    def calc_bearing_inverse(self):
        try:
            na = float(self.ent_na.get())
            ea = float(self.ent_ea.get())
            nb = float(self.ent_nb.get())
            eb = float(self.ent_eb.get())
            
            p1 = Point3D(na, ea)
            p2 = Point3D(nb, eb)
            
            dist, az = GeometryEngine.inverse(p1, p2)
            
            dn = nb - na
            de = eb - ea
            bearing_deg = math.degrees(math.atan2(abs(de), abs(dn)))
            degs = int(bearing_deg)
            mins = int((bearing_deg - degs) * 60)
            secs = round(((bearing_deg - degs) * 60 - mins) * 60)
            dms = f"{degs}°{mins:02d}'{secs:02d}\""
            
            if dn >= 0 and de >= 0: quad = f"N {dms} E"
            elif dn < 0 and de >= 0: quad = f"S {dms} E"
            elif dn < 0 and de < 0: quad = f"S {dms} W"
            else: quad = f"N {dms} W"
            
            self.txt_bearing_res.delete("1.0", tk.END)
            self.txt_bearing_res.insert(tk.END, "GEODETIC INVERSE RESULTS:\n")
            self.txt_bearing_res.insert(tk.END, "---------------------------\n")
            self.txt_bearing_res.insert(tk.END, f"Distance : {dist:.2f} ft\n")
            self.txt_bearing_res.insert(tk.END, f"Azimuth  : {az.degrees:.4f}°\n")
            self.txt_bearing_res.insert(tk.END, f"Bearing  : {quad}\n")
            self.txt_bearing_res.insert(tk.END, f"Delta N  : {dn:.2f} ft\n")
            self.txt_bearing_res.insert(tk.END, f"Delta E  : {de:.2f} ft\n")
        except Exception as e:
            messagebox.showerror("Error", f"Invalid coordinate inputs:\n{str(e)}")

    def calc_bearing_forward(self):
        try:
            na = float(self.ent_na.get())
            ea = float(self.ent_ea.get())
            dist = float(self.ent_trav_dist.get())
            brg_dms = float(self.ent_trav_brg.get())
            quad = int(self.ent_trav_quad.get())
            
            from rcs_cogo.primitives import Angle
            az = Angle.from_quadrant(quad, brg_dms)
            p1 = Point3D(na, ea)
            p2 = GeometryEngine.forward(p1, az, dist)
            
            self.txt_bearing_res.delete("1.0", tk.END)
            self.txt_bearing_res.insert(tk.END, "TRAVERSE FORWARD RESULTS:\n")
            self.txt_bearing_res.insert(tk.END, "---------------------------\n")
            self.txt_bearing_res.insert(tk.END, f"Start Pt  : N={na:.2f}, E={ea:.2f}\n")
            self.txt_bearing_res.insert(tk.END, f"Dest Pt   : N={p2.northing:.2f}, E={p2.easting:.2f}\n")
            
            self.ent_nb.delete(0, tk.END)
            self.ent_nb.insert(0, f"{p2.northing:.2f}")
            self.ent_eb.delete(0, tk.END)
            self.ent_eb.insert(0, f"{p2.easting:.2f}")
        except Exception as e:
            messagebox.showerror("Error", f"Invalid forward parameters:\n{str(e)}")

    def calc_horizontal_curve(self):
        try:
            r = float(self.ent_h_rad.get()) if self.ent_h_rad.get().strip() else None
            d = float(self.ent_h_delta.get()) if self.ent_h_delta.get().strip() else None
            arc = float(self.ent_h_arc.get()) if self.ent_h_arc.get().strip() else None
            tan = float(self.ent_h_tan.get()) if self.ent_h_tan.get().strip() else None
            chd = float(self.ent_h_chord.get()) if self.ent_h_chord.get().strip() else None
            
            params = [x for x in (r, d, arc, tan, chd) if x is not None]
            if len(params) < 2:
                messagebox.showwarning("Incomplete Inputs", "Please enter at least two parameters (e.g. Radius and Delta) to solve.")
                return
                
            from rcs_cogo.curves import CurveSolver
            res = CurveSolver.solve_horizontal_curve(radius=r, delta=d, arc_length=arc, tangent=tan, chord=chd)
            
            self.txt_curve_res.delete("1.0", tk.END)
            self.txt_curve_res.insert(tk.END, "HORIZONTAL CIRCULAR CURVE:\n")
            self.txt_curve_res.insert(tk.END, "---------------------------\n")
            self.txt_curve_res.insert(tk.END, f"Radius          : {res['Radius']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Delta Angle     : {res['Delta']:.6f}° ({CurveSolver.dd_to_dms(res['Delta']):.4f} DMS)\n")
            self.txt_curve_res.insert(tk.END, f"Arc Length (L)  : {res['ArcLength']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Tangent (T)     : {res['Tangent']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Chord (LC)      : {res['Chord']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"External (E)    : {res['External']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Mid Ordinate (M): {res['MiddleOrdinate']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Degree of Curve : {res['DegreeOfCurve']:.4f}°\n")
            
            # Populate entry fields
            self._fill_h_entry(self.ent_h_rad, res['Radius'])
            self._fill_h_entry(self.ent_h_delta, res['Delta'])
            self._fill_h_entry(self.ent_h_arc, res['ArcLength'])
            self._fill_h_entry(self.ent_h_tan, res['Tangent'])
            self._fill_h_entry(self.ent_h_chord, res['Chord'])
        except Exception as e:
            messagebox.showerror("Solver Error", f"Failed to solve horizontal curve: {str(e)}")

    def _fill_h_entry(self, entry, value):
        entry.delete(0, tk.END)
        entry.insert(0, f"{value}")

    def calc_vertical_curve(self):
        try:
            pvi_sta = float(self.ent_v_pvi_sta.get())
            pvi_el = float(self.ent_v_pvi_el.get())
            v_len = float(self.ent_v_len.get())
            g1 = float(self.ent_v_g1.get())
            g2 = float(self.ent_v_g2.get())
            
            from rcs_cogo.curves import CurveSolver
            res = CurveSolver.solve_vertical_curve(pvi_sta, pvi_el, v_len, g1, g2)
            
            self.txt_curve_res.delete("1.0", tk.END)
            self.txt_curve_res.insert(tk.END, "VERTICAL PARABOLIC PROFILE:\n")
            self.txt_curve_res.insert(tk.END, "---------------------------\n")
            self.txt_curve_res.insert(tk.END, f"PVC Station   : {res['PVC_Station']:.2f}\n")
            self.txt_curve_res.insert(tk.END, f"PVC Elevation : {res['PVC_Elevation']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"PVT Station   : {res['PVT_Station']:.2f}\n")
            self.txt_curve_res.insert(tk.END, f"PVT Elevation : {res['PVT_Elevation']:.2f} ft\n")
            self.txt_curve_res.insert(tk.END, f"Rate (r)      : {res['RateOfChange']:.6f} %/ft\n\n")
            
            self.txt_curve_res.insert(tk.END, f"Station     Elevation (ft)\n")
            self.txt_curve_res.insert(tk.END, "---------------------------\n")
            
            calc = res["ElevationCalculator"]
            start_sta = res['PVC_Station']
            end_sta = res['PVT_Station']
            
            current = math.ceil(start_sta / 50.0) * 50.0
            self.txt_curve_res.insert(tk.END, f"{start_sta:<11.2f} {calc(start_sta):.2f}\n")
            
            while current < end_sta:
                self.txt_curve_res.insert(tk.END, f"{current:<11.2f} {calc(current):.2f}\n")
                current += 50.0
                
            self.txt_curve_res.insert(tk.END, f"{end_sta:<11.2f} {calc(end_sta):.2f}\n")
        except Exception as e:
            messagebox.showerror("Error", f"Failed to solve vertical curve:\n{str(e)}")

    def _build_advanced_suite_tab(self):
        fr = ttk.Frame(self.tab_advanced, padding=10)
        fr.pack(fill=tk.BOTH, expand=True)

        lbl_title = ttk.Label(fr, text="Civil 3D & ESRI Advanced Calculations", font=("Segoe UI", 12, "bold"))
        lbl_title.pack(anchor=tk.W, pady=(0, 6))

        # Dropdown selection
        f_select = ttk.Frame(fr)
        f_select.pack(fill=tk.X, pady=4)
        
        ttk.Label(f_select, text="Select Tool:").pack(side=tk.LEFT, padx=(0, 6))
        
        from rcs_cogo.advanced_suite import AdvancedSurveySuite
        self.suite = AdvancedSurveySuite()
        suite_methods = sorted([attr for attr in dir(self.suite) if callable(getattr(self.suite, attr)) and not attr.startswith('_')])
        
        self.cb_tool = ttk.Combobox(f_select, values=suite_methods, width=50, state="readonly")
        self.cb_tool.pack(side=tk.LEFT, fill=tk.X, expand=True)
        self.cb_tool.bind("<<ComboboxSelected>>", self.on_tool_selected)
        
        # Metadata / Parameter Names label
        self.lbl_tool_params = ttk.Label(fr, text="Expected Parameters: Select a tool...", font=("Segoe UI", 9, "italic"), foreground="#89d4f5")
        self.lbl_tool_params.pack(anchor=tk.W, pady=6)

        # Arguments input
        f_args = ttk.LabelFrame(fr, text="Input Arguments (comma-separated list, e.g. 0, 0, 50)", padding=10)
        f_args.pack(fill=tk.X, pady=5)
        
        self.ent_tool_args = ttk.Entry(f_args, font=("Consolas", 10))
        self.ent_tool_args.pack(fill=tk.X, expand=True)
        
        # Run Button
        self.btn_run_tool = ttk.Button(fr, text="⚡ Run Geoprocessing Calculation", command=self.run_advanced_tool)
        self.btn_run_tool.pack(fill=tk.X, pady=6)

        # Output box
        lbl_out = ttk.Label(fr, text="Output Result:")
        lbl_out.pack(anchor=tk.W, pady=(6, 2))
        
        self.txt_tool_output = tk.Text(fr, height=12, bg="#2d2d2d", fg="#a6e22e", font=("Consolas", 9), bd=0, highlightthickness=1, highlightcolor=COLOR_ACCENT)
        self.txt_tool_output.pack(fill=tk.BOTH, expand=True)

        if suite_methods:
            self.cb_tool.set(suite_methods[0])
            self.on_tool_selected(None)

    def on_tool_selected(self, event):
        method_name = self.cb_tool.get()
        if not method_name: return
        
        import inspect
        method = getattr(self.suite, method_name)
        try:
            sig = inspect.signature(method)
            params = [name for name in sig.parameters.keys() if name != 'self']
            self.lbl_tool_params.configure(text=f"Expected Parameters: {', '.join(params)}")
            
            # Intelligently set default inputs to make testing easy for the user!
            defaults = []
            for p in params:
                p_lower = p.lower()
                if "x" in p_lower or "east" in p_lower: defaults.append("10000.00")
                elif "y" in p_lower or "north" in p_lower: defaults.append("10000.00")
                elif "z" in p_lower or "elev" in p_lower: defaults.append("10.0")
                elif "radius" in p_lower or "rad" in p_lower or p in ("r", "r1", "r2"): defaults.append("150.0")
                elif "bearing" in p_lower or "brg" in p_lower: defaults.append("n45.0000e")
                elif "dist" in p_lower or p in ("d", "dist"): defaults.append("250.0")
                elif "slope" in p_lower or p == "s": defaults.append("1.5")
                elif "diameter" in p_lower or "dia" in p_lower: defaults.append("8")
                elif "material" in p_lower: defaults.append("PVC")
                elif "pts" in p_lower or "coords" in p_lower or "list" in p_lower: defaults.append("[(10000, 10000), (10100, 10050), (10050, 10150)]")
                else: defaults.append("0")
            
            self.ent_tool_args.delete(0, tk.END)
            self.ent_tool_args.insert(0, ", ".join(defaults))
        except Exception:
            self.lbl_tool_params.configure(text="Expected Parameters: Unknown")

    def run_advanced_tool(self):
        method_name = self.cb_tool.get()
        if not method_name: return
        
        raw_args = self.ent_tool_args.get().strip()
        
        import ast
        try:
            if not raw_args:
                parsed = ()
            else:
                parsed = ast.literal_eval(f"({raw_args},)")
        except Exception:
            parsed = []
            for item in raw_args.split(','):
                item_str = item.strip()
                try:
                    parsed.append(float(item_str))
                except ValueError:
                    parsed.append(item_str)
            parsed = tuple(parsed)

        self.txt_tool_output.delete("1.0", tk.END)
        try:
            method = getattr(self.suite, method_name)
            if len(parsed) == 1 and not raw_args.endswith(','):
                arg_val = parsed[0]
                result = method(arg_val)
            else:
                import inspect
                sig = inspect.signature(method)
                params = [name for name in sig.parameters.keys() if name != 'self']
                if len(params) == 1 and len(parsed) > 1:
                    result = method(list(parsed))
                else:
                    result = method(*parsed)

            self.txt_tool_output.insert(tk.END, f"SUCCESS:\n{str(result)}")
        except Exception as e:
            self.txt_tool_output.insert(tk.END, f"ERROR EXECUTING TOOL:\n{str(e)}")

if __name__ == "__main__":
    root = tk.Tk()
    app = CogoGuiApp(root)
    root.mainloop()
