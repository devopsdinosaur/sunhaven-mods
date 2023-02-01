
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ZeroFormatter;


[BepInPlugin("devopsdinosaur.sunhaven.no_more_watering", "No More Watering", "0.0.3")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_more_watering");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_water_overnight;
	private static ConfigEntry<bool> m_water_during_day;
	private static ConfigEntry<bool> m_scarecrow;
	private static ConfigEntry<bool> m_totem_seasons;
	private static ConfigEntry<bool> m_totem_sunhaven;
	private static ConfigEntry<bool> m_totem_nelvari;
	private static ConfigEntry<bool> m_totem_withergate;
	private static ConfigEntry<bool> m_totem_exploration;
	private static ConfigEntry<bool> m_totem_farming;
	private static ConfigEntry<bool> m_totem_mining;
	private static ConfigEntry<bool> m_totem_combat;
	private static ConfigEntry<bool> m_totem_fishing;
	private static ConfigEntry<bool> m_totem_royal;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.no_more_watering v0.0.3 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_water_overnight = this.Config.Bind<bool>("General", "Water Overnight", true, "If true then all world tiles will be watered overnight");
		m_water_during_day = this.Config.Bind<bool>("General", "Water During Day", true, "If true then tiles will gradually be watered as they are hoed and so on (i.e. a freshly hoed tile should display as wet almost immediately unless tiles were not watered overnight or this is the first time mod was loaded on the savegame [in this case it will take a bit to catch up if there are a lot of dry tiles])");
		m_scarecrow = this.Config.Bind<bool>("General", "Everywhere Scarecrow", true, "If true then all crops will be protected from pests in all seasons");
		m_totem_seasons = this.Config.Bind<bool>("General", "Everywhere Totem: Seasons", true, "If true then all crops will be provided the effects of all seasonal totems (4% extra crop chance; immune to fire, entanglement, and freeze)");
		m_totem_sunhaven = this.Config.Bind<bool>("General", "Everywhere Totem: Sun Haven", true, "If true then Sun Haven crops can be planted anywhere");
		m_totem_nelvari = this.Config.Bind<bool>("General", "Everywhere Totem: Nelvari", true, "If true then Nelvari crops can be planted anywhere");
		m_totem_withergate = this.Config.Bind<bool>("General", "Everywhere Totem: Withergate", true, "If true then Withergate crops can be planted anywhere");
		m_totem_exploration = this.Config.Bind<bool>("General", "Everywhere Totem: Exploration", false, "If true then all crops will be covered by the Exploration totem aura (crops grant +2 experience for this skill)");
		m_totem_farming = this.Config.Bind<bool>("General", "Everywhere Totem: Farming", false, "If true then all crops will be covered by the Farming totem aura (crops grant +2 experience for this skill)");
		m_totem_mining = this.Config.Bind<bool>("General", "Everywhere Totem: Mining", false, "If true then all crops will be covered by the Mining totem aura (crops grant +2 experience for this skill)");
		m_totem_combat = this.Config.Bind<bool>("General", "Everywhere Totem: Combat", false, "If true then all crops will be covered by the Combat totem aura (crops grant +2 experience for this skill)");
		m_totem_fishing = this.Config.Bind<bool>("General", "Everywhere Totem: Fishing", false, "If true then all crops will be covered by the Fishing totem aura (crops grant +2 experience for this skill)");
		m_totem_royal = this.Config.Bind<bool>("General", "Everywhere Totem: Royal", false, "If true then all crops will be covered by the Royal totem aura (crops produce gold)");
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;

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
					if (TileManager.Instance.IsWaterable(position) && !TileManager.Instance.IsWatered(position)) {
						TileManager.Instance.Water(position, key);
					}
				}
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(Crop), "UpdateMetaOvernight")]
	class HarmonyPatch_Crop_UpdateMetaOvernight {

		private static bool Prefix(ref DecorationPositionData decorationData, ref Crop __instance) {
			if (!m_enabled.Value) {
				return true;
			}
			if (!Decoration.DeserializeMeta(decorationData.meta, ref __instance.data)) {
				__instance.data.scareCrowEffects = new List<ScareCrowEffect>();
				__instance.data.dayPlanted = DayCycle.Day;
				__instance.data.stage = 0;
			}
			if (m_scarecrow.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicSpring);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicSummer);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicFall);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicWinter);
			}
			if (m_totem_seasons.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Spring);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Summer);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Fall);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Winter);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Fire);
			}
			if (m_totem_sunhaven.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.SunHaven);
			}
			if (m_totem_nelvari.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Nelvari);
			}
			if (m_totem_withergate.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Withergate);
			}
			if (m_totem_royal.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Royal);
			}
			decorationData.meta = ZeroFormatterSerializer.Serialize(__instance.data);
			return true;
		}
	}

	[HarmonyPatch(typeof(Crop), "GetNearbyScarecrowEffects")]
	class HarmonyPatch_Crop_GetNearbyScarecrowEffects {

		private static bool Prefix(ref Crop __instance) {
			if (!m_enabled.Value) {
				return true;
			}
			if (m_scarecrow.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicSpring);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicSummer);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicFall);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.BasicWinter);
			}
			if (m_totem_seasons.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Spring);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Summer);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Fall);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Winter);
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Fire);
			}
			if (m_totem_royal.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Royal);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Crop), "AddScarecrowEffects")]
	class HarmonyPatch_Crop_AddScarecrowEffects {

		private static bool Prefix(ref Crop __instance) {
			if (!m_enabled.Value) {
				return true;
			}
			if (__instance.data.scareCrowEffects == null) {
				__instance.data.scareCrowEffects = new List<ScareCrowEffect>();
			}
			if (m_totem_exploration.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Exploration);
			}
			if (m_totem_farming.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Farming);
			}
			if (m_totem_mining.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Mining);
			}
			if (m_totem_combat.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Combat);
			}
			if (m_totem_fishing.Value) {
				__instance.data.scareCrowEffects.Add(ScareCrowEffect.Fishing);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Crop), "CanBePlacedBecauseScarecrowNearby")]
	class HarmonyPatch_Crop_CanBePlacedBecauseScarecrowNearby {

		private static bool Prefix(ref Crop __instance, ref bool __result) {
			if (!m_enabled.Value) {
				return true;
			}
			__result =
				(m_totem_sunhaven.Value && __instance.SeedData.farmType == FarmType.Normal) ||
				(m_totem_nelvari.Value && __instance.SeedData.farmType == FarmType.Nelvari) ||
				(m_totem_withergate.Value && __instance.SeedData.farmType == FarmType.Withergate);
			return !__result;
		}
	}
}