using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using TerraAngel.Net;
using Terraria.GameContent;

namespace TerraAngel.WorldEdits;

public class TileSection
{
    public int Width;

    public int Height;

    public NativeTileMap? Tiles;

    public TileSection(int width, int height)
    {
        Tiles = new NativeTileMap(width, height);
        Width = width;
        Height = height;
    }

    public TileSection(int x, int y, int width, int height)
        : this(Main.tile, x, y, width, height)
    {

    }

    public TileSection(NativeTileMap copyFrom, int x, int y, int width, int height)
    {
        // do a gay little switch around so that width and height can be negative
        if (width < 0)
        {
            x += width;
            width = -width;
        }
        if (height < 0)
        {
            y += height;
            height = -height;
        }

        if (width * height <= 0)
            return;

        Tiles = new NativeTileMap(width, height);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Tiles[i, j].CopyFrom(copyFrom[x + i, y + j]);
            }
        }

        Width = width;
        Height = height;
    }
}

public class TileSectionRenderer : IDisposable
{
    private static TilePaintSystemV2 paintSystem => Main.instance.TilePaintSystem;

    private static Texture2D GetTileTexture(Tile tile)
    {
        Texture2D result = TextureAssets.Tile[tile.type].Value;
        int tileType = tile.type;
        Texture2D? texture2D = paintSystem.TryGetTileAndRequestIfNotReady(tileType, 0, tile.color());
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (texture2D is not null)
        {
            result = texture2D;
        }
        return result;
    }

    private static Texture2D GetWallDrawTexture(Tile tile)
    {
        Texture2D result = TextureAssets.Wall[tile.wall].Value;
        int wall = tile.wall;
        Texture2D? texture2D = paintSystem.TryGetWallAndRequestIfNotReady(wall, tile.wallColor());
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (texture2D is not null)
        {
            result = texture2D;
        }
        return result;
    }

    public unsafe void DrawDetailed(TileSection section, Vector2 origin, Vector2 clipRectMin, Vector2 clipRectMax, bool showEmptyTile = false)
    {
        if (section.Width < 1 || section.Height < 1 || section.Tiles is null)
            return;

        SpriteBatch sb = Main.spriteBatch;

        Rectangle tileRectCache = new Rectangle(0, 0, 16, 16);
        Rectangle wallRectCache = new Rectangle(0, 0, 32, 32);

        // origin is now (origin) - (Main.screenPosition) [in world coord]
        origin = Vector2.Transform(origin, Main.GameViewMatrix.InverseZoomMatrix);

        // convert from [screen coord] to [world coord]
        clipRectMin = Util.ScreenToWorldWorld(clipRectMin);
        clipRectMax = Util.ScreenToWorldWorld(clipRectMax);

        // originInWorld is (origin) [in world coord]
        var originInWorld = origin + Main.screenPosition;
        var clippedMinX = Math.Clamp((int)Math.Floor((clipRectMin.X - originInWorld.X) / 16f) - 1, 0, section.Width);
        var clippedMinY = Math.Clamp((int)Math.Floor((clipRectMin.Y - originInWorld.Y) / 16f) - 1, 0, section.Height);
        var clippedMaxX = Math.Clamp((int)Math.Ceiling((clipRectMax.X - originInWorld.X) / 16f), 0, section.Width);
        var clippedMaxY = Math.Clamp((int)Math.Ceiling((clipRectMax.Y - originInWorld.Y) / 16f), 0, section.Height);

        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
        // empty tile pass
        if (showEmptyTile)
        {
            for (var x = clippedMinX; x < clippedMaxX; x++)
            {
                for (var y = clippedMinY; y < clippedMaxY; y++)
                {
                    if (section.Tiles[x, y].Data == null)
                        continue;

                    Tile tile = section.Tiles[x, y];

                    if (!tile.active() && tile.wall == 0)
                    {
                        sb.Draw(GraphicsUtility.BlankTexture, origin + new Vector2(x * 16, y * 16), tileRectCache, new Color(0.5f, 0f, 0f, 0.5f));
                    }
                }
            }
        }
        // wall pass
        for (var x = clippedMinX; x < clippedMaxX; x++)
        {
            for (var y = clippedMinY; y < clippedMaxY; y++)
            {
                if (section.Tiles[x, y].Data == null)
                    continue;

                Tile tile = section.Tiles[x, y];

                if (tile.wall != 0)
                {
                    Main.instance.LoadWall(tile.wall);
                    wallRectCache.X = tile.wallFrameX();
                    // wall animation disabled because it is too buggy
                    // wallRectCache.Y = tile.wallFrameY() + Main.wallFrame[tile.wall] * 180;
                    wallRectCache.Y = tile.wallFrameY();
                    Texture2D wallTexture = GetWallDrawTexture(tile);
                    sb.Draw(wallTexture, origin + new Vector2(x * 16, y * 16) - new Vector2(8, 8), wallRectCache, Color.White);
                }
            }
        }
        // tile pass
        for (var x = clippedMinX; x < clippedMaxX; x++)
        {
            for (var y = clippedMinY; y < clippedMaxY; y++)
            {
                if (section.Tiles[x, y].Data == null)
                    continue;

                Tile tile = section.Tiles[x, y];

                if (tile.active())
                {
                    Main.instance.LoadTiles(tile.type);
                    // tile animation disabled because it is too buggy
                    // tileRectCache.X = tile.frameX + Main.tileFrame[tile.type] * 38;
                    tileRectCache.X = tile.frameX;
                    tileRectCache.Y = tile.frameY;
                    Texture2D tileTexture = GetTileTexture(tile);
                    sb.Draw(tileTexture, origin + new Vector2(x * 16, y * 16), tileRectCache, Color.White);
                }
            }
        }
        sb.End();
    }

