
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;
using System.Reflection;
using System.Diagnostics;


[BepInPlugin("devopsdinosaur.sunhaven.green_man", "Green Man", "0.0.2")]
public class GreenManPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.green_man");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_influence_radius;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_influence_radius = this.Config.Bind<int>("General", "Green Influence Radius", 2, "Radius of tiles around the player in which 'green' influence spreads (int, note that larger values could significantly increase computation time)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.green_man v0.0.2" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void be_the_green_man() {
		try {
			Vector2Int player_pos = new Vector2Int((int) Player.Instance.ExactPosition.x, (int) Player.Instance.ExactPosition.y);
			for (int y = player_pos.y - m_influence_radius.Value; y < player_pos.y + m_influence_radius.Value; y++) {
				for (int x = player_pos.x - m_influence_radius.Value; x <= player_pos.x + m_influence_radius.Value; x++) {
					Vector2Int pos = new Vector2Int(x, y);
					if (GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(x * 6, y * 6, 0), out Crop crop)) {
						if (!crop.CheckGrowth) {
							logger.LogInfo(crop);
							crop.GrowToMax();
							crop.data.stage = crop.SeedData.cropStages.Length - 1;
						}
						continue;
					} 
					if (GameManager.Instance.TryGetObjectSubTile<Tree>(new Vector3Int(x * 6, y * 6, 0), out Tree tree)) {
						List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float current_health = (float) tree.GetType().GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float max_health = (float) tree.GetType().GetProperty("MaxHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						if (sprites != null && current_health >= max_health) {
							tree.SetTreeStage(sprites.Count + 1);
						}
					}
				}
			}
		} catch (Exception e) {
			logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 0.1f;
		static float m_elapsed = CHECK_FREQUENCY;

		private static bool Prefix(ref Player __instance) {
			try {
				if (!m_enabled.Value || 
					(m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY || 
					GameManager.Instance == null || 
					TileManager.Instance == null ||
					Player.Instance == null
				) {
					return true;
				}
				m_elapsed = 0f;
				be_the_green_man();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}