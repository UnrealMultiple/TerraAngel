using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ReLogic.OS.FNA
{
    public class FNAWindow : IWindowService
    {
        public float GetScaling()
        {
            return 1f;
        }

        public void SetQuickEditEnabled(bool enabled)
        {

        }

        public void Activate(GameWindow window)
        {
            throw new NotImplementedException();
        }

        public bool IsSizeable(GameWindow window)
        {
            throw new NotImplementedException();
        }

        public void SetPosition(GameWindow window, int x, int y)
        {
            throw new NotImplementedException();
        }

        public Rectangle GetBounds(GameWindow window)
        {
            throw new NotImplementedException();
        }

        public void SetUnicodeTitle(GameWindow window, string title)
        {
            SDL3.SDL.SDL_SetWindowTitle(window.Handle, title);
        }

        public void StartFlashingIcon(GameWindow window)
        {
            SDL3.SDL.SDL_FlashWindow(window.Handle, SDL3.SDL.SDL_FlashOperation.SDL_FLASH_BRIEFLY);
        }

        public void StopFlashingIcon(GameWindow window)
        {
            SDL3.SDL.SDL_FlashWindow(window.Handle, SDL3.SDL.SDL_FlashOperation.SDL_FLASH_CANCEL);
        }
    }
}
