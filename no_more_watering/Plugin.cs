﻿
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_water_overnight;
	private static ConfigEntry<bool> m_water_during_day;
	private static ConfigEntry<bool> m_remove_pests;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.no_more_watering v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_water_overnight = this.Config.Bind<bool>("General", "Water Overnight", true, "If true then all world tiles will be watered overnight");
		m_water_during_day = this.Config.Bind<bool>("General", "Water During Day", true, "If true then tiles will gradually be watered as they are hoed and so on (i.e. a freshly hoed tile should display as wet almost immediately unless tiles were not watered overnight or this is the first time mod was loaded on the savegame [in this case it will take a bit to catch up if there are a lot of dry tiles])");
		m_remove_pests = this.Config.Bind<bool>("General", "No Farm Pests", true, "If true then farm pests will be disabled, i.e. no need for basic scarecrows");
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = 0f;

		private static bool Prefix(ref Player __instance) {
			if (!m_enabled.Value || !m_water_during_day.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
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
			if (!m_enabled.Value || !m_water_overnight.Value) {
				return true;
			}
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
			return !(m_enabled.Value && m_remove_pests.Value);
		}
	}
}