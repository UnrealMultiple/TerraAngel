using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Input;

namespace TerraAngel.Graphics;

public class ImGuiRenderer
{
    private readonly Game TargetGame;

    // Graphics
    public GraphicsDevice GraphicsDevice;

    public Stack<ImGuiEffect> ImGuiEffectStack = new();

    // Textures
    public Dictionary<ImTextureID, Texture2D> LoadedTextures;
    private int NextTextureId = 1;

    private RasterizerState RasterizerState;

    private VertexBuffer? VertexBuffer;
    private byte[]? VertexData;
    private int VertexBufferSize;

    private IndexBuffer? IndexBuffer;
    private byte[]? IndexData;
    private int IndexBufferSize;

    private byte[]? TextureUpdateBuffer;

    // Input
    private int ScrollWheelValue;
    private Keys[] AllKeys = Enum.GetValues<Keys>();

    private Queue<Action> PreDrawActionQueue = new(5);

    public ImGuiRenderer(Game game)
    {
        TargetGame = game ?? throw new ArgumentNullException(nameof(game));
        GraphicsDevice = game.GraphicsDevice;

        ImGuiContextPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        LoadedTextures = new Dictionary<ImTextureID, Texture2D>();

        RasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0
        };

        ImGuiEffectStack.Push(new ImGuiEffect(GraphicsDevice, Path.Combine(ClientLoader.AssetPath, "ImGuiShader.xnb")));

