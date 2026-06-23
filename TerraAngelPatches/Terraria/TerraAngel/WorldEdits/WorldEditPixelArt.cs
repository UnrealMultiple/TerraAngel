using SDL3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace TerraAngel.WorldEdits;

public class WorldEditPixelArt : WorldEdit
{
    public override bool RunEveryFrame => false;

    public TileSectionRenderer Renderer = new();
    public TileSection? CopiedSection;

    private string? _pendingPath;
    public string SelectedPath = string.Empty;
    private Image<Rgba32>? _image;
    private Texture2DContainer _textureContainer;

    private bool _enableDithering = true;
    private ColorMatchAlgorithm _algorithm = ColorMatchAlgorithm.RgbDistance;
    private readonly Dictionary<Color, TileColor> _colorCache = [];

    private int _targetWidth;
    private int _targetHeight;
    private float _targetRotation;

    private bool IsImageActiveAndBound => !string.IsNullOrEmpty(SelectedPath) && _image is not null && _textureContainer.IsActive;

    #region Color Matching

    public enum ColorMatchAlgorithm { LabDeltaE, RgbDistance }

    public TileColor FindClosest(Color target)
    {
        if (_colorCache.TryGetValue(target, out var cached))
            return cached;

        TileColor closest = _algorithm == ColorMatchAlgorithm.RgbDistance
            ? FindClosestRgb(target)
            : FindClosestLab(target);

        return _colorCache[target] = closest;
    }