    public unsafe void DrawPrimitive(TileSection section, Vector2 origin, Vector2 clipRectMin, Vector2 clipRectMax, bool showEmptyTile = false)
    {
        if (section.Width < 1 || section.Height < 1 || section.Tiles is null)
            return;

        SpriteBatch sb = Main.spriteBatch;

        Rectangle rectCache = new Rectangle(0, 0, 1, 1);

        // origin is now (origin) - (Main.screenPosition) [in world coord]
        origin = Vector2.Transform(origin, Main.GameViewMatrix.InverseZoomMatrix);

        // convert from [screen coord] to [world coord]
        clipRectMin = Util.ScreenToWorldWorld(clipRectMin);
        clipRectMax = Util.ScreenToWorldWorld(clipRectMax);

        // originInWorld is (origin) [in world coord]
        var originInWorld = origin + Main.screenPosition;
        var clippedMinX = Math.Clamp((int)Math.Floor((clipRectMin.X - originInWorld.X) / 16f) - 1, 0, section.Width);
        var clippedMinY = Math.Clamp((int)Math.Floor((clipRectMin.Y - originInWorld.Y) / 16f) - 1, 0, section.Height);
        var clippedMaxX = Math.Clamp((int)Math.Ceiling((clipRectMax.X - originInWorld.X) / 16f), 0, section.Width);
        var clippedMaxY = Math.Clamp((int)Math.Ceiling((clipRectMax.Y - originInWorld.Y) / 16f), 0, section.Height);

        sb.Begin(SpriteSortMode.Deferred, showEmptyTile ? BlendState.AlphaBlend : BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
        // empty tile pass
        if (showEmptyTile)
        {
            for (var x = clippedMinX; x < clippedMaxX; x++)
            {
                for (var y = clippedMinY; y < clippedMaxY; y++)
                {
                    if (section.Tiles[x, y].Data == null)
                        continue;

                    Tile tile = section.Tiles[x, y];

                    if (!tile.active() && tile.wall == 0)
                    {
                        sb.Draw(GraphicsUtility.BlankTexture, new Rectangle((int)MathF.Ceiling(origin.X + x * 16), (int)MathF.Ceiling(origin.Y + y * 16f), 16, 16), rectCache, new Color(0.5f, 0f, 0f, 0.5f));
                    }
                }
            }
        }
        // wall pass
        for (var x = clippedMinX; x < clippedMaxX; x++)
        {
            for (var y = clippedMinY; y < clippedMaxY; y++)
            {
                if (section.Tiles[x, y].Data == null)
                    continue;

                Tile tile = section.Tiles[x, y];

                if (tile.wall != 0)
                {
                    sb.Draw(GraphicsUtility.BlankTexture, new Rectangle(((int)MathF.Ceiling(origin.X + x * 16)), ((int)MathF.Ceiling(origin.Y + y * 16f)), 16, 16), rectCache, Utility.TileUtil.GetWallColor(tile.wall, tile.wallColor()));
                }
            }
        }
        // tile pass
        for (var x = clippedMinX; x < clippedMaxX; x++)
        {
            for (var y = clippedMinY; y < clippedMaxY; y++)
            {
                if (section.Tiles[x, y].Data == null)
                    continue;

                Tile tile = section.Tiles[x, y];

                if (tile.active())
                {
                    sb.Draw(GraphicsUtility.BlankTexture, new Rectangle(((int)MathF.Ceiling(origin.X + x * 16)), ((int)MathF.Ceiling(origin.Y + y * 16f)), 16, 16), rectCache, Utility.TileUtil.GetWallColor(tile.type, tile.color()));
                }
            }
        }
        sb.End();
    }

    public RenderTarget2D? DrawPrimitiveMapCache { get; private set; }
    public TileSection? DrawPrimitiveMapCacheTileSection { get; private set; }

    public void InvalidateDrawPrimitiveMapCache()
    {
        DrawPrimitiveMapCache?.Dispose();
        DrawPrimitiveMapCache = null;
        DrawPrimitiveMapCacheTileSection = null;
    }

    public unsafe void DrawPrimitiveMap(TileSection section, Vector2 worldPoint, Vector2 clipRectMin, Vector2 clipRectMax, bool showEmptyTile = false, bool enableCaching = false)
    {
        if (section.Width < 1 || section.Height < 1 || section.Tiles is null)
            return;

        SpriteBatch sb = Main.spriteBatch;

        Rectangle rectCache = new Rectangle(0, 0, 1, 1);

        // convert to [world coord]
        clipRectMin = Util.ScreenToWorldFullscreenMap(clipRectMin);
        clipRectMax = Util.ScreenToWorldFullscreenMap(clipRectMax);

        var clippedMinX = Math.Clamp((int)Math.Floor((clipRectMin.X - worldPoint.X) / 16f) - 1, 0, section.Width);
        var clippedMinY = Math.Clamp((int)Math.Floor((clipRectMin.Y - worldPoint.Y) / 16f) - 1, 0, section.Height);
        var clippedMaxX = Math.Clamp((int)Math.Ceiling((clipRectMax.X - worldPoint.X) / 16f), 0, section.Width);
        var clippedMaxY = Math.Clamp((int)Math.Ceiling((clipRectMax.Y - worldPoint.Y) / 16f), 0, section.Height);

        if (showEmptyTile)
        {
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            // Top Left and Bottom Right
            var worldTL = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(clippedMinX, clippedMinY) * 16); 
            var worldBR = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(clippedMaxX, clippedMaxY) * 16);

            var rect = new Rectangle(
                (int)MathF.Ceiling(worldTL.X),
                (int)MathF.Ceiling(worldTL.Y),
                (int)MathF.Ceiling(worldBR.X - worldTL.X),
                (int)MathF.Ceiling(worldBR.Y - worldTL.Y));

            sb.Draw(GraphicsUtility.BlankTexture, rect, rectCache, new Color(0.5f, 0f, 0f, 0.5f));
            sb.End();
        }

