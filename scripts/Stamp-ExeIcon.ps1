param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,
    [string]$IconPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($IconPath)) {
    $IconPath = Join-Path $root "Keirohi.Avalonia\Assets\AppIcon.ico"
}

if (-not (Test-Path $ExePath)) { throw "Exe not found: $ExePath" }
if (-not (Test-Path $IconPath)) { throw "Icon not found: $IconPath" }

Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Runtime.InteropServices;

public static class KeirohiIconStamp {
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr BeginUpdateResource(string fileName, bool deleteExisting);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool EndUpdateResource(IntPtr update, bool discard);

    public static void Apply(string exePath, string iconPath) {
        byte[] ico = File.ReadAllBytes(iconPath);
        if (ico.Length < 22 || ico[2] != 1 || ico[3] != 0) {
            throw new InvalidDataException("AppIcon.ico is not an icon file.");
        }

        int count = BitConverter.ToUInt16(ico, 4);
        if (count < 1 || count > 32) {
            throw new InvalidDataException("Unexpected icon image count: " + count);
        }

        IntPtr RT_ICON = (IntPtr)3;
        IntPtr RT_GROUP_ICON = (IntPtr)14;
        const int groupId = 32512;
        byte[] group = new byte[6 + (count * 14)];
        group[2] = 1;
        group[4] = (byte)(count & 0xFF);
        group[5] = (byte)((count >> 8) & 0xFF);

        IntPtr handle = BeginUpdateResource(exePath, false);
        if (handle == IntPtr.Zero) {
            throw new InvalidOperationException("BeginUpdateResource failed: " + Marshal.GetLastWin32Error());
        }

        try {
            for (int i = 0; i < count; i++) {
                int entry = 6 + (i * 16);
                int groupEntry = 6 + (i * 14);
                Buffer.BlockCopy(ico, entry, group, groupEntry, 12);
                int id = i + 1;
                group[groupEntry + 12] = (byte)(id & 0xFF);
                group[groupEntry + 13] = (byte)((id >> 8) & 0xFF);

                int size = BitConverter.ToInt32(ico, entry + 8);
                int offset = BitConverter.ToInt32(ico, entry + 12);
                if (size <= 0 || offset < 0 || offset + size > ico.Length) {
                    throw new InvalidDataException("Icon entry " + id + " is outside the file.");
                }

                byte[] image = new byte[size];
                Buffer.BlockCopy(ico, offset, image, 0, size);
                if (!UpdateResource(handle, RT_ICON, (IntPtr)id, 0, image, (uint)image.Length)) {
                    throw new InvalidOperationException("UpdateResource icon " + id + " failed: " + Marshal.GetLastWin32Error());
                }
            }

            if (!UpdateResource(handle, RT_GROUP_ICON, (IntPtr)groupId, 0, group, (uint)group.Length)) {
                throw new InvalidOperationException("UpdateResource group failed: " + Marshal.GetLastWin32Error());
            }

            if (!EndUpdateResource(handle, false)) {
                handle = IntPtr.Zero;
                throw new InvalidOperationException("EndUpdateResource failed: " + Marshal.GetLastWin32Error());
            }

            handle = IntPtr.Zero;
        }
        catch {
            if (handle != IntPtr.Zero) {
                EndUpdateResource(handle, true);
            }
            throw;
        }
    }
}
"@

[KeirohiIconStamp]::Apply((Resolve-Path $ExePath).Path, (Resolve-Path $IconPath).Path)
Write-Host "Stamped icon: $ExePath"
