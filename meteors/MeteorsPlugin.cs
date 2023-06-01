using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.meteors", "Meteors", "0.0.1")]
public class EasyAnimalsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.meteors");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.meteors v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(FoliageManager), "UpdateDataTileOvernightTrees")]
	class HarmonyPatch_FoliageManager_UpdateDataTileOvernightTrees {

		private static bool Prefix(Vector2Int position, SerializedDataTile dataTile, int scene, float seed, ref SceneSettings sceneSettings) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				//Vector3Int vector3Int = new Vector3Int(position.x, position.y, 0);
				//if (UnityEngine.Random.value < sceneSettings.treeRespawnRate * Mathf.PerlinNoise((float)vector3Int.x / 16f + seed / 3.13f, (float)vector3Int.y / 16f + seed) && !TileManager.Instance.HasTileOrFarmingTile(position, (byte)scene)) {
				//	GameManager.Instance.SetDecorationSubTile(new Vector3Int(vector3Int.x * 6, vector3Int.y * 6, vector3Int.z), ItemID.SuniteOreNode, scene, new byte[0], sendPlaceEvent: true, saveDecoration: true, animation: false, ignoreDataLayerPlacement: true, canDestroyDecorations: false, 0);
				//}
				return true;
			} catch (Exception e) {
				logger.LogError("FoliageManager.UpdateDataTileOvernightTrees.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}