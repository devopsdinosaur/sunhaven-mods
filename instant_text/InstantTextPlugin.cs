using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.instant_text", "Instant Text", "0.0.1")]
public class InstantTextPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.instant_text");
	public static ManualLogSource logger;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.instant_text v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(DialogueController), "Awake")]
	class HarmonyPatch_DialogueController_Awake {

		private static bool Prefix(ref float ____dialogueScrollSpeed) {
			____dialogueScrollSpeed = 99999f;
			return true;
		}
	}

}