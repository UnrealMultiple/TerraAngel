using Terraria.GameContent.Creative;

namespace TerraAngel.Tools.Developer;

public class TimeTool : Tool
{
    public override string Name => GetString("Time");
    
    public override ToolTabs Tab => ToolTabs.NewTab;
    
    private float _targetTime; // in hours
    private int _targetTimeRate = 1;
    private bool _isTimeFreeze;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.TextUnformatted(GetString("Time")); ImGui.SameLine();
        if (ImGui.SliderFloat("##Time", ref _targetTime, 0f, 24f, GetFormattedTime(_targetTime)))
        {
            SetTimeInHours(_targetTime);
        }
            
        if (Main.dayTime)
            ImGui.TextUnformatted(GetString($"Precise Time: {Main.time}"));
        else
            ImGui.TextUnformatted(GetString($"Precise Time: {Main.time} (Night)"));

        if (ImGui.Checkbox(GetString("Freeze Time"), ref _isTimeFreeze))
        {
            CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled = _isTimeFreeze;
        }
        
        ImGui.TextUnformatted(GetString("Time Rate")); ImGui.SameLine();
        if (ImGui.SliderInt("##TimeRate", ref _targetTimeRate, 1, 60, "%dx"))
        {
            CreativePowerManager.Instance.GetPower<CreativePowers.ModifyTimeRate>().TargetTimeRate = _targetTimeRate;
        }
        
        ImGui.TextUnformatted(GetString($"Day Rate: {Main.dayRate}x"));
    }

    public override void Update()
    {
        _targetTime = GetTimeInHours();
        _targetTimeRate = CreativePowerManager.Instance.GetPower<CreativePowers.ModifyTimeRate>().TargetTimeRate;
        _isTimeFreeze = CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled;
    }

    public static float GetTimeInHours()
    {
        var gameTime = Main.time;
        var isDay = Main.dayTime;
        float convertedTime;
        if (isDay)
        {
            convertedTime = (float)(gameTime / 60.0 / 60.0 + 4.5);
        }
        else
        {
            convertedTime = (float)(gameTime / 60.0 / 60.0 + 19.5);
        }

        while (convertedTime < 0f)
            convertedTime += 24f;
        convertedTime %= 24f;
        return convertedTime;
    }

    public static string GetFormattedTime(float time)
    {
        var hours = (int)time;
        var minutes = (int)((time - hours) * 60);
        return $"{hours:00}:{minutes:00}";
    }

    public static void SetTimeInHours(float hours)
    {
        hours -= 4.5f; // Adjust for Terraria's time starting at 4:30 AM
        while (hours < 0f)
            hours += 24f;
        hours %= 24f;
        if (hours < 15f) // Daytime
        {
            Main.SkipToTime((int)(hours * 60 * 60), true);
        }
        else // Nighttime
        {
            Main.SkipToTime((int)((hours - 15f) * 60 * 60), false);
        }
    }
}