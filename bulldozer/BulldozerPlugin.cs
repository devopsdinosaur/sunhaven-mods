
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using System;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.bulldozer", "Bulldozer", "0.0.1")]
public class BulldozerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.bulldozer");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_harvest_crops;
	private static ConfigEntry<bool> m_harvest_trees;
	private static ConfigEntry<int> m_influence_radius;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_harvest_crops = this.Config.Bind<bool>("General", "Harvest Crops", true, "Set to false to disable crop harvest.");
			m_harvest_trees = this.Config.Bind<bool>("General", "Harvest Trees", true, "Set to false to disable tree harvest.");
			m_influence_radius = this.Config.Bind<int>("General", "Bulldoze Radius", 2, "Radius of tiles around the player to bulldoze (int, note that larger values could significantly increase computation time)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.bulldozer v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void be_the_bulldozer() {
		try {
			if (!(m_harvest_crops.Value || m_harvest_trees.Value)) {
				return;
			}
			Vector2Int player_pos = new Vector2Int((int) Player.Instance.ExactPosition.x, (int) Player.Instance.ExactPosition.y);
			for (int y = player_pos.y - m_influence_radius.Value; y < player_pos.y + m_influence_radius.Value; y++) {
				for (int x = player_pos.x - m_influence_radius.Value; x <= player_pos.x + m_influence_radius.Value; x++) {
					Vector2Int pos = new Vector2Int(x, y);
					if (GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(x * 6, y * 6, 0), out Crop crop)) {
						if (m_harvest_crops.Value && !crop.CheckGrowth) {
							logger.LogInfo(crop);
							crop.GrowToMax();
							crop.data.stage = crop.SeedData.cropStages.Length - 1;
						}
						continue;
					} 
					if (m_harvest_trees.Value && GameManager.Instance.TryGetObjectSubTile<Tree>(new Vector3Int(x * 6, y * 6, 0), out Tree tree)) {
						List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						if (sprites != null && tree.data.stage == sprites.Count + 1) {
							
						}
						continue;
					}
					if (m_harvest_trees.Value && GameManager.Instance.TryGetObjectSubTile<ForageTree>(new Vector3Int(x * 6, y * 6, 0), out ForageTree forage_tree)) {
						List<Sprite> sprites = (List<Sprite>) forage_tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(forage_tree);
						if (sprites != null && forage_tree.data.stage == sprites.Count + 1) {
							
						}
						continue;
					}
				}
			}
		} catch (Exception e) {
			logger.LogError("** be_the_bulldozer ERROR - " + e);
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
				be_the_bulldozer();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}