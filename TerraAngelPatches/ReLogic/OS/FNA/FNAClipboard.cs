using System;
using System.Threading;
using ReLogic.OS.Base;

namespace ReLogic.OS.FNA
{
    internal class FNAClipboard : ReLogic.OS.Base.Clipboard
    {
        protected override string GetClipboard()
        {
            return SDL3.SDL.SDL_GetClipboardText();
        }

        protected override void SetClipboard(string text)
        {
            SDL3.SDL.SDL_SetClipboardText(text);
        }
    }
}
