# -*- coding: utf-8 -*-
"""
PDF Table Extractor - OCR + Table-Line Detection
=================================================
Uses pypdfium2 to render pages at high DPI, then:
 1. Uses OpenCV to detect the table grid lines
 2. Uses pytesseract data output to place each word into the correct cell
 3. Writes one DataFrame per detected table to Excel
"""

import sys
import os
import re
import math

import cv2
import numpy as np
import pypdfium2 as pdfium
import pytesseract
import pandas as pd
from PIL import Image

PDF_PATH   = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE.pdf'
EXCEL_PATH = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE_Tables.xlsx'

# Pages to process (1-indexed)
DATA_PAGES = list(range(2, 10))

RENDER_DPI = 200    # Higher = more accurate OCR but slower
MIN_TABLE_ROWS = 2
MIN_TABLE_COLS = 2


# ─── Helper: render a PDF page to a PIL Image ───────────────────────────────
def render_page(pdf_doc, page_idx, dpi=200):
    page = pdf_doc[page_idx]
    scale = dpi / 72.0
    bitmap = page.render(scale=scale)
    pil_img = bitmap.to_pil()
    return pil_img


# ─── Helper: detect table grid using HoughLines ─────────────────────────────
def detect_table_regions(img_np):
    """
    Find rectangular table regions in the image using horizontal/vertical lines.
    Returns a list of (x, y, w, h) bounding boxes.
    """
    gray = cv2.cvtColor(img_np, cv2.COLOR_RGB2GRAY)
    # Threshold
    _, binary = cv2.threshold(gray, 200, 255, cv2.THRESH_BINARY_INV)

    # Detect horizontal lines
    h_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (80, 1))
    h_lines  = cv2.morphologyEx(binary, cv2.MORPH_OPEN, h_kernel)

    # Detect vertical lines
    v_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, 40))
    v_lines  = cv2.morphologyEx(binary, cv2.MORPH_OPEN, v_kernel)

    # Combine
    grid = cv2.add(h_lines, v_lines)

    # Dilate to close small gaps
    dil_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (3, 3))
    grid = cv2.dilate(grid, dil_kernel, iterations=2)

    # Find contours
    contours, _ = cv2.findContours(grid, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    regions = []
    img_h, img_w = img_np.shape[:2]
    for cnt in contours:
        x, y, w, h = cv2.boundingRect(cnt)
        # Filter tiny noise rectangles
        if w < 100 or h < 50:
            continue
        # Filter if too large (probably page border)
        if w > img_w * 0.98 and h > img_h * 0.90:
            continue
        regions.append((x, y, w, h))

    # Sort top-to-bottom
    regions.sort(key=lambda r: r[1])
    return regions


def get_row_col_lines(roi_img):
    """
    Given a cropped table ROI, extract row and column separator positions.
    Returns (row_ys, col_xs) as sorted lists of pixel positions.
    """
    gray = cv2.cvtColor(roi_img, cv2.COLOR_RGB2GRAY)
    _, binary = cv2.threshold(gray, 200, 255, cv2.THRESH_BINARY_INV)

    # Horizontal separators → row Y boundaries
    h_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (60, 1))
    h_lines  = cv2.morphologyEx(binary, cv2.MORPH_OPEN, h_kernel)
    h_proj   = np.sum(h_lines, axis=1)
    row_ys   = [0]
    in_line  = False
    for y_idx, val in enumerate(h_proj):
        if val > 50 and not in_line:
            row_ys.append(y_idx)
            in_line = True
        elif val <= 50:
            in_line = False
    row_ys.append(roi_img.shape[0])

    # Vertical separators → col X boundaries
    v_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, 30))
    v_lines  = cv2.morphologyEx(binary, cv2.MORPH_OPEN, v_kernel)
    v_proj   = np.sum(v_lines, axis=0)
    col_xs   = [0]
    in_line  = False
    for x_idx, val in enumerate(v_proj):
        if val > 30 and not in_line:
            col_xs.append(x_idx)
            in_line = True
        elif val <= 30:
            in_line = False
    col_xs.append(roi_img.shape[1])

    return sorted(set(row_ys)), sorted(set(col_xs))


def ocr_cell(cell_img):
    """Run Tesseract on a single cell image and return text."""
    pil = Image.fromarray(cell_img)
    # Scale up small cells for better OCR
    w, h = pil.size
    if w < 80 or h < 20:
        pil = pil.resize((max(w * 2, 80), max(h * 2, 20)), Image.LANCZOS)
    text = pytesseract.image_to_string(pil, config='--psm 7 --oem 3').strip()
    return text


def extract_table_from_roi(roi_img):
    """
    Given a cropped table image, detect rows/cols and OCR each cell.
    Returns a 2-D list of strings (the table data).
    """
    row_ys, col_xs = get_row_col_lines(roi_img)

    # Filter out positions too close together
    def filter_close(positions, min_gap=15):
        filtered = [positions[0]]
        for p in positions[1:]:
            if p - filtered[-1] >= min_gap:
                filtered.append(p)
        return filtered

    row_ys = filter_close(row_ys, min_gap=15)
    col_xs = filter_close(col_xs, min_gap=20)

    if len(row_ys) < 2 or len(col_xs) < 2:
        return []

    rows = []
    for r_idx in range(len(row_ys) - 1):
        y1 = row_ys[r_idx]
        y2 = row_ys[r_idx + 1]
        if y2 - y1 < 8:
            continue
        row = []
        for c_idx in range(len(col_xs) - 1):
            x1 = col_xs[c_idx]
            x2 = col_xs[c_idx + 1]
            if x2 - x1 < 10:
                continue
            cell = roi_img[y1:y2, x1:x2]
            text = ocr_cell(cell)
            row.append(text)
        if any(t.strip() for t in row):
            rows.append(row)

    return rows


