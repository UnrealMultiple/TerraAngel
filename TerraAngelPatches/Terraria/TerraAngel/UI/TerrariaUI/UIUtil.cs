using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace TerraAngel.UI.TerrariaUI;

public static class UIUtil
{
    public static Color BGColor = new Color(0.10f, 0.10f, 0.10f);
    public static Color BGColor2 = Color.Lerp(Color.Black, BGColor, 0.52f);
    public static Color ButtonColor = Color.Lerp(new Color(0.19f, 0.19f, 0.19f), BGColor, 0.54f);
    public static Color ButtonHoveredColor = Color.Lerp(new Color(0.5f, 0.5f, 0.5f), BGColor, 0.54f);
    public static Color ButtonPressedColor = new Color(0.20f, 0.22f, 0.23f);
    public static Color ScrollbarBGColor = Color.Lerp(new Color(0.5f, 0.5f, 0.5f), BGColor, 0.54f);
    public static Color ScrollbarColor = Color.Lerp(new Color(0.34f, 0.34f, 0.34f), BGColor, 0.54f);

    public static T WithFadedMouseOver<T>(this T elem, Color origColor = default, Color hoverdColor = default, Color pressedColor = default) where T : UIPanel
    {
        if (origColor == default)
            origColor = ButtonColor * 0.98f;

        if (hoverdColor == default)
            hoverdColor = ButtonHoveredColor * 0.98f;

        if (pressedColor == default)
            pressedColor = ButtonPressedColor * 0.98f;

        elem.OnMouseOver += (evt, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            elem.BackgroundColor = hoverdColor;
        };
        elem.OnMouseOut += (evt, _) =>
        {
            elem.BackgroundColor = origColor;
        };
        elem.OnLeftMouseDown += (evt, _) =>
        {
            elem.BackgroundColor = pressedColor;
        };
        elem.OnLeftMouseUp += (evt, _) =>
        {
            elem.BackgroundColor = origColor;
        };
        return elem;
    }

    public static T WithPadding<T>(this T elem, float pixels) where T : UIElement
    {
        elem.SetPadding(pixels);
        return elem;
    }

    public static T WithPadding<T>(this T elem, string name, int id, Vector2? anchor = null, Vector2? offset = null) where T : UIElement
    {
        elem.SetSnapPoint(name, id, anchor, offset);
        return elem;
    }

    public static T WithView<T>(this T elem, float viewSize, float maxViewSize) where T : UIScrollbar
    {
        elem.SetView(viewSize, maxViewSize);
        return elem;
    }

    public static void BuildHorizontalSnapPoints(int offset, List<SnapPoint> snapPoints, List<string> pointNames)
    {
        for (var x = 0; x < pointNames.Count; x++)
        {
            var pt = UILinkPointNavigator.Points[offset + x];
            pt.Unlink();
            var sp = snapPoints.FirstOrDefault(p => p.Name == pointNames[x]);
            if (sp is null)
                continue;
            UILinkPointNavigator.SetPosition(pt.ID, sp.Position);
        }
        LinkHorizontalSnapPoints(offset, pointNames.Count, pointNames.Count, 1);
    }

    public static void BuildVerticalListSnapPoints(int offset, List<SnapPoint> snapPoints, List<string> pointNames, int height)
    {
        var stride = pointNames.Count;
        for (var x = 0; x < stride; x++)
        {
            var arr = new SnapPoint[height];
            foreach (var p in snapPoints.Where(p => p.Name == pointNames[x]))
            {
                arr[p.Id] = p;
            }
            for (var y = 0; y < height; y++)
            {
                var pt = UILinkPointNavigator.Points[offset + y * stride + x];
                pt.Unlink();
                UILinkPointNavigator.SetPosition(pt.ID, arr[y].Position);
            }
        }
        LinkHorizontalSnapPoints(offset, pointNames.Count, pointNames.Count, height);
        LinkVerticalSnapPoints(offset, pointNames.Count, pointNames.Count, height);
    }

    public static void LinkHorizontalSnapPoints(int offset, int stride, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pt = UILinkPointNavigator.Points[offset + y * stride + x];
                if (x > 0)
                    pt.Left = offset + y * stride + (x - 1);
                if (x < width - 1)
                    pt.Right = offset + y * stride + (x + 1);
            }
        }
    }

    public static void LinkVerticalSnapPoints(int offset, int stride, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pt = UILinkPointNavigator.Points[offset + y * stride + x];
                if (y > 0)
                    pt.Up = offset + (y - 1) * stride + x;
                if (y < height - 1)
                    pt.Down = offset + (y + 1) * stride + x;
            }
        }
    }

    public static void SetHorizontalSnapPoints(int offset, int width, Action<UILinkPoint> callback)
    {
        for (var x = 0; x < width; x++)
        {
            var pt = UILinkPointNavigator.Points[offset + x];
            callback(pt);
        }
    }
}