using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.always_open", "Always Open", "0.0.2")]
public class AlwaysOpenPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.always_open");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_shops_enabled;
	private static ConfigEntry<bool> m_houses_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_shops_enabled = this.Config.Bind<bool>("General", "Shops Enabled", true, "If true then shops will always be open, otherwise default open/close time");
			m_houses_enabled = this.Config.Bind<bool>("General", "Houses Enabled", true, "If true then NPC houses will always be open, otherwise default open/close time based on relationship");
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.always_open v0.0.2 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(ScenePortalSpot), "Awake")]
	class HarmonyPatch_ScenePortalSpot_Awake {

		private static void Postfix(
			ref bool ___hasOpenAndCloseTime, 
			ref bool ___hasRelationshipRequirement,
			bool ____hasKnock
		) {
			try {
				if (!m_enabled.Value || (____hasKnock && !m_houses_enabled.Value) || (!____hasKnock && !m_shops_enabled.Value)) {
					return;
				}
				___hasOpenAndCloseTime = false;
				___hasRelationshipRequirement = false;
			} catch (Exception e) {
				logger.LogError("** ScenePortalSpot.Awake_Postfix ERROR - " + e);
			}
		}
	}

}