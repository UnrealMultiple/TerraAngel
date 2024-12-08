using System.Runtime.InteropServices;

namespace ReLogic.Localization.IME.WinImm32;

public static class NativeMethods
{
    [DllImport("Imm32.dll")]
    public static extern bool ImmSetOpenStatus(nint hImc, bool bOpen);

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern nint ImmGetContext(nint hWnd);

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern bool ImmReleaseContext(nint hWnd, nint hImc);

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern nint ImmCreateContext();

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern bool ImmDestroyContext(nint hImc);

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern nint ImmAssociateContext(nint hWnd, nint hImc);

    [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
    public static extern int ImmGetCompositionString(nint hImc, uint dwIndex, ref byte lpBuf, int dwBufLen);

    [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
    public static extern bool ImmSetCompositionString(nint hImc, uint dwIndex, string lpComp, int dwCompLen, string lpRead, int dwReadLen);

    [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
    public static extern int ImmGetCandidateList(nint hImc, uint dwIndex, ref byte lpCandList, int dwBufLen);

    [DllImport("imm32.dll")]
    public static extern nint ImmGetDefaultIMEWnd(nint hWnd);

    [DllImport("Imm32.dll", CharSet = CharSet.Unicode)]
    public static extern bool ImmNotifyIME(nint hImc, uint dwAction, uint dwIndex, uint dwValue);
}