import re

target_file = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\InstalledAssetsView.xaml'

with open(target_file, 'r', encoding='utf-8') as f:
    xaml = f.read()

global_columns = """                             <DataGridTextColumn Header="Subtype" Binding="{Binding Subtype}"/>
                             <DataGridTextColumn Header="Facility Owner" Binding="{Binding FacilityOwner}"/>
                             <DataGridTextColumn Header="Size Sec" Binding="{Binding SizeSecondary}"/>
                             <DataGridTextColumn Header="Pipe Class" Binding="{Binding PipeClass}"/>
                             <DataGridTextColumn Header="Lining Manuf" Binding="{Binding LiningManufacturer}"/>
                             <DataGridTextColumn Header="Lining Mat" Binding="{Binding LiningMaterial}"/>
                             <DataGridTextColumn Header="Orientation" Binding="{Binding Orientation}"/>
                             <DataGridTextColumn Header="Pipe Role" Binding="{Binding PipeRole}"/>
                             <DataGridTextColumn Header="RFID/Barcode" Binding="{Binding RfidBarcode}"/>
                             <DataGridTextColumn Header="Drop Type" Binding="{Binding DropType}"/>
                             <DataGridTextColumn Header="Invert Elev Dirs" Binding="{Binding InvertElevationsWithDirections}"/>
                             <DataGridTextColumn Header="Joint Tape Type" Binding="{Binding ExteriorJointTapeType}"/>
                             <DataGridTextColumn Header="Joint Tape Man" Binding="{Binding ExteriorJointTapeManufacturer}"/>"""

valve_columns = """                             <DataGridTextColumn Header="Open Dir" Binding="{Binding OpenDirection}"/>
                             <DataGridTextColumn Header="Turns To Open" Binding="{Binding TurnsToOpen}"/>
                             <DataGridTextColumn Header="Nut Elev" Binding="{Binding NutElevation}"/>"""

pipe_columns = """                             <DataGridTextColumn Header="Grade Elev Start" Binding="{Binding GradeElevationAtInvertStart}"/>
                             <DataGridTextColumn Header="Grade Elev End" Binding="{Binding GradeElevationAtInvertEnd}"/>"""

crossing_columns = """                             <DataGridTextColumn Header="Crossing Num" Binding="{Binding CrossingNumber}"/>
                             <DataGridTextColumn Header="Upper Type" Binding="{Binding UpperPipeType}"/>
                             <DataGridTextColumn Header="Upper Size" Binding="{Binding UpperPipeSize}"/>
                             <DataGridTextColumn Header="Finish Grade Elev" Binding="{Binding FinishedGradeElevation}"/>
                             <DataGridTextColumn Header="Upper Top Elev" Binding="{Binding UpperPipeTopElevation}"/>
                             <DataGridTextColumn Header="Upper Cover" Binding="{Binding UpperCover}"/>
                             <DataGridTextColumn Header="Upper Bot Elev" Binding="{Binding UpperPipeBottomElevation}"/>
                             <DataGridTextColumn Header="Lower Type" Binding="{Binding LowerPipeType}"/>
                             <DataGridTextColumn Header="Lower Size" Binding="{Binding LowerPipeSize}"/>
                             <DataGridTextColumn Header="Lower Top Elev" Binding="{Binding LowerPipeTopElevation}"/>
                             <DataGridTextColumn Header="Lower Cover" Binding="{Binding LowerCover}"/>
                             <DataGridTextColumn Header="Separation" Binding="{Binding Separation}"/>"""

def process_expander(match):
    header = match.group(1)
    content = match.group(2)
    
    if "Figure Assets" in header:
        return match.group(0)

    # Prevent double run
    if "Binding=\"{Binding Subtype}\"" in content or "Binding=\"{Binding CrossingNumber}\"" in content:
        return match.group(0)
        
    insert_text = global_columns
    
    if "Crossing" in header:
        insert_text += "\n" + crossing_columns
    else:
        if "Valve" in header:
             insert_text += "\n" + valve_columns
        if "Pipe" in header:
             insert_text += "\n" + pipe_columns
             
    if '<DataGridTextColumn Header="N"' in content:
        content = content.replace('<DataGridTextColumn Header="N"', insert_text + '\n                             <DataGridTextColumn Header="N"')
    elif '<DataGridTextColumn Header="N Start"' in content:
        content = content.replace('<DataGridTextColumn Header="N Start"', insert_text + '\n                             <DataGridTextColumn Header="N Start"')
    else:
        content = content.replace('</DataGrid.Columns>', insert_text + '\n                         </DataGrid.Columns>')
            
    return f'<Expander Header="{header}">{content}</Expander>'

pattern = re.compile(r'<Expander Header="([^"]+)">(.*?)</Expander>', re.DOTALL)
new_xaml = pattern.sub(process_expander, xaml)

with open(target_file, 'w', encoding='utf-8') as f:
    f.write(new_xaml)

print("Updated InstalledAssetsView.xaml successfully.")
