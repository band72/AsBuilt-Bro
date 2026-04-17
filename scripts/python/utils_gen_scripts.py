import random

def generate_script(utility_name, short_code, is_gravity, material, start_id, file_path):
    # Determine structure types
    if utility_name == "Water":
        struct_types = ["Valve", "Hydrant"]
    elif utility_name == "Gas" or utility_name == "Electric":
        struct_types = ["Valve", "Vault"]
    else:  # Storm, Sewer, Waste Water
        struct_types = ["Manhole", "CatchBasin"] if utility_name == "Storm" else ["Manhole"]

    # Generate 1100 lines (500 pts, 600 piping lines)
    num_points = 500
    
    with open(file_path, "w") as f:
        f.write(f"// {utility_name} Robust Pipe Connectivity Test V2 (1000+ Lines)\n")
        f.write("// Storing foundational coordinates...\n")
        
        # 1. Generate Points
        for i in range(num_points):
            n = 5000.0 + random.uniform(-1000.0, 1000.0)
            e = 5000.0 + random.uniform(-1000.0, 1000.0)
            z = 100.0 + (i * 0.1) # Slowly step up elevation for gravity logic
            f.write(f"NEZ {start_id + i} {n:.3f} {e:.3f} {z:.3f} NODE_{start_id+i}\n")
        
        f.write("\nPIPE-ENGINE-ON\n\n")
        
        # 2. Run declarations
        # We will do 5 large PRUN blocks to get lines up
        points_per_run = 100
        for run_idx in range(5):
            f.write(f"PRUN START {short_code} DIAM 8 MAT {material}\n")
            
            run_start = start_id + (run_idx * points_per_run)
            
            f.write(f"{short_code}-B {run_start}\n")
            
            for pt_offset in range(1, points_per_run):
                pt = run_start + pt_offset
                
                # Drop random structures every ~10 points
                if pt_offset % 10 == 0:
                    str_type = random.choice(struct_types)
                    f.write(f"{short_code}-C {pt} {str_type}\n")
                else:
                    f.write(f"{short_code}-C {pt}\n")
            
            f.write(f"{short_code}-E\n")
            f.write("PRUN END\n\n")
            
        # 3. Dedicated structure storage (SS-C) to flush out line count
        f.write("// Dedicated structural nodes offset\n")
        f.write(f"PRUN START {short_code} DIAM 10 MAT {material}\n")
        for st in range(50):
            str_type = random.choice(struct_types)
            pt = start_id + st + 450 # Use the tail end of points
            f.write(f"SS-C {pt} {str_type}\n")
        f.write("PRUN END\n")
        
        f.write("\n// Script End.")
        
    print(f"Generated {file_path}")

print("Generating Water...")
generate_script("Water", "W", False, "PVC", 1000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_WATER_V2.txt")

print("Generating Storm...")
generate_script("Storm", "ST", True, "RCP", 2000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_STORM_V2.txt")

print("Generating Sewer...")
generate_script("Sewer", "SEW", True, "PVC", 3000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_SEWER_V2.txt")

print("Generating Waste Water...")
generate_script("Waste Water", "WW", True, "PVC", 4000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_WASTE_WATER_V2.txt")

print("Generating Gas...")
generate_script("Gas", "G", False, "PE", 5000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_GAS_V2.txt")

print("Generating Electric...")
generate_script("Electric", "E", False, "PVC", 6000, "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_ELECTRIC_V2.txt")
