using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using UnityEngine.UI;
using System.Text;


[BepInPlugin("devopsdinosaur.sunhaven.no_more_keys", "No More Keys", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_more_keys");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.no_more_keys v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(HelpTooltips), "SendNotification")]
	class HarmonyPatch_HelpTooltips_SendNotification {

		private static bool Prefix(string title) {
			return title != "Exiting Mines";
		}
	}

	[HarmonyPatch(typeof(GameSave), "GetProgressBoolWorld")]
	class HarmonyPatch_GameSave_GetProgressBoolWorld {

		private static bool Prefix(string progressID, ref bool __result) {
			if (progressID.StartsWith("minesUnlock") && !progressID.EndsWith("Temp")) {
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(WiltMinesCutscene), "CheckHasKey")]
	class HarmonyPatch_WiltMinesCutscene_CheckHasKey {

		private static bool Prefix(ref WiltMinesCutscene __instance, string optionText, int keyType, ref bool[] ___hasKeyType, ref string __result) {
			string item_string = "";
			___hasKeyType[keyType] = true;
			switch (keyType) {
			case 0:	item_string = "<color=\"green\">Adament</color> (small mine)"; break;
			case 1: item_string = "<color=\"purple\">Mithril</color> (medium mine)"; break;
			case 2: item_string = "<color=\"yellow\">Sunite</color> (large mine)"; break;
			}
			__result = "[ Jedi mind trick a fake ** " + item_string + " ** key for Wilt ]";
			return false;
		}
	}

	[HarmonyPatch(typeof(WiltMinesCutscene), "DetermineMineSize")]
	class HarmonyPatch_WiltMinesCutscene_DetermineMineSize {

		private static bool IsValueBetween(float p1, float p2, float m) {
			if (!(p1 < m) || !(m < p2)) {
				if (p2 < m) {
					return m < p1;
				}
				return false;
			}
			return true;
		}

		private static bool Prefix(ref WiltMinesCutscene __instance, int keyType, ref string __result) {
			float num = Random.Range(0f, 99f);
			switch (keyType) {
			case 0:
				__result = ((num <= 49f) ? "Small" : (IsValueBetween(50f, 74f, num) ? "Medium" : (IsValueBetween(75f, 99f, num) ? "Large" : "Error")));
				break;
			case 1:
				__result = ((num <= 14f) ? "Small" : (IsValueBetween(15f, 64f, num) ? "Medium" : (IsValueBetween(65f, 99f, num) ? "Large" : "Error")));
				break;
			default:
				__result = ((num <= 19f) ? "Medium" : (IsValueBetween(20f, 99f, num) ? "Large" : "Error"));
				break;
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(OneTimeChest), "Awake")]
	class HarmonyPatch_OneTimeChest_Awake {

		private static bool Prefix(ref ItemData ___requiredKey, ref Image ___keyImage) {
			___requiredKey = null;
			___keyImage = null;
			return true;
		}
	}
}