        if (enableCaching)
        {
            if (DrawPrimitiveMapCache == null && DrawPrimitiveMapCacheTileSection == null)
            {
                // TODO: should we copy this?
                DrawPrimitiveMapCacheTileSection = section;
            }

            if (DrawPrimitiveMapCache != null)
            {
                // let's draw it!
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

                // target Top Left and Bottom Right
                Vector2 worldTL = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(clippedMinX, clippedMinY) * 16f);
                Vector2 worldBR = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(clippedMaxX, clippedMaxY) * 16f);

                Rectangle targetRect = new Rectangle(
                    (int)MathF.Ceiling(worldTL.X),
                    (int)MathF.Ceiling(worldTL.Y),
                    (int)MathF.Ceiling(worldBR.X - worldTL.X),
                    (int)MathF.Ceiling(worldBR.Y - worldTL.Y));

                Rectangle sourceRect = new Rectangle(
                    (int)MathF.Ceiling(clippedMinX),
                    (int)MathF.Ceiling(clippedMinY),
                    (int)MathF.Ceiling(clippedMaxX - clippedMinX),
                    (int)MathF.Ceiling(clippedMaxY - clippedMinY));

                sb.Draw(DrawPrimitiveMapCache, targetRect, sourceRect, Color.White);
                sb.End();

