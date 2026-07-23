using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Terraria;

public unsafe class NativeTileMap : IDisposable
{
    public readonly uint Width;
    public readonly uint Height;

    /// <summary>
    /// Size of the heap in bytes
    /// </summary>
    public readonly long HeapSize;

    public readonly TileData[,] TileHeap;
    // ReSharper disable once InconsistentNaming
    private PinnedGCHandle<TileData[,]> TileHeapGCHandle;

    public bool[,] LoadedTileSections = new bool[0, 0];

    public NativeTileMap(int width, int height)
    {
        Width = (uint)width;
        Height = (uint)height;
        HeapSize = Width * Height * sizeof(TileData);

        TileHeap = new TileData[Width, Height];
        TileHeapGCHandle = new PinnedGCHandle<TileData[,]>(TileHeap);

        LoadedTileSections = new bool[Width / Main.sectionWidth, Height / Main.sectionHeight];
    }

    ~NativeTileMap() => Dispose(false);
    
    private void ReleaseUnmanagedResources()
    {
        TileHeapGCHandle.Dispose();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Tile this[int x, int y]
    {
        get => new(ref TileHeap[x, y]);
        set => TileHeap[x, y] = value.RefData;
    }

    public Tile this[Vector2i position]
    {
        get => new(ref TileHeap[position.X, position.Y]);
        set => TileHeap[position.X, position.Y] = value.RefData;
    }

    public ref TileData GetTileRef(int x, int y)
    {
        return ref TileHeap[x, y];
    }

    /// <summary>
    /// Clears the heap
    /// </summary>
    public void ResetHeap()
    {
        Array.Clear(TileHeap);
    }

    public bool InWorld(Vector2 position) => InWorld((int)(position.X / 16f), (int)(position.Y / 16f));

    public bool InWorld(Point position) => InWorld(position.X, position.Y);

    public bool InWorld(Vector2i position) => InWorld(position.X, position.Y);

    public bool InWorld(int x, int y)
    {
        if ((uint)x >= Width || (uint)y >= Height)
        {
            return false;
        }
        return true;
    }

    public bool InWorldAndLoaded(int x, int y)
    {
        return InWorld(x, y) && IsTileInLoadedSection(x, y);
    }

    public bool IsTileSectionLoaded(int sectionX, int sectionY)
    {
        if (sectionX < 0 || sectionY < 0 || sectionX >= Main.maxSectionsX || sectionY >= Main.maxSectionsY) return false;
        if (Main.netMode == 0) return true;
        if (Main.netMode == 1) return LoadedTileSections[sectionX, sectionY];
        return true;
    }

    public bool IsTileInLoadedSection(int x, int y)
    {
        if (Main.netMode == 0) return true;
        if (Main.netMode == 1) return IsTileSectionLoaded(x / Main.sectionWidth, y / Main.sectionHeight);
        return true;
    }
}