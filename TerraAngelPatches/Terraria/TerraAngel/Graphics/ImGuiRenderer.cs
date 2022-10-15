﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace TerraAngel.Graphics
{
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
    public class TerraImGuiRenderer
    {
        private Game _game;

        // Graphics
        public GraphicsDevice GraphicsDevice;
        public BasicEffect ImGuiShader;
        public IntPtr? FontTextureId;
        public Dictionary<IntPtr, Texture2D> LoadedTextures;

        private RasterizerState RasterizerState;
        private long TextureId = 0;

        private VertexBuffer VertexBuffer;
        private byte[] VertexData;
        private int VertexBufferSize;

        private IndexBuffer IndexBuffer;
        private byte[] IndexData;
        private int IndexBufferSize;

        private int ScrollWheelValue;
        private List<int> keyRemappings = new List<int>();

        private DateTime lastTime = DateTime.UtcNow;

        private Queue<Action> preDrawActionQueue = new Queue<Action>(5);

        public TerraImGuiRenderer(Game game)
        {
            SetupContext();
            SetupGraphics(game);
            SetupInput();
        }

        protected virtual void SetupContext()
        {
            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
        }
        protected virtual void SetupGraphics(Game game)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
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

            ImGuiShader = new BasicEffect(GraphicsDevice);
            ImGuiShader.TextureEnabled = true;
            ImGuiShader.VertexColorEnabled = true;
        }
        protected virtual void SetupInput()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Tab] =        (int)Keys.Tab);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.LeftArrow] =  (int)Keys.Left);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.UpArrow] =    (int)Keys.Up);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.DownArrow] =  (int)Keys.Down);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.PageUp] =     (int)Keys.PageUp);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.PageDown] =   (int)Keys.PageDown);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Home] =       (int)Keys.Home);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.End] =        (int)Keys.End);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Delete] =     (int)Keys.Delete);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Backspace] =  (int)Keys.Back);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Enter] =      (int)Keys.Enter);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Escape] =     (int)Keys.Escape);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Space] =      (int)Keys.Space);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.A] =          (int)Keys.A);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.C] =          (int)Keys.C);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.V] =          (int)Keys.V);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.X] =          (int)Keys.X);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Y] =          (int)Keys.Y);
            keyRemappings.Add(io.KeyMap[(int)ImGuiKey.Z] =          (int)Keys.Z);

            io.Fonts.AddFontDefault();
            Client.ClientAssets.LoadFonts(ImGui.GetIO());
        }

        public virtual IntPtr BindTexture(Texture2D texture)
        {
            IntPtr id = new IntPtr(TextureId++);

            LoadedTextures.Add(id, texture);

            return id;
        }
        public virtual void UnbindTexture(IntPtr textureId)
        {
            LoadedTextures.Remove(textureId);
        }

        public virtual void BeforeLayout(GameTime gameTime)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DeltaTime = (float)(DateTime.UtcNow - lastTime).TotalSeconds;
            lastTime = DateTime.UtcNow;
            UpdateInput(gameTime);

            while (preDrawActionQueue.Count > 0)
                preDrawActionQueue.Dequeue()?.Invoke();

            ImGuiShader.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            ImGui.NewFrame();
        }
        public virtual void AfterLayout()
        {
            ImGui.Render();
            unsafe { RenderDrawData(ImGui.GetDrawData()); }
        }

        public virtual unsafe void RebuildFontAtlas()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (!io.Fonts.Build()) throw new InvalidOperationException("Failed to build font");
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            if (pixelData == null) throw new NullReferenceException($"Failed to get font data '{nameof(pixelData)}' was null");

            Texture2D tex2d = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);
            tex2d.SetDataPointerEXT(0, null, (IntPtr)pixelData, width * height * bytesPerPixel);

            if (FontTextureId.HasValue) UnbindTexture(FontTextureId.Value);

            FontTextureId = BindTexture(tex2d);

            io.Fonts.SetTexID(FontTextureId.Value);
            io.Fonts.ClearTexData();
        }
        protected virtual void SetEffectTexture(Texture2D texture)
        {
            ImGuiShader.Texture = texture;
        }
        protected virtual void UpdateInput(GameTime gameTime)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            for (int i = 0; i < 256; i++)
            {
                io.KeysDown[i] = keyboard.IsKeyDown((Keys)i) && _game.IsActive;
            }

            for (int i = 0; i < keyRemappings.Count; i++)
            {
                io.KeysDown[keyRemappings[i]] = keyboard.IsKeyDown((Keys)keyRemappings[i]) && _game.IsActive;
            }


            io.KeyShift = (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)) && _game.IsActive;
            io.KeyCtrl = (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)) && _game.IsActive;
            io.KeyAlt = (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)) && _game.IsActive;
            io.KeySuper = (keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows)) && _game.IsActive;

            io.DisplaySize = new Vector2(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new Vector2(1f, 1f);

            io.MousePos = new Vector2(mouse.X, mouse.Y);

            io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed && _game.IsActive;
            io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed && _game.IsActive;
            io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed && _game.IsActive;
            int scrollDelta = mouse.ScrollWheelValue - ScrollWheelValue;
            io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
            ScrollWheelValue = mouse.ScrollWheelValue;
        }

        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            Viewport lastViewport = GraphicsDevice.Viewport;
            Rectangle lastScissorBox = GraphicsDevice.ScissorRectangle;
            SamplerState lastSamplerState = GraphicsDevice.SamplerStates[0];
            BlendState lastBlendState = GraphicsDevice.BlendState;
            RasterizerState lastRasterizerState = GraphicsDevice.RasterizerState;
            Viewport lastViewPort = GraphicsDevice.Viewport;

            GraphicsDevice.BlendFactor = Color.White;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicWrap;
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
            GraphicsDevice.Viewport = lastViewPort;
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

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[i];

                fixed (void* vtxDstPtr = &VertexData[vtxOffset * DrawVertDeclaration.Size])
                fixed (void* idxDstPtr = &IndexData[idxOffset * sizeof(ushort)])
                {
                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, VertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, IndexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }
            
            VertexBuffer.SetData(VertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
            IndexBuffer.SetData(IndexData, 0, drawData.TotalIdxCount * sizeof(ushort));
        }
        private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
        {
            GraphicsDevice.SetVertexBuffer(VertexBuffer);
            GraphicsDevice.Indices = IndexBuffer;
            EffectPass pass = ImGuiShader.CurrentTechnique.Passes[0];


            int vtxOffset = 0;
            int idxOffset = 0;

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[i];

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

                    idxOffset += (int)drawCmd.ElemCount;
                }

                vtxOffset += cmdList.VtxBuffer.Size;
            }
        }

        public void EnqueuePreDrawAction(Action action)
        {
            preDrawActionQueue.Enqueue(action);
        }
    }
}
