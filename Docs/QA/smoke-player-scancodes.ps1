Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class QaInputNative
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
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int inputSize);

    public static bool SendScan(ushort scanCode, bool released)
    {
        Input input = new Input();
        input.Type = 1;
        input.Data.Keyboard = new KeyboardInput
        {
            VirtualKey = 0,
            ScanCode = scanCode,
            Flags = (uint)(8 | (released ? 2 : 0)),
            Time = 0,
            ExtraInfo = UIntPtr.Zero
        };
        return SendInput(1, new[] { input }, Marshal.SizeOf(typeof(Input))) == 1;
    }
}
'@

$qaRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$qaExe = Join-Path $qaRoot 'Builds\Windows\Sleepwalking.exe'
$qaLog = Join-Path $PSScriptRoot 'player-scan-smoke.log'
$qaPlayer = Start-Process -FilePath $qaExe -ArgumentList '-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',$qaLog -WindowStyle Normal -PassThru
$qaWindow = [IntPtr]::Zero

function Assert-OwnForeground
{
    $qaPlayer.Refresh()
    if ($qaPlayer.HasExited) { throw 'Player exited before input' }
    $qaForeground = [QaInputNative]::GetForegroundWindow()
    $qaOwner = [uint32]0
    [QaInputNative]::GetWindowThreadProcessId($qaForeground, [ref]$qaOwner) | Out-Null
    if ($qaForeground -ne $qaWindow -or $qaOwner -ne [uint32]$qaPlayer.Id)
    {
        throw "Foreground belongs to another window: handle=$qaForeground owner=$qaOwner expectedPid=$($qaPlayer.Id)"
    }
}

function Save-OwnWindow([string]$name)
{
    $qaOwner = [uint32]0
    [QaInputNative]::GetWindowThreadProcessId($qaWindow, [ref]$qaOwner) | Out-Null
    if ($qaOwner -ne [uint32]$qaPlayer.Id) { throw 'Capture handle no longer belongs to Player' }

    $qaRect = New-Object QaInputNative+Rect
    if (-not [QaInputNative]::GetWindowRect($qaWindow, [ref]$qaRect))
    {
        throw 'GetWindowRect failed'
    }
    $qaWidth = $qaRect.Right - $qaRect.Left
    $qaHeight = $qaRect.Bottom - $qaRect.Top
    if ($qaWidth -lt 64 -or $qaHeight -lt 64) { throw 'Player window is too small for capture' }

    $qaBitmap = [System.Drawing.Bitmap]::new($qaWidth, $qaHeight)
    $qaGraphics = [System.Drawing.Graphics]::FromImage($qaBitmap)
    $qaContext = $qaGraphics.GetHdc()
    try
    {
        $qaPrinted = [QaInputNative]::PrintWindow($qaWindow, $qaContext, 2)
    }
    finally
    {
        $qaGraphics.ReleaseHdc($qaContext)
    }
    $qaPath = Join-Path $PSScriptRoot $name
    $qaBitmap.Save($qaPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $qaGraphics.Dispose()
    $qaBitmap.Dispose()
    Write-Output "Capture=$qaPath PrintWindow=$qaPrinted"
}

function Send-OwnKey([ushort]$scanCode, [string]$name)
{
    Assert-OwnForeground
    $qaDown = [QaInputNative]::SendScan($scanCode, $false)
    Start-Sleep -Milliseconds 120
    Assert-OwnForeground
    $qaUp = [QaInputNative]::SendScan($scanCode, $true)
    Write-Output "Sent=$name Down=$qaDown Up=$qaUp PID=$($qaPlayer.Id) Foreground=$qaWindow"
}

try
{
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

    Start-Sleep -Seconds 8
    Assert-OwnForeground
    Save-OwnWindow 'player-scan-menu.png'

    Send-OwnKey 0x0F 'Tab'
    Start-Sleep -Milliseconds 500
    Save-OwnWindow 'player-scan-tab.png'

    Send-OwnKey 0x1C 'Enter'
    Start-Sleep -Seconds 2
    Save-OwnWindow 'player-scan-after-enter.png'

    Send-OwnKey 0x01 'Escape'
    Start-Sleep -Milliseconds 700
    Save-OwnWindow 'player-scan-after-escape.png'
}
finally
{
    if (-not $qaPlayer.HasExited)
    {
        Stop-Process -Id $qaPlayer.Id -Force
    }
}
