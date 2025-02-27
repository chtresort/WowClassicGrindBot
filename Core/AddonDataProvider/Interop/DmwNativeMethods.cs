﻿using System;
using System.Runtime.InteropServices;

namespace Core.AddonDataProvider.Interop;

internal sealed class DmwNativeMethods
{
    public delegate bool DwmGetDxSharedSurfaceDelegate(IntPtr hWnd, out IntPtr phSurface, out long pAdapterLuid, out long pFmtWindow, out long pPresentFlags, out long pWin32KUpdateId);

    public static DwmGetDxSharedSurfaceDelegate DwmGetDxSharedSurface;

    static DmwNativeMethods()
    {
        IntPtr ptr = GetProcAddress(GetModuleHandle("user32"), nameof(DwmGetDxSharedSurface));
        DwmGetDxSharedSurface = Marshal.GetDelegateForFunctionPointer<DwmGetDxSharedSurfaceDelegate>(ptr);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}
