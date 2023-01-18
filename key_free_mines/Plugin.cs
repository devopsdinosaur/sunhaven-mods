using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.key_free_mines", "Key-Free Mines", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.key_free_mines");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.key_free_mines v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(WiltMinesCutscene), "TalkingToWilt")]
	class HarmonyPatch_WiltMinesCutscene_TalkingToWilt {

		private static bool Prefix(ref WiltMinesCutscene __instance, ref IEnumerator __result) {
			___hasOpenAndCloseTime = false;
			___hasRelationshipRequirement = false;
		}
	}

}