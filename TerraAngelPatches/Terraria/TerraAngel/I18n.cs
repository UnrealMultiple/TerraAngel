global using static TerraAngel.I18n;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using GetText;
using Terraria.Localization;

namespace TerraAngel;

// ReSharper disable once InconsistentNaming
internal static class I18n
{
    private static Catalog? _c;
    private static Catalog C => _c ??= GetCatalog();
    public static CultureInfo CurrentCulture => Redirect(Language.ActiveCulture.CultureInfo);

    private static Catalog GetCatalog()
    {
        if (!ClientConfig.Settings.IsFollowGameTranslation)
            return new Catalog();

        var culture = Redirect(Language.ActiveCulture.CultureInfo);
        var moPath = $"{ClientLoader.AssetPath}/i18n/{culture.Name}.mo";
        return File.Exists(moPath) ? new Catalog(File.OpenRead(moPath), culture) : new Catalog();

        
    }
    
    private static CultureInfo Redirect(CultureInfo cultureInfo)
        => cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;
    
    // call me by using reflection
    // ReSharper disable once UnusedMember.Local
    private static void ReloadCatalog()
    {
        _c = GetCatalog();
    }
    

    public static string GetString(FormattableStringAdapter text)
    {
        return C.GetString(text);
    }

    public static string GetString(FormattableStringAdapter text, params object[] args)
    {
        return C.GetString(text, args);
    }

    public static string GetString(FormattableString text)
    {
        return C.GetString(text);
    }

    public static string GetPluralString(FormattableStringAdapter text, FormattableStringAdapter pluralText, long n)
    {
        return C.GetPluralString(text, pluralText, n);
    }

    public static string GetPluralString(FormattableString text, FormattableString pluralText, long n)
    {
        return C.GetPluralString(text, pluralText, n);
    }

    public static string GetPluralString(FormattableStringAdapter text, FormattableStringAdapter pluralText, long n,
        params object[] args)
    {
        return C.GetPluralString(text, pluralText, n, args);
    }

    public static string GetParticularString(string context, FormattableStringAdapter text)
    {
        return C.GetParticularString(context, text);
    }

    public static string GetParticularString(string context, FormattableString text)
    {
        return C.GetParticularString(context, text);
    }

    public static string GetParticularString(string context, FormattableStringAdapter text, params object[] args)
    {
        return C.GetParticularString(context, text, args);
    }

    public static string GetParticularPluralString(string context, FormattableStringAdapter text,
        FormattableStringAdapter pluralText, long n)
    {
        return C.GetParticularPluralString(context, text, pluralText, n);
    }

    public static string GetParticularPluralString(string context, FormattableString text,
        FormattableString pluralText, long n)
    {
        return C.GetParticularPluralString(context, text, pluralText, n);
    }

    public static string GetParticularPluralString(string context, FormattableStringAdapter text,
        FormattableStringAdapter pluralText, long n, params object[] args)
    {
        return C.GetParticularPluralString(context, text, pluralText, n, args);
    }
}