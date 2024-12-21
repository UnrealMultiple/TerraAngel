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

    public Stack<ImGuiEffect> ImGuiEffectStack = new Stack<ImGuiEffect>();

    // Textures
    public Dictionary<nint, Texture2D> LoadedTextures;
    private nint TextureId = 0;
    public nint? FontTextureId;

    private RasterizerState RasterizerState;

    private VertexBuffer? VertexBuffer;
    private byte[]? VertexData;
    private int VertexBufferSize;

    private IndexBuffer? IndexBuffer;
    private byte[]? IndexData;
    private int IndexBufferSize;

    // Input
    private int ScrollWheelValue;
    private List<int> KeyRemappings = new List<int>();
    private Keys[] AllKeys = Enum.GetValues<Keys>();

    private Queue<Action> PreDrawActionQueue = new Queue<Action>(5);

    public ImGuiRenderer(Game game)
    {
        ImGui.CreateContext();

        TargetGame = game ?? throw new ArgumentNullException(nameof(game));
        GraphicsDevice = game.GraphicsDevice;

        LoadedTextures = new Dictionary<IntPtr, Texture2D>
        {
            { IntPtr.Zero, null } // bind null texture to id 0
        };
        TextureId = 1;

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

        SetupGraphics();

        SetupInput();

        SetupFonts();
    }

    protected virtual void SetupGraphics()
    {

    }

    protected virtual void SetupInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();
    }

    protected virtual void SetupFonts()
    {
        ImGui.GetIO().Fonts.AddFontDefault();
        ClientAssets.LoadFonts(ImGui.GetIO());
    }

    public nint BindTexture(Texture2D texture)
    {
        nint id = TextureId++;

        LoadedTextures.Add(id, texture);

        return id;
    }

    public void UnbindTexture(nint textureId)
    {
        LoadedTextures.Remove(textureId);
    }

    public void PreRender()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DeltaTime = Time.DrawDeltaTime;
        UpdateInput();

        while (PreDrawActionQueue.Count > 0)
            PreDrawActionQueue.Dequeue()?.Invoke();

        ImGuiEffectStack.Peek().Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        ImGui.NewFrame();
    }

    public void PostRender()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    public unsafe void RebuildFontAtlas()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        if (!io.Fonts.Build()) throw new InvalidOperationException(GetString("Failed to build font"));

        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        if (pixelData == null) throw new NullReferenceException(GetString($"Failed to get font data '{nameof(pixelData)}' was null"));

        Texture2D tex2d = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);

        tex2d.SetDataPointerEXT(0, null, (IntPtr)pixelData, width * height * bytesPerPixel);

        if (FontTextureId.HasValue) UnbindTexture(FontTextureId.Value);

        FontTextureId = BindTexture(tex2d);

        io.Fonts.SetTexID(FontTextureId.Value);

        io.Fonts.ClearTexData();
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
        
        foreach (var key in AllKeys)
        {
            if (TryMapKeys(key, out ImGuiKey imguikey))
            {
                io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
            }
        }

        io.KeyShift = (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)) && TargetGame.IsActive;
        io.KeyCtrl = (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)) && TargetGame.IsActive;
        io.KeyAlt = (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)) && TargetGame.IsActive;
        io.KeySuper = (keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows)) && TargetGame.IsActive;

        io.DisplaySize = new Vector2(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);

        io.MousePos = new Vector2(mouse.X, mouse.Y);

        io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed && TargetGame.IsActive;
        io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed && TargetGame.IsActive;
        io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed && TargetGame.IsActive;

        int scrollDelta = mouse.ScrollWheelValue - ScrollWheelValue;
        io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;

        ScrollWheelValue = mouse.ScrollWheelValue;
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
                >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
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

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        Viewport lastViewport = GraphicsDevice.Viewport;
        Rectangle lastScissorBox = GraphicsDevice.ScissorRectangle;
        SamplerState lastSamplerState = GraphicsDevice.SamplerStates[0];
        BlendState lastBlendState = GraphicsDevice.BlendState;
        RasterizerState lastRasterizerState = GraphicsDevice.RasterizerState;

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

        // idk feels like it could be opimtized -chair
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

    public delegate void UserCallback(bool isPost);

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
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[j];

                if (!LoadedTextures.TryGetValue(drawCmd.TextureId, out Texture2D? cmdTexture))
                {
                    cmdTexture = GraphicsUtility.MissingTexture;
                }

                GraphicsDevice.ScissorRectangle = new Rectangle(
                    (int)drawCmd.ClipRect.X,
                    (int)drawCmd.ClipRect.Y,
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                UserCallback? userCallback = null;

                if (drawCmd.UserCallback != -1 && drawCmd.UserCallback != 0)
                {
                    if (drawCmd.UserCallbackData == drawCmd.TextureId)
                    {
                        userCallback = Marshal.GetDelegateForFunctionPointer<UserCallback>(drawCmd.UserCallback);
                        userCallback?.Invoke(false);
                    }
                }

                SetEffectTexture(cmdTexture);

                pass.Apply();

                GraphicsDevice.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    baseVertex: vtxOffset,
                    minVertexIndex: 0,
                    numVertices: cmdList.VtxBuffer.Size,
                    startIndex: idxOffset,
                    primitiveCount: (int)drawCmd.ElemCount / 3
                );

                userCallback?.Invoke(true);

                idxOffset += (int)drawCmd.ElemCount;
            }

            vtxOffset += cmdList.VtxBuffer.Size;
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
}