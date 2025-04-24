using System.Collections.Generic;

namespace TerraAngel.Tools.Developer;

public class ItemEditorTool : Tool
{
    public static readonly Vector2 ItemDrawSize = new Vector2(32, 32);
    public Item? heldItem => Main.LocalPlayer?.inventory[Main.LocalPlayer.selectedItem];

    public override string Name => GetString("Item Editor");

    public override ToolTabs Tab => base.Tab;

    public override void DrawUI(ImGuiIOPtr io)
    {
        if (heldItem.stack != 0)
            ImGuiUtil.ItemButton(heldItem, "InspectorItem", new Vector2(32f), true);
        ImGui.SameLine();
        ImGui.Text(heldItem.Name);
        ImGui.NewLine();
        if (heldItem.stack == 0)
        {
            ImGui.NewLine();
            ImGui.NewLine();
        }

        ImGui.Text("Forge: ");
        ImGui.SameLine();
        int prefixIndex = heldItem.prefix;
        if (ImGui.Combo("##Prefix", ref prefixIndex, ItemPrefixes.ToArray(), ItemPrefixes.ToArray().Length))
        {
            heldItem.prefix = (byte)prefixIndex;
        }

        ImGui.Text("Damage: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemDamage", ref heldItem.damage, 0, 100000);

        ImGui.Text("Usetime: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemUseTime", ref heldItem.useTime, 0, 100);

        ImGui.Text("Animation Speed: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemAnimationSpeed", ref heldItem.useAnimation, 0, 100);

        ImGui.Text("Size: ");
        ImGui.SameLine();
        ImGui.SliderFloat("##ItemSize", ref heldItem.scale, 0, 10);

        ImGui.Text("Projectile: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemProjectile", ref heldItem.shoot, 0, 1021);

        ImGui.Text("Projectile Speed: ");
        ImGui.SameLine();
        ImGui.SliderFloat("##ItemProjectileSpeed", ref heldItem.shootSpeed, 0f, 1000f);

        ImGui.Text("Pick Power: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemPickPower", ref heldItem.pick, 0, 10000);

        ImGui.Text("Axe Power: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemAxePower", ref heldItem.axe, 0, 10000);

        ImGui.Text("Hammer Power: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemHammerPower", ref heldItem.hammer, 0, 10000);

        ImGui.Text("Stack: ");
        ImGui.SameLine();
        ImGui.SliderInt("##ItemStack", ref heldItem.stack, 1, Item.CommonMaxStack);

        ImGui.Checkbox("Auto-Swing", ref heldItem.autoReuse);

        if (ImGui.Button("Restore Item to Default"))
        {
            int stack = heldItem.stack;
            byte prefix = heldItem.prefix;
            heldItem.SetDefaults(heldItem.type);
            heldItem.stack = stack;
            heldItem.prefix = prefix;
        }
    }
    private static readonly List<string> ItemPrefixes = new()
    {
        "Unmodified Item",
        "Large",
        "Massive",
        "Dangerous",
        "Savage",
        "Sharp",
        "Pointy",
        "Tiny",
        "Terrible",
        "Small",
        "Dull",
        "Unhappy",
        "Bulky",
        "Shameful",
        "Heavy",
        "Light",
        "Sighted",
        "Rapid",
        "Hasty",
        "Intimidating",
        "Deadly (Ranged weapons)",
        "Staunch",
        "Awful",
        "Lethargic",
        "Awkward",
        "Powerful",
        "Mystic",
        "Adept",
        "Masterful",
        "Inept",
        "Ignorant",
        "Deranged",
        "Intense",
        "Taboo",
        "Celestial",
        "Furious",
        "Keen",
        "Superior",
        "Forceful",
        "Broken",
        "Damaged",
        "Shoddy",
        "Quick",
        "Deadly",
        "Agile",
        "Nimble",
        "Murderous",
        "Slow",
        "Sluggish",
        "Lazy",
        "Annoying",
        "Nasty",
        "Manic",
        "Hurtful",
        "Strong",
        "Unpleasant",
        "Weak",
        "Ruthless",
        "Frenzying",
        "Godly",
        "Demonic",
        "Zealous",
        "Hard",
        "Guarding",
        "Armored",
        "Warding",
        "Arcane",
        "Precise",
        "Lucky",
        "Jagged",
        "Spiked",
        "Angry",
        "Menacing",
        "Brisk",
        "Fleeting",
        "Hasty",
        "Quick",
        "Wild",
        "Rash",
        "Intrepid",
        "Violent",
        "Legendary",
        "Unreal",
        "Mythical",
        "Legendary (Terrarian variant)"
    };
}
