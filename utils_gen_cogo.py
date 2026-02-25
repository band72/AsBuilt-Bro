import random

file_path = "C:\\Users\\Daryl Banks\\source\\repos\\RCS.Cogo.Enterprise.Modern\\TEST_COGO_V2.txt"

with open(file_path, "w") as f:
    f.write("// ROBUST COGO STRESS TEST V2 (1000+ Lines)\n")
    f.write("// Testing all implemented COGO routines\n\n")
    
    f.write("COGO-ENGINE-ON\n")
    
    f.write("// 1. Auto Point Numbering (AP) Toggle\n")
    f.write("AP ON\n")
    for i in range(1, 51):
        f.write(f"NEZ {i} {5000 + i*10:.3f} {5000 + i*5:.3f} 10.0 BASE_PT_{i}\n")
    f.write("AP OFF\n\n")
    
    f.write("// 2. Standard Setup & Traverses (OC, BS, FS, TRAV)\n")
    f.write("OC 1 5.25\n")
    f.write("BS 2 45.0000\n")
    
    pt = 101
    for i in range(300):
        if i % 2 == 0:
            f.write(f"FS {pt} {random.uniform(10, 80):.4f} {random.uniform(50, 150):.2f} SIDE_SHOT\n")
        else:
            f.write(f"TRAV {pt} {random.uniform(90, 180):.4f} {random.uniform(100, 200):.2f} TRAV_PT\n")
        pt += 1

    f.write("\n// 3. Analytics (INV, AZ, LN)\n")
    for i in range(1, 101):
        f.write(f"INV {i} {i+1}\n")
        f.write(f"AZ {i} {i+1}\n")
        f.write(f"LN {i} {i+1}\n")
        
    f.write("\n// 4. Intersections (AZAZ, BB, LNLN, RKRK)\n")
    for i in range(25):
        p1 = random.randint(1, 50)
        p2 = random.randint(51, 100)
        p3 = random.randint(101, 150)
        p4 = random.randint(151, 200)
        
        f.write(f"AZAZ {pt} {p1} 45.0000 {p2} 135.0000 AZAZ_PT\n")
        pt += 1
        f.write(f"BB {pt} {p1} 45.0000 1 {p2} 45.0000 4 BB_PT\n")
        pt += 1
        f.write(f"LNLN {pt} {p1} {p2} {p3} {p4} LNLN_PT\n")
        pt += 1
        f.write(f"RKRK {pt} {p1} 150.0 {p2} 200.0 RKRK_PT\n")
        pt += 1
        
    f.write("\n// 5. Directional Definitions (BD, AD, DD, ZD)\n")
    for i in range(100):
        f.write(f"BD {pt} 45.3000 1 100.0 BRG_DIST\n")
        pt += 1
        f.write(f"AD {pt} 90.0000 50.0 ANG_RIGHT\n")
        pt += 1
        f.write(f"DD {pt} 45.0000 75.0 DEFLECT\n")
        pt += 1
        f.write(f"ZD {pt} 85.0000 120.0 ZENITH\n")
        pt += 1

    f.write("\n// 6. Figures & Geometry (B, L, CONT, XC, C, E)\n")
    for i in range(20):
        base = 1 + (i*5)
        f.write(f"B {base}\n")
        f.write(f"L {base+1}\n")
        f.write(f"CONT {base+2}\n")
        f.write(f"XC PTS 100.0 {base+3} {base+4}\n")
        f.write("C\n")
        
        # Second figure left open
        f.write(f"BEG {base+50}\n")
        f.write(f"L {base+51}\n")
        f.write(f"L {base+52}\n")
        f.write("END\n")

    f.write("\n// 7. Transformations (TRN, ROT)\n")
    # Using User's requested formatting semantics
    f.write("TRN 1 100 1-50\n")
    f.write("ROT 1-2 2-3 51-100\n")
    
    f.write("\n// END OF COGO STRESS TEST")
    
print(f"Generated {file_path}")
