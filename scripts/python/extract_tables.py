import pdfplumber
import pandas as pd
import sys
import os

pdf_path = sys.argv[1]
excel_path = sys.argv[2]

tables_found = 0
with pdfplumber.open(pdf_path) as pdf:
    # Use pandas ExcelWriter
    with pd.ExcelWriter(excel_path, engine='openpyxl') as writer:
        for i, page in enumerate(pdf.pages):
            tables = page.extract_tables()
            for j, table in enumerate(tables):
                # Clean up empty headers / first rows if needed here
                # Using the first row as columns, the rest as data
                
                # Check if the table has any rows
                if not table: continue
                
                # Sometime first row is partial. But we'll try basic approach
                # We need to make sure we don't have duplicate column names, which pandas hates
                df = pd.DataFrame(table)
                
                # We'll avoid setting first row as column header automatically if it causes issues,
                # but let's try it:
                if len(table) > 1:
                    df.columns = df.iloc[0]
                    df = df[1:]
                
                sheet_name = f"Page_{i+1}_Table_{j+1}"
                df.to_excel(writer, sheet_name=sheet_name, index=False)
                tables_found += 1

# If no tables found, let's create a dummy file so process doesn't fail completely, or just a printed message.
if tables_found == 0:
    print(f"No tables found in the PDF.")
else:
    print(f"Extracted {tables_found} tables to {excel_path}")
