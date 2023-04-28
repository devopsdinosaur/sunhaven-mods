
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ZeroFormatter;
using System;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.no_more_watering", "No More Watering", "0.0.10")]
public class NoMoreWateringPlugin : BaseUnityPlugin {

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
	private static ConfigEntry<bool> m_fertilize_earth2;
	private static ConfigEntry<bool> m_fertilize_fire2;
	private static ConfigEntry<bool> m_hide_fertilizer_particles;
	private static ConfigEntry<bool> m_any_season_planting;
	private static ConfigEntry<bool> m_water_after_harvest;
	private static ConfigEntry<bool> m_weapons_harvest_crops;
	private static ConfigEntry<bool> m_auto_mana;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_water_overnight = this.Config.Bind<bool>("General", "Water Overnight", true, "If true then all world tiles will be watered overnight");
			m_water_during_day = this.Config.Bind<bool>("General", "Water During Day", true, "If true then tiles will gradually be watered as they are hoed and so on (i.e. a freshly hoed tile should display as wet almost immediately unless tiles were not watered overnight or this is the first time mod was loaded on the savegame [in this case it will take a bit to catch up if there are a lot of dry tiles])");
			m_water_after_harvest = this.Config.Bind<bool>("General", "Water Tile After Harvest", true, "If true then tiles will stay hoed and watered after crops are harvested");
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
			m_fertilize_earth2 = this.Config.Bind<bool>("General", "Fertilize Earth2", false, "If true then all crops will be automatically fertilized with Earth Fertilizer 2 (can be combined with Fertilize Fire2 [combined fertilizer will produce a white floating particle])");
			m_fertilize_fire2 = this.Config.Bind<bool>("General", "Fertilize Fire2", false, "If true then all crops will be automatically fertilized with Fire Fertilizer 2 (can be combined with Fertilize Earth2 [combined fertilizer will produce a white floating particle])");
			m_hide_fertilizer_particles = this.Config.Bind<bool>("General", "Hide Fertilizer Particles", false, "If true then fertilized crops will not display the floating particles (helps a little for performance and visibility with a lot of crops)");
			m_any_season_planting = this.Config.Bind<bool>("General", "Any Season Planting", false, "If true then all seeds can be planted in all seasons");
			m_weapons_harvest_crops = this.Config.Bind<bool>("General", "Weapons Harvest Crops", false, "If true then any weapon (sword / crossbow) hit will harvest a crop (note: axes and pickaxes have special code and will not work for this)");
			m_auto_mana = this.Config.Bind<bool>("General", "Auto Mana", true, "If true then crops will automatically get mana infusion if needed (during day and overnight)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.no_more_watering v0.0.10" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}	
	}

	private static void update_tile(Vector2Int pos, bool do_water) {
		if (!GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(pos.x * 6, pos.y * 6, 0), out Crop crop)) {
			if (do_water && TileManager.Instance.IsWaterable(pos) && !TileManager.Instance.IsWatered(pos)) {
				TileManager.Instance.Water(pos, ScenePortalManager.ActiveSceneIndex);
			}
			return;
		}
		if (do_water && !crop.data.watered) {
			if (crop.data.onFire) {
				crop.PutOutFire();
			}
			crop.Water();
			TileManager.Instance.SetFarmTileFromRPC(pos, ScenePortalManager.ActiveSceneIndex, FarmingTileInfo.Watered);
			TileManager.onFarm?.Invoke(pos, ScenePortalManager.ActiveSceneIndex, 3);
		}
		if (m_auto_mana.Value && crop._seedItem.manaInfusable && !crop.data.manaInfused) {
			logger.LogInfo("infusing " + crop);
			crop.data.manaInfused = true;
			crop.SaveMeta();
			crop.SendNewMeta(crop.meta);
			crop.GetType().GetMethod("SetInfusionParticles", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(crop, new object[] {});
		}
		if (m_fertilize_earth2.Value && crop.data.fertilizerType == FertilizerType.None) {
			crop.Fertilize(FertilizerType.Earth2);
		}
		if (m_fertilize_fire2.Value && crop.data.fertilizerType == FertilizerType.None) {
			crop.Fertilize(FertilizerType.Fire2);
		}
		if (crop.data.fertilizerType == FertilizerType.None) {
			return;
		}
		GameObject _fertilized = (GameObject) crop.GetType().GetTypeInfo().GetField("_fertilized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crop);
		ParticleSystem.MainModule main = _fertilized.GetComponent<ParticleSystem>().main;
		if (m_hide_fertilizer_particles.Value) {
			_fertilized.SetActive(false);
		} else if (m_fertilize_earth2.Value && m_fertilize_fire2.Value) {
			main.startColor = new Color(1f, 1f, 1f);
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;

		private static bool Prefix(ref Player __instance) {
			try {
				if (!m_enabled.Value || (m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY || TileManager.Instance == null) {
					return true;
				}
				m_elapsed = 0f;
				foreach (Vector2Int pos in new List<Vector2Int>(TileManager.Instance.farmingData.Keys)) {
					update_tile(pos, m_water_during_day.Value);
				}
			} catch (Exception e) {
				logger.LogError(e);
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
			if (m_any_season_planting.Value) {
				__instance._seedItem.seasons = new List<Season> {Season.Spring, Season.Summer, Season.Fall, Season.Winter};
			}
			if (m_auto_mana.Value && __instance._seedItem.manaInfusable && !__instance.data.manaInfused) {
				__instance.data.manaInfused = true;
				__instance.SaveMeta();
				__instance.SendNewMeta(__instance.meta);
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

	[HarmonyPatch(typeof(Crop), "GetDropAmount")]
	class HarmonyPatch_Crop_GetDropAmount {

		private static bool Prefix(ref Crop __instance, ref float __result) {
			if (!(m_enabled.Value && m_fertilize_earth2.Value)) {
				return true;
			}
			float num = UnityEngine.Random.Range(__instance._seedItem.dropRange.x, __instance._seedItem.dropRange.y) + 0.25f;
			if (__instance.data.scareCrowEffects != null) {
				num += (float) __instance.data.scareCrowEffects.Count((ScareCrowEffect effect) => effect == ScareCrowEffect.Spring) * 0.04f;
			}
			int nodeAmount = GameSave.Farming.GetNodeAmount("Farming8a");
			if (nodeAmount > 0 && __instance.data.fertilizerType != 0) {
				num += 0.04f * (float) nodeAmount;
			}
			if (GameSave.Farming.GetNodeAmount("Farming6a") > 0) {
				num += 0.02f + 0.02f * (float) nodeAmount;
			}
			__result = num + Player.Instance.GetStat(StatType.ExtraCropChance);
			return false;
		}
	}

	[HarmonyPatch(typeof(Crop), "AdjustedDaysToGrow")]
	class HarmonyPatch_Crop_AdjustedDaysToGrow {

		private static bool Prefix(int i, ref Crop __instance, ref int __result) {
			if (!(m_enabled.Value && m_fertilize_fire2.Value)) {
				return true;
			}
			float num = __instance._seedItem.cropStages[i + 1].daysToGrow;
			float num2 = 1f;
			if (__instance.data != null) {
				if (__instance.data.scareCrowEffects != null && __instance.data.scareCrowEffects.Count > 0) {
					num2 += (float) __instance.data.scareCrowEffects.Count((ScareCrowEffect effect) => effect == ScareCrowEffect.Fire) * 0.15f;
				}
				num /= num2 + 0.5f;
			}
			__result = Mathf.CeilToInt(num);
			return false;
		}
	}

	[HarmonyPatch(typeof(Seeds), "Use1")]
	class HarmonyPatch_Seeds_Use1 {

		private static bool Prefix(SeedData ____seedItem) {
			if (!(m_enabled.Value && m_any_season_planting.Value)) {
				return true;
			}
			____seedItem.seasons = new List<Season> {Season.Spring, Season.Summer, Season.Fall, Season.Winter};
			return true;
		}
	}

	[HarmonyPatch(typeof(Crop), "ReceiveDamage")]
	class HarmonyPatch_Crop_ReceiveDamage {

		private static bool Prefix(DamageInfo damageInfo) {
			try {
				if (!(m_enabled.Value && m_weapons_harvest_crops.Value)) {
					return true;
				}
				damageInfo.hitType = HitType.Scythe;
				return true;
			} catch (Exception e) {
				logger.LogError("** Crop_ReceiveDamage_Prefix ERROR - " + e);
			}
			return true;
		}

		private static void Postfix(Crop __instance, DamageHit __result) {
			try {
				if (!(m_enabled.Value && m_water_after_harvest.Value && __result != null && __result.hit && __result.damageTaken == 1f)) {
					return;
				}
				TileManager.Instance.SetFarmTileFromRPC(
					new Vector2Int(__instance.Position.x, __instance.Position.y) / 6,
					ScenePortalManager.ActiveSceneIndex, 
					FarmingTileInfo.Watered
				);
				return;
			} catch (Exception e) {
				logger.LogError("** Crop_ReceiveDamage_Postfix ERROR - " + e);
			}
		}
	}
}