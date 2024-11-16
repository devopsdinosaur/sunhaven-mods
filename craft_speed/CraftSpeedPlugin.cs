using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.craft_speed", "Craft Speed", "0.0.7")]
public class CraftSpeedPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.craft_speed");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_craft_speed;

	// -- NOTES --
	//
	// * The CraftingTable instances do not retain (as far as I can find) a reference to their creating ItemID
	//   when instantiated in the world, so I need to map the game object's name.
	// * This is clunky as hell, but I don't feel like pulling the item data from assets to generate this info
	// * I decided not to use the "interact text" popup as the key, since that will be hacked by the end-user's 
	//   translate mod (or localization if they ever add it...)

	private static string[] m_table_names = {
		"Advance Furniture Table", 
		"Alchemy Table",
		"Anvil", 
		"Bakers Station",
		"Basic Furniture Table", 
		"BeeHiveBox",
		"Composter Table", 
		"Construction Table", 
		"Cooking Pot", 
		"Crafting Table", 
		"Elven Crafting Table", 
		"Elven Furnace",
		"Elven Furniture Table",
		"Elven Juicer", 
		"Elven Loom", 
		"Elven Seed Maker", 
		"Farmers Crafting Table",
		"Farmers Table", 
		"Fish Grill", 
		"Furnace", 
		"Furniture Table",
		"Grinder", 
		"Ice Cream Cart",
		"Industrial Cooking Stove",
		"Jam Maker",
		"Jewelry Table",
		"Juicer", 
		"Keg",
		"Loom", 
		"Mana Anvil", 
		"Mana Composter",
		"Monster Crafting Table", 
		"Mana Infuser Table", 
		"Mana Siphoner",
		"Monster Anvil", 
		"Monster Composter", 
		"Monster Furnace", 
		"Monster Furniture Table", 
		"Monster Juicer", 
		"Monster Loom", 
		"Monster Seed Maker", 
		"Monster Sushi Table", 
		"Nursery Crafting Table",
		"Oven", 
		"Painters Easel", 
		"Recycling Machine", 
		"Seed Maker", 
		"Soda Machine", 
		"Sushi Table", 
		"Tea Kettle", 
		"Tile Maker",
		"Withergate Anvil",
		"Withergate Furnace",
		"Wizard Crafting Table"
	};
	private static ConfigEntry<bool>[] m_table_enabled = new ConfigEntry<bool>[m_table_names.Length];
	private static ConfigEntry<float>[] m_table_speeds = new ConfigEntry<float>[m_table_names.Length];
	private static int m_beebox_index = -1;
	private static Dictionary<int, float> m_honey_times = new Dictionary<int, float>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_craft_speed = this.Config.Bind<float>("General", "Craft Speed Multiplier", 10f, "Speed multiplier for item crafting (float, 1 = game default (1.2 for humans) [note: this stomps the human 20% passive; should not affect anything else])");
			for (int index = 0; index < m_table_names.Length; index++) {
				if (m_table_names[index] == "BeeHiveBox") {
					m_beebox_index = index;
				}
				m_table_enabled[index] = this.Config.Bind<bool>("General", m_table_names[index] + " Enabled", true, "If true then the '" + m_table_names[index] + "' table will use the craft speed multiplier; if false then it will use the game default speed.");
				m_table_speeds[index] = this.Config.Bind<float>("General", m_table_names[index] + " Speed Multiplier", 0f, "If this value is non-zero and '" + m_table_names[index] + " Enabled' is true then this will be the craft speed multiplier used for the '" + m_table_names[index] + "' table (overriding the global one).  If this value is 0 then the global multiplier will be used (if table is enabled).");
			}
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo((object) "devopsdinosaur.sunhaven.craft_speed v0.0.7" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static void Postfix(CraftingTable __instance, ref float ___craftSpeedMultiplier) {
			try {
				if (!m_enabled.Value) {
					return;
				}
				string name = __instance.name.Replace("new_", "").Replace("(Clone)", "").Trim();
				string key = "";
				for (int index = 0; index < m_table_names.Length; index++) {
					key = m_table_names[index];
					if (key.Replace(" ", "") == name || key.ToLower().Replace(" ", "_") == name) {
						if (m_table_enabled[index].Value) {
							___craftSpeedMultiplier = (m_table_speeds[index].Value > 0f ? m_table_speeds[index].Value : m_craft_speed.Value);
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
				if (!m_enabled.Value) {
					return;
				}
				string name = __instance.name.Replace("new_", "").Replace("(Clone)", "").Trim();
				string key = "";
				for (int index = 0; index < m_table_names.Length; index++) {
					key = m_table_names[index];
					if (key.Replace(" ", "") == name || key.ToLower().Replace(" ", "_") == name) {
						if (m_table_enabled[index].Value) {
							___craftSpeedMultiplier = (m_table_speeds[index].Value > 0f ? m_table_speeds[index].Value : m_craft_speed.Value);
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