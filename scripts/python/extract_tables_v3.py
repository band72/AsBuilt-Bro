"""
PDF Table Extractor - Positional Text Clustering Approach
=========================================================
For PDFs where tables are vector-drawn (not native PDF table objects),
we reconstruct rows and columns by clustering text by Y-position,
then grouping text fragments into columns by X-position.
"""

import pdfplumber
import pandas as pd
import re
from collections import defaultdict

pdf_path = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE.pdf'
excel_path = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE_Tables.xlsx'

# Pages that contain actual data (1-indexed)
DATA_PAGES = list(range(2, 10))

ROW_TOLERANCE   = 5    # pts  – words within this Y-range go in same row
COL_TOLERANCE   = 15   # pts  – gap larger than this = new column

def words_to_grid(words, row_tol=ROW_TOLERANCE, col_tol=COL_TOLERANCE):
    """
    Convert a list of pdfplumber word dicts into a 2-D grid (list of lists of str).
    Groups words into rows by Y-centroid proximity, then into columns by X.
    Returns list of rows (each row is a list of strings).
    """
    if not words:
        return []

    # Sort by y0 (top), then x0 (left)
    words = sorted(words, key=lambda w: (round(w["top"] / row_tol), w["x0"]))

    # Cluster into rows
    rows_raw = []
    cur_row_y = None
    cur_row = []
    for w in words:
        y = w["top"]
        if cur_row_y is None or abs(y - cur_row_y) > row_tol:
            if cur_row:
                rows_raw.append(cur_row)
            cur_row = [w]
            cur_row_y = y
        else:
            cur_row.append(w)
    if cur_row:
        rows_raw.append(cur_row)

    if not rows_raw:
        return []

    # Determine column breakpoints from the full set of x-positions
    all_x0 = sorted(set(round(w["x0"]) for w in words))

    # Merge x-positions that are close together into a single column boundary
    col_boundaries = []
    for x in all_x0:
        if not col_boundaries or x - col_boundaries[-1] > col_tol:
            col_boundaries.append(x)

    num_cols = len(col_boundaries)

    def col_idx(x0):
        best = 0
        best_dist = abs(x0 - col_boundaries[0])
        for i, bx in enumerate(col_boundaries):
            d = abs(x0 - bx)
            if d < best_dist:
                best_dist = d
                best = i
        return best

    # Build grid
    grid = []
    for row_words in rows_raw:
        cells = [""] * num_cols
        # Sort words in the row by x0
        row_words_sorted = sorted(row_words, key=lambda w: w["x0"])
        # Group words that belong to the same cell (close x0), join text
        ci = None
        ci_text = ""
        for w in row_words_sorted:
            c = col_idx(w["x0"])
            if c == ci:
                ci_text += " " + w["text"]
            else:
                if ci is not None:
                    cells[ci] = (cells[ci] + " " + ci_text).strip()
                ci = c
                ci_text = w["text"]
        if ci is not None:
            cells[ci] = (cells[ci] + " " + ci_text).strip()
        grid.append(cells)

    return grid


def split_tables_by_blank_rows(grid, blank_threshold=0.85):
    """
    Segment a full-page grid into individual tables by detecting
    rows where >= blank_threshold of cells are empty.
    Returns list of grids (each grid is a candidate table).
    """
    tables = []
    current = []
    for row in grid:
        empty_ratio = sum(1 for c in row if not c.strip()) / max(len(row), 1)
        if empty_ratio >= blank_threshold:
            if current:
                tables.append(current)
                current = []
        else:
            current.append(row)
    if current:
        tables.append(current)
    return tables


def make_dataframe(grid):
    """
    Turn a grid (list of row-lists) into a DataFrame.
    The first non-empty row is used as header.
    """
    # Drop entirely blank rows
    grid = [r for r in grid if any(c.strip() for c in r)]
    if len(grid) < 2:
        return None

    # First row = header
    header = [c.strip() or f"Col_{i}" for i, c in enumerate(grid[0])]

    # Deduplicate column names
    seen = {}
    clean_header = []
    for h in header:
        if h in seen:
            seen[h] += 1
            clean_header.append(f"{h}_{seen[h]}")
        else:
            seen[h] = 0
            clean_header.append(h)

    data = []
    for row in grid[1:]:
        row = list(row) + [""] * (len(clean_header) - len(row))
        row = row[:len(clean_header)]
        if sum(1 for c in row if c.strip()) == 0:
            continue
        data.append(row)

    if not data:
        return None

    return pd.DataFrame(data, columns=clean_header)


def sanitize_sheet_name(name, existing):
    """Excel sheet names: max 31 chars, no special chars, unique."""
    name = re.sub(r'[\\/*?\[\]:]', ' ', name).strip()[:31]
    if not name:
        name = "Sheet"
    base, suffix = name, 1
    while name in existing:
        name = f"{base[:28]}_{suffix}"
        suffix += 1
    return name


# ─────────────────────────────────────────────────────────────────────────────
print(f"Opening: {pdf_path}")

with pdfplumber.open(pdf_path) as pdf:
    print(f"Total pages: {len(pdf.pages)}")
    all_tables = {}    # sheet_name -> DataFrame

    for pg_num in DATA_PAGES:
        if pg_num > len(pdf.pages):
            continue

        page = pdf.pages[pg_num - 1]
        words = page.extract_words(use_text_flow=False, keep_blank_chars=False)

        if not words:
            print(f"  Page {pg_num}: no text found")
            continue

        grid = words_to_grid(words)
        candidate_tables = split_tables_by_blank_rows(grid)

        print(f"  Page {pg_num}: {len(candidate_tables)} candidate segment(s) from {len(grid)} rows of text")

        for t_idx, tbl_grid in enumerate(candidate_tables, start=1):
            df = make_dataframe(tbl_grid)
            if df is None or df.empty:
                continue
            if df.shape[0] < 2 or df.shape[1] < 2:
                continue

            # Try to find a title in the 1–3 rows above this block
            # (Use first row of the grid as candidate title)
            title_row = tbl_grid[0] if tbl_grid else []
            title_text = " ".join(c for c in title_row if c.strip())[:30].strip()
            if not title_text:
                title_text = f"Pg{pg_num}_T{t_idx}"

            sheet_name = sanitize_sheet_name(title_text, set(all_tables.keys()))
            all_tables[sheet_name] = df
            print(f"    → sheet '{sheet_name}' ({df.shape[0]} rows × {df.shape[1]} cols)")

    if not all_tables:
        print("\n⚠  No tables found. Check PDF text layer.")
    else:
        with pd.ExcelWriter(excel_path, engine="openpyxl") as writer:
            for sheet_name, df in all_tables.items():
                df.to_excel(writer, sheet_name=sheet_name, index=False)

        print(f"\n✅ {len(all_tables)} sheet(s) written to:\n   {excel_path}")