        SetupBackendCapabilities();
        SetupGraphics();
        SetupInput();
        SetupFonts();
    }

    protected virtual void SetupBackendCapabilities()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;

        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
        platformIO.RendererTextureMaxWidth = 4096;
        platformIO.RendererTextureMaxHeight = 4096;
    }

    protected virtual void SetupGraphics()
    {

    }

    protected virtual void SetupInput()
    {
    }

    protected virtual unsafe void SetupFonts()
    {
        ImGui.GetIO().Fonts.AddFontDefault();
        ClientAssets.LoadFonts();
    }

    public unsafe ImTextureRef BindTexture(Texture2D texture)
    {
        ImTextureID id = new ImTextureID((ulong)NextTextureId++);

        LoadedTextures.Add(id, texture);

        return new ImTextureRef(null, id);
    }

    public void UnbindTexture(ImTextureRef textureRef)
    {
        if (textureRef.TexID.IsNull)
            return;

        LoadedTextures.Remove(textureRef.TexID);
    }

    public void PreRender()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DeltaTime = Time.DrawDeltaTime;
        io.DisplaySize = new Vector2(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);
        UpdateInput();

        while (PreDrawActionQueue.Count > 0)
            PreDrawActionQueue.Dequeue()?.Invoke();

        ImGuiEffectStack.Peek().Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        ImGui.NewFrame();
    }

    public void PostRender()
    {
        ImGui.Render();
        ImDrawDataPtr drawData = ImGui.GetDrawData();
        ProcessTextureUpdates(drawData);
        RenderDrawData(drawData);
    }

    public void PushEffect(ImGuiEffect newEffect)
    {
        ImGuiEffectStack.Push(newEffect);
    }

    public void PopEffect()
    {
        ImGuiEffectStack.Pop();
    }

    protected void SetEffectTexture(Texture2D texture)
    {
        ImGuiEffectStack.Peek().Texture = texture;
    }

    protected void UpdateInput()
    {
        if (!TargetGame.IsActive)
            return;

        ImGuiIOPtr io = ImGui.GetIO();

        MouseState mouse = Mouse.GetState();
        KeyboardState keyboard = Keyboard.GetState();

        io.AddMousePosEvent(mouse.X, mouse.Y);

        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed && TargetGame.IsActive);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed && TargetGame.IsActive);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed && TargetGame.IsActive);
        io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed && TargetGame.IsActive);
        io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed && TargetGame.IsActive);

        int scrollDelta = mouse.ScrollWheelValue - ScrollWheelValue;
        io.AddMouseWheelEvent(0f, scrollDelta > 0 ? 1f : scrollDelta < 0 ? -1f : 0f);

        ScrollWheelValue = mouse.ScrollWheelValue;

        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

        foreach (var key in AllKeys)
        {
            if (TryMapKeys(key, out ImGuiKey imguikey))
            {
                io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
            }
        }
    }

    private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
    {
        if (key == Keys.None)
        {
            imguikey = ImGuiKey.None;
            return true;
        }

        imguikey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey.Key0 + (key - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F24 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift => ImGuiKey.ModShift,
            Keys.LeftControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt => ImGuiKey.ModAlt,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            Keys.BrowserBack => ImGuiKey.AppBack,
            Keys.BrowserForward => ImGuiKey.AppForward,
            _ => ImGuiKey.None,
        };

        return imguikey != ImGuiKey.None;
    }

    private unsafe void ProcessTextureUpdates(ImDrawDataPtr drawData)
    {
        ImVector<ImTextureDataPtr> textures = drawData.Textures;
        if (textures.Data == null)
            return;

        for (int i = 0; i < textures.Size; i++)
            UpdateTexture(textures.Data[i]);
    }

    private unsafe void UpdateTexture(ImTextureDataPtr textureData)
    {
        switch (textureData.Status)
        {
            case ImTextureStatus.WantCreate:
                CreateTexture(textureData);
                break;

            case ImTextureStatus.WantUpdates:
                UpdateTextureData(textureData);
                break;

            case ImTextureStatus.WantDestroy:
                DestroyTexture(textureData);
                break;
        }
    }

    private unsafe void CreateTexture(ImTextureDataPtr textureData)
    {
        SurfaceFormat format = textureData.Format == ImTextureFormat.Rgba32 ? SurfaceFormat.Color : SurfaceFormat.Alpha8;
        Texture2D texture = new Texture2D(GraphicsDevice, textureData.Width, textureData.Height, false, format);

        UploadTexturePixels(textureData, texture);

        ImTextureID id = new ImTextureID((ulong)NextTextureId++);
        textureData.SetTexID(id);
        LoadedTextures.Add(id, texture);
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    private unsafe void UpdateTextureData(ImTextureDataPtr textureData)
    {
        if (!LoadedTextures.TryGetValue(textureData.GetTexID(), out Texture2D? texture))
        {
            textureData.SetStatus(ImTextureStatus.Ok);
            return;
        }

        SurfaceFormat format = textureData.Format == ImTextureFormat.Rgba32 ? SurfaceFormat.Color : SurfaceFormat.Alpha8;
        if (texture.Width != textureData.Width || texture.Height != textureData.Height || texture.Format != format)
        {
            texture.Dispose();
            texture = new Texture2D(GraphicsDevice, textureData.Width, textureData.Height, false, format);
            LoadedTextures[textureData.GetTexID()] = texture;
            UploadTexturePixels(textureData, texture);
        }
        else if (textureData.Updates.Size > 0)
        {
            UploadTextureUpdates(textureData, texture);
        }
        else
        {
            UploadTexturePixels(textureData, texture);
        }

        textureData.SetStatus(ImTextureStatus.Ok);
    }

    private void DestroyTexture(ImTextureDataPtr textureData)
    {
        if (LoadedTextures.Remove(textureData.GetTexID(), out Texture2D? texture))
            texture?.Dispose();

        textureData.SetTexID(ImTextureID.Null);
        textureData.SetStatus(ImTextureStatus.Destroyed);
    }

    private unsafe void UploadTexturePixels(ImTextureDataPtr textureData, Texture2D texture)
    {
        if (textureData.Pixels == null)
            return;

        texture.SetDataPointerEXT(0, null, (IntPtr)textureData.Pixels, textureData.Width * textureData.Height * textureData.BytesPerPixel);
    }

    private unsafe void UploadTextureUpdates(ImTextureDataPtr textureData, Texture2D texture)
    {
        int bytesPerPixel = textureData.BytesPerPixel;
        int pitch = textureData.GetPitch();

        ImVector<ImTextureRect> updates = textureData.Updates;
        for (int i = 0; i < updates.Size; i++)
        {
            ImTextureRect rect = updates[i];
            int width = rect.W;
            int height = rect.H;
            int rowBytes = width * bytesPerPixel;
            int dataSize = rowBytes * height;

            if (TextureUpdateBuffer is null || TextureUpdateBuffer.Length < dataSize)
                TextureUpdateBuffer = new byte[dataSize];

            byte* src = (byte*)textureData.GetPixelsAt(rect.X, rect.Y);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy((IntPtr)(src + y * pitch), TextureUpdateBuffer, y * rowBytes, rowBytes);
            }

            fixed (byte* dst = TextureUpdateBuffer)
            {
                texture.SetDataPointerEXT(0, new Rectangle(rect.X, rect.Y, width, height), (IntPtr)dst, dataSize);
            }
        }
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
            return;

        Viewport lastViewport = GraphicsDevice.Viewport;
        Rectangle lastScissorBox = GraphicsDevice.ScissorRectangle;
        SamplerState lastSamplerState = GraphicsDevice.SamplerStates[0];
        BlendState lastBlendState = GraphicsDevice.BlendState;
        RasterizerState lastRasterizerState = GraphicsDevice.RasterizerState;
        DepthStencilState lastDepthStencilState = GraphicsDevice.DepthStencilState;
        Color lastBlendFactor = GraphicsDevice.BlendFactor;

        GraphicsDevice.BlendFactor = Color.White;
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;
        GraphicsDevice.RasterizerState = RasterizerState;
        GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);

        GraphicsDevice.Viewport = lastViewport;
        GraphicsDevice.ScissorRectangle = lastScissorBox;
        GraphicsDevice.SamplerStates[0] = lastSamplerState;
        GraphicsDevice.BlendState = lastBlendState;
        GraphicsDevice.RasterizerState = lastRasterizerState;
        GraphicsDevice.DepthStencilState = lastDepthStencilState;
        GraphicsDevice.BlendFactor = lastBlendFactor;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        if (drawData.TotalVtxCount > VertexBufferSize)
        {
            VertexBuffer?.Dispose();

            VertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            VertexBuffer = new VertexBuffer(GraphicsDevice, DrawVertDeclaration.Declaration, VertexBufferSize, BufferUsage.None);
            VertexData = new byte[VertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > IndexBufferSize)
        {
            IndexBuffer?.Dispose();

            IndexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, IndexBufferSize, BufferUsage.None);
            IndexData = new byte[IndexBufferSize * sizeof(ushort)];
        }

        int vtxOffset = 0;
        int idxOffset = 0;

        Span<byte> vertexDataSpan = new Span<byte>(VertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        Span<byte> indexDataSpan = new Span<byte>(IndexData, 0, drawData.TotalIdxCount * sizeof(ushort));

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            fixed (void* vtxDstPtr = &vertexDataSpan[vtxOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &indexDataSpan[idxOffset * sizeof(ushort)])
            {
                Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, vertexDataSpan.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, indexDataSpan.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        VertexBuffer!.SetData(VertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        IndexBuffer!.SetData(IndexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        GraphicsDevice.SetVertexBuffer(VertexBuffer);
        GraphicsDevice.Indices = IndexBuffer;
        EffectPass pass = ImGuiEffectStack.Peek().CurrentTechnique.Passes[0];

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
            {
                ImDrawCmdPtr drawCmd = &cmdList.CmdBuffer.Data[j];

                if (drawCmd.ElemCount == 0)
                    continue;

                if (!LoadedTextures.TryGetValue(drawCmd.GetTexID(), out Texture2D? cmdTexture))
                {
                    cmdTexture = GraphicsUtility.MissingTexture;
                }

                GraphicsDevice.ScissorRectangle = new Rectangle(
                    (int)drawCmd.ClipRect.X,
                    (int)drawCmd.ClipRect.Y,
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                SetEffectTexture(cmdTexture);

                pass.Apply();

                GraphicsDevice.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    baseVertex: (int)(vtxOffset + drawCmd.VtxOffset),
                    minVertexIndex: 0,
                    numVertices: cmdList.VtxBuffer.Size,
                    startIndex: (int)(idxOffset + drawCmd.IdxOffset),
                    primitiveCount: (int)drawCmd.ElemCount / 3
                );
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    public void EnqueuePreDrawAction(Action action)
    {
        PreDrawActionQueue.Enqueue(action);
    }

    public static class DrawVertDeclaration
    {
        public static readonly VertexDeclaration Declaration;

        public static readonly int Size;

        static DrawVertDeclaration()
        {
            unsafe { Size = sizeof(ImDrawVert); }

            Declaration = new VertexDeclaration(
                Size,

                // Position
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),

                // UV
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),

                // Color
                new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    protected unsafe delegate byte* GetClipboardTextFn(void* userData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    protected unsafe delegate void SetClipboardTextFn(void* userData, byte* text);
}
