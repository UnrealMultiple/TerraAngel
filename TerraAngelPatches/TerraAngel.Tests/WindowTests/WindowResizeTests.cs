using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL3;
using TerraAngel.Timing;
using TerraAngel.Vectors;
using Terraria;

namespace TerraAngel.Tests.WindowTests;
public class WindowResizeTests
{
    [Test]
    public void TestResizeWindow()
    {
        const int ChangeToWidth = 459;
        const int ChangeToHeight = 689;

        ClientLoader.WindowManager!.Width = ChangeToWidth;
        ClientLoader.WindowManager!.Height = ChangeToHeight;

        Thread.Sleep(500);

        SDL.SDL_GetWindowSizeInPixels(Main.instance.Window.Handle, out int windowWidth, out int windowHeight);

        int graphicsWidth = Main.graphics.GraphicsDevice.Viewport.Width;
        int graphicsHeight = Main.graphics.GraphicsDevice.Viewport.Height;

        Assert.Multiple(() =>
        {
            // The window is placed at the requested physical size (within the compositor's rounding).
            Assert.That(windowWidth, Is.InRange(ChangeToWidth - 1, ChangeToWidth + 1));
            Assert.That(windowHeight, Is.InRange(ChangeToHeight - 1, ChangeToHeight + 1));

            // The backbuffer is clamped down to an even size but the window may stay one pixel bigger.
            Assert.That(graphicsWidth, Is.EqualTo(windowWidth & ~1));
            Assert.That(graphicsHeight, Is.EqualTo(windowHeight & ~1));

            Assert.That(ClientLoader.WindowManager.Width, Is.EqualTo(graphicsWidth));
            Assert.That(ClientLoader.WindowManager.Height, Is.EqualTo(graphicsHeight));
        });
    }

    [Test]
    public void TestInvalidSize()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ClientLoader.WindowManager!.Width = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => ClientLoader.WindowManager!.Height = -1);

            Assert.Throws<ArgumentOutOfRangeException>(() => ClientLoader.WindowManager!.Width = int.MaxValue / 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => ClientLoader.WindowManager!.Height = int.MaxValue / 2);
        });
    }
}
