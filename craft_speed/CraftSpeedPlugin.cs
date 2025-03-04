using BepInEx;
using HarmonyLib;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Wish;

public static class PluginInfo {

    public const string TITLE = "Craft Speed";
    public const string NAME = "craft_speed";
    public const string SHORT_DESCRIPTION = "";
	public const string EXTRA_DETAILS = "This mod does not make any permanent changes to any items.  It simply modifies the stats on the item in memory for the duration of the game.  Removing the mod and restarting the game will revert the item to its default state.";

	public const string VERSION = "0.0.8";

    public const string AUTHOR = "devopsdinosaur";
    public const string GAME_TITLE = "Sun Haven";
    public const string GAME = "sunhaven";
    public const string GUID = AUTHOR + "." + GAME + "." + NAME;
    public const string REPO = "sunhaven-mods";

    public static Dictionary<string, string> to_dict() {
        Dictionary<string, string> info = new Dictionary<string, string>();
        foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
            info[field.Name.ToLower()] = (string) field.GetValue(null);
        }
        return info;
    }
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class TestingPlugin:DDPlugin {
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
        logger = this.Logger;
        try {
            this.m_plugin_info = PluginInfo.to_dict();
            DDPlugin.set_log_level(Settings.m_log_level.Value);
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static void Postfix(CraftingTable __instance, ref float ___craftSpeedMultiplier) {
			try {
				if (!Settings.m_enabled.Value) {
					return;
				}
				string name = __instance.name.Replace("new_", "").Replace("(Clone)", "").Trim();
				string key = "";
				for (int index = 0; index < Settings.m_table_names.Length; index++) {
					key = Settings.m_table_names[index];
					if (key.Replace(" ", "") == name || key.ToLower().Replace(" ", "_") == name) {
						if (Settings.m_table_enabled[index].Value) {
							___craftSpeedMultiplier = (Settings.m_table_speeds[index].Value > 0f ? Settings.m_table_speeds[index].Value : Settings.m_craft_speed.Value);
						}
						return;
					}
				}
				logger.LogWarning("* unknown crafting table name '" + name + "'; this table will be ignored.  Please let @devopsdinosaur on the official Sun Haven Discord game-mods channel.");
			} catch (Exception e) {
				logger.LogError("** CraftingTable.Awake_Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(CraftingMachine), "Awake")]
	class HarmonyPatch_CraftingMachine_Awake {

		private static void Postfix(CraftingMachine __instance, ref float ___craftSpeedMultiplier) {
			try {
				if (!Settings.m_enabled.Value) {
					return;
				}
				string name = __instance.name.Replace("new_", "").Replace("(Clone)", "").Trim();
				string key = "";
				for (int index = 0; index < Settings.m_table_names.Length; index++) {
					key = Settings.m_table_names[index];
					if (key.Replace(" ", "") == name || key.ToLower().Replace(" ", "_") == name) {
						if (Settings.m_table_enabled[index].Value) {
							___craftSpeedMultiplier = (Settings.m_table_speeds[index].Value > 0f ? Settings.m_table_speeds[index].Value : Settings.m_craft_speed.Value);
						}
						return;
					}
				}
				logger.LogWarning("* unknown crafting machine name '" + name + "'; this machine will be ignored.  Please let devopsdinosaur know via email or Nexus PM.");
			} catch (Exception e) {
				logger.LogError("** CraftingMachine.Awake_Postfix ERROR - " + e);
			}
		}
	}
}