using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Microsoft.Xna.Framework.Color;

namespace TerraAngel.WorldEdits;

public class WorldEditPixelArt : WorldEdit
{
    public override bool RunEveryFrame => false;

    public string SelectedPath = string.Empty;

    private string? _pendingPath;
    private Color[,]? _imageData;
    private Texture2D? _selectedTexture;
    private IntPtr _selectedTextureId = IntPtr.Zero;

    public override bool DrawUITab(ImGuiIOPtr io)
    {
        if (ImGui.BeginTabItem(GetString("Pixel Art")))
        {
            CheckAndLoadImage();

            ImGuiUtil.HelpMarkerTopRight(GetString("Press middle mouse button to paste"));

            ImGui.Text(GetString("Still under construction...."));

            if (ImGui.Button(GetString("Select Image")))
            {
                OpenFile(l =>
                {
                    SelectedPath = l.FirstOrDefault(string.Empty);
                    if (!string.IsNullOrEmpty(SelectedPath))
                        _pendingPath = SelectedPath;
                });
            }

            ImGui.Text(GetString($"Selected File: {SelectedPath}"));

            if (_selectedTextureId != IntPtr.Zero && _selectedTexture != null && _imageData != null)
            {
                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                var width = contentAvail.X;
                var height = _imageData.GetLength(1) * width / _imageData.GetLength(0);
                ImGui.Image(_selectedTextureId, new Vector2(width, height));
            }

            ImGui.EndTabItem();
            return true;
        }
        return false;
    }

    public override void DrawPreviewInWorld(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
    }

    public override void DrawPreviewInMap(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
    }

    public override void Edit(Vector2 cursorTilePosition)
    {
    }

    private void CheckAndLoadImage()
    {
        if (_pendingPath == null)
            return;

        if (!File.Exists(_pendingPath))
        {
            _pendingPath = null;
            return;
        }

        var pendingPathCopy = _pendingPath;
        _pendingPath = null;

        // Disposing... Cleaning up...
        if (_selectedTextureId != IntPtr.Zero)
        {
            ClientLoader.MainRenderer!.UnbindTexture(_selectedTextureId);
        }

        if (_selectedTexture != null)
        {
            _selectedTexture.Dispose();
            _selectedTexture = null;
        }

        _imageData = null;

        // Load!
        Color[,]? imageData = null;
        Texture2D? texture = null;
        IntPtr textureId = IntPtr.Zero;

        try
        {
            var image = Image.Load<Rgba32>(pendingPathCopy);
            imageData = new Color[image.Width, image.Height];
            var textureData = new Color[image.Width * image.Height];
            image.ProcessPixelRows(a =>
            {
                for (var y = 0; y < a.Height; y++)
                {
                    var row = a.GetRowSpan(y);

                    for (var x = 0; x < a.Width; x++)
                    {
                        ref var pixel = ref row[x];
                        // ReSharper disable once AccessToModifiedClosure
                        imageData[x, y] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
                        // ReSharper disable once AccessToModifiedClosure
                        textureData[y * a.Width + x] = imageData[x, y];
                    }
                }
            });

            texture = new Texture2D(Main.graphics.GraphicsDevice, image.Width, image.Height);
            texture.SetData(textureData);

            textureId = ClientLoader.MainRenderer!.BindTexture(texture);
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error loading image: {ex}"));
            if (textureId != IntPtr.Zero)
            {
                ClientLoader.MainRenderer!.UnbindTexture(textureId);
                textureId = IntPtr.Zero;
            }
            texture?.Dispose();
            texture = null;
            imageData = null;
        }

        _imageData = imageData;
        _selectedTexture = texture;
        _selectedTextureId = textureId;
    }

    #region SDL3 File Selection Dialog
    // TODO: move this to a dedicated util class?

    private static Action<List<string>>? _openFileManagedCallback;

    private static void OpenFile(Action<List<string>> callback)
    {
        _openFileManagedCallback = callback;
        SDL.SDL_ShowOpenFileDialog( 
            OpenFileNativeCallback,
            IntPtr.Zero,
            Main.instance.Window.Handle,
            null,
            0,
            null,
            false);
    }

    private static unsafe void OpenFileNativeCallback(IntPtr userdata, IntPtr fileList, int filter)
    {
        var callback = _openFileManagedCallback;
        _openFileManagedCallback = null;

        var fileListPtr = (byte**)fileList;
        if (fileListPtr == null)
        {
            ClientLoader.Console.WriteError(GetString($"SDL ShowOpenFileDialog Err: {SDL.SDL_GetError()}"));
            return;
        }

        if (*fileListPtr == null)
        {
            return;
        }

        List<string> selected = [];

        for (var p = fileListPtr; *p != null; p++)
        {
            selected.Add(Marshal.PtrToStringUTF8(new IntPtr(*p)) ?? string.Empty);
        }
        callback?.Invoke(selected);
    }

    #endregion
}