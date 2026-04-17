import pdfplumber
import pandas as pd
import re
import sys

pdf_path = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE.pdf'
excel_path = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE_Tables.xlsx'

# --- Pages known to contain actual data tables (1-indexed) ---
DATA_PAGES = list(range(2, 10))   # Pages 2–9

def clean_text(val):
    """Strip whitespace and normalize None/empty."""
    if val is None:
        return ""
    return str(val).strip()

def is_empty_row(row):
    """True if every cell in the row is blank."""
    return all(clean_text(c) == "" for c in row)

def is_mostly_empty(row, threshold=0.75):
    """True if more than `threshold` fraction of cells are blank."""
    empty = sum(1 for c in row if clean_text(c) == "")
    return (empty / len(row)) >= threshold if row else True

def deduplicate_columns(cols):
    """Make column names unique by appending _1, _2 … when duplicated."""
    seen = {}
    result = []
    for c in cols:
        c = clean_text(c) or "Column"
        if c in seen:
            seen[c] += 1
            result.append(f"{c}_{seen[c]}")
        else:
            seen[c] = 0
            result.append(c)
    return result

def find_header_row(table):
    """
    Scan from the top to find the first row that has a meaningful
    number of non-empty cells — treat that as the header.
    Returns (header_idx, header_list).
    """
    for i, row in enumerate(table):
        non_empty = sum(1 for c in row if clean_text(c))
        if non_empty >= max(2, len(row) * 0.25):   # at least 25% filled
            return i, row
    return 0, table[0]

def guess_table_name(page_num, table_idx, table):
    """
    Look for a row that looks like a table title (single long text cell).
    Fall back to 'Page{N}_Table{M}'.
    """
    for row in table[:5]:
        texts = [clean_text(c) for c in row if clean_text(c)]
        if len(texts) == 1 and len(texts[0]) > 8:
            # Looks like a title row — shorten it for a sheet name
            title = re.sub(r'[\\/*?\[\]:]', ' ', texts[0])[:30].strip()
            return title
    return f"Pg{page_num}_T{table_idx}"

def build_dataframe(table):
    """
    Turn a raw pdfplumber table (list of lists) into a clean DataFrame.
    """
    if not table or len(table) < 2:
        return None

    # Drop rows that are completely empty
    table = [r for r in table if not is_empty_row(r)]
    if len(table) < 2:
        return None

    # Find header row
    h_idx, header_row = find_header_row(table)
    data_rows = table[h_idx + 1:]

    # Build DataFrame
    cols = deduplicate_columns(header_row)
    rows_cleaned = []
    for row in data_rows:
        # Pad / trim to match column count
        row = list(row) + [""] * (len(cols) - len(row))
        row = row[:len(cols)]
        if is_mostly_empty(row, threshold=0.9):
            continue   # skip near-blank data rows
        rows_cleaned.append([clean_text(c) for c in row])

    if not rows_cleaned:
        return None

    df = pd.DataFrame(rows_cleaned, columns=cols)
    return df

# ------------------------------------------------------------------ #
#  Main extraction                                                     #
# ------------------------------------------------------------------ #
tables_written = 0
skipped = 0

extraction_settings = {
    "vertical_strategy": "lines",
    "horizontal_strategy": "lines",
    "snap_tolerance": 5,
    "join_tolerance": 3,
}

print(f"Opening PDF: {pdf_path}")
with pdfplumber.open(pdf_path) as pdf:
    total_pages = len(pdf.pages)
    print(f"Total pages in PDF: {total_pages}")

    with pd.ExcelWriter(excel_path, engine="openpyxl") as writer:
        for pg_num in DATA_PAGES:
            if pg_num > total_pages:
                continue
            page = pdf.pages[pg_num - 1]   # 0-indexed
            
            # Try with explicit line settings first, fall back to text strategy
            for settings in [extraction_settings, None]:
                try:
                    tables = page.extract_tables(settings) if settings else page.extract_tables()
                    break
                except Exception:
                    tables = page.extract_tables()
                    break

            if not tables:
                print(f"  Page {pg_num}: no tables found")
                continue

            print(f"  Page {pg_num}: {len(tables)} raw table(s) found")
            for t_idx, raw_table in enumerate(tables, start=1):
                sheet_name = guess_table_name(pg_num, t_idx, raw_table)
                # Ensure uniqueness
                base = sheet_name
                suffix = 1
                while sheet_name in writer.sheets:
                    sheet_name = f"{base[:26]}_{suffix}"
                    suffix += 1

                df = build_dataframe(raw_table)
                if df is None or df.empty:
                    skipped += 1
                    print(f"    Table {t_idx} → skipped (empty or too sparse)")
                    continue

                df.to_excel(writer, sheet_name=sheet_name, index=False)
                tables_written += 1
                print(f"    Table {t_idx} → sheet '{sheet_name}' ({len(df)} rows × {len(df.columns)} cols)")

        # Also do a fallback text-based scan of pages 1–9 using bounding-box
        # text extraction to catch tables that have no visible rules/lines
        print("\n  Running supplemental text-layout extraction on pages 1-9...")
        for pg_num in DATA_PAGES:
            if pg_num > total_pages:
                continue
            page = pdf.pages[pg_num - 1]
            
            # pdfplumber text-based table strategy
            try:
                text_tables = page.extract_tables({
                    "vertical_strategy": "text",
                    "horizontal_strategy": "text",
                    "snap_tolerance": 4,
                    "join_tolerance": 4,
                })
            except Exception:
                continue

            if not text_tables:
                continue

            for t_idx, raw_table in enumerate(text_tables, start=1):
                # Check if this exact table was already captured (compare first row)
                df = build_dataframe(raw_table)
                if df is None or df.empty:
                    continue

                sheet_name = f"Pg{pg_num}_Txt{t_idx}"
                # Avoid duplicates: try to detect if same data already written
                base = sheet_name
                suffix = 1
                while sheet_name in writer.sheets:
                    sheet_name = f"{base[:26]}_{suffix}"
                    suffix += 1

                df.to_excel(writer, sheet_name=sheet_name, index=False)
                tables_written += 1
                print(f"    [Text] Page {pg_num}, Table {t_idx} → sheet '{sheet_name}' ({len(df)} rows × {len(df.columns)} cols)")

print(f"\n✅ Done. {tables_written} sheets written to:\n   {excel_path}")
if skipped:
    print(f"   ({skipped} sparse/empty tables were skipped)")