                // if caching hit we leave early
                return;
            }
            // or else we fall back to traditional way until cache is ready
        }

        // traditional way
        sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        for (var x = clippedMinX; x < clippedMaxX; x++)
        {
            for (var y = clippedMinY; y < clippedMaxY; y++)
            {
                if (section.Tiles[x, y].Data == null)
                    continue;

                // Top Left and Bottom Right
                Vector2 worldTL = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(x * 16f, y * 16f));
                Vector2 worldBR = Util.WorldToScreenFullscreenMap(worldPoint + new Vector2(x * 16f + 16f, y * 16f + 16f));

                Tile tile = section.Tiles[x, y];

                Rectangle rect = new Rectangle(
                    (int)MathF.Ceiling(worldTL.X),
                    (int)MathF.Ceiling(worldTL.Y),
                    (int)MathF.Ceiling(worldBR.X - worldTL.X),
                    (int)MathF.Ceiling(worldBR.Y - worldTL.Y));

                if (tile.active())
                {
                    sb.Draw(GraphicsUtility.BlankTexture, rect, rectCache, TileUtil.GetTileColor(tile.type, tile.color()));
                    continue;
                }

                if (tile.wall != 0)
                {
                    sb.Draw(GraphicsUtility.BlankTexture, rect, rectCache, TileUtil.GetWallColor(tile.wall, tile.wallColor()));
                }
            }
        }
        sb.End();
    }

    // remember to call me!!!
    public unsafe void PreUpdate()
    {
        // building DrawPrimitiveMapCache...
        if (DrawPrimitiveMapCacheTileSection != null)
        {
            var section = DrawPrimitiveMapCacheTileSection;
            DrawPrimitiveMapCacheTileSection = null;

            if (section.Width < 1 || section.Height < 1 || section.Tiles is null)
                return;

            Rectangle rectCache = new Rectangle(0, 0, 1, 1);

            SpriteBatch sb = Main.spriteBatch;

            // let's initialize the cache
            InvalidateDrawPrimitiveMapCache();
            DrawPrimitiveMapCache = new RenderTarget2D(Main.graphics.GraphicsDevice, section.Width, section.Height);

            Main.graphics.GraphicsDevice.SetRenderTarget(DrawPrimitiveMapCache);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            // starts preparing the cache...
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            for (var x = 0; x < section.Width; x++)
            {
                for (var y = 0; y < section.Height; y++)
                {
                    if (section.Tiles[x, y].Data == null)
                        continue;

                    Tile tile = section.Tiles[x, y];

                    Rectangle rect = new Rectangle(
                        x,
                        y,
                        1,
                        1);

                    if (tile.active())
                    {
                        sb.Draw(GraphicsUtility.BlankTexture, rect, rectCache, TileUtil.GetTileColor(tile.type, tile.color()));
                        continue;
                    }

                    if (tile.wall != 0)
                    {
                        sb.Draw(GraphicsUtility.BlankTexture, rect, rectCache, TileUtil.GetWallColor(tile.wall, tile.wallColor()));
                    }
                }
            }
            sb.End();

            // recover render targets
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
        }
    }

    public void Dispose()
    {
        InvalidateDrawPrimitiveMapCache();
    }
}

public class TileSectionPaster
{
    public Task PasteBySendTileRectInNewTask(TileSection section, Vector2i originTile, bool isDestroyTiles, CancellationToken token = default) =>
        Task.Run(() => PasteBySendTileRect(section, originTile, isDestroyTiles, token), token);

    public void PasteBySendTileRect(TileSection section, Vector2i originTile, bool isDestroyTiles, CancellationToken token = default)
    {
        if (section.Tiles is null)
            return;

        CopyTilesForSendTileRect(section, originTile, isDestroyTiles, true, token);
        CopyTilesForSendTileRect(section, originTile, isDestroyTiles, false, token);

        // pass three, for framing and syncing
        for (int y = section.Height - 1; y > -1; y--)
        for (int x = 0; x < section.Width; x++)
        {
            token.ThrowIfCancellationRequested();

            var world = originTile + new Vector2i(x, y);

            if (!WorldGen.InWorld(world.X, world.Y))
                continue;

            WorldGen.SquareTileFrame(world.X, world.Y);
            WorldGen.SquareWallFrame(world.X, world.Y);

            NetMessage.SendTileSquare(Main.myPlayer, world.X, world.Y);
        }
    }