    private static TileColor FindClosestLab(Color target)
    {
        double[] targetLab = ColorUtil.RgbToLab(target);
        var colors = TileColorData.Colors;

        TileColor closest = colors[0];
        double minDistance = ColorUtil.DeltaE(targetLab, colors[0].GetLab());

        for (int i = 1; i < colors.Length; i++)
        {
            double distance = ColorUtil.DeltaE(targetLab, colors[i].GetLab());
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = colors[i];
            }
        }
        return closest;
    }

    private static TileColor FindClosestRgb(Color target)
    {
        var colors = TileColorData.Colors;
        TileColor closest = colors[0];
        double minDistance = ColorUtil.RgbDistance(target, colors[0].Color);

        for (int i = 1; i < colors.Length; i++)
        {
            double distance = ColorUtil.RgbDistance(target, colors[i].Color);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = colors[i];
            }
        }
        return closest;
    }

    #endregion

    #region Color Helpers

    private static Color Rgba32ToColor(Rgba32 rgba32) => new() { PackedValue = rgba32.PackedValue };

    #endregion

    public override bool DrawUITab(ImGuiIOPtr io)
    {
        if (!ImGui.BeginTabItem(GetString("Pixel Art")))
            return false;

        CheckPendingImageAndLoad();

        DrawControls();
        DrawImagePreview();

        ImGui.EndTabItem();
        return true;
    }

    private void DrawControls()
    {
        ImGuiUtil.HelpMarkerTopRight(GetString("Press middle mouse button to paste"));
        ImGui.Text(GetString("Multiplayer pasting is still under construction...."));

        if (ImGui.Button(GetString("Select Image")))
            OpenFileDialog();
        ImGui.SameLine();
        if (ImGui.Button(GetString("Clear Selection")))
            UnloadResources();

        string[] algorithms = [GetString("Lab DeltaE (Accurate)"), GetString("RGB Distance (Fast)")];
        int currentAlgorithm = (int)_algorithm;
        if (ImGui.Combo(GetString("Color Match Algorithm"), ref currentAlgorithm, algorithms, algorithms.Length))
        {
            _algorithm = (ColorMatchAlgorithm)currentAlgorithm;
            InvalidateCopiedSection();
            InvalidateColorCache();
        }

        if (ImGui.Checkbox(GetString("Enable Dithering"), ref _enableDithering))
            InvalidateCopiedSection();

        if (!IsImageActiveAndBound)
            return;

        ImGui.Text($"Original: {_image!.Width}x{_image!.Height}");
        ImGui.Text($"Target: {_targetWidth}x{_targetHeight}");

        int newWidth = _targetWidth;
        int newHeight = _targetHeight;
        if (ImGui.InputInt(GetString("Width"), ref newWidth) && newWidth > 0)
        {
            // reset other mutate fields
            _targetRotation = 0f;

            _targetWidth = newWidth;
            ResizePreviewImage();
        }
        if (ImGui.InputInt(GetString("Height"), ref newHeight) && newHeight > 0)
        {
            // reset other mutate fields
            _targetRotation = 0f;

            _targetHeight = newHeight;
            ResizePreviewImage();
        }

        if (ImGui.Button(GetString("Apply Resize")))
        {
            // reset other mutate fields
            _targetRotation = 0f;

            ResizeRealImage();
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;
        }

        ImGui.Separator();
        ImGui.Text(GetString("Rotation"));

        float rotation = _targetRotation;
        if (ImGui.SliderFloat(GetString("Degrees"), ref rotation, 0f, 360f))
        {
            // reset other mutate fields
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;

            _targetRotation = rotation;
            RotatePreviewImage();
        }

        if (ImGui.Button(GetString("Rotate 90°")))
        {
            // reset other mutate fields
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;

            _targetRotation = (_targetRotation + 90f) % 360f;
            RotatePreviewImage();
        }
        ImGui.SameLine();
        if (ImGui.Button(GetString("Rotate -90°")))
        {
            // reset other mutate fields
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;

            _targetRotation = (_targetRotation - 90f + 360f) % 360f;
            RotatePreviewImage();
        }
        ImGui.SameLine();
        if (ImGui.Button(GetString("Reset")))
        {
            // reset other mutate fields
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;

            _targetRotation = 0f;
            RotatePreviewImage();
        }

        if (ImGui.Button(GetString("Apply Rotation")))
        {
            // reset other mutate fields
            _targetWidth = _image!.Width;
            _targetHeight = _image!.Height;

            RotateRealImage();
            _targetRotation = 0f;
        }

        ImGui.Text(GetString($"File: {SelectedPath}"));
    }

    private void DrawImagePreview()
    {
        if (!IsImageActiveAndBound)
            return;

        var contentAvail = ImGui.GetContentRegionAvail();
        var height = _textureContainer.Texture!.Height * contentAvail.X / _textureContainer.Texture.Width;
        ImGui.Image(_textureContainer.TextureBindId, contentAvail with { Y = height });
    }

    public override void DrawPreviewInWorld(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        UpdateCopiedSection();
        if (CopiedSection == null)
            return;

        Vector2 tileMouse = (Util.ScreenToWorldDynamic(InputSystem.MousePosition) / 16f).Floor();
        Renderer.DrawDetailed(CopiedSection, Util.WorldToScreenWorld(tileMouse * 16f), Vector2.Zero, io.DisplaySize, false);
        DrawSelectionRect(drawList, tileMouse, false);
    }

    public override void DrawPreviewInMap(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        UpdateCopiedSection();
        if (CopiedSection == null)
            return;

        Vector2 tileMouse = (Util.ScreenToWorldDynamic(InputSystem.MousePosition) / 16f).Floor();
        Renderer.DrawPrimitiveMap(CopiedSection, tileMouse * 16f, Vector2.Zero, io.DisplaySize, false);
        DrawSelectionRect(drawList, tileMouse, true);
    }

    private void UpdateCopiedSection()
    {
        if (!IsImageActiveAndBound || CopiedSection != null)
            return;

        CopiedSection = new TileSection(_image!.Width, _image!.Height);

        if (_enableDithering)
            ApplyFloydSteinbergDithering();
        else
            ProcessPixelsWithoutDithering();
    }

    private void ProcessPixelsWithoutDithering()
    {
        for (var y = 0; y < _image!.Height && y < CopiedSection!.Height; y++)
        for (var x = 0; x < _image.Width && x < CopiedSection.Width; x++)
        {
            var pixelColor = Rgba32ToColor(_image![x, y]);
            if (pixelColor.A < 128)
                continue;
            SetTileData(x, y, FindClosest(pixelColor));
        }
    }

    private void ApplyFloydSteinbergDithering()
    {
        var width = Math.Min(_image!.Width, CopiedSection!.Width);
        var height = Math.Min(_image.Height, CopiedSection.Height);

        // TODO: can we rent this from ArrayPool?
        var errorBuffer = new double[width, height, 3];

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            Color originalColor = Rgba32ToColor(_image![x, y]);
            if (originalColor.A < 128)
                continue;

            double r = Math.Clamp(originalColor.R + errorBuffer[x, y, 0], 0, 255);
            double g = Math.Clamp(originalColor.G + errorBuffer[x, y, 1], 0, 255);
            double b = Math.Clamp(originalColor.B + errorBuffer[x, y, 2], 0, 255);

            var adjustedColor = new Color((byte)r, (byte)g, (byte)b, originalColor.A);
            TileColor tileColor = FindClosest(adjustedColor);
            SetTileData(x, y, tileColor);

            Color matchedColor = tileColor.Color;
            double errorR = r - matchedColor.R;
            double errorG = g - matchedColor.G;
            double errorB = b - matchedColor.B;

            if (x + 1 < width)
            {
                errorBuffer[x + 1, y, 0] += errorR * 7 / 16.0;
                errorBuffer[x + 1, y, 1] += errorG * 7 / 16.0;
                errorBuffer[x + 1, y, 2] += errorB * 7 / 16.0;
            }
            if (x - 1 >= 0 && y + 1 < height)
            {
                errorBuffer[x - 1, y + 1, 0] += errorR * 3 / 16.0;
                errorBuffer[x - 1, y + 1, 1] += errorG * 3 / 16.0;
                errorBuffer[x - 1, y + 1, 2] += errorB * 3 / 16.0;
            }
            if (y + 1 < height)
            {
                errorBuffer[x, y + 1, 0] += errorR * 5 / 16.0;
                errorBuffer[x, y + 1, 1] += errorG * 5 / 16.0;
                errorBuffer[x, y + 1, 2] += errorB * 5 / 16.0;
            }
            if (x + 1 < width && y + 1 < height)
            {
                errorBuffer[x + 1, y + 1, 0] += errorR * 1 / 16.0;
                errorBuffer[x + 1, y + 1, 1] += errorG * 1 / 16.0;
                errorBuffer[x + 1, y + 1, 2] += errorB * 1 / 16.0;
            }
        }
    }

    private void SetTileData(int x, int y, TileColor tileColor)
    {
        ref TileData tileData = ref CopiedSection!.Tiles!.GetTileRef(x, y);
        tileData.type = (ushort)tileColor.Type;
        tileData.sTileHeader = 32;
        tileData.color((byte)tileColor.Paint);
        tileData.wall = 0;
    }

    private void DrawSelectionRect(ImDrawListPtr drawList, Vector2 tileMouse, bool isMap)
    {
        if (CopiedSection == null) return;

        Vector2 start = tileMouse * 16f;
        Vector2 end = start + new Vector2(CopiedSection.Width, CopiedSection.Height) * 16f;

        Vector2 screenStart = isMap ? Util.WorldToScreenFullscreenMap(start) : Util.WorldToScreenWorld(start);
        Vector2 screenEnd = isMap ? Util.WorldToScreenFullscreenMap(end) : Util.WorldToScreenWorld(end);

        drawList.AddRect(screenStart, screenEnd, Color.LimeGreen.PackedValue, 0f, ImDrawFlags.None, isMap ? 2f : 1f);
    }

    public override void Edit(Vector2 cursorTilePosition)
    {
        if (CopiedSection == null) return;
        PastePixelArt(cursorTilePosition);
    }

    private void PastePixelArt(Vector2 originTile)
    {
        originTile = originTile.Floor();
        int originX = (int)originTile.X, originY = (int)originTile.Y;

        Task.Run(() =>
        {
            for (int y = 0; y < CopiedSection!.Height; y++)
                for (int x = 0; x < CopiedSection.Width; x++)
                    PasteTile(x, y, originX, originY);

            for (int y = 0; y < CopiedSection.Height; y++)
                for (int x = 0; x < CopiedSection.Width; x++)
                    FrameTile(x, y, originX, originY);
        });
    }

    private void PasteTile(int x, int y, int originX, int originY)
    {
        int worldX = originX + x, worldY = originY + y;
        if (!WorldGen.InWorld(worldX, worldY)) return;

        Tile tile = Main.tile[worldX, worldY];
        Tile copiedTile = CopiedSection!.Tiles[x, y];

        if (tile == null || copiedTile == null || !copiedTile.active()) return;

        tile.CopyFrom(copiedTile);
        if (tile.active())
            NetMessage.SendData(MessageID.PaintTile, -1, -1, null, 1, worldX, worldY, tile.type);
    }

    private void FrameTile(int x, int y, int originX, int originY)
    {
        int worldX = originX + x, worldY = originY + y;
        if (!WorldGen.InWorld(worldX, worldY)) return;

        Tile copiedTile = CopiedSection!.Tiles[x, y];
        if (copiedTile != null && copiedTile.active())
        {
            WorldGen.SquareTileFrame(worldX, worldY);
            NetMessage.SendTileSquare(-1, worldX, worldY);
        }
    }

    private void OpenFileDialog()
    {
        OpenFile(l =>
        {
            var selectedPath = l.FirstOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(selectedPath))
                _pendingPath = selectedPath;
        });
    }

    private void CheckPendingImageAndLoad()
    {
        if (_pendingPath == null || !File.Exists(_pendingPath))
        {
            _pendingPath = null;
            return;
        }

        var path = _pendingPath;
        _pendingPath = null;

        LoadImage(path);
    }

    private void LoadImage(string path)
    {
        try
        {
            UnloadResources();

            var image = Image.Load<Rgba32>(path);

            if (image.Width > 1024 || image.Height > 1024)
            {
                var ratio = Math.Min(1024f / image.Width, 1024f / image.Height);
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            SelectedPath = path;
            _textureContainer.Load(image);
            _image = image;

            // recover mutate options
            _targetWidth = image.Width;
            _targetHeight = image.Height;
            _targetRotation = 0f;
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            UnloadResources();
        }
    }

    private void ResizePreviewImage()
    {
        if (!IsImageActiveAndBound)
            return;

        if (_targetWidth <= 0 || _targetHeight <= 0)
            return;

        try
        {
            // fast path
            if (_targetWidth == _image!.Width && _targetHeight == _image.Height)
            {
                _textureContainer.Load(_image);
                return;
            }

            using var previewImage = _image!.Clone();
            previewImage.Mutate(x => x.Resize(_targetWidth, _targetHeight));
            _textureContainer.Load(previewImage);
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            UnloadResources();
        }
    }

    private void ResizeRealImage()
    {
        if (!IsImageActiveAndBound)
            return;

        if (_targetWidth <= 0 || _targetHeight <= 0)
            return;

        try
        {
            // fast path
            if (_targetWidth == _image!.Width && _targetHeight == _image.Height)
            {
                _textureContainer.Load(_image);
                return;
            }

            _image.Mutate(x => x.Resize(_targetWidth, _targetHeight));
            _textureContainer.Load(_image);

            InvalidateCopiedSection();
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            UnloadResources();
        }
    }

    private void RotatePreviewImage()
    {
        if (!IsImageActiveAndBound)
            return;

        try
        {
            // fast path
            if (_targetRotation == 0f)
            {
                _textureContainer.Load(_image!);
                return;
            }

            using var previewImage = _image!.Clone();
            previewImage.Mutate(x => x.Rotate(_targetRotation));
            _textureContainer.Load(previewImage);

            // image size will change after rotation
            _targetWidth = previewImage.Width;
            _targetHeight = previewImage.Height;
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            UnloadResources();
        }
    }

    private void RotateRealImage()
    {
        if (!IsImageActiveAndBound)
            return;

        try
        {
            // fast path
            if (_targetRotation == 0f)
            {
                _textureContainer.Load(_image!);
                return;
            }

            _image!.Mutate(x => x.Rotate(_targetRotation));
            _textureContainer.Load(_image!);

            // image size will change after rotation
            _targetWidth = _image!.Width;
            _targetHeight = _image.Height;

            InvalidateCopiedSection();
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            UnloadResources();
        }
    }

    private void UnloadResources()
    {
        SelectedPath = string.Empty;
        _pendingPath = null;

        InvalidateCopiedSection();
        InvalidateColorCache();

        _textureContainer.Unload();
        _image?.Dispose();
        _image = null;

        _targetWidth = 0;
        _targetHeight = 0;
        _targetRotation = 0f;
    }

    private void InvalidateCopiedSection() => CopiedSection = null;
    private void InvalidateColorCache() => _colorCache.Clear();

    #region File Dialog Helpers

    private static Action<List<string>>? _fileCallback;

    private static void OpenFile(Action<List<string>> callback)
    {
        _fileCallback = callback;
        SDL.SDL_ShowOpenFileDialog(FileCallback, IntPtr.Zero, Main.instance.Window.Handle, null, 0, null, false);
    }

    private static unsafe void FileCallback(IntPtr userdata, IntPtr fileList, int filter)
    {
        var callback = _fileCallback;
        _fileCallback = null;

        var fileListPtr = (byte**)fileList;
        if (fileListPtr == null)
        {
            ClientLoader.Console.WriteError(GetString($"SDL ShowOpenFileDialog Err: {SDL.SDL_GetError()}"));
            return;
        }

        if (*fileListPtr == null) return;

        List<string> selected = new();
        for (var p = fileListPtr; *p != null; p++)
            selected.Add(Marshal.PtrToStringUTF8(new IntPtr(*p)) ?? string.Empty);

        callback?.Invoke(selected);
    }

    #endregion
}
