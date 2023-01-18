using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using System.Collections.Generic;


[BepInPlugin("devopsdinosaur.sunhaven.stack_size", "Stack Size", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.stack_size");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.stack_size v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ItemDatabase), "ConstructDatabase", new[] { typeof(IList<ItemData>) })]
	class HarmonyPatch_ItemDatabase_ConstructDatabase {

		private static void Postfix() {
			foreach (int id in ItemDatabase.ids.Values) {
				ItemDatabase.items[id].stackSize = 9999;
			}
		}
	}

}