    private void CopyTilesForSendTileRect(TileSection section, Vector2i originTile, bool isDestroyTiles, bool isCopySolidTiles, CancellationToken token)
    {
        if (section.Tiles is null)
            return;

        for (int y = section.Height - 1; y > -1; y--)
        for (int x = 0; x < section.Width; x++)
        {
            token.ThrowIfCancellationRequested();

            var world = originTile + new Vector2i(x, y);

            if (!WorldGen.InWorld(world.X, world.Y))
                continue;

            Tile tile = Main.tile[world.X, world.Y];
            Tile copiedTile = section.Tiles[x, y];

            bool isSolidCopiedTile = Main.tileSolid[copiedTile.type] &&
                                     copiedTile.type != TileID.GolfTee &&
                                     copiedTile.type != TileID.GolfHole &&
                                     copiedTile.type != TileID.GolfCupFlag;
            if (isCopySolidTiles != isSolidCopiedTile)
                continue;

            bool isCopiedTileEmpty = !(copiedTile.active() || copiedTile.wall > 0);
            if (isCopiedTileEmpty && !isDestroyTiles)
                continue;

            tile.CopyFrom(copiedTile);
        }
    }

    public void PasteByTileManipulation(TileSection section, Vector2i originTile, bool isDestroyTiles, int tilePerSecond = -1) =>
        PasteByTileManipulationAsync(section, originTile, isDestroyTiles, tilePerSecond)
            .AsTask() // using ValueTask.GetAwaiter().GetResult() is UB
            .Wait();

    public Task PasteByTileManipulationInNewTask(TileSection section, Vector2i originTile, bool isDestroyTiles, int tilePerSecond = -1, CancellationToken token = default) =>
        Task.Run(async () => await PasteByTileManipulationAsync(section, originTile, isDestroyTiles, tilePerSecond, token), token);

