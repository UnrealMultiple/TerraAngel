using System;

namespace ReLogic.OS.Windows;

public struct Message
{
    public nint HWnd;

    public int Msg;

    public nint WParam;

    public nint LParam;

    public nint Result;

    public static Message Create(nint hWnd, int msg, nint wparam, nint lparam)
    {
        Message result = default(Message);
        result.HWnd = hWnd;
        result.Msg = msg;
        result.WParam = wparam;
        result.LParam = lparam;
        result.Result = IntPtr.Zero;
        return result;
    }
}