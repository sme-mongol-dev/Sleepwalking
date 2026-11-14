param(
    [int]$Width = 1280,
    [int]$Height = 720,
    [string]$Tag = '16x9',
    [string]$Name = 'illustrated',
    [string]$PlayerPath = '',
    [int]$MoveRightMs = 0,
    [int]$ClickX = -1,
    [int]$ClickY = -1,
    [int]$WaitMs = 8000,
    [string[]]$ExtraArgs = @()
)

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class IllustratedCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extraInfo);
}
'@

$projectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
if ([string]::IsNullOrWhiteSpace($PlayerPath))
{
    $PlayerPath = Join-Path $projectRoot 'Builds\VisualPreview\SleepwalkingIllustrated.exe'
}
$qaRoot = Join-Path $projectRoot 'Docs\QA'
$playerLog = Join-Path $qaRoot "player-$Name-$Tag.log"
$screenshot = Join-Path $qaRoot "player-screen-$Name-$Tag.png"
$arguments = @('-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',$playerLog) + $ExtraArgs
$player = Start-Process -FilePath $PlayerPath -ArgumentList $arguments -WindowStyle Normal -PassThru

try
{
    $window = [IntPtr]::Zero
    for ($q = 0; $q -lt 30; $q++)
    {
        Start-Sleep -Milliseconds 500
        $player.Refresh()
        if ($player.HasExited) { break }
        $window = $player.MainWindowHandle
        if ($window -ne [IntPtr]::Zero) { break }
    }
    if ($window -eq [IntPtr]::Zero) { throw 'Illustrated Player window was not created.' }

    # Первый кадр может быть заставкой Unity; снимаем уже открытое меню.
    Start-Sleep -Milliseconds $WaitMs
    $player.Refresh()
    if ($player.HasExited) { throw 'Illustrated Player exited before capture.' }
    $window = $player.MainWindowHandle
    if ($MoveRightMs -gt 0)
    {
        [void][IllustratedCaptureNative]::SetForegroundWindow($window)
        Start-Sleep -Milliseconds 200
        [IllustratedCaptureNative]::keybd_event(0x44, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds $MoveRightMs
        [IllustratedCaptureNative]::keybd_event(0x44, 0, 2, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200
    }
    $rect = New-Object IllustratedCaptureNative+Rect
    if (-not [IllustratedCaptureNative]::GetWindowRect($window, [ref]$rect)) { throw 'GetWindowRect failed.' }
    $imageWidth = $rect.Right - $rect.Left
    $imageHeight = $rect.Bottom - $rect.Top
    Write-Output "WindowRect=$($rect.Left),$($rect.Top),$imageWidth,$imageHeight"
    if ($imageWidth -lt 64 -or $imageHeight -lt 64) { throw 'Player window is too small.' }

    if ($ClickX -ge 0 -and $ClickY -ge 0)
    {
        [void][IllustratedCaptureNative]::SetForegroundWindow($window)
        [void][IllustratedCaptureNative]::SetCursorPos($rect.Left + $ClickX, $rect.Top + $ClickY)
        Start-Sleep -Milliseconds 300
        [IllustratedCaptureNative]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
        [IllustratedCaptureNative]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Seconds 4
    }

    [void][IllustratedCaptureNative]::SetForegroundWindow($window)
    Start-Sleep -Milliseconds 500
    $bitmap = [System.Drawing.Bitmap]::new($imageWidth, $imageHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    if ([IllustratedCaptureNative]::GetForegroundWindow() -eq $window)
    {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0,
            [System.Drawing.Size]::new($imageWidth, $imageHeight))
    }
    else
    {
        $dc = $graphics.GetHdc()
        try { $printed = [IllustratedCaptureNative]::PrintWindow($window, $dc, 2) }
        finally { $graphics.ReleaseHdc($dc) }
        if (-not $printed) { $graphics.Dispose(); $bitmap.Dispose(); throw 'PrintWindow failed.' }
    }
    $graphics.Dispose()
    $bitmap.Save($screenshot, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Output $screenshot
}
finally
{
    if (-not $player.HasExited) { Stop-Process -Id $player.Id -Force }
}
