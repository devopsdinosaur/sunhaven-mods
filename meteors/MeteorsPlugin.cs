using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;


[BepInPlugin("devopsdinosaur.sunhaven.meteors", "Meteors", "0.0.1")]
public class EasyAnimalsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.meteors");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_spawn_chance;
	
	private static List<int> m_node_ids = new List<int>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_spawn_chance = this.Config.Bind<float>("General", "Meteor Spawn Chance", 0.0044f, "Float value between 0 (no chance) and infinity (higher == higher chance) of tree respawn in random tiles (this value is used as a multiplier with a perlin noise map for random node placement, default is game default for tree spawn == 0.0044f).");
			foreach (FieldInfo field_info in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static)) {
				if (field_info.IsLiteral && !field_info.IsInitOnly && field_info.Name.EndsWith("Node")) {
					m_node_ids.Add((int) field_info.GetRawConstantValue());
				}
			}
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.meteors v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(FoliageManager), "UpdateDataTileOvernightTrees")]
	class HarmonyPatch_FoliageManager_UpdateDataTileOvernightTrees {

		private static void Postfix(Vector2Int position, SerializedDataTile dataTile, int scene, float seed, ref SceneSettings sceneSettings) {
			try {
				if (!m_enabled.Value || sceneSettings.mapType != MapType.Farm) {
					return;
				}
				Vector3Int vector3Int = new Vector3Int(position.x, position.y, 0);
				if (UnityEngine.Random.value >= m_spawn_chance.Value * Mathf.PerlinNoise((float)vector3Int.x / 16f + seed / 3.13f, (float) vector3Int.y / 16f + seed) || TileManager.Instance.HasTileOrFarmingTile(position, (byte) scene)) {
					return;
				}
				GameManager.Instance.SetDecorationSubTile(
					new Vector3Int(vector3Int.x * 6, vector3Int.y * 6, vector3Int.z), 
					Utilities.RandomItem<int>(m_node_ids), 
					scene, 
					new byte[0], 
					sendPlaceEvent: true, 
					saveDecoration: true, 
					animation: false, 
					ignoreDataLayerPlacement: true, 
					canDestroyDecorations: false, 
					0
				);
				return;
			} catch (Exception e) {
				logger.LogError("FoliageManager.UpdateDataTileOvernightTrees.Postfix ERROR - " + e);
			}
		}
	}
}