# -*- coding: utf-8 -*-
"""
PDF Table Extractor - OpenCV Grid Detection + Region OCR
=========================================================
1. Renders each page at moderate DPI.
2. Uses OpenCV morphological operations to find horizontal/vertical table
   line segments and cluster them into a grid of rows x cols.
3. OCRs the WHOLE table region (one Tesseract call per table) using
   image_to_data, then assigns each word to its nearest (row, col) cell.
4. Writes one Excel sheet per table.
"""

import re
import sys
import cv2
import numpy as np
import pypdfium2 as pdfium
import pytesseract
import pandas as pd
from PIL import Image

PDF_PATH   = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE.pdf'
EXCEL_PATH = r'C:\Users\Daryl Banks\Downloads\70498-S1A_TEST FILE - Standard (1)\70498-S1A_TEST FILE_Tables.xlsx'

DATA_PAGES = list(range(2, 10))
DPI        = 150        # ~150 DPI is fast and sufficient for OCR


# ─── Render page ──────────────────────────────────────────────────────────────
def render_page(pdf_doc, page_idx, dpi=DPI):
    page  = pdf_doc[page_idx]
    bmp   = page.render(scale=dpi / 72.0)
    pil   = bmp.to_pil()
    return pil, np.array(pil)


# ─── Detect table bounding boxes using line morphology ────────────────────────
def find_table_boxes(img_np, min_w=100, min_h=50):
    """
    Returns list of (x, y, w, h) for every detected table rectangle.
    Uses morphological line detection on a binarized grayscale image.
    """
    gray = cv2.cvtColor(img_np, cv2.COLOR_RGB2GRAY)
    # Adaptive threshold works better than global for engineering drawings
    binary = cv2.adaptiveThreshold(gray, 255,
                                   cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
                                   cv2.THRESH_BINARY_INV, 15, 2)

    ih, iw = gray.shape
    # Horizontal lines: must be at least 8% of page width
    min_hlen = int(iw * 0.05)
    hk = cv2.getStructuringElement(cv2.MORPH_RECT, (min_hlen, 1))
    h_lines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, hk)

    # Vertical lines: must be at least 4% of page height
    min_vlen = int(ih * 0.02)
    vk = cv2.getStructuringElement(cv2.MORPH_RECT, (1, min_vlen))
    v_lines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, vk)

    # Combine
    combined = cv2.add(h_lines, v_lines)
    # Dilate to bridge small gaps
    dk = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
    combined = cv2.dilate(combined, dk, iterations=3)

    cnts, _ = cv2.findContours(combined, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    boxes = []
    for cnt in cnts:
        x, y, w, h = cv2.boundingRect(cnt)
        if w < min_w or h < min_h:
            continue
        if w > iw * 0.98 and h > ih * 0.92:
            continue  # whole-page border
        boxes.append((x, y, w, h))

    boxes.sort(key=lambda b: b[1])   # top-to-bottom order
    return boxes


# ─── Extract row/col grid from a table region image ───────────────────────────
def extract_grid_lines(roi, min_gap=8):
    """
    From a cropped table ROI, find horizontal and vertical separator lines.
    Returns (row_ys, col_xs) — these are the dividing pixel positions.
    """
    gray   = cv2.cvtColor(roi, cv2.COLOR_RGB2GRAY)
    binary = cv2.adaptiveThreshold(gray, 255,
                                   cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
                                   cv2.THRESH_BINARY_INV, 11, 2)
    h, w = gray.shape

    # Horizontal separators
    hk    = cv2.getStructuringElement(cv2.MORPH_RECT, (max(w // 8, 30), 1))
    hlines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, hk)
    hproj  = np.sum(hlines > 0, axis=1)

    row_ys = [0]
    in_l = False
    for y, v in enumerate(hproj):
        if v > w * 0.05 and not in_l:
            row_ys.append(y)
            in_l = True
        elif v <= w * 0.05:
            in_l = False
    row_ys.append(h)

    # Vertical separators
    vk    = cv2.getStructuringElement(cv2.MORPH_RECT, (1, max(h // 8, 20)))
    vlines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, vk)
    vproj  = np.sum(vlines > 0, axis=0)

    col_xs = [0]
    in_l = False
    for x, v in enumerate(vproj):
        if v > h * 0.03 and not in_l:
            col_xs.append(x)
            in_l = True
        elif v <= h * 0.03:
            in_l = False
    col_xs.append(w)

    # Filter too-close positions
    def _dedup(lst):
        out = [lst[0]]
        for v in lst[1:]:
            if v - out[-1] >= min_gap:
                out.append(v)
        return out

    return _dedup(sorted(set(row_ys))), _dedup(sorted(set(col_xs)))


# ─── OCR a ROI and assign words to (row, col) cells ──────────────────────────
def ocr_table_roi(roi_np, row_ys, col_xs):
    """
    One Tesseract call on the whole ROI.
    Assigns each word to the table cell it falls in.
    Returns 2-D list of strings.
    """
    pil  = Image.fromarray(roi_np)
    data = pytesseract.image_to_data(pil, config='--psm 6 --oem 3',
                                     output_type=pytesseract.Output.DATAFRAME)
    data = data[(data['conf'] >= 20) & (data['text'].str.strip() != '')]

    n_rows = len(row_ys) - 1
    n_cols = len(col_xs) - 1
    if n_rows < 1 or n_cols < 1:
        return []

    # Initialize empty grid
    grid = [['' for _ in range(n_cols)] for _ in range(n_rows)]

    for _, w in data.iterrows():
        wx = w['left'] + w['width'] / 2
        wy = w['top']  + w['height'] / 2
        # Find row
        ri = -1
        for i in range(len(row_ys) - 1):
            if row_ys[i] <= wy < row_ys[i+1]:
                ri = i; break
        if ri < 0: continue
        # Find col
        ci = -1
        for j in range(len(col_xs) - 1):
            if col_xs[j] <= wx < col_xs[j+1]:
                ci = j; break
        if ci < 0: continue
        t = str(w['text']).strip()
        grid[ri][ci] = (grid[ri][ci] + ' ' + t).strip() if grid[ri][ci] else t

    # Drop all-empty rows and cols
    grid = [r for r in grid if any(c for c in r)]
    return grid


# ─── Fallback: no grid lines — just OCR + sort by Y/X ───────────────────────
def fallback_ocr_no_grid(roi_np, col_gap_fraction=0.012):
    """
    When no grid lines are found, cluster words spatially.
    col_gap_fraction: fraction of image width to use as column gap threshold.
    """
    pil  = Image.fromarray(roi_np)
    ih, iw = roi_np.shape[:2]
    col_gap = max(int(iw * col_gap_fraction), 15)
    row_gap = max(int(ih * 0.012), 10)

    data = pytesseract.image_to_data(pil, config='--psm 6 --oem 3',
                                     output_type=pytesseract.Output.DATAFRAME)
    data = data[(data['conf'] >= 20) & (data['text'].str.strip() != '')]
    if data.empty:
        return []

    words = data.sort_values(['top', 'left']).to_dict('records')

    # Cluster rows
    row_clusters = []
    cur_y, cur = None, []
    for w in words:
        y = w['top']
        if cur_y is None or abs(y - cur_y) > row_gap:
            if cur: row_clusters.append(cur)
            cur, cur_y = [w], y
        else:
            cur.append(w)
    if cur: row_clusters.append(cur)

    if not row_clusters:
        return []

    # Global col boundaries
    all_x = sorted(set(w['left'] for rg in row_clusters for w in rg))
    col_bounds = []
    for x in all_x:
        if not col_bounds or x - col_bounds[-1] > col_gap:
            col_bounds.append(x)

    def nearest_col(x):
        return min(range(len(col_bounds)), key=lambda i: abs(col_bounds[i] - x))

    grid = []
    for rg in row_clusters:
        cells = [''] * len(col_bounds)
        for w in sorted(rg, key=lambda x: x['left']):
            ci = nearest_col(w['left'])
            t  = str(w['text']).strip()
            cells[ci] = (cells[ci] + ' ' + t).strip() if cells[ci] else t
        if any(c for c in cells):
            grid.append(cells)

    return grid


# ─── Grid → DataFrame ────────────────────────────────────────────────────────
def grid_to_df(grid):
    if not grid or len(grid) < 2:
        return None
    hdr = [(c.strip() or f'Col_{i}') for i, c in enumerate(grid[0])]
    seen = {}
    clean = []
    for h in hdr:
        if h in seen:
            seen[h] += 1; clean.append(f'{h}_{seen[h]}')
        else:
            seen[h] = 0; clean.append(h)
    rows = []
    for row in grid[1:]:
        row = (list(row) + [''] * len(clean))[:len(clean)]
        if any(c.strip() for c in row):
            rows.append([c.strip() for c in row])
    return pd.DataFrame(rows, columns=clean) if rows else None


def safe_sheet_name(name, used):
    name = re.sub(r'[\\/*?\[\]:\n\r]', ' ', str(name)).strip()[:31] or 'Sheet'
    base, n = name, 1
    while name in used:
        name = f'{base[:27]}_{n}'; n += 1
    return name


# ─── Main ────────────────────────────────────────────────────────────────────
print(f'PDF: {PDF_PATH}')
pdf_doc = pdfium.PdfDocument(PDF_PATH)
print(f'Pages: {len(pdf_doc)}')

all_sheets = {}

for pg in DATA_PAGES:
    if pg > len(pdf_doc): continue
    print(f'\n=== Page {pg} ===', flush=True)
    pil, img = render_page(pdf_doc, pg - 1)
    print(f'  Image: {img.shape[1]}x{img.shape[0]}')

    boxes = find_table_boxes(img)
    print(f'  Table regions found: {len(boxes)}')

    if not boxes:
        # Fall back: treat whole page as one region
        boxes = [(0, 0, img.shape[1], img.shape[0])]

    for b_idx, (bx, by, bw, bh) in enumerate(boxes, 1):
        roi = img[by:by+bh, bx:bx+bw]
        row_ys, col_xs = extract_grid_lines(roi)
        n_rows = len(row_ys) - 1
        n_cols = len(col_xs) - 1
        print(f'  Box {b_idx}: grid={n_rows}r x {n_cols}c', end='', flush=True)

        if n_rows >= 2 and n_cols >= 2:
            grid = ocr_table_roi(roi, row_ys, col_xs)
        else:
            print(' (no grid -> fallback)', end='', flush=True)
            grid = fallback_ocr_no_grid(roi)

        df = grid_to_df(grid)
        if df is None or df.empty or df.shape[1] < 2:
            print(' => skipped (too sparse)')
            continue

        sname = safe_sheet_name(f'Pg{pg}_T{b_idx}', set(all_sheets.keys()))
        all_sheets[sname] = df
        print(f' => "{sname}" ({df.shape[0]}r x {df.shape[1]}c)')

pdf_doc.close()

if not all_sheets:
    print('\nNo usable tables found.')
else:
    with pd.ExcelWriter(EXCEL_PATH, engine='openpyxl') as writer:
        for sname, df in all_sheets.items():
            df.to_excel(writer, sheet_name=sname, index=False)
    print(f'\nDone! {len(all_sheets)} tab(s) written to:')
    print(f'  {EXCEL_PATH}')
