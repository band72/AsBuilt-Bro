;;; ============================================================
;;; DUMPTABLE.LSP - Dumps all nested entity data from selected
;;; *U anonymous block references to a file.
;;; 
;;; Usage inside Civil 3D:
;;;   1. APPLOAD -> load this file
;;;   2. Type: DUMPTABLE -> Enter
;;;   3. Select your table block reference -> Enter
;;;   Output: C:\Temp\table_dump.txt
;;; ============================================================

(defun c:DUMPTABLE ( / ss i ent edata blk-name blk-ent blk-data child-ent child-data etype txt ins-x ins-y f out-file all-texts sorted-texts current-y current-row rows)

  (setq out-file "C:\\Temp\\table_dump.txt")
  
  ;; Create C:\Temp if it doesn't exist
  (vl-mkdir "C:\\Temp")

  (princ "\nDUMPTABLE: Select your table block(s) and press ENTER: ")
  (setq ss (ssget))

  (if (null ss)
    (progn (princ "\nNothing selected.") (exit))
  )

  (setq all-texts '())
  (setq i 0)

  (while (< i (sslength ss))
    (setq ent (ssname ss i))
    (setq edata (entget ent))
    (setq etype (cdr (assoc 0 edata)))

    (princ (strcat "\nItem " (itoa (1+ i)) ": " etype))

    (cond
      ;; Direct text entity
      ((or (= etype "TEXT") (= etype "MTEXT"))
       (setq txt (cdr (assoc 1 edata)))
       (setq ins-x (car (cdr (assoc 10 edata))))
       (setq ins-y (cadr (cdr (assoc 10 edata))))
       (if txt
         (setq all-texts (cons (list ins-x ins-y txt) all-texts))
       )
      )

      ;; Block reference - crawl its definition
      ((= etype "INSERT")
       (setq blk-name (cdr (assoc 2 edata)))
       (princ (strcat " [Block: " (if blk-name blk-name "nil") "]"))

       ;; Walk through block def using entnext
       (if blk-name
         (progn
           (setq blk-ent (tblobjname "BLOCK" blk-name))
           (if blk-ent
             (progn
               (setq child-ent (entnext blk-ent))
               (while (and child-ent
                           (not (equal (cdr (assoc 0 (entget child-ent))) "ENDBLK")))
                 (setq child-data (entget child-ent))
                 (setq etype-c (cdr (assoc 0 child-data)))
                 (if (or (= etype-c "TEXT") (= etype-c "MTEXT") (= etype-c "ATTDEF"))
                   (progn
                     (setq txt (cdr (assoc 1 child-data)))
                     (setq ins-x (car (cdr (assoc 10 child-data))))
                     (setq ins-y (cadr (cdr (assoc 10 child-data))))
                     (if (and txt (> (strlen txt) 0))
                       (progn
                         (princ (strcat "\n  Found text: [" txt "] at (" (rtos ins-x 2 3) "," (rtos ins-y 2 3) ")"))
                         (setq all-texts (cons (list ins-x ins-y txt) all-texts))
                       )
                     )
                   )
                 )
                 (setq child-ent (entnext child-ent))
               )
             )
             (princ " - Block def NOT FOUND in table")
           )
         )
       )
      )
    )

    (setq i (1+ i))
  )

  (princ (strcat "\n\nTotal texts found: " (itoa (length all-texts))))

  (if (= 0 (length all-texts))
    (progn
      (princ "\nNo text content found inside the selected blocks.")
      (princ "\nNote: Civil 3D Point Tables store data dynamically - try DATAEXTRACTION instead.")
      (exit)
    )
  )

  ;; Sort by Y descending, then X ascending
  (setq sorted-texts
    (vl-sort all-texts
      '(lambda (a b)
         (if (equal (cadr a) (cadr b) 1.5)
           (< (car a) (car b))
           (> (cadr a) (cadr b))
         )
       )
    )
  )

  ;; Write CSV
  (setq f (open out-file "w"))
  (setq current-y nil)
  (setq current-row '())

  (foreach item sorted-texts
    (setq ix (car item))
    (setq iy (cadr item))
    (setq it (caddr item))

    (cond
      ((null current-y)
       (setq current-y iy)
       (setq current-row (list it))
      )
      ((<= (abs (- iy current-y)) 1.5)
       (setq current-row (append current-row (list it)))
      )
      (T
       (write-line
         (apply 'strcat (mapcar '(lambda (s) (strcat "\"" s "\",")) current-row))
         f
       )
       (setq current-row (list it))
       (setq current-y iy)
      )
    )
  )

  ;; Flush last row
  (if current-row
    (write-line
      (apply 'strcat (mapcar '(lambda (s) (strcat "\"" s "\",")) current-row))
      f
    )
  )

  (close f)
  (princ (strcat "\n\nSUCCESS! CSV written to: " out-file))
  (princ)
)