    public async ValueTask PasteByTileManipulationAsync(TileSection section, Vector2i originTile, bool isDestroyTiles, int tilePerSecond = -1, CancellationToken token = default)
    {
        if (section.Tiles is null)
            return;

        async ValueTask LimiterAcquireAsync(RateLimiter? limiter)
        {
            if (limiter is null)
                return;
            using var lease = await limiter.AcquireAsync(cancellationToken: token);
            if (!lease.IsAcquired)
                throw new Exception(
                    $"[{nameof(TileSectionPaster)}] PasteByTileManipulationAsync failed to acquire limiter lease");
        }

        void LimiterDrain(RateLimiter? limiter)
        {
            if (limiter is null)
                return;

            while (true)
            {
                using var tryLease = limiter.AttemptAcquire(0);
                if (!tryLease.IsAcquired)
                    break;
                using var lease2 = limiter.AttemptAcquire();
            }
        }

        bool IsDisabledByTShock()
        {
            if (Main.netMode == 0)
                return false;

            return Main.LocalPlayer.FindBuffIndex(BuffID.Webbed) != -1;
        }

        // var limitOptions = tilePerSecond != -1
        //     ? new TokenBucketRateLimiterOptions
        //     {
        //         TokenLimit = tilePerSecond,
        //         QueueLimit = int.MaxValue,
        //         QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        //         AutoReplenishment = true,
        //         ReplenishmentPeriod = TimeSpan.FromSeconds(1) / tilePerSecond,
        //         TokensPerPeriod = 1
        //     }
        //     : null;
        var limitOptions = tilePerSecond != -1
            ? new TokenBucketRateLimiterOptions
            {
                TokenLimit = tilePerSecond,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
                ReplenishmentPeriod = tilePerSecond >= 60 ? TimeSpan.FromSeconds(1) / 60 : TimeSpan.FromSeconds(1) / tilePerSecond,
                TokensPerPeriod = tilePerSecond >= 60 ? tilePerSecond / 60 : 1
            }
            : null;
        await using var placeLimiter = tilePerSecond != -1
            ? new TokenBucketRateLimiter(limitOptions!)
            : null;
        await using var killLimiter = tilePerSecond != -1
            ? new TokenBucketRateLimiter(limitOptions!)
            : null;
        await using var paintLimiter = tilePerSecond != -1
            ? new TokenBucketRateLimiter(limitOptions!)
            : null;

        LimiterDrain(placeLimiter);
        LimiterDrain(killLimiter);
        LimiterDrain(paintLimiter);

        await using var pb = new PacketBuilder();

        TileData origWorldTileData = new();

        // pass one
        for (int y = section.Height - 1; y > -1; y--)
        for (int x = 0; x < section.Width; x++)
        {
            token.ThrowIfCancellationRequested();

            if (IsDisabledByTShock())
            {
                ClientLoader.Console.WriteError($"[{nameof(TileSectionPaster)}] Player disabled by TShock, wait until we are enabled");

                var backtrackCount = 0;
                while (true)
                {
                    if (backtrackCount >= tilePerSecond)
                        break;

                    // backtrack a tile
                    x--;
                    if (x < 0)
                    {
                        x = section.Width - 1;
                        y++;
                    }

                    if (y > section.Height - 1)
                    {
                        y = section.Height - 1;
                        break;
                    }

                    if (section.Tiles[x, y].active() || section.Tiles[x, y].wall > 0)
                        backtrackCount++;
                }

                while (IsDisabledByTShock())
                {
                    await Task.Delay(1000, token);
                }
                LimiterDrain(placeLimiter);
                LimiterDrain(killLimiter);
                LimiterDrain(paintLimiter);
            }

            var world = originTile + new Vector2i(x, y);
            if (!WorldGen.InWorld(world.X, world.Y))
                continue;

            Tile tile = Main.tile[world.X, world.Y];
            Tile copiedTile = section.Tiles[x, y];

            bool isCopiedTileEmpty = !(copiedTile.active() || copiedTile.wall > 0);
            if (isCopiedTileEmpty && !isDestroyTiles)
                continue;

            origWorldTileData.ClearEverything();
            origWorldTileData.CopyFrom(ref tile.RefData);
            tile.CopyFrom(copiedTile);

            // frame it!
            WorldGen.SquareTileFrame(world.X, world.Y);
            WorldGen.SquareWallFrame(world.X, world.Y);

            if (Main.netMode != 0)
            {
                // TODO: this is incomplete, replace is not implemented
                if (tile.active())
                {
                    // some special cases... F
                    if (TileUtil.TileDirtGrass[tile.type])
                    {
                        await LimiterAcquireAsync(placeLimiter);
                        pb.WritePlayerPlaceTile(x, y, TileID.Dirt, resetToNormal: false);
                    }
                    if (TileUtil.TileMudGrass[tile.type])
                    {
                        await LimiterAcquireAsync(placeLimiter);
                        pb.WritePlayerPlaceTile(x, y, TileID.Mud, resetToNormal: false);
                    }
                    if (TileUtil.TileAshGrass[tile.type])
                    {
                        await LimiterAcquireAsync(placeLimiter);
                        pb.WritePlayerPlaceTile(x, y, TileID.Ash, resetToNormal: false);
                    }
                    if (Main.tileMoss[tile.type])
                    {
                        await LimiterAcquireAsync(placeLimiter);
                        pb.WritePlayerPlaceTile(x, y, TileID.Stone, resetToNormal: false);
                    }

                    await LimiterAcquireAsync(placeLimiter);
                    pb.WritePlayerPlaceTile(world.X, world.Y, tile.type, resetToNormal: false);
                }
                else if (origWorldTileData.active())
                {
                    await LimiterAcquireAsync(killLimiter);
                    pb.WritePlayerKillTile(world.X, world.Y, resetToNormal: false);
                }

                if (tile.active() && (tile.slope() > 0 || tile.halfBrick()))
                {
                    var slopeData = tile.halfBrick() ? 1 : tile.slope();
                    pb.WritePlayerSlopeTile(world.X, world.Y, slopeData, resetToNormal: false);
                }

                if (tile.wall > 0)
                {
                    await LimiterAcquireAsync(placeLimiter);
                    pb.WritePlayerPlaceWall(world.X, world.Y, tile.wall, resetToNormal: false);
                }
                else if (origWorldTileData.wall > 0)
                {
                    await LimiterAcquireAsync(killLimiter);
                    pb.WritePlayerKillWall(world.X, world.Y, resetToNormal: false);
                }

                if (tile.liquid > 0)
                {
                    pb.WritePlayerUpdateLiquid(world.X, world.Y, tile.liquidType(), tile.liquid, resetToNormal: false);
                }

                if (tile.color() > 0)
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintTile(world.X, world.Y, tile.color(), false, resetToNormal: false);
                }

                if (tile.fullbrightBlock())
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintTile(world.X, world.Y, 1, true, resetToNormal: false);
                }
                if (tile.invisibleBlock())
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintTile(world.X, world.Y, 2, true, resetToNormal: false);
                }

