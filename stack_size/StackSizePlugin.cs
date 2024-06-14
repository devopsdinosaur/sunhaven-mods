using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using PSS;
using System.Reflection;
using UnityEngine;

[BepInPlugin("devopsdinosaur.sunhaven.stack_size", "Stack Size", "0.0.3")]
public class StackSizePlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.stack_size");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_stack_size;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_stack_size = this.Config.Bind<int>("General", "Stack Size", 9999, "Maximum stack size (int, not sure what the max the game can handle is, 9999 seems a safe bet)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.stack_size v0.0.3" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(ItemData), "Awake")]
	class HarmonyPatch_ItemData_Awake {

		private static void Postfix(ItemData __instance) {
			if (m_enabled.Value) {
				__instance.stackSize = m_stack_size.Value;
			}
		}
	}
}