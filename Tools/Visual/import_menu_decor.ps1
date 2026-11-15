$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourceDirectory = 'C:\Users\TIMBUHTUK\Downloads\Telegram Desktop'
$textures = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Assets/SleepwalkingVisual/Textures'))
$originals = Join-Path $textures 'Source/MenuDecor'
$processed = Join-Path $textures 'MenuDecor'
$metaTemplate = [IO.File]::ReadAllText((Join-Path $textures 'Menu/Clock.png.meta'))

$assets = @(
    @{ Source = 'Tapochki.png'; Name = 'Slippers'; Palette = @('161318', '3C2030', '8C5050', 'B4202A', 'E04020', 'F8D4B8') },
    @{ Source = 'T_Eye01 (3).png'; Name = 'Eye'; Palette = @('161318', '222036', '3C2030', '229EDE', 'B4B8D4') },
    @{ Source = 'T_Eyeball01 (3).png'; Name = 'Eyeball'; Palette = @('161318', '3C2030', '6E1830', 'B4202A', 'E04020') },
    @{ Source = 'ClockArrowSmall (3).png'; Name = 'ClockArrowSmall'; Palette = @('161318') },
    @{ Source = 'ClockArrowBig (3).png'; Name = 'ClockArrowBig'; Palette = @('161318') }
)

function Ensure-Meta([string]$path)
{
    $metaPath = $path + '.meta'
    if (Test-Path -LiteralPath $metaPath) { return }
    $guid = [guid]::NewGuid().ToString('N')
    $meta = [regex]::Replace($metaTemplate, '(?m)^guid: [0-9a-f]{32}\r?$', "guid: $guid")
    [IO.File]::WriteAllText($metaPath, $meta)
}

function Ensure-FolderMeta([string]$path)
{
    if (!(Test-Path -LiteralPath $path)) { New-Item -ItemType Directory -Path $path | Out-Null }
    $metaPath = $path + '.meta'
    if (Test-Path -LiteralPath $metaPath) { return }
    $guid = [guid]::NewGuid().ToString('N')
    [IO.File]::WriteAllText($metaPath, "fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}

Ensure-FolderMeta $originals
Ensure-FolderMeta $processed

foreach ($asset in $assets)
{
    $sourcePath = Join-Path $sourceDirectory $asset.Source
    if (!(Test-Path -LiteralPath $sourcePath)) { throw "Missing source texture: $sourcePath" }
    $originalPath = Join-Path $originals ($asset.Name + '.png')
    Copy-Item -LiteralPath $sourcePath -Destination $originalPath -Force
    Ensure-Meta $originalPath

    $source = [Drawing.Bitmap]::new($originalPath)
    $image = [Drawing.Bitmap]::new($source.Width, $source.Height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $colors = @($asset.Palette | ForEach-Object { [Drawing.ColorTranslator]::FromHtml('#' + $_) })
    try
    {
        for ($y = 0; $y -lt $source.Height; $y++)
        {
            for ($x = 0; $x -lt $source.Width; $x++)
            {
                $pixel = $source.GetPixel($x, $y)
                if ($pixel.A -eq 0) { continue }
                $best = $colors[0]
                $distance = [double]::PositiveInfinity
                foreach ($color in $colors)
                {
                    $red = $pixel.R - $color.R
                    $green = $pixel.G - $color.G
                    $blue = $pixel.B - $color.B
                    $value = $red * $red * 0.30 + $green * $green * 0.59 + $blue * $blue * 0.11
                    if ($value -lt $distance) { $distance = $value; $best = $color }
                }
                $image.SetPixel($x, $y, [Drawing.Color]::FromArgb($pixel.A, $best.R, $best.G, $best.B))
            }
        }

        $processedPath = Join-Path $processed ($asset.Name + '.png')
        $image.Save($processedPath, [Drawing.Imaging.ImageFormat]::Png)
        Ensure-Meta $processedPath
        Write-Output "$($asset.Name): $($source.Width)x$($source.Height)"
    }
    finally
    {
        $image.Dispose()
        $source.Dispose()
    }
}
