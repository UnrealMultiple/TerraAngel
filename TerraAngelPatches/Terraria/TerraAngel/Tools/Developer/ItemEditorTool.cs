namespace TerraAngel.Tools.Developer;

public class ItemEditorTool : Tool
{
    private static Item HeldItem => Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem];

    public override string Name => GetString("Item Editor");

    public string[]? ItemPrefixes;
    
    public string[]? BuffNames;

    public override void DrawUI(ImGuiIOPtr io)
    {
        if (HeldItem.stack == 0)
        {
            ImGui.Text(GetString("Hold an item to modify!!!"));
            return;
        }
        ImGuiUtil.ItemButton(HeldItem, "InspectorItem", new Vector2(32f));
        ImGui.SameLine();
        ImGui.Text(HeldItem.Name);

        if (ImGui.BeginTable("ItemTable", 2))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150.0f);
            ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthStretch);

            // WEAPONS --------------------------------------------------------


            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("WEAPONS"));
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Prefix: "));
            int prefixIndex = HeldItem.prefix;
            ItemPrefixes ??= GetItemPrefixes();
            ImGui.TableNextColumn();
            if (ImGui.Combo("##Prefix", ref prefixIndex, ItemPrefixes, ItemPrefixes.Length))
            {
                HeldItem.prefix = (byte)prefixIndex;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Damage: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemDamage", ref HeldItem.damage);
            
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Crit Chance: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemCritChance", ref HeldItem.crit);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Armor Penetration: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemArmorPenetration", ref HeldItem.armorPenetration);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Knockback: "));
            ImGui.TableNextColumn();
            ImGui.InputFloat("##ItemKnockback", ref HeldItem.knockBack);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Size: "));
            ImGui.TableNextColumn();
            ImGui.InputFloat("##ItemSize", ref HeldItem.scale);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Projectile: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemProjectile", ref HeldItem.shoot);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Projectile Speed: "));
            ImGui.TableNextColumn();
            ImGui.InputFloat("##ItemProjectileSpeed", ref HeldItem.shootSpeed);

            // TOOLS ----------------------------------------------------------
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("TOOLS"));
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Usetime: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemUseTime", ref HeldItem.useTime);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Animation Speed: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemAnimationSpeed", ref HeldItem.useAnimation);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Pick Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemPickPower", ref HeldItem.pick);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Axe Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemAxePower", ref HeldItem.axe);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Hammer Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemHammerPower", ref HeldItem.hammer);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Extra Tile Range: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemExtraTileRange", ref HeldItem.tileBoost);

            // WEARABLES ------------------------------------------------------
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("WEARABLES"));
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Defense: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemDefense", ref HeldItem.defense);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Life Regen: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemLifeRegen", ref HeldItem.lifeRegen);


            // CONSUMABLES ----------------------------------------------------
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("CONSUMABLES")); 
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Buff: "));
            int buffnameIndex = HeldItem.buffType;
            BuffNames ??= GetBuffNames();
            ImGui.TableNextColumn();
            if (ImGui.Combo("##Buff", ref buffnameIndex, BuffNames, BuffNames.Length))
            {
                HeldItem.buffType = (byte)buffnameIndex;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Buff Duration: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemBuffDuration", ref HeldItem.buffTime);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Heal Amount: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemHealAmount", ref HeldItem.healLife);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Mana Restore: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemManaRestore", ref HeldItem.healMana);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Mana Consumption: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemManaConsumption", ref HeldItem.mana);

            // FISHING --------------------------------------------------------
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("FISHING"));
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Pole Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemPolePower", ref HeldItem.fishingPole);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Bait Power: "));
            ImGui.TableNextColumn();
            ImGui.InputInt("##ItemBaitPower", ref HeldItem.bait);

            // GENERAL --------------------------------------------------------
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("GENERAL"));
            ImGui.TableNextColumn();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(GetString("  Stack: "));
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
            prefixes[i] = Lang.prefix[i].Value + $" ({i})";
        }
        prefixes[0] = GetString("None");
        return prefixes;
    }
    
    private static string[] GetBuffNames()
    {
        string[] buffnames = new string[BuffID.Count];
        for (var i = 0; i < BuffID.Count; i++)
        {
            buffnames[i] = Lang.GetBuffName(i) + $" ({i})";
        }
        buffnames[0] = GetString("None");
        return buffnames;
    }
}
