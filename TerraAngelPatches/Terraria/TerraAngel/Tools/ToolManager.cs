using System;
using System.Collections.Generic;

namespace TerraAngel.Tools;

public class ToolManager
{
    private static Dictionary<string, Tool> LoadedTools = new Dictionary<string, Tool>();
    private static List<Tool>[] ToolTabs;
    private static List<Tool> AllTools;

    static ToolManager()
    {
        ToolTabs = new List<Tool>[Enum.GetValues<ToolTabs>().Length];
        for (int i = 0; i < ToolTabs.Length; i++)
        {
            ToolTabs[i] = new List<Tool>(10);
        }

        AllTools = new List<Tool>(30);
    }

    public static T GetTool<T>() where T : Tool => (T)GetTool(typeof(T));

    public static void AddTool<T>() where T : Tool => AddTool(typeof(T));

    public static void RemoveTool<T>() where T : Tool => RemoveTool(typeof(T));

    private static string GetToolKey(Type type)
    {
        // Use a combination of module name and full type name to ensure uniqueness across assemblies
        return $"{type.Module.Name}:{type.FullName}";
    }

    public static Tool GetTool(Type type)
    {
        string key = GetToolKey(type);
        if (LoadedTools.TryGetValue(key, out Tool? tool))
        {
            return tool;
        }
        
        // Fallback: try to find by type full name only (for backward compatibility)
        foreach (var kvp in LoadedTools)
        {
            if (kvp.Value.GetType() == type)
            {
                return kvp.Value;
            }
        }
        
        throw new KeyNotFoundException($"Tool of type {type.FullName} is not registered.");
    }

    public static void AddTool(Type type)
    {
        string key = GetToolKey(type);
        
        // Check if already registered
        if (LoadedTools.ContainsKey(key))
        {
            return;
        }

        Tool cringe = (Tool)Activator.CreateInstance(type)!;
        ToolTabs[(int)cringe.Tab].Add(cringe);
        LoadedTools.Add(key, cringe);
        AllTools.Add(cringe);
    }

    public static void RemoveTool(Type type)
    {
        Tool cringe = GetTool(type);
        string key = GetToolKey(type);
        
        ToolTabs[(int)cringe.Tab].Remove(cringe);
        LoadedTools.Remove(key);
        AllTools.Remove(cringe);
    }

    public static List<Tool> GetToolsOfTab(ToolTabs tab)
    {
        return ToolTabs[(int)tab];
    }

    public static void SortTabs()
    {
        for (int i = 0; i < ToolTabs.Length; i++)
        {
            List<Tool> tab = ToolTabs[i];
            tab.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
    }

    public static void Update()
    {
        for (int i = 0; i < AllTools.Count; i++)
        {
            Tool tool = AllTools[i];
            tool.Update();
        }
    }

    public static void Clear()
    {
        LoadedTools.Clear();
        AllTools.Clear();
        Array.Clear(ToolTabs);

        ToolTabs = new List<Tool>[Enum.GetValues<ToolTabs>().Length];
        for (int i = 0; i < ToolTabs.Length; i++)
        {
            ToolTabs[i] = new List<Tool>(5);
        }

        AllTools = new List<Tool>(30);
    }
}
