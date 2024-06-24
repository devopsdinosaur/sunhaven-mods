using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using UnityEngine.UI;
using System;

[BepInPlugin("devopsdinosaur.sunhaven.no_more_keys", "No More Keys", "0.0.4")]
public class NoMoreKeysPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_more_keys");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_no_keys_for_chests;
	private static ConfigEntry<bool> m_no_keys_for_mine_doors;
	private static ConfigEntry<bool> m_no_keys_for_wilt;
	private static ConfigEntry<bool> m_no_tickets_for_gerald;
	private static ConfigEntry<bool> m_infinite_tries_combat_dungeon;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_no_keys_for_chests = this.Config.Bind<bool>("General", "No Keys for Chests", true, "If true then keys are not required for chests (will display image but won't check/use key)");
			m_no_keys_for_mine_doors = this.Config.Bind<bool>("General", "No Keys for Mine Doors", true, "If true then all doors in the Sun Haven mine will be open");
			m_no_keys_for_wilt = this.Config.Bind<bool>("General", "No Keys for Wilt", true, "If true then you cruelly trick the poor slow tree guy into thinking you're giving him keys (shame on you!)");
			m_no_tickets_for_gerald = this.Config.Bind<bool>("General", "No Tickets for Gerald", true, "If true then you give fake tickets to Gerald in Withergate");
			m_infinite_tries_combat_dungeon = this.Config.Bind<bool>("General", "Infinite Combat Dungeon Retries", false, "If true then you can do the combat dungeon as many times as you like in a single day");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.no_more_keys v0.0.4" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(HelpTooltips), "SendNotification")]
	class HarmonyPatch_HelpTooltips_SendNotification {

		private static bool Prefix(string title) {
			return (m_enabled.Value ? title != "Exiting Mines" : true);
		}
	}

	[HarmonyPatch(typeof(GameSave), "GetProgressBoolWorld")]
	class HarmonyPatch_GameSave_GetProgressBoolWorld {

		private static bool Prefix(string progressID, ref bool __result) {
			if (m_enabled.Value && m_no_keys_for_mine_doors.Value && progressID.StartsWith("minesUnlock") && !progressID.EndsWith("Temp")) {
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameSave), "GetProgressBoolCharacter")]
	class HarmonyPatch_GameSave_GetProgressBoolCharacter {

		private static bool Prefix(string progressID, ref bool __result) {
			if (m_enabled.Value && m_infinite_tries_combat_dungeon.Value && progressID == "CompletedDungeonForTheDay") {
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(WiltMinesCutscene), "CheckHasKey")]
	class HarmonyPatch_WiltMinesCutscene_CheckHasKey {

		private static bool Prefix(ref WiltMinesCutscene __instance, string optionText, int keyType, ref bool[] ___hasKeyType, ref string __result) {
			try {
				if (!m_enabled.Value || !m_no_keys_for_wilt.Value) {
					return true;
				}
				string item_string = "";
				___hasKeyType[keyType] = true;
				switch (keyType) {
				case 0:	item_string = "<color=\"green\">Adament</color> (small mine)"; break;
				case 1: item_string = "<color=\"purple\">Mithril</color> (medium mine)"; break;
				case 2: item_string = "<color=\"yellow\">Sunite</color> (large mine)"; break;
				}
				__result = "[ Jedi mind trick a fake ** " + item_string + " ** key for Wilt ]";
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_WiltMinesCutscene_CheckHasKey.Prefix - " + e);
			}
			return true;
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
			try {
				if (!m_enabled.Value || !m_no_keys_for_wilt.Value) {
					return true;
				}
				float num = UnityEngine.Random.Range(0f, 99f);
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
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_WiltMinesCutscene_DetermineMineSize.Prefix - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(OneTimeChest), "Awake")]
	class HarmonyPatch_OneTimeChest_Awake {

		private static bool Prefix(ref ItemData ___requiredKey, ref Image ___keyImage) {
			if (m_enabled.Value && m_no_keys_for_chests.Value) {
				___requiredKey = null;
				___keyImage = null;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GeraldMinesCutscene), "CheckHasKey")]
	class HarmonyPatch_GeraldMinesCutscene_CheckHasKey {

		private static bool Prefix(ref GeraldMinesCutscene __instance, string optionText, int keyType, ref bool[] ___hasKeyType, ref string __result) {
			try {
				if (!m_enabled.Value || !m_no_tickets_for_gerald.Value) {
					return true;
				}
				string item_string = "";
				___hasKeyType[keyType] = true;
				switch (keyType) {
				case 0: item_string = "<color=\"green\">Economy</color> (small mine)"; break;
				case 1: item_string = "<color=\"purple\">First Class</color> (medium mine)"; break;
				case 2: item_string = "<color=\"yellow\">Kingly</color> (large mine)"; break;
				}
				__result = "[ Jedi mind trick a fake ** " + item_string + " ** ticket for Gerald ]";
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_GeraldMinesCutscene_CheckHasKey.Prefix - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GeraldMinesCutscene), "DetermineMineSize")]
	class HarmonyPatch_GeraldMinesCutscene_DetermineMineSize {

		private static bool IsValueBetween(float p1, float p2, float m) {
			if (!(p1 < m) || !(m < p2)) {
				if (p2 < m) {
					return m < p1;
				}
				return false;
			}
			return true;
		}

		private static bool Prefix(ref GeraldMinesCutscene __instance, int keyType, ref string __result) {
			try {
				if (!m_enabled.Value || !m_no_tickets_for_gerald.Value) {
					return true;
				}
				float num = UnityEngine.Random.Range(0f, 99f);
				switch (keyType) {
				case 0:
					__result = ((num <= 49f) ? "Small" : (IsValueBetween(50f, 74f, num) ? "Medium" : (IsValueBetween(75f, 99f, num) ? "Large" : "Error")));
					break;
				case 1:
					__result = ((num <= 14f) ? "Small" : (IsValueBetween(15f, 64f, num) ? "Medium" : (IsValueBetween(65f, 99f, num) ? "Large" : "Error")));
					break;
				case 2:
					__result = ((num <= 19f) ? "Medium" : (IsValueBetween(20f, 99f, num) ? "Large" : "Error"));
					break;
				}
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_GeraldMinesCutscene_DetermineMineSize.Prefix - " + e);
			}
			return true;
		}
	}
}