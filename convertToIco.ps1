$pngPath = "C:\Users\Daryl Banks\.gemini\antigravity\brain\907f8341-1adb-4361-9039-d64c4aeaad38\rcs_cogo_icon_1776890972603.png"
$icoPath = "C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\rcs_cogo_icon.ico"

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($pngPath)
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx.DrawImage($img, 0, 0, 256, 256)
$gfx.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()
$ms.Dispose()
$bmp.Dispose()
$img.Dispose()

$icoStream = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($icoStream)

# ICO header (6 bytes)
$bw.Write([uint16]0) # Reserved
$bw.Write([uint16]1) # Type: 1 for ICO
$bw.Write([uint16]1) # Image count: 1

# Image directory entry (16 bytes)
$bw.Write([byte]0)   # Width (0 means 256)
$bw.Write([byte]0)   # Height (0 means 256)
$bw.Write([byte]0)   # Colors
$bw.Write([byte]0)   # Reserved
$bw.Write([uint16]1) # Color planes
$bw.Write([uint16]32)# Bits per pixel
$bw.Write([uint32]$pngBytes.Length) # Image size
$bw.Write([uint32]22) # Offset of image data

# Write the PNG data
$bw.Write($pngBytes)

# Save to file
[System.IO.File]::WriteAllBytes($icoPath, $icoStream.ToArray())
$bw.Dispose()
$icoStream.Dispose()
