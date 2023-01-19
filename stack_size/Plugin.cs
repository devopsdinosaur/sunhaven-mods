using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.stack_size", "Stack Size", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.stack_size");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_stack_size;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.stack_size v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_stack_size = this.Config.Bind<int>("General", "Stack Size", 9999, "Maximum stack size (int, not sure what the max the game can handle is, 9999 seems a safe bet)");
	}

	[HarmonyPatch(typeof(ItemDatabase), "ConstructDatabase", new[] { typeof(IList<ItemData>) })]
	class HarmonyPatch_ItemDatabase_ConstructDatabase {

		private static void Postfix() {
			if (m_enabled.Value) {
				foreach (int id in ItemDatabase.ids.Values) {
					ItemDatabase.items[id].stackSize = m_stack_size.Value;
				}
			}
		}
	}

}