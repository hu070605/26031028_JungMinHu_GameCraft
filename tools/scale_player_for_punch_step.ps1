Add-Type -AssemblyName System.Drawing

$sourceDirectory = 'C:\Users\azxc9\OneDrive\Desktop\26031028_JungMinHu_GameCraft\resource\animation\animation_player'
$outputDirectory = Join-Path $sourceDirectory 'punch_step_1280x720_scale'
$backgroundPath = 'C:\Users\azxc9\OneDrive\Desktop\26031028_JungMinHu_GameCraft\resource\background\PunchStep_Background.png'
$platformPath = 'C:\Users\azxc9\OneDrive\Desktop\26031028_JungMinHu_GameCraft\resource\background\PunchStep_Platform.png'
$previewPath = Join-Path $outputDirectory 'PunchStep_CharacterScale_Preview_1280x720.png'

$frameCount = 8
$targetFrameWidth = 160
$targetFrameHeight = 96
$targetSheetWidth = $targetFrameWidth * $frameCount
$targetSheetHeight = $targetFrameHeight

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$sourceFiles = Get-ChildItem -LiteralPath $sourceDirectory -Filter 'Player_*.png' | Sort-Object Name

foreach ($file in $sourceFiles) {
    $source = [System.Drawing.Bitmap]::new($file.FullName)
    $sourceFrameWidth = [int]($source.Width / $frameCount)

    if ($sourceFrameWidth -eq 320) {
        $scaledCellWidth = 108
    }
    elseif ($sourceFrameWidth -eq 420) {
        $scaledCellWidth = 142
    }
    else {
        throw "Unsupported frame width $sourceFrameWidth in $($file.Name)"
    }

    $scaledCellHeight = 74
    $cellOffsetX = [int](($targetFrameWidth - $scaledCellWidth) / 2)
    $cellOffsetY = [int](($targetFrameHeight - $scaledCellHeight) / 2)
    $target = [System.Drawing.Bitmap]::new($targetSheetWidth, $targetSheetHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($target)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    for ($frame = 0; $frame -lt $frameCount; $frame++) {
        $sourceRect = [System.Drawing.Rectangle]::new($frame * $sourceFrameWidth, 0, $sourceFrameWidth, $source.Height)
        $targetRect = [System.Drawing.Rectangle]::new(
            ($frame * $targetFrameWidth) + $cellOffsetX,
            $cellOffsetY,
            $scaledCellWidth,
            $scaledCellHeight
        )
        $graphics.DrawImage($source, $targetRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    }

    $graphics.Dispose()
    $source.Dispose()
    $destination = Join-Path $outputDirectory $file.Name
    $target.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $target.Dispose()
}

# Build a 1280x720 scale check using the first right-running frame.
$background = [System.Drawing.Bitmap]::new($backgroundPath)
$platform = [System.Drawing.Bitmap]::new($platformPath)
$runSheet = [System.Drawing.Bitmap]::new((Join-Path $outputDirectory 'Player_Run_Right.png'))
$preview = [System.Drawing.Bitmap]::new(1280, 720, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
$previewGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$previewGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$previewGraphics.DrawImageUnscaled($background, 0, 0)
$previewGraphics.DrawImageUnscaled($platform, 0, 0)

$frameBottom = -1
for ($y = 0; $y -lt $targetFrameHeight; $y++) {
    for ($x = 0; $x -lt $targetFrameWidth; $x++) {
        if ($runSheet.GetPixel($x, $y).A -gt 10 -and $y -gt $frameBottom) {
            $frameBottom = $y
        }
    }
}

$platformTopY = 413
$frameX = [int](640 - ($targetFrameWidth / 2))
$frameY = $platformTopY - $frameBottom
$previewGraphics.DrawImage(
    $runSheet,
    [System.Drawing.Rectangle]::new($frameX, $frameY, $targetFrameWidth, $targetFrameHeight),
    [System.Drawing.Rectangle]::new(0, 0, $targetFrameWidth, $targetFrameHeight),
    [System.Drawing.GraphicsUnit]::Pixel
)

$previewGraphics.Dispose()
$background.Dispose()
$platform.Dispose()
$runSheet.Dispose()
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Write-Output "OutputDirectory=$outputDirectory"
Write-Output "Preview=$previewPath"
