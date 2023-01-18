
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;


[BepInPlugin("devopsdinosaur.sunhaven.easy_fishing", "Easy Fishing", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.easy_fishing");
	public static ManualLogSource logger;
	public static bool m_do_force_chance = false;
	public static bool m_chance_result = false;
	public static FishingRod m_fishing_rod = null;
	public static bool m_ready_for_fish = true;
	public static bool m_in_cast_bar = false;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_no_more_nibbles;
	private static ConfigEntry<int> m_cast_speed;
	private static ConfigEntry<float> m_spawn_multiplier;
	private static ConfigEntry<int> m_spawn_limit;
	private static ConfigEntry<float> m_minigame_max_speed;
	private static ConfigEntry<float> m_minigame_winarea_size_multiplier;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.easy_fishing v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_no_more_nibbles = this.Config.Bind<bool>("General", "No More Nibbles", true, "If true then fish will bite every time.");
		m_cast_speed = this.Config.Bind<int>("General", "Cast Speed", 20, "Fishing rod progress bar cast speed (int, kind of an arbitrary value, 20 is a good number [note that the fishing perk for faster cast speed is ignored in favor of this, so you can use my other mod to reset that one away])");
		m_spawn_multiplier = this.Config.Bind<float>("General", "Spawn Multiplier", 50f, "Global fish spawn multiplier (float, set this to a high value (50+) to keep the pools filling up when fishing)");
		m_spawn_limit = this.Config.Bind<int>("General", "Spawn Limit", 100, "Limit to number of fish in current zone (int, set this to a high value (50+) to keep the pools filling up when fishing)");
		m_minigame_max_speed = this.Config.Bind<float>("General", "Minigame Max Speed", 0.5f, "Maximum speed for slider in minigame (float between 0-1f, set to a lower number to make it easier)");
		m_minigame_winarea_size_multiplier = this.Config.Bind<float>("General", "Minigame Win Area Size Multiplier", 2f, "Multiplier to increase/decrease the size of the win/perfect area (float, set this to a huge number and the entire bar will be the perfect zone)");
	}

	[HarmonyPatch(typeof(Utilities), "Chance")]
	class HarmonyPatch_Utilities_Chance {

		private static bool Prefix(ref bool __result) {
			if (m_enabled.Value && m_no_more_nibbles.Value && m_do_force_chance) {
				__result = m_chance_result;
				return false;
			}
			return true;
		}
	}
	
	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		private static void Postfix(ref Player __instance) {
			// if fishing then force the Utilities.Chance method to return false,
			// thereby bypassing the SmallBite() nibbles and always going for full Bite()
			if (m_enabled.Value) {
				m_do_force_chance = (__instance.UseItem.Using && __instance.UseItem is FishingRod);
				m_chance_result = false;
			}
		}
	}
	
	[HarmonyPatch(typeof(FishSpawnManager), "Start")]
	class HarmonyPatch_FishSpawnManager_Start {

		private static void Postfix(ref int ___spawnLimit) {
			if (m_enabled.Value) {
				FishSpawnManager.fishSpawnGlobalMultiplier = m_spawn_multiplier.Value;
				___spawnLimit = m_spawn_limit.Value;
			}
		}
	}

	[HarmonyPatch(typeof(FishingRod), "Awake")]
	class HarmonyPatch_FishingRod_Awake {

		private static void Postfix(ref FishingRod __instance) {
			m_fishing_rod = __instance;
		}
	}

	[HarmonyPatch(typeof(Profession), "GetNodeAmount")]
	class HarmonyPatch_Profession_GetNodeAmount {

		private static bool Prefix(string node, ref int __result) {
			// yes, this is hacky, but it's easier than messing with all
			// the reflection crap required to do it cleanly in FishingRod.Use1
			if (m_enabled.Value && m_in_cast_bar && node == "Fishing3b") {
				__result = m_cast_speed.Value;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(FishingRod), "Use1")]
	class HarmonyPatch_FishingRod_Use1 {

		private static bool Prefix() {
			m_in_cast_bar = true;
			return true;
		}

		private static void Postfix() {
			m_in_cast_bar = false;
		}
	}

	[HarmonyPatch(typeof(Bobber), "GenerateWinArea")]
	class HarmonyPatch_Bobber_GenerateWinArea {

		private static bool Prefix(ref Bobber __instance, ref FishingMiniGame miniGame) {
			if (m_enabled.Value) {
				miniGame.winAreaSize = Math.Min(1f, miniGame.winAreaSize * m_minigame_winarea_size_multiplier.Value);
				miniGame.barMovementSpeed = Math.Min(m_minigame_max_speed.Value, miniGame.barMovementSpeed);
				miniGame.sweetSpots[0].sweetSpotSize = Math.Min(1f, miniGame.sweetSpots[0].sweetSpotSize * m_minigame_winarea_size_multiplier.Value);
			}
			return true;
		}
	}
}