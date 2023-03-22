using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.respawn_timer", "Respawn Timer", "0.0.1")]
public class RespawnTimerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.respawn_timer");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_enemy_respawn_timer;
	
	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.respawn_timer v0.0.1 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		m_enemy_respawn_timer = this.Config.Bind<float>("General", "Enemy Respawn Timer", 36f, "Time in real-time seconds between enemy death and respawn (float, the game default is 18f)");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(EnemySpawnGroup), "SetupSpawnTime")]
	class HarmonyPatch_EnemySpawnGroup_SetupSpawnTime {

		private static bool Prefix(ref float ___spawnTime) {
			if (!m_enabled.Value) {
				return true;
			}
			___spawnTime = (
				SceneSettingsManager.Instance.GetCurrentSceneSettings && (SceneSettingsManager.Instance.GetCurrentSceneSettings.mapType == MapType.Mine || SceneSettingsManager.Instance.GetCurrentSceneSettings.mapType == MapType.Dungeon)
				? 99999f 
				: m_enemy_respawn_timer.Value
			);
			return false;
		}
	}
}