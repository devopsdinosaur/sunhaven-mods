using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using System;
using System.Reflection;

[BepInPlugin("devopsdinosaur.sunhaven.green_man", "Green Man", "0.0.6")]
public class GreenManPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.green_man");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_grow_crops;
	private static ConfigEntry<bool> m_grow_trees;
	private static ConfigEntry<int> m_influence_radius;
	private static ConfigEntry<string> m_hotkey_modifier;
	private static ConfigEntry<string> m_hotkey_enable_toggle;

	private const int HOTKEY_MODIFIER = 0;
	private const int HOTKEY_ENABLE_TOGGLE = 1;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

	private static bool m_temp_disable = false;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_hotkey_modifier = this.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_hotkey_enable_toggle = this.Config.Bind<string>("General", "Temporary Disable Toggle Hotkey", "End", "Comma-separated list of Unity Keycodes, any of which will toggle the temporary disabling of mod functionality.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
			m_grow_crops = this.Config.Bind<bool>("General", "Insta-grow Crops", true, "Set to false to disable crop insta-growth.");
			m_grow_trees = this.Config.Bind<bool>("General", "Insta-grow Trees", true, "Set to false to disable tree insta-growth.");
			m_influence_radius = this.Config.Bind<int>("General", "Green Influence Radius", 2, "Radius of tiles around the player in which 'green' influence spreads (int, note that larger values could significantly increase computation time)");
			m_hotkeys = new Dictionary<int, List<KeyCode>>();
			set_hotkey(m_hotkey_modifier.Value, HOTKEY_MODIFIER);
			set_hotkey(m_hotkey_enable_toggle.Value, HOTKEY_ENABLE_TOGGLE);
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.green_man v0.0.6" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	private static void set_hotkey(string keys_string, int key_index) {
		m_hotkeys[key_index] = new List<KeyCode>();
		foreach (string key in keys_string.Split(',')) {
			string trimmed_key = key.Trim();
			if (trimmed_key != "") {
				m_hotkeys[key_index].Add((KeyCode) System.Enum.Parse(typeof(KeyCode), trimmed_key));
			}
		}
	}

	private static bool is_modifier_hotkey_down() {
		if (m_hotkeys[HOTKEY_MODIFIER].Count == 0) {
			return true;
		}
		foreach (KeyCode key in m_hotkeys[HOTKEY_MODIFIER]) {
			if (Input.GetKey(key)) {
				return true;
			}
		}
		return false;
	}

	private static bool is_hotkey_down(int key_index) {
		foreach (KeyCode key in m_hotkeys[key_index]) {
			if (Input.GetKeyDown(key)) {
				return true;
			}
		}
		return false;
	}

	private static void notify(string message) {
		logger.LogInfo(message);
		NotificationStack.Instance.SendNotification(message);
	}

	private static void be_the_green_man() {

		void grow_forage_tree(ForageTree tree, List<Sprite> sprites) {
			tree.data = new ForageTreeSaveData {
				spot1 = false,
				spot2 = false,
				spot3 = false,
				golden = false,
				stage = sprites.Count + 1
			};
			for (int index = 0; index < 3; index++) {
				tree.spots[index].sprite = null;
			}
			MeshGenerator treeMesh = (MeshGenerator) tree.GetType().GetField("treeMesh", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
			treeMesh.sprite = sprites[Mathf.Clamp(tree.data.stage - 2, 0, sprites.Count - 1)];
			((GameObject) tree.GetType().GetField("_decals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree)).SetActive(tree.data.stage >= 4);
			treeMesh.SetDefault();
			tree.GetType().GetMethod("SetDecalsEnabledBySeason", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tree, new object[] {});
			tree.SaveMeta();
		}

		try {
			if (!(m_grow_crops.Value || m_grow_trees.Value)) {
				return;
			}

			/*
			void action_if_in_radius<T>(float radius, Action<T> callback) where T : Component {
				foreach (T component in Resources.FindObjectsOfTypeAll<T>()) {
					if (Vector3.Distance(Player.Instance.transform.position, component.transform.position) <= radius) {
						callback(component);
					}
				}
			}

			void grow_tree(Wish.Tree tree) {
				logger.LogInfo($"tree at {tree.transform.position}");
			}

			action_if_in_radius<Wish.Tree>(m_influence_radius.Value, grow_tree);
			*/

			return;

			/*
			Vector2Int player_pos = new Vector2Int((int) Player.Instance.ExactPosition.x, (int) Player.Instance.ExactPosition.y);
			for (int y = player_pos.y - m_influence_radius.Value; y < player_pos.y + m_influence_radius.Value; y++) {
				for (int x = player_pos.x - m_influence_radius.Value; x <= player_pos.x + m_influence_radius.Value; x++) {
					Vector3Int pos = new Vector3Int(x, y, 0);
					if (!GameManager.Instance.TryGetObjectSubTile<Component>(pos, out Component component)) {
						continue;
					}
					logger.LogInfo(component.GetType().ToString());
					foreach (Component _component in component.gameObject.GetComponents<Component>()) {
						logger.LogInfo($"{pos} - {_component.GetType().Name}");
					}
					continue;
					if (GameManager.Instance.TryGetObjectSubTile<Crop>(new Vector3Int(x * 6, y * 6, 0), out Crop crop)) {
						if (m_grow_crops.Value && !crop.CheckGrowth) {
							crop.GrowToMax();
							crop.data.stage = crop.SeedData.cropStages.Length - 1;
						}
						continue;
					} 
					if (m_grow_trees.Value && GameManager.Instance.TryGetObjectSubTile<Wish.Tree>(new Vector3Int(x * 6, y * 6, 0), out Wish.Tree tree)) {
						List<Sprite> sprites = (List<Sprite>) tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float current_health = (float) tree.GetType().GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						float max_health = (float) tree.GetType().GetProperty("MaxHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tree);
						logger.LogInfo($"{sprites} {current_health} {max_health}");
						if (sprites != null && current_health >= max_health) {
							tree.SetTreeStage(sprites.Count + 1);
						}
						continue;
					}
					if (m_grow_trees.Value && GameManager.Instance.TryGetObjectSubTile<ForageTree>(new Vector3Int(x * 6, y * 6, 0), out ForageTree forage_tree)) {
						List<Sprite> sprites = (List<Sprite>) forage_tree.GetType().GetProperty("TreeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(forage_tree);
						if (sprites != null && forage_tree.data.stage < sprites.Count + 1) {
							grow_forage_tree(forage_tree, sprites);
						}
						continue;
					}
				}
			}
			*/
		} catch (Exception e) {
			logger.LogError("** be_the_green_man ERROR - " + e);
		}
	}

	//class 
	
	[HarmonyPatch(typeof(Player), "Awake")]
	class HarmonyPatch_Player_Awake {

		private static void Postfix() {
			
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 0.1f;
		static float m_elapsed = CHECK_FREQUENCY;

		private static bool Prefix(ref Player __instance) {
			try {
				if (!m_enabled.Value || !__instance.IsOwner || GameManager.Instance == null || TileManager.Instance == null ||Player.Instance == null) {
					return true;
				}
				if (is_modifier_hotkey_down() && is_hotkey_down(HOTKEY_ENABLE_TOGGLE)) {
					m_temp_disable = !m_temp_disable;
					notify("[Green Man] Functionality " + (m_temp_disable ? "temporarily disabled." : "re-enabled."));
				}
				if ((m_elapsed += Time.fixedDeltaTime) < CHECK_FREQUENCY) {
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