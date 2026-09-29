Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assetDir = Join-Path $root 'assets'
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null

$bitmap = [System.Drawing.Bitmap]::new(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::FromArgb(10, 16, 26))

$accent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(42, 213, 190))
$muted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(236, 243, 248))
$graphics.FillRectangle($accent, 18, 18, 220, 15)
$graphics.FillRectangle($accent, 18, 223, 220, 15)
$font = [System.Drawing.Font]::new('Segoe UI', 84, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$small = [System.Drawing.Font]::new('Segoe UI', 25, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$graphics.DrawString('DK', $font, $muted, 26, 54)
$graphics.DrawString('RP', $small, $accent, 183, 178)

$pngPath = Join-Path $assetDir 'dk.png'
$icoPath = Join-Path $assetDir 'dk.ico'
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

$memory = [System.IO.MemoryStream]::new()
$bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $memory.ToArray()
$file = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]1)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([byte]0)
$writer.Write([uint16]1)
$writer.Write([uint16]32)
$writer.Write([uint32]$png.Length)
$writer.Write([uint32]22)
$writer.Write($png)
$writer.Dispose()
$memory.Dispose()
$font.Dispose()
$small.Dispose()
$accent.Dispose()
$muted.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