def fallback_full_page_ocr(page_img_np):
    """
    If no clear grid found, OCR the whole page with tsv layout data
    and reconstruct rows by grouping text by Y position.
    """
    pil = Image.fromarray(page_img_np)
    data = pytesseract.image_to_data(pil, output_type=pytesseract.Output.DATAFRAME)
    data = data[data.conf > 20]   # filter low-confidence
    data = data[data.text.str.strip() != '']

    if data.empty:
        return []

    ROW_GAP = 12  # pixels
    data = data.sort_values(['top', 'left'])
    rows = []
    cur_row_y = None
    cur_row_words = []

    for _, w in data.iterrows():
        y = w['top']
        if cur_row_y is None or abs(y - cur_row_y) > ROW_GAP:
            if cur_row_words:
                rows.append(cur_row_words)
            cur_row_words = [w]
            cur_row_y = y
        else:
            cur_row_words.append(w)

    if cur_row_words:
        rows.append(cur_row_words)

    if not rows:
        return []

    # Determine column boundaries from x-positions across all rows
    all_lefts = sorted(set(round(w['left'] / 15) * 15 for row in rows for w in row))
    # Cluster close left positions
    col_breaks = []
    for x in all_lefts:
        if not col_breaks or x - col_breaks[-1] > 20:
            col_breaks.append(x)

    def assign_col(x):
        best_col, best_dist = 0, float('inf')
        for i, bx in enumerate(col_breaks):
            d = abs(x - bx)
            if d < best_dist:
                best_dist = d
                best_col = i
        return best_col

    num_cols = len(col_breaks)
    grid = []
    for row_words in rows:
        cells = [''] * num_cols
        for w in sorted(row_words, key=lambda x: x['left']):
            c = assign_col(w['left'])
            cells[c] = (cells[c] + ' ' + str(w['text'])).strip()
        if any(c.strip() for c in cells):
            grid.append(cells)

    return grid


def grid_to_dataframe(grid):
    if not grid or len(grid) < 2:
        return None
    header = [c.strip() or f'Col_{i}' for i, c in enumerate(grid[0])]
    seen = {}
    clean_header = []
    for h in header:
        if h in seen:
            seen[h] += 1
            clean_header.append(f'{h}_{seen[h]}')
        else:
            seen[h] = 0
            clean_header.append(h)

    data = []
    for row in grid[1:]:
        row = (row + [''] * len(clean_header))[:len(clean_header)]
        if any(c.strip() for c in row):
            data.append([c.strip() for c in row])

    if not data:
        return None
    return pd.DataFrame(data, columns=clean_header)


def sanitize_sheet(name, existing):
    name = re.sub(r'[\\/*?\[\]:\n\r]', ' ', str(name)).strip()[:31] or 'Sheet'
    base, n = name, 1
    while name in existing:
        name = f'{base[:27]}_{n}'
        n += 1
    return name


# ─────────────────────────────────────────────────────────────────────────────
print(f'Opening PDF: {PDF_PATH}')
pdf_doc = pdfium.PdfDocument(PDF_PATH)
print(f'Total pages: {len(pdf_doc)}')

all_sheets = {}   # sheet_name -> DataFrame

for pg_num in DATA_PAGES:
    if pg_num > len(pdf_doc):
        continue
    print(f'\n--- Page {pg_num} ---')
    pil_img = render_page(pdf_doc, pg_num - 1, dpi=RENDER_DPI)
    img_np  = np.array(pil_img)

    # Try grid-line approach first
    regions = detect_table_regions(img_np)
    print(f'  Detected {len(regions)} table region(s) via line detection')

    if regions:
        for r_idx, (rx, ry, rw, rh) in enumerate(regions, 1):
            roi = img_np[ry:ry+rh, rx:rx+rw]
            grid = extract_table_from_roi(roi)
            if not grid:
                print(f'  Region {r_idx}: no grid data, falling back to full-region OCR')
                grid = fallback_full_page_ocr(roi)
            if not grid:
                print(f'  Region {r_idx}: no data found')
                continue
            df = grid_to_dataframe(grid)
            if df is None or df.empty or df.shape[1] < MIN_TABLE_COLS:
                continue
            sheet = sanitize_sheet(f'Pg{pg_num}_T{r_idx}', set(all_sheets.keys()))
            all_sheets[sheet] = df
            print(f'  Region {r_idx} -> "{sheet}" ({df.shape[0]} r x {df.shape[1]} c)')
    else:
        # No line-detected regions — OCR the whole page
        print(f'  No grid lines found, running full-page OCR...')
        grid = fallback_full_page_ocr(img_np)
        if grid:
            df = grid_to_dataframe(grid)
            if df is not None and not df.empty and df.shape[1] >= MIN_TABLE_COLS:
                sheet = sanitize_sheet(f'Pg{pg_num}_Full', set(all_sheets.keys()))
                all_sheets[sheet] = df
                print(f'  -> "{sheet}" ({df.shape[0]} r x {df.shape[1]} c)')

pdf_doc.close()

if not all_sheets:
    print('\nNo tables extracted.')
else:
    with pd.ExcelWriter(EXCEL_PATH, engine='openpyxl') as writer:
        for sheet_name, df in all_sheets.items():
            df.to_excel(writer, sheet_name=sheet_name, index=False)
    print(f'\nDone. {len(all_sheets)} sheet(s) written to:')
    print(f'  {EXCEL_PATH}')
