Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$output = Join-Path $projectRoot 'resource/animation/animation_portal'
$frames = Join-Path $projectRoot 'tools/portal-preview-frames'
New-Item -ItemType Directory -Path $output,$frames -Force | Out-Null
$sourceFolder = Join-Path $output 'source'
$rim = [System.Drawing.Bitmap]::new((Join-Path $sourceFolder 'Portal_Rim.png'))
$swirl = [System.Drawing.Bitmap]::new((Join-Path $sourceFolder 'Portal_Swirl.png'))

# Normalize the rim to find its opaque bounds without including transparent padding.
$small = [System.Drawing.Bitmap]::new(256,256)
$g = [System.Drawing.Graphics]::FromImage($small)
$g.DrawImage($rim,0,0,256,256)
$g.Dispose()
$left=256; $top=256; $right=-1; $bottom=-1
for($y=0;$y -lt 256;$y++){for($x=0;$x -lt 256;$x++){
 if($small.GetPixel($x,$y).A -ge 128){
  $left=[Math]::Min($left,$x); $top=[Math]::Min($top,$y)
  $right=[Math]::Max($right,$x); $bottom=[Math]::Max($bottom,$y)
 }
}}
$portalHeight=76
$portalWidth=[int][Math]::Round(($right-$left+1)*$portalHeight/($bottom-$top+1))
$base = [System.Drawing.Bitmap]::new(160,96)
$g=[System.Drawing.Graphics]::FromImage($base)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$scale=$rim.Width/256.0
$g.DrawImage($rim,[System.Drawing.Rectangle]::new([int]((160-$portalWidth)/2),10,$portalWidth,$portalHeight),
 [single]($left*$scale),[single]($top*$scale),[single](($right-$left+1)*$scale),[single](($bottom-$top+1)*$scale),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()

# Flood-fill only the enclosed transparent opening. The ring and feet never rotate.
$queue=[System.Collections.Generic.Queue[int]]::new()
$visited=[bool[]]::new(160*96)
$hole=[System.Collections.Generic.List[int]]::new()
$queue.Enqueue(48*160+80)
while($queue.Count -gt 0){
 $index=$queue.Dequeue()
 if($visited[$index]){continue}
 $visited[$index]=$true
 $x=$index%160; $y=[int][Math]::Floor($index/160)
 if($base.GetPixel($x,$y).A -gt 220){continue}
 if($x -eq 0 -or $x -eq 159 -or $y -eq 0 -or $y -eq 95){throw 'Portal opening is not enclosed'}
 $hole.Add($index)
 $queue.Enqueue($index-1);$queue.Enqueue($index+1);$queue.Enqueue($index-160);$queue.Enqueue($index+160)
}
$hx0=160;$hy0=96;$hx1=0;$hy1=0
foreach($index in $hole){$x=$index%160;$y=[int][Math]::Floor($index/160);$hx0=[Math]::Min($hx0,$x);$hx1=[Math]::Max($hx1,$x);$hy0=[Math]::Min($hy0,$y);$hy1=[Math]::Max($hy1,$y)}
$cx=($hx0+$hx1)/2.0;$cy=($hy0+$hy1)/2.0
$rx=($hx1-$hx0+1)/2.0;$ry=($hy1-$hy0+1)/2.0
$sheet=[System.Drawing.Bitmap]::new(160*24,96)
$sheetGraphics=[System.Drawing.Graphics]::FromImage($sheet)
$monster=[System.Drawing.Bitmap]::new((Join-Path $projectRoot 'resource/animation/animation_monster/HealthDrain_ThornParasite_160x96.png'))
$font=[System.Drawing.Font]::new('Malgun Gothic',16)
for($frameIndex=0;$frameIndex -lt 24;$frameIndex++){
 $frame=[System.Drawing.Bitmap]::new(160,96)
 $angle=-2*[Math]::PI*$frameIndex/24
 $cos=[Math]::Cos($angle);$sin=[Math]::Sin($angle)
 foreach($index in $hole){
  $x=$index%160;$y=[int][Math]::Floor($index/160)
  $u=($x-$cx)/$rx;$v=($y-$cy)/$ry
  $sx=[int][Math]::Clamp((0.5+0.35*($u*$cos-$v*$sin))*($swirl.Width-1),0,$swirl.Width-1)
  $sy=[int][Math]::Clamp((0.5+0.35*($u*$sin+$v*$cos))*($swirl.Height-1),0,$swirl.Height-1)
  $frame.SetPixel($x,$y,$swirl.GetPixel($sx,$sy))
 }
 $g=[System.Drawing.Graphics]::FromImage($frame)
 $g.DrawImageUnscaled($base,0,0)
 $g.Dispose()
 $frame.Save((Join-Path $frames ('portal_{0:D2}.png' -f $frameIndex)),[System.Drawing.Imaging.ImageFormat]::Png)
 if($frameIndex -eq 0){$frame.Save((Join-Path $output 'Portal_Idle.png'),[System.Drawing.Imaging.ImageFormat]::Png)}
 $sheetGraphics.DrawImageUnscaled($frame,160*$frameIndex,0)
 $preview=[System.Drawing.Bitmap]::new(640,240)
 $g=[System.Drawing.Graphics]::FromImage($preview)
 $g.Clear([System.Drawing.Color]::FromArgb(163,203,248))
 $g.DrawString('몬스터 (76px)',$font,[System.Drawing.Brushes]::MidnightBlue,90,10)
 $g.DrawString('포탈 (76px)',$font,[System.Drawing.Brushes]::MidnightBlue,410,10)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
 $g.DrawImage($monster,[System.Drawing.Rectangle]::new(0,40,320,192),0,0,160,96,[System.Drawing.GraphicsUnit]::Pixel)
 $g.DrawImage($frame,[System.Drawing.Rectangle]::new(320,40,320,192),0,0,160,96,[System.Drawing.GraphicsUnit]::Pixel)
 $preview.Save((Join-Path $frames ('preview_{0:D2}.png' -f $frameIndex)),[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$preview.Dispose();$frame.Dispose()
}
$sheet.Save((Join-Path $output 'Portal_Idle_24Frame.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sheetGraphics.Dispose();$sheet.Dispose();$monster.Dispose();$font.Dispose();$base.Dispose();$small.Dispose();$rim.Dispose();$swirl.Dispose()
@{frameWidth=160;frameHeight=96;frameCount=24;fps=20;durationSeconds=1.2;loop=$true;visibleHeight=76;visibleWidth=$portalWidth;footY=86} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'Portal_Idle_24Frame.json') -Encoding utf8
& ffmpeg -hide_banner -loglevel error -y -framerate 20 -i (Join-Path $frames 'portal_%02d.png') -filter_complex '[0:v]split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse=alpha_threshold=128' -loop 0 (Join-Path $output 'Portal_Idle_Preview.gif')
if($LASTEXITCODE -ne 0){throw 'Portal GIF export failed'}
& ffmpeg -hide_banner -loglevel error -y -framerate 20 -i (Join-Path $frames 'preview_%02d.png') -filter_complex '[0:v]split[a][b];[a]palettegen[p];[b][p]paletteuse' -loop 0 (Join-Path $output 'Portal_Monster_Comparison.gif')
if($LASTEXITCODE -ne 0){throw 'Comparison GIF export failed'}
Write-Output "Created 24 frames, 20 FPS, 1.2-second loop; visible portal ${portalWidth}x76, monster 75x76."
