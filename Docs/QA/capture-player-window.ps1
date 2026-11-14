param(
    [int]$Width = 1280,
    [int]$Height = 720,
    [string]$Tag = '',
    [string]$ExePath = ''
)

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class QaCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}
'@

$qaRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$qaExe = if ([string]::IsNullOrWhiteSpace($ExePath))
{
    Join-Path $qaRoot 'Builds\Windows\Sleepwalking.exe'
}
else
{
    (Resolve-Path -LiteralPath $ExePath).Path
}
$qaSuffix = if ([string]::IsNullOrWhiteSpace($Tag)) { '' } else { '-' + $Tag }
$qaLog = Join-Path $PSScriptRoot ('player-visible' + $qaSuffix + '.log')
$qaPrintPath = Join-Path $PSScriptRoot ('player-printwindow' + $qaSuffix + '.png')
$qaScreenPath = Join-Path $PSScriptRoot ('player-screen' + $qaSuffix + '.png')
$qaPlayer = Start-Process -FilePath $qaExe -ArgumentList '-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',$qaLog -WindowStyle Normal -PassThru

try
{
    $qaWindow = [IntPtr]::Zero
    for ($q = 0; $q -lt 30; $q++)
    {
        Start-Sleep -Milliseconds 500
        $qaPlayer.Refresh()
        if ($qaPlayer.HasExited) { break }
        $qaWindow = $qaPlayer.MainWindowHandle
        if ($qaWindow -ne [IntPtr]::Zero) { break }
    }

    Write-Output "PID=$($qaPlayer.Id) HasExited=$($qaPlayer.HasExited) MainWindowHandle=$qaWindow"
    if ($qaWindow -eq [IntPtr]::Zero) { return }

    # Захват после появления меню: первый кадр после создания окна был заставкой Unity.
    Start-Sleep -Seconds 8
    $qaPlayer.Refresh()
    if ($qaPlayer.HasExited)
    {
        Write-Output 'Player exited before delayed capture'
        return
    }
    $qaWindow = $qaPlayer.MainWindowHandle
    if ($qaWindow -eq [IntPtr]::Zero)
    {
        Write-Output 'Player window disappeared before delayed capture'
        return
    }

    $qaRect = New-Object QaCaptureNative+Rect
    if (-not [QaCaptureNative]::GetWindowRect($qaWindow, [ref]$qaRect))
    {
        Write-Output 'GetWindowRect failed'
        return
    }

    $qaWidth = $qaRect.Right - $qaRect.Left
    $qaHeight = $qaRect.Bottom - $qaRect.Top
    Write-Output "Rect=$($qaRect.Left),$($qaRect.Top),$qaWidth,$qaHeight"
    if ($qaWidth -lt 64 -or $qaHeight -lt 64) { return }

    $qaBitmap = [System.Drawing.Bitmap]::new($qaWidth, $qaHeight)
    $qaGraphics = [System.Drawing.Graphics]::FromImage($qaBitmap)
    $qaContext = $qaGraphics.GetHdc()
    try
    {
        $qaPrinted = [QaCaptureNative]::PrintWindow($qaWindow, $qaContext, 2)
    }
    finally
    {
        $qaGraphics.ReleaseHdc($qaContext)
    }
    $qaBitmap.Save($qaPrintPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "PrintWindow=$qaPrinted Path=$qaPrintPath"
    $qaGraphics.Dispose()
    $qaBitmap.Dispose()

    $qaForeground = [QaCaptureNative]::GetForegroundWindow()
    Write-Output "ForegroundWindow=$qaForeground"
    if ($qaForeground -ne $qaWindow) { return }

    $qaScreenBitmap = [System.Drawing.Bitmap]::new($qaWidth, $qaHeight)
    $qaScreenGraphics = [System.Drawing.Graphics]::FromImage($qaScreenBitmap)
    try
    {
        $qaScreenGraphics.CopyFromScreen($qaRect.Left, $qaRect.Top, 0, 0,
            [System.Drawing.Size]::new($qaWidth, $qaHeight))
        $qaScreenBitmap.Save($qaScreenPath, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output "CopyFromScreen=$qaScreenPath"
    }
    finally
    {
        $qaScreenGraphics.Dispose()
        $qaScreenBitmap.Dispose()
    }
}
finally
{
    if (-not $qaPlayer.HasExited)
    {
        Stop-Process -Id $qaPlayer.Id -Force
    }
}
