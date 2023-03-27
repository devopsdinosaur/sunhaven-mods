using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.craft_speed", "Craft Speed", "0.0.3")]
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
		"Cooking Pot", 
		"Crafting Table", 
		"Loom", 
		"Bakers Station", 
		"Fish Grill", 
		"Wizard Crafting Table", 
		"Anvil", 
		"Furnace", 
		"Farmers Table", 
		"Construction Table", 
		"Tile Maker", 
		"Painters Easel", 
		"Basic Furniture Table", 
		"Advance Furniture Table", 
		"Composter Table", 
		"Seed Maker", 
		"Ice Cream Cart", 
		"Oven", 
		"Sushi Table", 
		"Juicer", 
		"Mana Composter", 
		"Elven Furniture Table", 
		"Mana Anvil", 
		"Mana Infuser Table", 
		"Tea Kettle", 
		"Elven Loom", 
		"Elven Seed Maker", 
		"Elven Furnace", 
		"Recycling Machine", 
		"Monster Composter", 
		"Monster Furniture Table", 
		"Monster Anvil", 
		"Monster Sushi Table", 
		"Soda Machine", 
		"Monster Loom", 
		"Monster Seed Maker", 
		"Monster Furnace", 
		"Monster Juicer", 
		"Monster Crafting Table", 
		"Elven Crafting Table", 
		"Elven Juicer", 
		"Grinder", 
		"Jam Maker",
		"Farmers Crafting Table",
		"Alchemy Table",
		"Jewelry Table"
	};
	private static ConfigEntry<bool>[] m_table_enabled = new ConfigEntry<bool>[m_table_names.Length];

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.craft_speed v0.0.3 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_craft_speed = this.Config.Bind<float>("General", "Craft Speed Multiplier", 10f, "Speed multiplier for item crafting (float, 1 = game default (1.2 for humans) [note: this stomps the human 20% passive; should not affect anything else])");
		for (int index = 0; index < m_table_names.Length; index++) {
			m_table_enabled[index] = this.Config.Bind<bool>("General", m_table_names[index] + " Enabled", true, "If true then the '" + m_table_names[index] + "' table will use the craft speed multiplier; if false then it will use the game default speed.");
		}
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static void Postfix(CraftingTable __instance, ref float ___craftSpeedMultiplier) {
			if (!m_enabled.Value) {
				return;
			}
			string name = __instance.name.Replace("new_", "").Replace("(Clone)", "").Trim();
			string key = "";
			for (int index = 0; index < m_table_names.Length; index++) {
				key = m_table_names[index];
				if (key.Replace(" ", "") == name || key.ToLower().Replace(" ", "_") == name) {
					if (m_table_enabled[index].Value) {
						___craftSpeedMultiplier = m_craft_speed.Value;
					}
					return;
				}
			}
			logger.LogWarning("* unknown crafting table name '" + name + "'; this table will be ignored.  Please let devopsdinosaur know via email or Nexus PM.");
		}
	}
}