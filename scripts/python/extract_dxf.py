import sys

def parse_dxf(filepath):
    water_layers = { 'PR-WATER', 'X-WATER', 'CU-WATR-DOMC-WSYSTEMVALVE' }
    sewer_layers = { 'C-SSWR-CNTR', 'C-SSWR-PIPE', 'PR-SEWER LINE W LPS' }
    
    entities = []
    
    with open(filepath, 'r', encoding='latin-1', errors='ignore') as f:
        lines = [line.strip() for line in f.readlines()]
        
    in_entities = False
    
    i = 0
    while i < len(lines):
        if lines[i] == '0' and lines[i+1] == 'SECTION':
            if i+3 < len(lines) and lines[i+2] == '2' and lines[i+3] == 'ENTITIES':
                in_entities = True
                i += 4
                continue
                
        if in_entities and lines[i] == '0' and lines[i+1] == 'ENDSEC':
            in_entities = False
            
        if in_entities and lines[i] == '0':
            etype = lines[i+1]
            i += 2
            layer = None
            points_10 = []
            points_20 = []
            points_11 = []
            points_21 = []
            
            while i < len(lines) and lines[i] != '0':
                code = lines[i]
                val = lines[i+1]
                if code == '8':
                    layer = val
                elif code == '10':
                    points_10.append(float(val))
                elif code == '20':
                    points_20.append(float(val))
                elif code == '11':
                    points_11.append(float(val))
                elif code == '21':
                    points_21.append(float(val))
                i += 2
            
            # Now process the collected entity
            if layer in water_layers or layer in sewer_layers:
                if etype in {"LINE", "LWPOLYLINE", "CIRCLE", "INSERT"}:
                    pts = []
                    if etype == "LINE" and len(points_10)>0 and len(points_20)>0 and len(points_11)>0 and len(points_21)>0:
                        pts = [(points_10[0], points_20[0]), (points_11[0], points_21[0])]
                    elif etype == "LWPOLYLINE" and len(points_10)>0 and len(points_20)>0:
                        pts = list(zip(points_10, points_20))
                    elif etype == "CIRCLE" and len(points_10)>0 and len(points_20)>0:
                        pts = [(points_10[0], points_20[0])]
                    elif etype == "INSERT" and len(points_10)>0 and len(points_20)>0:
                        pts = [(points_10[0], points_20[0])]
                    
                    if pts:
                        entities.append((layer, etype, pts))
            continue
        i += 1
        
    return entities, water_layers, sewer_layers

def main():
    filepath = sys.argv[1]
    outpath = sys.argv[2]
    entities, water_layers, sewer_layers = parse_dxf(filepath)
    
    unique_points = {}
    pt_id = 1
    
    def get_pt(x, y):
        nonlocal pt_id
        k = (round(x, 2), round(y, 2))
        if k not in unique_points:
            unique_points[k] = (pt_id, x, y)
            pt_id += 1
        return unique_points[k][0]

    water_runs = []
    sewer_runs = []
    water_structs = []
    sewer_structs = []
    
    for layer, etype, pts in entities:
        util = 'W' if layer in water_layers else 'WW'
        
        if etype in {"LINE", "LWPOLYLINE"} and len(pts) > 1:
            run_pts = [get_pt(p[0], p[1]) for p in pts]
            if util == 'W':
                water_runs.append(run_pts)
            else:
                sewer_runs.append(run_pts)
                
        elif etype in {"CIRCLE", "INSERT"} and len(pts) > 0:
            pid = get_pt(pts[0][0], pts[0][1])
            if util == 'W':
                water_structs.append(pid)
            else:
                sewer_structs.append(pid)
                
    with open(outpath, 'w', encoding='utf-8') as f:
        f.write("PIPE-ENGINE-ON\n")
        for k, v in sorted(unique_points.items(), key=lambda x: x[1][0]):
            f.write(f"NEZ {v[0]} {v[2]:.3f} {v[1]:.3f} 0.000\n")
            
        f.write("\n")
        
        if water_runs or water_structs:
            f.write("PRUN START W DIAM 8 MAT PVC\n")
            for struct in water_structs:
                f.write(f"SS-C {struct} Valve\n")
            for run in water_runs:
                f.write(f"W-B {run[0]}\n")
                for n in run[1:]:
                    f.write(f"W-C {n}\n")
                f.write(f"W-E\n")
            f.write("PRUN END\n\n")
            
        if sewer_runs or sewer_structs:
            f.write("PRUN START WW DIAM 8 MAT PVC\n")
            for struct in sewer_structs:
                f.write(f"SS-C {struct} Manhole\n")
            for run in sewer_runs:
                f.write(f"WW-B {run[0]}\n")
                for n in run[1:]:
                    f.write(f"WW-C {n}\n")
                f.write(f"WW-E\n")
            f.write("PRUN END\n")

if __name__ == '__main__':
    main()
