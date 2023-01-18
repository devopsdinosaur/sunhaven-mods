
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ZeroFormatter;


[BepInPlugin("devopsdinosaur.sunhaven.no_more_watering", "No More Watering", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_more_watering");
	public static ManualLogSource logger;
	

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.no_more_watering v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = 0f;

		private static bool Prefix(ref Player __instance) {
			if ((m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
				return true;
			}
			m_elapsed = 0f;
			foreach (Vector2Int pos in TileManager.Instance.farmingData.Keys) {
				if (TileManager.Instance.IsWaterable(pos) && !TileManager.Instance.IsWatered(pos)) {
					TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "UpdateWaterOvernight")]
	class HarmonyPatch_GameManager_UpdateWaterOvernight {

		private static bool Prefix() {
			foreach (KeyValuePair<short, Dictionary<KeyTuple<ushort, ushort>, byte>> item in SingletonBehaviour<GameSave>.Instance.CurrentWorld.FarmingInfo.ToList()) {
				short key = item.Key;
				foreach (KeyValuePair<KeyTuple<ushort, ushort>, byte> item2 in item.Value.ToList()) {
					Vector2Int position = new Vector2Int(item2.Key.Item1, item2.Key.Item2);
					SingletonBehaviour<TileManager>.Instance.Water(position, key);
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(Crop), "SpawnPestToStealCrop")]
	class HarmonyPatch_Crop_SpawnPestToStealCrop {

		private static bool Prefix() {
			return false;
		}
	}
}