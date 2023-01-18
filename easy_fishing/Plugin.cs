
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.easy_fishing", "Easy Fishing", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.easy_fishing");
	public static ManualLogSource logger;
	public static bool m_do_force_chance = false;
	public static bool m_chance_result = false;
	public static FishingRod m_fishing_rod = null;
	public static bool m_ready_for_fish = true;

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.easy_fishing v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Utilities), "Chance")]
	class HarmonyPatch_Utilities_Chance {

		private static bool Prefix(ref bool __result) {
			if (m_do_force_chance) {
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
			m_do_force_chance = (__instance.UseItem.Using && __instance.UseItem is FishingRod);
			m_chance_result = false;
		}
	}
	
	[HarmonyPatch(typeof(FishSpawnManager), "Start")]
	class HarmonyPatch_FishSpawnManager_Start {

		private static void Postfix(ref int ___spawnLimit) {
			FishSpawnManager.fishSpawnGlobalMultiplier = 50f;
			___spawnLimit = 50;
		}
	}

	[HarmonyPatch(typeof(FishingRod), "Awake")]
	class HarmonyPatch_FishingRod_Awake {

		private static void Postfix(ref FishingRod __instance) {
			m_fishing_rod = __instance;
		}
	}

	public static bool m_in_cast_bar = false;

	[HarmonyPatch(typeof(Profession), "GetNodeAmount")]
	class HarmonyPatch_Profession_GetNodeAmount {

		private static bool Prefix(string node, ref int __result) {
			// yes, this is hacky, but it's easier than messing with all
			// the reflection crap required to do it cleanly in FishingRod.Use1
			if (m_in_cast_bar && node == "Fishing3b") {
				__result = 20;
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

	/*
	[HarmonyPatch(typeof(FishingRod))]
	[HarmonyPatch("ReadyForFish", MethodType.Getter)]
	class HarmonyPatch_FishingRod_ReadyForFish_Getter {

		private static bool Prefix(ref bool __result) {
			__result = m_ready_for_fish;
			return false;
		}
	}

	[HarmonyPatch(typeof(FishingRod))]
	[HarmonyPatch("ReadyForFish", MethodType.Constructor)]
	class HarmonyPatch_FishingRod_ReadyForFish_Setter {

		private static bool Prefix(object[] __args, ref bool __result) {
			__result = m_ready_for_fish = (bool) __args[0];
			return false;
		}
	}
	*/

	/*
	[HarmonyPatch(typeof(FishingRod), "ReelInFish")]
	class HarmonyPatch_FishingRod_ReelInFish {

		private static bool Prefix(
			FishingRod __instance,
			ref float ____frameRate,
			ref bool ____fishing,
			ref SwingAnimation ____swingAnimation,
			ref Vector2Int ___pos,
			ref float ____actionDelay
		) {
			____frameRate = 1f;
			//m_ready_for_fish = false;
			__instance.GetType().GetTypeInfo().GetField("Reeling", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, true);
			____fishing = !____fishing;
			____swingAnimation = (____fishing ? SwingAnimation.VerticalSlash : SwingAnimation.Pull);
			Vector2Int mousePos = ___pos;
			DOVirtual.DelayedCall(0.5f, delegate {
				__instance.GetType().GetTypeInfo().GetMethod("Action", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {mousePos});
			}, ignoreTimeScale: false);
			__instance.GetType().GetTypeInfo().GetMethod("SendFishingState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {3});
			return false;
		}
	}
	*/

	[HarmonyPatch(typeof(Pickup), "SpawnInternal", new Type[] {
		typeof(float), typeof(float), typeof(float), typeof(Item), 
		typeof(int), typeof(bool), typeof(float), typeof(Pickup.BounceAnimation),
		typeof(float), typeof(float), typeof(int), typeof(short)})]
	class HarmonyPatch_Pickup_SpawnInternal {

		private static void Postfix(Pickup.BounceAnimation bounceAnimation, ref Pickup __result) {
			
		}
	}

	[HarmonyPatch(typeof(Bobber), "GenerateWinArea")]
	class HarmonyPatch_Bobber_GenerateWinArea {

		private static bool Prefix(ref Bobber __instance, ref FishingMiniGame miniGame) {
			miniGame.winAreaSize *= 50f;
			miniGame.barMovementSpeed = 0.1f;
			miniGame.sweetSpots[0].sweetSpotSize *= 50f;
			return true;
		}
	}
}