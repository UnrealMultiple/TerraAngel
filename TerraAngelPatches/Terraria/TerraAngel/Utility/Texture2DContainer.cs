using System;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Microsoft.Xna.Framework.Color;

namespace TerraAngel.Utility;

public struct Texture2DContainer : IDisposable
{
    public IntPtr TextureBindId { get; private set; } = IntPtr.Zero;
    public Texture2D? Texture { get; private set; } = null;

    public bool IsActive => TextureBindId != IntPtr.Zero && Texture != null;

    public Texture2DContainer()
    {
    }

    public void Dispose()
    {
        Unload();
    }

    public void Load(string imageFilePath, Action<Image<Rgba32>>? preprocessingOperation = null)
    {
        using var image = Image.Load<Rgba32>(imageFilePath);
        preprocessingOperation?.Invoke(image);
        Load(image); // call image loading overload
    }

    public void Load(Image<Rgba32> image)
    {
        // rent a buffer
        var textureData = ArrayPool<Color>.Shared.Rent(image.Width * image.Height);
        try
        {
            // copying....
            // theoretically we can use image.DangerousTryGetSinglePixelMemory() but it might be unstable
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        textureData[y * accessor.Width + x] = new Color
                        {
                            PackedValue = row[x].PackedValue
                        };
                    }
                }
            });

            Load(textureData, image.Width, image.Height);
        }
        finally
        {
            // return the texture buffer
            ArrayPool<Color>.Shared.Return(textureData);
        }
    }

    public void Load(Color[] textureData, int width, int height)
    {
        // sanity check
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            width * height,
            textureData.Length,
            $"{nameof(width)} * {nameof(height)}");

        // place into variables first to avoid partial bind
        var texture = new Texture2D(Main.graphics.GraphicsDevice, width, height);
        var textureBindId = IntPtr.Zero;

        try
        {
            unsafe
            {
                fixed (Color* ptr = textureData)
                {
                    // hey! you cant simply use textureData.Length as it might be rented from an ArrayPool!
                    texture.SetDataPointerEXT(0, null, new IntPtr(ptr), width * height * sizeof(Color));
                }
            }

            // bind it!
            textureBindId = ClientLoader.MainRenderer?.BindTexture(texture) ?? throw new Exception("Failed to bind texture");
        }
        catch (Exception)
        {
            // we are fucked up...
            texture.Dispose();
            if (textureBindId != IntPtr.Zero)
                ClientLoader.MainRenderer?.UnbindTexture(textureBindId);
            throw;
        }

        // unload here
        Unload();

        // bind to container
        Texture = texture;
        TextureBindId = textureBindId;
    }

    public void Unload()
    {
        if (TextureBindId != IntPtr.Zero)
            ClientLoader.MainRenderer?.UnbindTexture(TextureBindId);
        TextureBindId = IntPtr.Zero;

        Texture?.Dispose();
        Texture = null;
    }
}