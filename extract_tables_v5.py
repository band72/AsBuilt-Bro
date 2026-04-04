# -*- coding: utf-8 -*-
"""
PDF Table Extractor - Fast OCR + Spatial Clustering
====================================================
One pytesseract call per page (uses image_to_data with bounding boxes).
Groups words into rows/columns by spatial position.
"""

import re
import numpy as np
import pypdfium2 as pdfium
import pytesseract
import pandas as pd
from PIL import Image, ImageFilter

PDF_PATH   = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE.pdf'
EXCEL_PATH = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE_Tables.xlsx'

DATA_PAGES  = list(range(2, 10))
RENDER_DPI  = 150    # Reduced for speed; still good enough for text OCR
ROW_SNAP    = 10     # Pixels — words within this Y-distance are in the same row
COL_CLUSTER = 25     # Pixels — X-gap threshold between columns


def render_page(pdf_doc, page_idx, dpi=150):
    page  = pdf_doc[page_idx]
    scale = dpi / 72.0
    bmp   = page.render(scale=scale)
    return bmp.to_pil()


def ocr_page(pil_img):
    """
    Run pytesseract on the whole page at once.
    Returns a DataFrame with columns: left, top, width, height, conf, text.
    """
    data = pytesseract.image_to_data(
        pil_img,
        config='--psm 3 --oem 3',   # auto page segmentation
        output_type=pytesseract.Output.DATAFRAME
    )
    # Keep only high-confidence, non-empty words
    data = data[(data['conf'] >= 25) & (data['text'].str.strip() != '')]
    data = data.reset_index(drop=True)
    return data


def words_to_grid(data, row_snap=ROW_SNAP, col_cluster=COL_CLUSTER):
    """
    Convert pytesseract word bounding-box data to a 2-D grid.
    Returns a list of rows, each row is a list of (col_index, text) pairs.
    To keep it simple we just return the final 2-D array.
    """
    if data.empty:
        return []

    words = data.sort_values(['top', 'left']).to_dict('records')

    # ── 1. Cluster into rows by top coordinate ──────────────────────────────
    row_groups = []
    cur_top = None
    cur_row = []
    for w in words:
        t = w['top']
        if cur_top is None or abs(t - cur_top) > row_snap:
            if cur_row:
                row_groups.append(cur_row)
            cur_row  = [w]
            cur_top  = t
        else:
            cur_row.append(w)
    if cur_row:
        row_groups.append(cur_row)

    if not row_groups:
        return []

    # ── 2. Determine global column boundaries from all left-edges ───────────
    all_lefts = sorted(set(w['left'] for rg in row_groups for w in rg))
    col_bounds = []
    for x in all_lefts:
        if not col_bounds or x - col_bounds[-1] > col_cluster:
            col_bounds.append(x)
    n_cols = len(col_bounds)

    def nearest_col(x):
        return min(range(n_cols), key=lambda i: abs(col_bounds[i] - x))

    # ── 3. Build 2-D grid ───────────────────────────────────────────────────
    grid = []
    for rg in row_groups:
        cells = [''] * n_cols
        for w in sorted(rg, key=lambda x: x['left']):
            ci = nearest_col(w['left'])
            t  = str(w['text']).strip()
            cells[ci] = (cells[ci] + ' ' + t).strip() if cells[ci] else t
        if any(c for c in cells):
            grid.append(cells)

    return grid


def split_into_tables(grid, blank_row_ratio=0.80):
    """
    Split a full-page grid into separate table blocks at blank rows.
    A 'blank row' has most cells empty.
    """
    tables, current = [], []
    for row in grid:
        n_empty = sum(1 for c in row if not c.strip())
        ratio   = n_empty / max(len(row), 1)
        if ratio >= blank_row_ratio:
            if current:
                tables.append(current)
                current = []
        else:
            current.append(row)
    if current:
        tables.append(current)
    return tables


def make_df(grid):
    """Convert a grid (list of row lists) to a clean DataFrame."""
    # Drop all-blank rows
    grid = [r for r in grid if any(c.strip() for c in r)]
    if len(grid) < 2:
        return None

    header = []
    for i, c in enumerate(grid[0]):
        h = c.strip() or f'Col_{i}'
        header.append(h)

    # Deduplicate
    seen = {}
    clean_hdr = []
    for h in header:
        if h in seen:
            seen[h] += 1
            clean_hdr.append(f'{h}_{seen[h]}')
        else:
            seen[h] = 0
            clean_hdr.append(h)

    rows = []
    for row in grid[1:]:
        row = (list(row) + [''] * len(clean_hdr))[:len(clean_hdr)]
        if any(c.strip() for c in row):
            rows.append([c.strip() for c in row])

    if not rows:
        return None
    return pd.DataFrame(rows, columns=clean_hdr)


def sanitize_sheet(name, existing):
    name = re.sub(r'[\\/*?\[\]:\n\r]', ' ', str(name)).strip()[:31] or 'Sheet'
    base, n = name, 1
    while name in existing:
        name = f'{base[:27]}_{n}'
        n += 1
    return name


# ─────────────────────────────────────────────────────────────────────────────
print(f'Opening: {PDF_PATH}')
pdf_doc = pdfium.PdfDocument(PDF_PATH)
print(f'Pages: {len(pdf_doc)}  |  Processing pages: {DATA_PAGES}')

all_sheets = {}

for pg_num in DATA_PAGES:
    if pg_num > len(pdf_doc):
        continue
    print(f'\n--- Page {pg_num} ---', flush=True)

    pil_img = render_page(pdf_doc, pg_num - 1, dpi=RENDER_DPI)
    print(f'  Rendered {pil_img.size[0]}x{pil_img.size[1]} px — OCR...', end='', flush=True)

    ocr_data = ocr_page(pil_img)
    print(f' {len(ocr_data)} words found')

    if ocr_data.empty:
        print('  No text detected — skipping')
        continue

    grid   = words_to_grid(ocr_data)
    tables = split_into_tables(grid)
    print(f'  {len(grid)} text rows -> {len(tables)} table segment(s)')

    for t_idx, tgrid in enumerate(tables, 1):
        df = make_df(tgrid)
        if df is None or df.empty or df.shape[1] < 2 or df.shape[0] < 1:
            continue

        # Try to find a title from the first row (single wide cell)
        first_row = tgrid[0] if tgrid else []
        non_empty = [c for c in first_row if c.strip()]
        title = non_empty[0][:30] if len(non_empty) == 1 and len(non_empty[0]) > 6 else f'Pg{pg_num}_T{t_idx}'

        sheet = sanitize_sheet(title, set(all_sheets.keys()))
        all_sheets[sheet] = df
        print(f'  T{t_idx} -> "{sheet}"  ({df.shape[0]} rows x {df.shape[1]} cols)')

pdf_doc.close()

if not all_sheets:
    print('\nNo tables extracted.')
else:
    with pd.ExcelWriter(EXCEL_PATH, engine='openpyxl') as writer:
        for sname, df in all_sheets.items():
            df.to_excel(writer, sheet_name=sname, index=False)
    print(f'\nDone! {len(all_sheets)} sheet(s) written to:')
    print(f'  {EXCEL_PATH}')
