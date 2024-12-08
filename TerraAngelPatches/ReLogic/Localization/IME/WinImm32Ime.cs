namespace ReLogic.Localization.IME;

using System;
using System.Runtime.InteropServices;
using System.Text;
using WinImm32;
using OS.Windows;

internal class WinImm32Ime : PlatformIme, IMessageFilter
{
    private nint _hWnd;

    private nint _hImc;

    private bool _isFocused;

    private WindowsMessageHook _wndProcHook;

    private bool _disposedValue;

    private string _compString;

    private string[] _candList = Array.Empty<string>();

    private uint _candSelection;

    private uint _candPageSize;

    public uint SelectedPage
    {
        get
        {
            if (_candPageSize != 0)
            {
                return _candSelection / _candPageSize;
            }
            return 0u;
        }
    }

    public override string CompositionString => _compString;

    public override bool IsCandidateListVisible => CandidateCount != 0;

    public override uint SelectedCandidate => _candSelection % _candPageSize;

    public override uint CandidateCount => Math.Min((uint)_candList.Length - SelectedPage * _candPageSize, _candPageSize);

    public WinImm32Ime(WindowsMessageHook wndProcHook, nint hWnd)
    {
        _wndProcHook = wndProcHook;
        _hWnd = hWnd;
        _hImc = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetContext(_hWnd);
        ReLogic.Localization.IME.WinImm32.NativeMethods.ImmReleaseContext(_hWnd, _hImc);
        _isFocused = ReLogic.OS.Windows.NativeMethods.GetForegroundWindow() == _hWnd;
        _wndProcHook.AddMessageFilter(this);
        SetEnabled(bEnable: false);
    }

    private void SetEnabled(bool bEnable)
    {
        ReLogic.Localization.IME.WinImm32.NativeMethods.ImmAssociateContext(_hWnd, bEnable ? _hImc : IntPtr.Zero);
    }

    private void FinalizeString(bool bSend = false)
    {
        nint hImc = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetContext(_hWnd);
        try
        {
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmNotifyIME(hImc, 21u, 4u, 0u);
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmSetCompositionString(hImc, 9u, "", 0, null, 0);
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmNotifyIME(hImc, 16u, 0u, 0u);
        }
        finally
        {
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    private string GetCompositionString()
    {
        nint hImc = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetContext(_hWnd);
        try
        {
            int size = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetCompositionString(hImc, 8u, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            if (size == 0)
            {
                return "";
            }
            Span<byte> buf = stackalloc byte[size];
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetCompositionString(hImc, 8u, ref MemoryMarshal.GetReference(buf), size);
            return Encoding.Unicode.GetString(buf.ToArray());
        }
        finally
        {
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    private void UpdateCandidateList()
    {
        nint hImc = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetContext(_hWnd);
        try
        {
            int size = ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetCandidateList(hImc, 0u, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            if (size == 0)
            {
                _candList = Array.Empty<string>();
                _candPageSize = 0u;
                _candSelection = 0u;
                return;
            }
            Span<byte> buf = stackalloc byte[size];
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmGetCandidateList(hImc, 0u, ref MemoryMarshal.GetReference(buf), size);
            ref CandidateList candList = ref MemoryMarshal.AsRef<CandidateList>(buf);
            ReadOnlySpan<uint> offsets = MemoryMarshal.CreateReadOnlySpan(ref candList.dwOffset, (int)candList.dwCount);
            string[] candStrList = new string[candList.dwCount];
            int next = buf.Length;
            for (int i = (int)(candList.dwCount - 1); i >= 0; i--)
            {
                int start = (int)offsets[i];
                int num = i;
                Encoding unicode = Encoding.Unicode;
                int num2 = start;
                candStrList[num] = unicode.GetString(buf.Slice(num2, next - 2 - num2));
                next = start;
            }
            _candList = candStrList;
            _candPageSize = candList.dwPageSize;
            _candSelection = candList.dwSelection;
        }
        finally
        {
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    public override string GetCandidate(uint index)
    {
        if (index < CandidateCount)
        {
            return _candList[index + SelectedPage * _candPageSize];
        }
        return "";
    }

    protected override void OnEnable()
    {
        if (_isFocused)
        {
            SetEnabled(bEnable: true);
        }
    }

    protected override void OnDisable()
    {
        FinalizeString();
        SetEnabled(bEnable: false);
    }

    public bool PreFilterMessage(ref Message message)
    {
        if (message.Msg == 8)
        {
            SetEnabled(bEnable: false);
            _isFocused = false;
            return true;
        }
        if (message.Msg == 7)
        {
            if (base.IsEnabled)
            {
                SetEnabled(bEnable: true);
            }
            _isFocused = true;
            return true;
        }
        if (message.Msg == 641)
        {
            message.LParam = IntPtr.Zero;
            return false;
        }
        if (!base.IsEnabled)
        {
            return false;
        }
        switch (message.Msg)
        {
        case 81:
            return true;
        case 269:
            _compString = "";
            return true;
        case 271:
            _compString = GetCompositionString();
            break;
        case 270:
            _compString = "";
            UpdateCandidateList();
            break;
        case 642:
        {
            int num = ((IntPtr)message.WParam).ToInt32();
            if ((uint)(num - 3) <= 2u)
            {
                UpdateCandidateList();
            }
            return true;
        }
        case 258:
            OnKeyPress((char)((IntPtr)message.WParam).ToInt32());
            break;
        }
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (base.IsEnabled)
            {
                Disable();
            }
            _wndProcHook.RemoveMessageFilter(this);
            ReLogic.Localization.IME.WinImm32.NativeMethods.ImmAssociateContext(_hWnd, _hImc);
            _disposedValue = true;
        }
    }

    ~WinImm32Ime()
    {
        Dispose(disposing: false);
    }
}