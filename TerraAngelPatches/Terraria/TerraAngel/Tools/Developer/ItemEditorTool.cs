namespace TerraAngel.Tools.Developer;

public class ItemEditorTool : Tool
{
    private static Item HeldItem => Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem];

    public override string Name => GetString("Item Editor");

    public string[]? ItemPrefixes;

    public override void DrawUI(ImGuiIOPtr io)
    {
        if (HeldItem.stack == 0)
        {
            ImGui.Text(GetString("Hold a item to modify!!!"));
            return;
        }
        ImGuiUtil.ItemButton(HeldItem, "InspectorItem", new Vector2(32f));
        ImGui.SameLine();
        ImGui.Text(HeldItem.Name);

        if (ImGui.BeginTable("ItemTable", 2))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150.0f);
            ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Prefix: "));
            int prefixIndex = HeldItem.prefix;
            ItemPrefixes ??= GetItemPrefixes();
            ImGui.TableNextColumn();
            if (ImGui.Combo("##Prefix", ref prefixIndex, ItemPrefixes, ItemPrefixes.Length))
            {
                HeldItem.prefix = (byte)prefixIndex;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Damage: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemDamage", ref HeldItem.damage);


            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Usetime: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemUseTime", ref HeldItem.useTime);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Animation Speed: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemAnimationSpeed", ref HeldItem.useAnimation);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Size: "));
            ImGui.TableNextColumn();
            ImGui.InputFloat("##ItemSize", ref HeldItem.scale);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Projectile: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemProjectile", ref HeldItem.shoot);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Projectile Speed: "));
            ImGui.TableNextColumn();
            ImGui.InputFloat("##ItemProjectileSpeed", ref HeldItem.shootSpeed);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Pick Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemPickPower", ref HeldItem.pick);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Axe Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemAxePower", ref HeldItem.axe);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Hammer Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemHammerPower", ref HeldItem.hammer);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("Stack: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemStack", ref HeldItem.stack);

            ImGui.EndTable();
        }


        ImGui.Checkbox(GetString("Auto-Swing"), ref HeldItem.autoReuse);

        if (ImGui.Button(GetString("Restore Item to Default")))
        {
            int stack = HeldItem.stack;
            byte prefix = HeldItem.prefix;
            HeldItem.SetDefaults(HeldItem.type);
            HeldItem.stack = stack;
            HeldItem.prefix = prefix;
        }
    }

    private static string[] GetItemPrefixes()
    {
        string[] prefixes = new string[PrefixID.Count];
        for (var i = 0; i < PrefixID.Count; i++)
        {
            prefixes[i] = Lang.prefix[i].Value;
        }
        prefixes[0] = GetString("None");
        return prefixes;

    }
}
