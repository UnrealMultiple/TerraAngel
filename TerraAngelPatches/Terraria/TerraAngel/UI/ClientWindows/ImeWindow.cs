using System.Text;
using ReLogic.Localization.IME;
using ReLogic.OS;

namespace TerraAngel.UI.ClientWindows;

public class ImeWindow : ClientWindow
{
    public override bool DefaultEnabled => true;
    public override bool IsToggleable => false;
    public override string Title => "Ime Window";
    public override bool IsGlobalToggle => false;

    public override void Draw(ImGuiIOPtr io)
    {
        var imeService = Platform.Get<IImeService>();
        if (!imeService.IsEnabled) // TODO: turning on IME depends on whether is inputting in ImGui textboxes
            imeService.Enable();
        if (!imeService.IsCandidateListVisible)
            return;
        
        ImGui.PushFont(ClientAssets.GetMonospaceFont(22.0f));
            
        var sb = new StringBuilder();
        for (uint i = 0; i < imeService.CandidateCount; i++)
        {
            sb.AppendFormat("{0}: {1} ", i + 1, imeService.GetCandidate(i));
        }
        ImGuiStylePtr style = ImGui.GetStyle();
        ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
        var str = sb.ToString();
        var size = ImGui.CalcTextSize(str);
        size.X += style.ItemSpacing.X;
        size.Y += style.ItemSpacing.Y * 3.0f;
        var origin = new Vector2(0.0f, io.DisplaySize.Y - size.Y);
        
        drawList.AddRectFilled(origin, origin + size, ImGui.GetColorU32(ImGuiCol.WindowBg));
        drawList.AddText(origin + style.ItemSpacing, Color.White.PackedValue, str);
            
        ImGui.PopFont();
    }
}