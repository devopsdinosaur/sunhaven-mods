
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Reflection;
using System;
using TMPro;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent) {
		Transform child;
		string indent_string = "";
		for (int counter = 0; counter < indent; counter++) {
			indent_string += " => ";
		}
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			logger.LogInfo(indent_string + child.gameObject.name);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			list_descendants(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_descendants(child, callback);
		}
		return true;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
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

	[HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result) {
			if (stat != StatType.Movespeed) {
				return true;
			}
			__result = 0.7f;
			if (GameSave.Exploration.GetNode("Exploration2a")) {
				__result += 0.02f + 0.02f * (float) GameSave.Exploration.GetNodeAmount("Exploration2a");
			}
			if (Player.Instance.Mounted && GameSave.Exploration.GetNode("Exploration8a")) {
				__result += 0.04f * (float) GameSave.Exploration.GetNodeAmount("Exploration8a");
			}
			if (GameSave.Exploration.GetNode("Exploration5a") && SingletonBehaviour<TileManager>.Instance.GetTileInfo(Player.Instance.Position) != 0) {
				__result += 0.05f + 0.05f * (float) GameSave.Exploration.GetNodeAmount("Exploration5a");
			}
			if (GameSave.Exploration.GetNode("Exploration6a") && Time.time < Player.Instance.lastPickupTime + 3.5f) {
				__result += 0.1f * (float) GameSave.Exploration.GetNodeAmount("Exploration6a");
			}
			if (Time.time < Player.Instance.lastPickaxeTime + 2.5f) {
				__result += Player.Instance.MiningStats.GetStat(StatType.MovementSpeedAfterRock);
			}
			return false;
		}
	}

}