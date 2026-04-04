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

    public string SelectedPath = string.Empty;
    public TileSectionRenderer Renderer = new();
    public TileSection? CopiedSection;

    private string? _pendingPath;
    private Color[,]? _imageData;
    private Texture2D? _selectedTexture;
    private IntPtr _selectedTextureId = IntPtr.Zero;

    private int _targetWidth = 200;
    private int _targetHeight = 200;
    private int _originalWidth;
    private int _originalHeight;
    private bool _sizeChanged = false;
    private bool _isFirstLoad = true;

    private bool _enableDithering = true;
    private ColorMatchAlgorithm _algorithm = ColorMatchAlgorithm.LabDeltaE;
    private readonly Dictionary<uint, TileColor> _colorCache = new();

    public enum ColorMatchAlgorithm
    {
        LabDeltaE,
        RgbDistance
    }

    public TileColor FindClosest(Color target)
    {
        var key = GetColorKey(target);
        if (_colorCache.TryGetValue(key, out var cached))
            return cached;

        TileColor closest = _algorithm switch
        {
            ColorMatchAlgorithm.RgbDistance => FindClosestRgb(target),
            _ => FindClosestLab(target)
        };

        _colorCache[key] = closest;
        return closest;
    }

    private static TileColor FindClosestLab(Color target)
    {
        double[] targetLab = ToLab(target);
        var colors = TileColorData.Colors;

        TileColor closest = colors[0];
        double minDistance = DeltaE(targetLab, colors[0].GetLab());

        for (int i = 1; i < colors.Length; i++)
        {
            double distance = DeltaE(targetLab, colors[i].GetLab());
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
        double minDistance = RgbDistance(target, colors[0].Color);

        for (int i = 1; i < colors.Length; i++)
        {
            double distance = RgbDistance(target, colors[i].Color);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = colors[i];
            }
        }

        return closest;
    }

    private static double RgbDistance(Color a, Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }

    private static uint GetColorKey(Color c) => ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;

    public override bool DrawUITab(ImGuiIOPtr io)
    {
        if (!ImGui.BeginTabItem(GetString("Pixel Art")))
            return false;

        CheckAndLoadImage();
        DrawControls();
        DrawImagePreview();

        ImGui.EndTabItem();
        return true;
    }

    private void DrawControls()
    {
        ImGuiUtil.HelpMarkerTopRight(GetString("Press middle mouse button to paste"));
        ImGui.Text(GetString("Still under construction...."));

        if (ImGui.Button(GetString("Select Image")))
            OpenFileDialog();

        if (ImGui.Button(GetString("Clear Selection")))
            ClearSelection();

        string[] algorithms = { "Lab DeltaE (Accurate)", "RGB Distance (Fast)" };
        int currentAlgorithm = (int)_algorithm;
        if (ImGui.Combo(GetString("Color Match Algorithm"), ref currentAlgorithm, algorithms, algorithms.Length))
        {
            _algorithm = (ColorMatchAlgorithm)currentAlgorithm;
            _colorCache.Clear();
            CopiedSection = null;
        }

        ImGui.Checkbox(GetString("Enable Dithering"), ref _enableDithering);
        if (_enableDithering)
        {
            ImGuiUtil.HelpMarker(GetString("Dithering creates the illusion of more colors by diffusing errors to neighboring pixels."));
        }

        if (_imageData != null)
        {
            ImGui.Text(GetString($"Original: {_originalWidth}x{_originalHeight}"));
            ImGui.Text(GetString($"Target: {_targetWidth}x{_targetHeight}"));

            int newWidth = _targetWidth;
            int newHeight = _targetHeight;

            if (ImGui.InputInt(GetString("Width"), ref newWidth))
            {
                if (newWidth > 0)
                {
                    _targetWidth = newWidth;
                    _sizeChanged = true;
                }
            }

            if (ImGui.InputInt(GetString("Height"), ref newHeight))
            {
                if (newHeight > 0)
                {
                    _targetHeight = newHeight;
                    _sizeChanged = true;
                }
            }

            if (_sizeChanged && ImGui.Button(GetString("Apply Resize")))
            {
                ReloadImageWithNewSize();
                _sizeChanged = false;
            }
        }

        ImGui.Text(GetString($"File: {SelectedPath}"));
    }

    private void DrawImagePreview()
    {
        if (_selectedTextureId == IntPtr.Zero || _imageData == null)
            return;

        Vector2 contentAvail = ImGui.GetContentRegionAvail();
        var height = _imageData.GetLength(1) * contentAvail.X / _imageData.GetLength(0);
        ImGui.Image(_selectedTextureId, new Vector2(contentAvail.X, height));
    }

    public override void DrawPreviewInWorld(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        UpdateCopiedSection();

        if (CopiedSection == null)
            return;

        Vector2 tileMouse = GetTileMousePosition(io, false);
        Renderer.DrawDetailed(CopiedSection, Util.WorldToScreenWorld(tileMouse * 16f), Vector2.Zero, io.DisplaySize, false);
        DrawSelectionRect(drawList, tileMouse, false);
    }

    public override void DrawPreviewInMap(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        UpdateCopiedSection();

        if (CopiedSection == null)
            return;

        Vector2 tileMouse = GetTileMousePosition(io, true);
        Renderer.DrawPrimitiveMap(CopiedSection, tileMouse * 16f, Vector2.Zero, io.DisplaySize, false);
        DrawSelectionRect(drawList, tileMouse, true);
    }

    private void UpdateCopiedSection()
    {
        if (_imageData == null || CopiedSection != null)
            return;

        int width = _imageData.GetLength(0);
        int height = _imageData.GetLength(1);
        CopiedSection = new TileSection(width, height);

        if (_enableDithering)
        {
            ApplyFloydSteinbergDithering(width, height);
        }
        else
        {
            for (int y = 0; y < height && y < CopiedSection.Height; y++)
            {
                for (int x = 0; x < width && x < CopiedSection.Width; x++)
                {
                    if (x < _imageData.GetLength(0) && y < _imageData.GetLength(1))
                    {
                        TileColor tileColor = FindClosest(_imageData[x, y]);
                        SetTileData(x, y, tileColor);
                    }
                }
            }
        }
    }

    private void ApplyFloydSteinbergDithering(int width, int height)
    {
        width = Math.Min(width, CopiedSection!.Width);
        height = Math.Min(height, CopiedSection.Height);

        var errorBuffer = new double[width, height, 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x >= _imageData.GetLength(0) || y >= _imageData.GetLength(1))
                    continue;

                Color originalColor = _imageData[x, y];

                double r = Clamp(originalColor.R + errorBuffer[x, y, 0], 0, 255);
                double g = Clamp(originalColor.G + errorBuffer[x, y, 1], 0, 255);
                double b = Clamp(originalColor.B + errorBuffer[x, y, 2], 0, 255);

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
    }

    private void SetTileData(int x, int y, TileColor tileColor)
    {
        ref TileData tileData = ref CopiedSection!.Tiles!.GetTileRef(x, y);
        tileData.type = (ushort)tileColor.Type;
        tileData.sTileHeader = 32;
        tileData.color((byte)tileColor.Paint);
        tileData.wall = 0;
    }

    private static Vector2 GetTileMousePosition(ImGuiIOPtr io, bool isMap)
    {
        Vector2 worldMouse = isMap
            ? Util.ScreenToWorldFullscreenMap(InputSystem.MousePosition)
            : Util.ScreenToWorldWorld(InputSystem.MousePosition);
        return (worldMouse / 16f).Floor();
    }

    private void DrawSelectionRect(ImDrawListPtr drawList, Vector2 tileMouse, bool isMap)
    {
        if (CopiedSection == null)
            return;

        Vector2 start = tileMouse * 16f;
        Vector2 end = start + new Vector2(CopiedSection.Width, CopiedSection.Height) * 16f;

        Vector2 screenStart = isMap ? Util.WorldToScreenFullscreenMap(start) : Util.WorldToScreenWorld(start);
        Vector2 screenEnd = isMap ? Util.WorldToScreenFullscreenMap(end) : Util.WorldToScreenWorld(end);

        drawList.AddRect(screenStart, screenEnd, Color.LimeGreen.PackedValue, 0f, ImDrawFlags.None, isMap ? 2f : 1f);
    }

    public override void Edit(Vector2 cursorTilePosition)
    {
        if (CopiedSection == null)
            return;

        PastePixelArt(cursorTilePosition);
    }

    private void PastePixelArt(Vector2 originTile)
    {
        originTile = originTile.Floor();
        int originX = (int)originTile.X;
        int originY = (int)originTile.Y;

        Task.Run(() =>
        {
            for (int y = 0; y < CopiedSection!.Height; y++)
            {
                for (int x = 0; x < CopiedSection.Width; x++)
                {
                    int worldX = originX + x;
                    int worldY = originY + y;

                    if (!WorldGen.InWorld(worldX, worldY))
                        continue;

                    Tile tile = Main.tile[worldX, worldY];
                    Tile copiedTile = CopiedSection.Tiles[x, y];

                    if (tile == null || copiedTile == null)
                        continue;

                    tile.CopyFrom(copiedTile);

                    if (tile.active())
                    {
                        NetMessage.SendData(MessageID.PaintTile, -1, -1, null, 1, worldX, worldY, tile.type);
                    }
                }
            }

            for (int y = 0; y < CopiedSection.Height; y++)
            {
                for (int x = 0; x < CopiedSection.Width; x++)
                {
                    int worldX = originX + x;
                    int worldY = originY + y;

                    if (!WorldGen.InWorld(worldX, worldY))
                        continue;

                    WorldGen.SquareTileFrame(worldX, worldY);
                    NetMessage.SendTileSquare(-1, worldX, worldY);
                }
            }
        });
    }

    private void OpenFileDialog()
    {
        OpenFile(l =>
        {
            SelectedPath = l.FirstOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(SelectedPath))
                _pendingPath = SelectedPath;
        });
    }

    private void ClearSelection()
    {
        SelectedPath = string.Empty;
        _pendingPath = null;
        _isFirstLoad = true;

        CleanupTexture();
        _colorCache.Clear();
    }

    private void CheckAndLoadImage()
    {
        if (_pendingPath == null || !File.Exists(_pendingPath))
        {
            _pendingPath = null;
            return;
        }

        string path = _pendingPath;
        _pendingPath = null;

        CleanupTexture();

        try
        {
            LoadImage(path);
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            CleanupTexture();
        }
    }

    private void LoadImage(string path)
    {
        using var image = Image.Load<Rgba32>(path);

        _originalWidth = image.Width;
        _originalHeight = image.Height;

        if (_isFirstLoad && (_originalWidth > 900 || _originalHeight > 900))
        {
            float ratio = Math.Min(900f / _originalWidth, 900f / _originalHeight);
            _targetWidth = (int)(_originalWidth * ratio);
            _targetHeight = (int)(_originalHeight * ratio);
            image.Mutate(x => x.Resize(_targetWidth, _targetHeight));
        }
        else
        {
            _targetWidth = _originalWidth;
            _targetHeight = _originalHeight;
        }

        _isFirstLoad = false;
        ProcessImageData(image);
    }

    private void ReloadImageWithNewSize()
    {
        if (string.IsNullOrEmpty(SelectedPath) || !File.Exists(SelectedPath))
            return;

        CleanupTexture();
        _colorCache.Clear();

        try
        {
            using var image = Image.Load<Rgba32>(SelectedPath);

            image.Mutate(x => x.Resize(_targetWidth, _targetHeight));
            ProcessImageData(image);
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError(GetString($"[{nameof(WorldEditPixelArt)}] Error: {ex}"));
            CleanupTexture();
        }
    }

    private void ProcessImageData(Image<Rgba32> image)
    {
        _imageData = new Color[image.Width, image.Height];
        var textureData = new Color[image.Width * image.Height];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    ref var pixel = ref row[x];
                    var color = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
                    _imageData[x, y] = color;
                    textureData[y * accessor.Width + x] = color;
                }
            }
        });

        _selectedTexture = new Texture2D(Main.graphics.GraphicsDevice, image.Width, image.Height);
        _selectedTexture.SetData(textureData);
        _selectedTextureId = ClientLoader.MainRenderer!.BindTexture(_selectedTexture);
    }

    private void CleanupTexture()
    {
        if (_selectedTextureId != IntPtr.Zero)
        {
            ClientLoader.MainRenderer!.UnbindTexture(_selectedTextureId);
            _selectedTextureId = IntPtr.Zero;
        }

        _selectedTexture?.Dispose();
        _selectedTexture = null;
        _imageData = null;
        CopiedSection = null;
    }

    private static double[] ToLab(Color c) => XyzToLab(RgbToXyz(c));

    private static double DeltaE(double[] lab1, double[] lab2)
    {
        double dL = lab1[0] - lab2[0];
        double da = lab1[1] - lab2[1];
        double db = lab1[2] - lab2[2];
        return Math.Sqrt(dL * dL + da * da + db * db);
    }

    private static double[] RgbToXyz(Color c)
    {
        double r = c.R / 255.0;
        double g = c.G / 255.0;
        double b = c.B / 255.0;

        r = r > 0.04045 ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
        g = g > 0.04045 ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
        b = b > 0.04045 ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;

        double x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
        double y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
        double z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

        return new[] { x, y, z };
    }

    private static double[] XyzToLab(double[] xyz)
    {
        double x = xyz[0] / 0.95047;
        double y = xyz[1];
        double z = xyz[2] / 1.08883;

        double F(double t) => t > 0.008856 ? Math.Pow(t, 1.0 / 3.0) : (7.787 * t + 16.0 / 116.0);

        double fx = F(x);
        double fy = F(y);
        double fz = F(z);

        double L = 116 * fy - 16;
        double a = 500 * (fx - fy);
        double b = 200 * (fy - fz);

        return new[] { L, a, b };
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

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

        if (*fileListPtr == null)
            return;

        List<string> selected = new();
        for (var p = fileListPtr; *p != null; p++)
            selected.Add(Marshal.PtrToStringUTF8(new IntPtr(*p)) ?? string.Empty);

        callback?.Invoke(selected);
    }
}
