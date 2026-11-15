param(
    [string]$ImageDirectory = (Join-Path $PSScriptRoot '../../Docs/QA/VisualLogs/GridMatrix')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$directory = [IO.Path]::GetFullPath($ImageDirectory)
$themes = @('Meat', 'PostApocalypse', 'DeepWater', 'Heaven')
$ratios = @(
    @{ Name = '16x9'; Width = 320; Height = 180 },
    @{ Name = '4x3'; Width = 320; Height = 240 }
)

foreach ($ratio in $ratios)
{
    $margin = 8
    $labelHeight = 26
    $cellWidth = $ratio.Width + $margin * 2
    $cellHeight = $ratio.Height + $labelHeight + $margin * 2
    $sheet = [Drawing.Bitmap]::new($cellWidth * 4, $cellHeight * 7,
        [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($sheet)
    $font = [Drawing.Font]::new('Consolas', 12, [Drawing.FontStyle]::Bold)
    $label = [Drawing.SolidBrush]::new([Drawing.Color]::White)
    try
    {
        $graphics.Clear([Drawing.Color]::FromArgb(24, 22, 29))
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        for ($level = 1; $level -le 7; $level++)
        {
            for ($theme = 0; $theme -lt 4; $theme++)
            {
                $name = 'L{0:D2}-{1}-{2}.png' -f $level, $themes[$theme], $ratio.Name
                $path = Join-Path $directory $name
                if (!(Test-Path -LiteralPath $path)) { throw "Missing matrix frame: $path" }
                $image = [Drawing.Bitmap]::new($path)
                try
                {
                    $x = $theme * $cellWidth + $margin
                    $y = ($level - 1) * $cellHeight + $margin
                    $graphics.DrawString(('L{0:D2}  {1}' -f $level, $themes[$theme]), $font, $label, $x, $y)
                    $graphics.DrawImage($image,
                        [Drawing.Rectangle]::new($x, $y + $labelHeight, $ratio.Width, $ratio.Height))
                }
                finally { $image.Dispose() }
            }
        }
        $output = Join-Path $directory ('GridMatrixContact-' + $ratio.Name + '.png')
        $sheet.Save($output, [Drawing.Imaging.ImageFormat]::Png)
        Write-Output $output
    }
    finally
    {
        $label.Dispose()
        $font.Dispose()
        $graphics.Dispose()
        $sheet.Dispose()
    }
}
