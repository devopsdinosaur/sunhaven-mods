using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.always_open", "Always Open", "0.0.1")]
public class AlwaysOpenPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.always_open");
	public static ManualLogSource logger;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.always_open v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ScenePortalSpot), "Awake")]
	class HarmonyPatch_ScenePortalSpot_Awake {

		private static void Postfix(ref ScenePortalSpot __instance, ref bool ___hasOpenAndCloseTime, ref bool ___hasRelationshipRequirement) {
			___hasOpenAndCloseTime = false;
			___hasRelationshipRequirement = false;
		}
	}

}