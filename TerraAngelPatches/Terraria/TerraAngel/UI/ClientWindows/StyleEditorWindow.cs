using Microsoft.Xna.Framework.Input;

namespace TerraAngel.UI.ClientWindows;

public class StyleEditorWindow : ClientWindow
{
    public override bool DefaultEnabled => false;
    public override bool IsGlobalToggle => true;
    public override bool IsToggleable => true;
    public override string Title => GetString("Style Editor");
    public override Keys ToggleKey => ClientConfig.Settings.ToggleStyleEditor;

    public static string[] ColorNames = StringExtensions.EnumFancyNames<ImGuiCol>();

    public override void Draw(ImGuiIOPtr io)
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        RangeAccessor<Vector4> colors = style.Colors;

        ImGui.PushFont(ClientAssets.GetMonospaceFont(16f));

        bool open = IsEnabled;
        ImGui.Begin(GetString("Style Editor"), ref open);
        IsEnabled = open;


        if (ImGui.CollapsingHeader(GetString("Colors")))
        {
            for (int i = 0; i < colors.Count; i++)
            {
                ImGui.ColorEdit4(ColorNames[i], ref colors[i]);
            }
        }

        if (ImGui.CollapsingHeader(GetString("Window")))
        {
            ImGui.SliderFloat(GetString("Window Rounding"), ref style.WindowRounding, 0f, 10f);
            ImGui.SliderFloat(GetString("Window Border Size"), ref style.WindowBorderSize, 0f, 5f);
            ImGui.SliderFloat2(GetString("Window Padding"), ref style.WindowPadding, 0f, 10f);
            ImGui.SliderFloat2(GetString("Window Title Align"), ref style.WindowTitleAlign, 0f, 1f);
            ImGui.SliderFloat2(GetString("Window Minimum Size"), ref style.WindowMinSize, 100f, 100f);
            ImGui.SliderFloat2(GetString("Display Window Padding"), ref style.DisplayWindowPadding, 0f, 10f);
        }

        if (ImGui.CollapsingHeader(GetString("Spacing")))
        {
            ImGui.SliderFloat(GetString("Indent Spacing"), ref style.IndentSpacing, 0f, 10f);
            ImGui.SliderFloat2(GetString("Item Spacing"), ref style.ItemSpacing, 0f, 10f);
            ImGui.SliderFloat2(GetString("Item Inner Spacing"), ref style.ItemInnerSpacing, 0f, 10f);
            ImGui.SliderFloat2(GetString("Frame Border Padding"), ref style.FramePadding, 0f, 5f);
            ImGui.SliderFloat2(GetString("Display Safe Area Padding"), ref style.DisplaySafeAreaPadding, 0f, 10f);
        }

        if (ImGui.CollapsingHeader(GetString("Widgets")))
        {
            ImGui.SliderFloat(GetString("Frame Rounding"), ref style.FrameRounding, 0f, 10f);
            ImGui.SliderFloat(GetString("Grab Rounding"), ref style.GrabRounding, 0f, 10f);
            ImGui.SliderFloat(GetString("Child Border Rounding"), ref style.ChildRounding, 0f, 10f);
            ImGui.SliderFloat(GetString("Scrollbar Rounding"), ref style.ScrollbarRounding, 0f, 10f);
            ImGui.SliderFloat(GetString("Child Border Size"), ref style.ChildBorderSize, 0f, 5f);
            ImGui.SliderFloat(GetString("Frame Border Size"), ref style.FrameBorderSize, 0f, 5f);
            ImGui.SliderFloat(GetString("Grab Minimum Size"), ref style.GrabMinSize, 0f, 15f);
            ImGui.SliderFloat(GetString("Scrollbar Size"), ref style.ScrollbarSize, 0f, 10f);
            ImGui.SliderFloat2(GetString("Button Text Align"), ref style.ButtonTextAlign, 0f, 1f);
        }

        if (ImGui.CollapsingHeader(GetString("Misc")))
        {
            ImGui.Checkbox(GetString("Anti-Aliased Fill"), ref style.AntiAliasedFill);
            ImGui.Checkbox(GetString("Anti-Aliased Lines"), ref style.AntiAliasedLines);
            ImGui.Checkbox(GetString("Anti-Aliased Lines Use Texture"), ref style.AntiAliasedLinesUseTex);
            ImGui.SliderFloat(GetString("Circle Tessellation Max Error"), ref style.CircleTessellationMaxError, 0f, 1f);
            ImGui.SliderFloat(GetString("Curve Tessellation Tol"), ref style.CurveTessellationTol, 0f, 2f);
            ImGui.SliderFloat(GetString("Alpha"), ref style.Alpha, 0f, 1f);
            ImGui.SliderFloat(GetString("Disabled Alpha"), ref style.DisabledAlpha, 0f, 1f);
        }

        ImGui.End();

        ImGui.PopFont();
    }
}
