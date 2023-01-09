
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ScenePortalSpot), "Awake")]
	class HarmonyPatch_ScenePortalSpot_Awake {

		private static void Postfix(ref ScenePortalSpot __instance, ref bool ___hasOpenAndCloseTime) {
			___hasOpenAndCloseTime = false;
		}
	}
}