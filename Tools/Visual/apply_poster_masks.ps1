$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$posters = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Assets/SleepwalkingVisual/Textures/Posters'))
$masks = Join-Path $posters 'Masks'
$processed = Join-Path $posters 'Masked'
$files = @(Get-ChildItem -LiteralPath $posters -File -Filter '*.png' | Sort-Object Name)
if ($files.Count -ne 12) { throw "Expected 12 posters, found $($files.Count)." }

foreach ($file in $files)
{
    $maskPath = Join-Path $masks ($file.BaseName + '-edge-mask.png')
    if (!(Test-Path -LiteralPath $maskPath)) { throw "Missing mask: $maskPath" }

    $source = [System.Drawing.Bitmap]::new($file.FullName)
    $mask = [System.Drawing.Bitmap]::new($maskPath)
    try
    {
        if ($source.Width -ne $mask.Width -or $source.Height -ne $mask.Height)
        { throw "Mask dimensions differ from $($file.Name)." }
    }
    finally
    {
        $source.Dispose()
        $mask.Dispose()
    }
}

if (!(Test-Path -LiteralPath $processed))
{
    New-Item -ItemType Directory -Path $processed | Out-Null
}

foreach ($file in $files)
{
    $maskPath = Join-Path $masks ($file.BaseName + '-edge-mask.png')
    $destination = Join-Path $processed $file.Name
    $temporary = Join-Path $processed ('.' + $file.BaseName + '.masked.tmp.png')
    if (Test-Path -LiteralPath $temporary) { throw "Temporary file already exists: $temporary" }

    $source = [System.Drawing.Bitmap]::new($file.FullName)
    $mask = [System.Drawing.Bitmap]::new($maskPath)
    $result = [System.Drawing.Bitmap]::new($source.Width, $source.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try
    {
        for ($y = 0; $y -lt $source.Height; $y++)
        {
            for ($x = 0; $x -lt $source.Width; $x++)
            {
                $pixel = $source.GetPixel($x, $y)
                $alpha = [int][Math]::Round($pixel.A * $mask.GetPixel($x, $y).A / 255.0)
                $result.SetPixel($x, $y,
                    [System.Drawing.Color]::FromArgb($alpha, $pixel.R, $pixel.G, $pixel.B))
            }
        }

        $result.Save($temporary, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally
    {
        $result.Dispose()
        $mask.Dispose()
        $source.Dispose()
    }

    [IO.File]::Move($temporary, $destination, $true)

    $metaPath = $destination + '.meta'
    if (!(Test-Path -LiteralPath $metaPath))
    {
        $meta = [IO.File]::ReadAllText($file.FullName + '.meta')
        $meta = [regex]::Replace($meta, '(?m)^guid: [0-9a-f]{32}\r?$', 'guid: ' + [guid]::NewGuid().ToString('N'))
        [IO.File]::WriteAllText($metaPath, $meta)
    }

    Write-Output "Masked $($file.Name) -> $destination"
}
