using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.kickstarter", "Kickstarter", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.kickstarter");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.kickstarter v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_Player_Awake {

		private static bool Prefix() {
			GameSave.Instance.SetProgressBoolCharacter("BabyDragon", value: true);
			GameSave.Instance.SetProgressBoolCharacter("BabyTiger", value: true);
			GameSave.Instance.SetProgressBoolCharacter("WithergateMask1", value: true);
			GameSave.Instance.SetProgressBoolCharacter("SunArmor", value: true);
			GameSave.Instance.SetProgressBoolCharacter("GoldRecord", value: true);
			return true;
		}
	}
}