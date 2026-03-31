import pandas as pd
import numpy as np
import os

filepath = r"c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\SampleScripts\Simulated_JEA_AsBuilt_Template.xlsx"

# Coordinates generation
base_x = 422000.0
base_y = 2170000.0

def gen_coords(count):
    return [base_x + (i * 50) for i in range(count)], [base_y + (i * 50) for i in range(count)]

rows = 15
xs, ys = gen_coords(rows)

base_elev = 20.0

# 1. Sewer Manhole
sm_data = {
    'Manhole Number': [f"SM-{1000+i}" for i in range(rows)],
    'Subtype': ['Sewer Manhole'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Manhole Type': ['Standard'] * rows,
    'Manhole Drop Type': ['Inside'] * rows,
    'Manhole Size (Feet)': [4] * rows,
    'Material': ['Precast Concrete'] * rows,
    'Lining Material': ['Epoxy'] * rows,
    'Depth (Feet)': [10.5] * rows,
    'Rim Elevation (Feet)': [base_elev + i for i in range(rows)],
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 2. Sewer Pipe (connecting the manholes)
sp_data = {
    'Pipe Number': [f"SP-{100+i}" for i in range(rows)],
    'Subtype': ['Sewer Gravity Main'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Material': ['PVC'] * rows,
    'Pipe Size (Inches)': [8] * rows,
    'Upstream Manhole': [f"SM-{1000+i}" for i in range(rows)],
    'Downstream Manhole': [f"SM-{1000+i+1}" for i in range(rows)],
    'Upstream Invert': [base_elev + i - 10.5 for i in range(rows)],
    'Downstream Invert': [base_elev + i - 10.8 for i in range(rows)],
    'Length (Feet)': [50.0] * rows,
    'Slope (%)': [0.6] * rows
}

# 3. Water Pipe
wp_data = {
    'Pipe Number': [f"WP-{200+i}" for i in range(rows)],
    'Subtype': ['Water Distribution Main'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Material': ['DIP'] * rows,
    'Pipe Size (Inches)': [12] * rows,
    'Length (Feet)': [50.0] * rows,
    'GPS Start Y Coord': ys,
    'GPS Start X Coord': xs,
    'GPS End Y Coord': [y + 50.0 for y in ys],
    'GPS End X Coord': [x + 50.0 for x in xs]
}

# 4. Water Fitting
wf_data = {
    'Fitting Number': [f"WF-{300+i}" for i in range(rows)],
    'Subtype': ['Tee' if i%2==0 else 'Bend 45' for i in range(rows)],
    'Facility Owner': ['JEA'] * rows,
    'Fitting Size Primary (Inches)': [12] * rows,
    'Fitting Size Reducer (Inches)': [8 if i%2==0 else '' for i in range(rows)],
    'Material': ['DIP'] * rows,
    'Elevation (Feet)': [base_elev - 3.0] * rows,
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 5. Water Valve
wv_data = {
    'Valve Number': [f"WV-{400+i}" for i in range(rows)],
    'Subtype': ['Gate Valve'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Valve Size (Inches)': [12] * rows,
    'Open Direction': ['Left'] * rows,
    'Turns To Open': [38] * rows,
    'Elevation (Feet)': [base_elev - 3.0] * rows,
    'GPS Y Coord': [y + 5.0 for y in ys],
    'GPS X Coord': [x + 5.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 6. Water Hydrant
wh_data = {
    'Hydrant Number': [f"WH-{500+i}" for i in range(rows)],
    'Subtype': ['Fire Hydrant'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Manufacturer': ['Mueller'] * rows,
    'Elevation (Feet)': [base_elev] * rows,
    'GPS Y Coord': [y + 10.0 for y in ys],
    'GPS X Coord': [x + 10.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 7. Water Meter
wm_data = {
    'Meter Number': [f"WM-{600+i}" for i in range(rows)],
    'Subtype': ['Potable Water Meter'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Meter Size (Inches)': [1] * rows,
    'GPS Y Coord': [y + 15.0 for y in ys],
    'GPS X Coord': [x + 15.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 8. Sewer Fitting
sf_data = {
    'Fitting Number': [f"SF-{700+i}" for i in range(rows)],
    'Subtype': ['Wye'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Fitting Size Primary (Inches)': [8] * rows,
    'Fitting Size Reducer (Inches)': [6] * rows,
    'Material': ['PVC'] * rows,
    'Elevation (Feet)': [base_elev - 10.0] * rows,
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 9. Sewer Valve
sv_data = {
    'Valve Number': [f"SV-{800+i}" for i in range(rows)],
    'Subtype': ['Air Release Valve'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Valve Size (Inches)': [2] * rows,
    'Elevation (Feet)': [base_elev - 4.0] * rows,
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 10. Sewer Meter
s_meter_data = {
    'Meter Number': [f"SMT-{900+i}" for i in range(rows)],
    'Subtype': ['Sewer Force Main Meter'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Meter Size (Inches)': [4] * rows,
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 11. Reclaimed Pipe
rp_data = {
    'Pipe Number': [f"RP-{200+i}" for i in range(rows)],
    'Subtype': ['Reclaimed Water Main'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Material': ['PVC'] * rows,
    'Pipe Size (Inches)': [16] * rows,
    'Length (Feet)': [50.0] * rows,
    'GPS Start Y Coord': [y + 10.0 for y in ys],
    'GPS Start X Coord': [x + 10.0 for x in xs],
    'GPS End Y Coord': [y + 60.0 for y in ys],
    'GPS End X Coord': [x + 60.0 for x in xs]
}

# 12. Reclaimed Fitting
rf_data = {
    'Fitting Number': [f"RF-{300+i}" for i in range(rows)],
    'Subtype': ['Tee' if i%2==0 else 'Bend 45' for i in range(rows)],
    'Facility Owner': ['JEA'] * rows,
    'Fitting Size Primary (Inches)': [16] * rows,
    'Fitting Size Reducer (Inches)': [12 if i%2==0 else '' for i in range(rows)],
    'Material': ['PVC'] * rows,
    'Elevation (Feet)': [base_elev - 4.0] * rows,
    'GPS Y Coord': ys,
    'GPS X Coord': xs,
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 13. Reclaimed Valve
rv_data = {
    'Valve Number': [f"RV-{400+i}" for i in range(rows)],
    'Subtype': ['Gate Valve'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Valve Size (Inches)': [16] * rows,
    'Open Direction': ['Left'] * rows,
    'Turns To Open': [50] * rows,
    'Elevation (Feet)': [base_elev - 4.0] * rows,
    'GPS Y Coord': [y + 5.0 for y in ys],
    'GPS X Coord': [x + 5.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 14. Reclaimed Hydrant
rh_data = {
    'Hydrant Number': [f"RH-{500+i}" for i in range(rows)],
    'Subtype': ['Reclaimed Hydrant'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Manufacturer': ['Clow'] * rows,
    'Elevation (Feet)': [base_elev] * rows,
    'GPS Y Coord': [y + 10.0 for y in ys],
    'GPS X Coord': [x + 10.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# 15. Reclaimed Meter
rm_data = {
    'Meter Number': [f"RM-{600+i}" for i in range(rows)],
    'Subtype': ['Reclaimed Water Meter'] * rows,
    'Facility Owner': ['JEA'] * rows,
    'Meter Size (Inches)': [2] * rows,
    'GPS Y Coord': [y + 15.0 for y in ys],
    'GPS X Coord': [x + 15.0 for x in xs],
    'GPS Latitude': [30.3 + (i*0.0001) for i in range(rows)],
    'GPS Longitude': [-81.6 + (i*0.0001) for i in range(rows)]
}

# Write sheets
with pd.ExcelWriter(filepath) as writer:
    pd.DataFrame(sm_data).to_excel(writer, sheet_name='Sewer Manhole', index=False)
    pd.DataFrame(sp_data).to_excel(writer, sheet_name='Sewer Pipe', index=False)
    pd.DataFrame(wp_data).to_excel(writer, sheet_name='Water Pipe', index=False)
    pd.DataFrame(wf_data).to_excel(writer, sheet_name='Water Fitting', index=False)
    pd.DataFrame(wv_data).to_excel(writer, sheet_name='Water Valve', index=False)
    pd.DataFrame(wh_data).to_excel(writer, sheet_name='Water Hydrant', index=False)
    pd.DataFrame(wm_data).to_excel(writer, sheet_name='Water Meter', index=False)
    
    pd.DataFrame(sf_data).to_excel(writer, sheet_name='Sewer Fitting', index=False)
    pd.DataFrame(sv_data).to_excel(writer, sheet_name='Sewer Valve', index=False)
    pd.DataFrame(s_meter_data).to_excel(writer, sheet_name='Sewer Meter', index=False)
    
    pd.DataFrame(rp_data).to_excel(writer, sheet_name='Reclaimed Pipe', index=False)
    pd.DataFrame(rf_data).to_excel(writer, sheet_name='Reclaimed Fitting', index=False)
    pd.DataFrame(rv_data).to_excel(writer, sheet_name='Reclaimed Valve', index=False)
    pd.DataFrame(rh_data).to_excel(writer, sheet_name='Reclaimed Hydrant', index=False)
    pd.DataFrame(rm_data).to_excel(writer, sheet_name='Reclaimed Meter', index=False)

print(f"Successfully generated {filepath} with 15 populated tabs!")