                if (tile.wallColor() > 0)
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintWall(world.X, world.Y, tile.wallColor(), false, resetToNormal: false);
                }

                if (tile.fullbrightWall())
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintWall(world.X, world.Y, 1, true, resetToNormal: false);
                }
                if (tile.invisibleWall())
                {
                    await LimiterAcquireAsync(paintLimiter);
                    pb.WritePlayerPaintWall(world.X, world.Y, 2, true, resetToNormal: false);
                }

                if (tile.wire())
                {
                    pb.WritePlayerPlaceWire(world.X, world.Y, 1, resetToNormal: false);
                }
                else if (origWorldTileData.wire())
                {
                    pb.WritePlayerKillWire(world.X, world.Y, 1, resetToNormal: false);
                }

                if (tile.wire2())
                {
                    pb.WritePlayerPlaceWire(world.X, world.Y, 2, resetToNormal: false);
                }
                else if (origWorldTileData.wire2())
                {
                    pb.WritePlayerKillWire(world.X, world.Y, 2, resetToNormal: false);
                }

                if (tile.wire3())
                {
                    pb.WritePlayerPlaceWire(world.X, world.Y, 3, resetToNormal: false);
                }
                else if (origWorldTileData.wire3())
                {
                    pb.WritePlayerKillWire(world.X, world.Y, 3, resetToNormal: false);
                }

                if (tile.wire4())
                {
                    pb.WritePlayerPlaceWire(world.X, world.Y, 4, resetToNormal: false);
                }
                else if (origWorldTileData.wire4())
                {
                    pb.WritePlayerKillWire(world.X, world.Y, 4, resetToNormal: false);
                }

                if (tile.actuator())
                {
                    pb.WritePlayerPlaceActuator(world.X, world.Y, resetToNormal: false);
                }
                else if (origWorldTileData.actuator())
                {
                    pb.WritePlayerKillActuator(world.X, world.Y, resetToNormal: false);
                }

                if (tile.inActive())
                {
                    if (!tile.actuator())
                    {
                        pb.WritePlayerPlaceActuator(world.X, world.Y, resetToNormal: false);
                    }
                    pb.WritePlayerActuate(world.X, world.Y, resetToNormal: false);
                    if (!tile.actuator())
                    {
                        pb.WritePlayerKillActuator(world.X, world.Y, resetToNormal: false);
                    }
                }

                // reset to normal
                pb.WriteSyncEquipmentPacketNormal(0);
                pb.WritePlayerControlsPacketNormal();
                pb.Send();
                pb.Clear();
            }
        }

        // // force tshock to send back tilesquare
        // for (int y = section.Height - 1; y > -1; y--)
        // for (int x = 0; x < section.Width; x++)
        // {
        //     token.ThrowIfCancellationRequested();
        //
        //     var world = originTile + new Vector2i(x, y);
        //     if (!WorldGen.InWorld(world.X, world.Y))
        //         continue;
        //
        //     Tile copiedTile = section.Tiles[x, y];
        //
        //     bool isCopiedTileEmpty = !(copiedTile.active() || copiedTile.wall > 0);
        //     if (isCopiedTileEmpty && !isDestroyTiles)
        //         continue;
        //
        //     // frame it!
        //     WorldGen.SquareTileFrame(world.X, world.Y);
        //     WorldGen.SquareWallFrame(world.X, world.Y);
        //
        //     if (Main.netMode != 0)
        //     {
        //         if (copiedTile.active())
        //         {
        //             await LimiterAcquireAsync(killLimiter);
        //             pb.WritePlayerKillTile(world.X, world.Y, true, resetToNormal: false);
        //         }
        //
        //         if (copiedTile.wall > 0)
        //         {
        //             await LimiterAcquireAsync(killLimiter);
        //             pb.WritePlayerKillWall(world.X, world.Y, true, resetToNormal: false);
        //         }
        //
        //         // reset to normal
        //         pb.WriteSyncEquipmentPacketNormal(0);
        //         pb.WritePlayerControlsPacketNormal();
        //         pb.Send();
        //         pb.Clear();
        //     }
        // }

        // TODO: pass two
    }
}