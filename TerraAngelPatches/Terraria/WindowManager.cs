using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDL3;
using Terraria.GameInput;
using Terraria.UI;

namespace Terraria;
public class WindowManager
{
    public enum WindowState
    {
        Windowed,
        BorderlessFullscreen,
        Fullscreen
    }

    [JsonProperty("Width")]
    private int width = 800;

    [JsonProperty("Height")]
    private int height = 600;

    [JsonProperty("Maximized")]
    private bool maximized = false;

    [JsonProperty("WindowState")]
    private WindowState windowState = WindowState.Windowed;

    [JsonProperty("Vsync")]
    private bool vsync = true;

    [JsonProperty("CapFPS")]
    private bool capFPS = false;

    [JsonProperty("FPSCap")]
    private int fPSCap = 60;

    [JsonProperty("CapFPSUnfocused")]
    private bool capFPSUnfocused = true;

    [JsonProperty("FPSCapUnfocused")]
    private int fPSCapUnfocused = 15;

    private bool wantToApplyGraphics = false;
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;
    private bool centerWindow = false;

    [JsonIgnore]
    public GraphicsDeviceManager? Graphics = null;
    [JsonIgnore]
    public IntPtr WindowHandle = IntPtr.Zero;

    [JsonIgnore]
    public int Width
    {
        get => width;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than zero");
            }
            if (value > 16384)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be less than 16384");
            }

            width = value;
            ApplyWindowSize();
        }
    }

    [JsonIgnore]
    public int Height
    {
        get => height;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than or equal to zero");
            }
            if (value > 16384)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be less than 16384");
            }

            height = value;
            ApplyWindowSize();
        }
    }

    [JsonIgnore]
    public Vector2i Size
    {
        get => new(Width, Height);
        set
        {
            width = value.X;
            height = value.Y;
            ApplyWindowSize();
        }
    }

    // Width/Height are physical pixels: they are the backbuffer size the game renders into. SDL's
    // window size is in logical units, so convert with the window's pixel density. On Wayland a
    // fractional scale makes the two differ (logical 1707 <-> physical 2561).
    private void ApplyWindowSize()
    {
        if (WindowHandle == IntPtr.Zero)
        {
            return;
        }

        float density = GetPixelDensity();
        int logicalWidth = Math.Max(1, (int)Math.Round(width / density));
        int logicalHeight = Math.Max(1, (int)Math.Round(height / density));
        SDL.SDL_SetWindowSize(WindowHandle, logicalWidth, logicalHeight);
        // Keep ApplyChanges' preferred size on the size we just asked for. Otherwise a later
        // ApplyChanges (vsync/state) resets the window to the stale default backbuffer.
        if (Graphics is not null)
        {
            Graphics.PreferredBackBufferWidth = width;
            Graphics.PreferredBackBufferHeight = height;
        }
    }

    private float GetPixelDensity()
    {
        if (WindowHandle == IntPtr.Zero)
        {
            return 1f;
        }

        float density = SDL.SDL_GetWindowPixelDensity(WindowHandle);
        return density > 0f ? density : 1f;
    }

    [JsonIgnore]
    public WindowState State
    {
        get => windowState;
        set
        {
            windowState = value;
            switch (windowState)
            {
                case WindowState.Windowed:
                    {
                        bool wasFullscreen = Graphics!.IsFullScreen;
                        SDL.SDL_SetWindowFullscreen(WindowHandle, false);
                        SDL.SDL_SetWindowBordered(WindowHandle, true);
                        SDL.SDL_SetWindowResizable(WindowHandle, true);
                        Graphics.IsFullScreen = false;
                        ApplyWindowSize();

                        centerWindow = true;
                        // A normal window needs no ApplyChanges unless we are leaving exclusive
                        // fullscreen; on startup that would reset the backbuffer to the stale
                        // default size and undo the restored window size.
                        wantToApplyGraphics |= wasFullscreen;
                    }
                    break;
                case WindowState.BorderlessFullscreen:
                    {
                        SDL.SDL_SetWindowFullscreen(WindowHandle, false);
                        SDL.SDL_SetWindowBordered(WindowHandle, false);
                        SDL.SDL_SetWindowResizable(WindowHandle, false);
                        Graphics!.IsFullScreen = false;
                        SetPreferredToDesktopMode();

                        wantToApplyGraphics = true;
                    }
                    break;
                case WindowState.Fullscreen:
                    {
                        SDL.SDL_SetWindowFullscreen(WindowHandle, true);
                        SDL.SDL_SetWindowBordered(WindowHandle, false);
                        SDL.SDL_SetWindowResizable(WindowHandle, false);
                        Graphics!.IsFullScreen = true;
                        SetPreferredToDesktopMode();

                        wantToApplyGraphics = true;
                    }
                    break;
            }
        }
    }

    [JsonIgnore]
    public bool Maximized
    {
        get => State == WindowState.Windowed && maximized;
        set
        {
            maximized = value;
            if (State == WindowState.Windowed)
            {
                if (maximized) SDL.SDL_MaximizeWindow(WindowHandle);
            }
        }
    }

    [JsonIgnore]
    public Vector2i MaximumWindowSize
    {
        get
        {
            SDL.SDL_GetDisplayBounds(SDL.SDL_GetDisplayForWindow(WindowHandle), out SDL.SDL_Rect rect);
            var density = GetPixelDensity();
            return new Vector2i(rect.w * density, rect.h * density);
        }
    }

    [JsonIgnore]
    public bool Vsync
    {
        get => vsync;
        set
        {
            if (vsync == value)
            {
                return;
            }

            vsync = value;
            Main.graphics.SynchronizeWithVerticalRetrace = vsync;

            // Apply the interval without ApplyChanges: that would convert the backbuffer size back
            // to a logical window size and resize (or unmaximize) the window.
            if (Graphics?.GraphicsDevice is not null)
            {
                PresentationParameters presentation = Graphics.GraphicsDevice.PresentationParameters;
                presentation.PresentationInterval = vsync ? PresentInterval.One : PresentInterval.Immediate;
                Graphics.GraphicsDevice.Reset(presentation);
            }
        }
    }

    [JsonIgnore]
    public bool CapFPS
    {
        get => capFPS;
        set
        {
            capFPS = value;
            Main.instance.IsFixedTimeStep = value;
        }
    }

    [JsonIgnore]
    public int FPSCap
    {
        get => fPSCap;
        set
        {
            fPSCap = value;
            Main.instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double)fPSCap);
        }
    }

    [JsonIgnore]
    public bool CapFPSUnfocused
    {
        get => capFPSUnfocused;
        set => capFPSUnfocused = value;
    }

    [JsonIgnore]
    public int FPSCapUnfocused
    {
        get => fPSCapUnfocused;
        set => fPSCapUnfocused = value;
    }

    public WindowManager() { }
    public WindowManager(Game game)
    {
        WindowHandle = game.Window.Handle;
        Graphics = Main.graphics;

        if (!File.Exists(ClientLoader.WindowConfigPath))
            WriteToFile();
    }

    public void Init()
    {
        string s = File.ReadAllText(ClientLoader.WindowConfigPath);

        WindowManager? windowSettings = JsonConvert.DeserializeObject<WindowManager>(s);
        if (windowSettings is not null)
        {
            width = windowSettings.width;
            height = windowSettings.height;

            State = windowSettings.State;
            if (State == WindowState.Windowed)
            {
                Size = new Vector2i(width, height);
                Maximized = windowSettings.maximized;
            }
            Vsync = windowSettings.Vsync;
            FPSCap = windowSettings.FPSCap;
            CapFPS = windowSettings.CapFPS;
            FPSCapUnfocused = windowSettings.FPSCapUnfocused;
            CapFPSUnfocused = windowSettings.CapFPSUnfocused;
        }

        centerWindow = true;
    }

    private bool wasActive = true;
    public void Update()
    {
        if (wantToApplyGraphics)
        {
            wantToApplyGraphics = false;
            ApplyGraphics();
        }

        // The window's drawable size is the single source of truth for the backbuffer. Syncing
        // every frame picks up user resizes, Wayland scale changes and FNA's own ApplyChanges.
        SyncGraphics();

        if (State == WindowState.Windowed && centerWindow && !maximized)
        {
            SDL.SDL_GetDisplayBounds(SDL.SDL_GetDisplayForWindow(WindowHandle), out SDL.SDL_Rect rect);
            // SDL_GetDisplayBounds and SDL_SetWindowPosition work in logical units, so center using
            // the logical window size, not the physical Width/Height.
            SDL.SDL_GetWindowSize(WindowHandle, out int logicalWidth, out int logicalHeight);
            SDL.SDL_SetWindowPosition(WindowHandle, rect.x + rect.w / 2 - logicalWidth / 2, rect.y + rect.h / 2 - logicalHeight / 2);
            centerWindow = false;
        }
        if (State == WindowState.Windowed)
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_GetWindowFlags(WindowHandle);
            maximized = (flags & SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
        }

        // Handle unfocused FPS cap
        bool isActive = Main.instance.IsActive;
        if (isActive != wasActive)
        {
            wasActive = isActive;
            if (!isActive && CapFPSUnfocused)
            {
                // Window lost focus - apply unfocused FPS cap
                Main.instance.IsFixedTimeStep = true;
                Main.instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double)FPSCapUnfocused);
            }
            else if (isActive)
            {
                // Window gained focus - restore focused settings
                Main.instance.IsFixedTimeStep = CapFPS;
                if (CapFPS)
                {
                    Main.instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double)FPSCap);
                }
            }
        }

        // Set InactiveSleepTime for additional throttling when unfocused
        if (CapFPSUnfocused)
        {
            Main.instance.InactiveSleepTime = TimeSpan.FromSeconds(1d / (double)FPSCapUnfocused);
        }
        else
        {
            Main.instance.InactiveSleepTime = TimeSpan.Zero;
        }
    }

    private void SyncGraphics()
    {
        if (WindowHandle == IntPtr.Zero || Graphics?.GraphicsDevice is null)
        {
            return;
        }

        SDL.SDL_GetWindowSizeInPixels(WindowHandle, out int pixelWidth, out int pixelHeight);
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }
        
        // the real BackBuffer size should match what the os reported
        // if the os reported odd number width and height, pass it back for real BackBuffer to make it happy
        // or else they will fk everything up and make the whole game window blurry as sh*t 
        var presentations = Graphics.GraphicsDevice.PresentationParameters;
        if (presentations.BackBufferWidth != pixelWidth || presentations.BackBufferHeight != pixelHeight)
        {
            presentations.BackBufferWidth = pixelWidth;
            presentations.BackBufferHeight = pixelHeight;
            Graphics.GraphicsDevice.Reset(presentations);
        }

        if (Graphics.PreferredBackBufferWidth != pixelWidth || Graphics.PreferredBackBufferHeight != pixelHeight)
        {
            Graphics.PreferredBackBufferWidth = pixelWidth;
            Graphics.PreferredBackBufferHeight = pixelHeight;
        }

        // fk wayland for your `logical window size`. How can a protocol not providing ANY api for
        // querying physical window size.
        // What? U expect us to calculate by using dpi scale?
        // THEN HOW THE FUCK CAN WE HANDLE FLOATING POINT ERRORS!
        // LMAO my screen width is 2560, and I fking got 2561 from SDL
        // can wayland devs explain where the 1px came from? from your brain tumors?
        // FK WAYLAND

        width = pixelWidth;
        height = pixelHeight;

        if (pixelWidth != lastScreenWidth || pixelHeight != lastScreenHeight)
        {
            lastScreenWidth = pixelWidth;
            lastScreenHeight = pixelHeight;
            UpdateScreenMetrics();
            RebuildTargets();
        }
    }

    // The scene render targets are sized from PresentationParameters, so they must be recreated
    // whenever the backbuffer size changes - including when FNA resized it on its own.
    private void RebuildTargets()
    {
        Lighting.Initialize();
        if (!Main.drawToScreen && !Main._isResizingAndRemakingTargets)
        {
            Main._isResizingAndRemakingTargets = true;
            Main.instance.InitTargets();
            Main._isResizingAndRemakingTargets = false;
        }
    }

    private void UpdateScreenMetrics()
    {
        // set the Main.screenWidth and Main.screenHeight to rounded down even number
        // to make Terraria happy
        // so tiles, camera and player sprites won't jitter like Kobe Bryant fking his own helicopter
        Main.screenWidth = ToEven(width);
        Main.screenHeight = ToEven(height);

        // FNA's Mouse.GetState already converts window (logical) coordinates to backbuffer
        // (physical) coordinates using the window/backbuffer ratio, so the game's mouse position is
        // already in screen space and must not be scaled again here.
        PlayerInput.RawMouseScale = Vector2.One;

        Main.TryPickingDefaultUIScale(Main.screenHeight);
        PlayerInput.CacheOriginalScreenDimensions();
        Main.FixUIScale();
        Main.PendingResolutionWidth = Main.screenWidth;
        Main.PendingResolutionHeight = Main.screenHeight;
        UserInterface.ActiveInstance.Recalculate();
        Main.instance._needsMenuUIRecalculation = true;
    }

    private void SetPreferredToDesktopMode()
    {
        unsafe
        {
            IntPtr modePointer = SDL.SDL_GetDesktopDisplayMode(SDL.SDL_GetDisplayForWindow(WindowHandle));
            if (modePointer != IntPtr.Zero)
            {
                SDL.SDL_DisplayMode* mode = (SDL.SDL_DisplayMode*)(void*)modePointer;
                if (mode->w > 0 && mode->h > 0)
                {
                    Graphics!.PreferredBackBufferWidth = (int)Math.Round(mode->w * mode->pixel_density);
                    Graphics.PreferredBackBufferHeight = (int)Math.Round(mode->h * mode->pixel_density);
                }
            }
        }
    }

    public void ApplyGraphics()
    {
        Main.graphics.ApplyChanges();
    }

    public void CenterWindow()
    {
        centerWindow = true;
    }

    private static Lock FileLock = new();

    public Task WriteToFile()
    {
        lock (FileLock)
        {
            string s = JsonConvert.SerializeObject(this);
            File.WriteAllText(ClientLoader.WindowConfigPath, s);
        }

        return Task.CompletedTask;
    }

    private static int ToEven(int value) => value & ~1;
}
