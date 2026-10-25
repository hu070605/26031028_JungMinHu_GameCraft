Add-Type -AssemblyName System.Drawing

$backgroundSource = 'C:\Users\azxc9\.codex\generated_images\01a16351-e3da-7690-bd43-5f402acefb8f\exec-7ee46198-ffcf-4fc3-8a17-81e586e252ca.png'
$platformSource = 'C:\Users\azxc9\.codex\generated_images\01a16351-e3da-7690-bd43-5f402acefb8f\exec-e349cf44-d98e-4811-b484-93a892899fee.png'
$outputDirectory = 'C:\Users\azxc9\OneDrive\Desktop\26031028_JungMinHu_GameCraft\resource\background\arena'
$backgroundOutput = Join-Path $outputDirectory 'Arena_Background_1280x720.png'
$platformOutput = Join-Path $outputDirectory 'Arena_Platform_1280x720.png'
$previewOutput = Join-Path $outputDirectory 'Arena_Composite_Preview_1280x720.png'

function Resize-Png {
    param(
        [string]$Source,
        [string]$Destination,
        [System.Drawing.Imaging.PixelFormat]$PixelFormat,
        [bool]$Transparent
    )

    $sourceBitmap = [System.Drawing.Bitmap]::new($Source)
    $targetBitmap = [System.Drawing.Bitmap]::new(1280, 720, $PixelFormat)
    $graphics = [System.Drawing.Graphics]::FromImage($targetBitmap)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    if ($Transparent) {
        $graphics.Clear([System.Drawing.Color]::Transparent)
    }
    $graphics.DrawImage($sourceBitmap, [System.Drawing.Rectangle]::new(0, 0, 1280, 720))
    $graphics.Dispose()
    $sourceBitmap.Dispose()
    $targetBitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $targetBitmap.Dispose()
}

function Export-AlignedPlatform {
    param(
        [string]$Source,
        [string]$Destination
    )

    $sourceBitmap = [System.Drawing.Bitmap]::new($Source)
    $targetBitmap = [System.Drawing.Bitmap]::new(1280, 720, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($targetBitmap)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    # The generator isolated the correct art but moved it upward. Map its alpha
    # bounds back onto the platform bounds measured from the original scene.
    $sourceBounds = [System.Drawing.Rectangle]::new(42, 442, 1588, 243)
    $targetBounds = [System.Drawing.Rectangle]::new(33, 413, 1214, 182)
    $graphics.DrawImage($sourceBitmap, $targetBounds, $sourceBounds, [System.Drawing.GraphicsUnit]::Pixel)

    $graphics.Dispose()
    $sourceBitmap.Dispose()
    $targetBitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $targetBitmap.Dispose()
}

Resize-Png -Source $backgroundSource -Destination $backgroundOutput -PixelFormat ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb) -Transparent $false
Export-AlignedPlatform -Source $platformSource -Destination $platformOutput

$background = [System.Drawing.Bitmap]::new($backgroundOutput)
$platform = [System.Drawing.Bitmap]::new($platformOutput)
$preview = [System.Drawing.Bitmap]::new(1280, 720, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
$previewGraphics.DrawImageUnscaled($background, 0, 0)
$previewGraphics.DrawImageUnscaled($platform, 0, 0)
$previewGraphics.Dispose()
$preview.Save($previewOutput, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$background.Dispose()
$platform.Dispose()

Write-Output $backgroundOutput
Write-Output $platformOutput
Write-Output $previewOutput
