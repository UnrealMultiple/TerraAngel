using System;
using Color = Microsoft.Xna.Framework.Color;

namespace TerraAngel.Utility;

public static class ColorUtil
{
    public static double[] RgbToLab(Color c)
    {
        double[] xyz = RgbToXyz(c);
        return XyzToLab(xyz);
    }

    public static double[] RgbToXyz(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;

        r = r > 0.04045 ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
        g = g > 0.04045 ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
        b = b > 0.04045 ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;

        return [
            r * 0.4124564 + g * 0.3575761 + b * 0.1804375,
            r * 0.2126729 + g * 0.7151522 + b * 0.0721750,
            r * 0.0193339 + g * 0.1191920 + b * 0.9503041
        ];
    }

    public static double[] XyzToLab(double[] xyz)
    {
        double x = xyz[0] / 0.95047, y = xyz[1], z = xyz[2] / 1.08883;

        double F(double t) => t > 0.008856 ? Math.Pow(t, 1.0 / 3.0) : 7.787 * t + 16.0 / 116.0;

        double fy = F(y);
        return [
            116 * fy - 16,
            500 * (F(x) - fy),
            200 * (fy - F(z))
        ];
    }

    public static double DeltaE(double[] lab1, double[] lab2)
    {
        double dL = lab1[0] - lab2[0], da = lab1[1] - lab2[1], db = lab1[2] - lab2[2];
        return Math.Sqrt(dL * dL + da * da + db * db);
    }

    public static double RgbDistance(Color a, Color b)
    {
        int dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }
}
