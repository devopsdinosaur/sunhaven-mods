using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

[BepInPlugin("devopsdinosaur.sunhaven.self_portrait", "Self Portrait", "0.0.1")]
public class SoundManagerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.self_portrait");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<string> m_subdir;
	private static ConfigEntry<string> m_default_username;
	private static ConfigEntry<string> m_force_outfit;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_subdir = this.Config.Bind<string>("General", "Subfolder", "self_portrait", "Subfolder under 'plugins' in which per-user self portrait folders will be located.");
			m_default_username = this.Config.Bind<string>("General", "Default Username", "default", "Fallback self portrait directory to use if there is none for current user.");
			m_force_outfit = this.Config.Bind<string>("General", "Force Outfit", "", "Specify one of (Summer, Fall, Winter, Wedding, or Swimsuit) to override the game logic for portrait outfit selection for self and NPC (set to empty or invalid string to disable this setting).");
			this.m_harmony.PatchAll();
			logger.LogInfo("devopsdinosaur.sunhaven.self_portrait v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	class SelfBustController : MonoBehaviour {
		private static SelfBustController m_instance = null;
		public static SelfBustController Instance {
			get {
				return m_instance;
			}
		}
		enum PortraitKey {
			Normal,
			Summer,
			Fall,
			Winter,
			Wedding,
			Swimsuit,
			None
		};
		private bool m_is_loaded = false;
		private static Dictionary<PortraitKey, string> portrait_map = new Dictionary<PortraitKey, string>() {
			{PortraitKey.Normal, "Normal"},
			{PortraitKey.Summer, "Summer"},
			{PortraitKey.Fall, "Fall"},
			{PortraitKey.Winter, "Winter"},
			{PortraitKey.Wedding, "Wedding"},
			{PortraitKey.Swimsuit, "Swimsuit"}
		};
		private Dictionary<PortraitKey, Sprite> m_portrait_sprites = new Dictionary<PortraitKey, Sprite>();
		private GameObject m_bust = null;
		private RectTransform m_rect_transform = null;
		private Image m_image = null;
		private Dictionary<PortraitKey, Dictionary<string, List<Sprite>>> npc_emotes = new Dictionary<PortraitKey, Dictionary<string, List<Sprite>>>();
		private Dictionary<PortraitKey, Dictionary<string, AssetReferenceSprite[]>> npc_emotes2 = new Dictionary<PortraitKey, Dictionary<string, AssetReferenceSprite[]>>();

		private void initialize() {
			try {
				if (this.m_bust != null) {
					return;
				}
				Transform bust_offset = this.gameObject.transform.Find("BustOffset");
				this.m_bust = GameObject.Instantiate<GameObject>(bust_offset.gameObject, this.gameObject.transform).transform.GetChild(0).gameObject;
				this.m_bust.name = "SelfPortrait_Bust";
				this.m_rect_transform = this.m_bust.GetComponent<RectTransform>();
				this.m_image = this.m_bust.GetComponent<UnityEngine.UI.Image>();
			} catch (Exception e) {
				logger.LogError("** SelfBustController.initialize ERROR - " + e);
			}
		}

		public void load_images(string root_dir, string player_name) {
			try {
				if (m_instance == null) {
					m_instance = this;
					this.initialize();
				}
				this.m_is_loaded = false;
				string player_files_dir = Path.Combine(root_dir, player_name);
				logger.LogInfo($"Loading bust portrait files for player_name '{player_name}' from directory, '{player_files_dir}'.");
				if (!Directory.Exists(player_files_dir)) {
					logger.LogInfo("Directory does not exist; disabling self portraits for this player.");
					return;
				}
				Sprite fallback = null;
				foreach (KeyValuePair<PortraitKey, string> item in portrait_map) {
					string file_name = $"{player_name}_{item.Value}.png";
					string full_path = Path.Combine(player_files_dir, file_name);
					if (!File.Exists(full_path)) {
						logger.LogInfo($"'{file_name}' does not exist (fallback will be used).");
						this.m_portrait_sprites[item.Key] = null;
						continue;
					}
					Texture2D texture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, new TextureCreationFlags());
					texture.LoadImage(File.ReadAllBytes(full_path));
					this.m_portrait_sprites[item.Key] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					if (fallback == null) {
						logger.LogInfo($"{item.Value} portrait will be used as fallback.");
						fallback = this.m_portrait_sprites[item.Key];
					}
				}
				if (fallback == null) {
					logger.LogInfo("Directory contains no portrait files; disabling self portraits for this player.");
					return;
				}
				foreach (KeyValuePair<PortraitKey, string> item in portrait_map) {
					if (this.m_portrait_sprites[item.Key] == null) {
						logger.LogInfo($"Using fallback portrait for {item.Value}.");
						this.m_portrait_sprites[item.Key] = fallback;
					}
				}
				this.m_is_loaded = true;
			} catch (Exception e) {
				logger.LogError("** SelfBustController.load_images ERROR - " + e);
			}
		}

		public void show_self_portrait(bool isMarriageBust, bool isSwimsuitBust, bool hideName, bool isRefreshBust) {
			try {
				if (!m_enabled.Value || !this.m_is_loaded) {
					return;
				}
				this.m_image.gameObject.SetActive(!isRefreshBust);
				if (isMarriageBust) {
					this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Wedding];
				} else if (isSwimsuitBust) {
					this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Swimsuit];
				} else {
					switch (SingletonBehaviour<DayCycle>.Instance.Season) {
					case Season.Summer: this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Summer]; break;
					case Season.Fall: this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Fall]; break;
					case Season.Winter: this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Winter]; break;
					default: this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Normal]; break;
					}
				}
				this.m_rect_transform.anchoredPosition = new Vector2(-216, 0);
				this.m_rect_transform.sizeDelta = new Vector2(166, 199);
				this.m_image.gameObject.SetActive(true);
			} catch (Exception e) {
				logger.LogError("** SelfBustController.show_self_portrait ERROR - " + e);
			}
		}

		[HarmonyPatch(typeof(DialogueController), "Awake")]
		class HarmonyPatch_DialogueController_Awake {

			private static bool Prefix(DialogueController __instance, GameObject ____dialoguePanel) {
				try {
					____dialoguePanel.AddComponent<SelfBustController>().load_images("C:/tmp/textures/sunhaven/self_portrait_test", "devopsdinosaur");
					return true;
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_Awake.Postfix ERROR - " + e);
				}
				return true;
			}
		}

		private static PortraitKey get_force_portrait_key() {
			string val = m_force_outfit.Value;
			val = val.ToLower().Trim();
			if (string.IsNullOrEmpty(val)) {
				return PortraitKey.None;
			}
			val = Char.ToUpper(val[0]) + val.Substring(1);
			foreach (KeyValuePair<PortraitKey, string> item in portrait_map) {
				if (item.Value == val) {
					return item.Key;
				}
			}
			return PortraitKey.None;
		}

		[HarmonyPatch(typeof(DialogueController), "SetDialogueBustVisualsOptimized", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
		class HarmonyPatch_DialogueController_SetDialogueBustVisualsOptimized {

			private static bool Prefix(string name, bool small, ref bool isMarriageBust, ref bool isSwimsuitBust, bool hideName, bool isRefreshBust) {
				try {
					if (!m_enabled.Value || !SelfBustController.Instance.m_is_loaded) {
						return true;
					}
					PortraitKey key = get_force_portrait_key();
					if (key == PortraitKey.Wedding) {
						isMarriageBust = true;
					} else if (key == PortraitKey.Swimsuit) {
						isSwimsuitBust = true;
					}
					return true;
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_SetDialogueBustVisualsOptimized.Prefix ERROR - " + e);
				}
				return true;
			}

			private static void Postfix(string name, bool small, bool isMarriageBust, bool isSwimsuitBust, bool hideName, bool isRefreshBust) {
				try {
					SelfBustController.Instance.show_self_portrait(isMarriageBust, isSwimsuitBust, hideName, isRefreshBust);
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_SetDialogueBustVisualsOptimized.Postfix ERROR - " + e);
				}
			}
		}

		/*
		[HarmonyPatch(typeof(DayCycle), "Season", MethodType.Getter)]
		class HarmonyPatch_DayCycle_Season_Getter {

			private static bool Prefix(ref Season __result) {
				try {
					logger.LogInfo(new StackTrace(1, false).GetFrame(1).GetMethod().Name);
					__result = Season.Winter;
					return false;
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DayCycle_Season_Getter.Prefix ERROR - " + e);
				}
				return true;
			}
		}
		*/
		
		[HarmonyPatch(typeof(DialogueController), "SetInitialBust")]
		class HarmonyPatch_DialogueController_SetInitialBust {

			private static bool Prefix(
				ref bool isMarriageBust, 
				ref bool isSwimsuitBust, 
				string ___npcName, 
				Dictionary<string, List<Sprite>> ____npcWeddingEmotes, 
				Image ____bust, 
				Dictionary<string, List<Sprite>> ____npcSwimsuitEmotes,
				Dictionary<string, List<Sprite>> ____npcEmotes,
				Dictionary<string, List<Sprite>> ____npcSummerEmotes,
				Dictionary<string, List<Sprite>> ____npcFallEmotes,
				Dictionary<string, List<Sprite>> ____npcWinterEmotes
			) {
				try {
					if (!m_enabled.Value || !SelfBustController.Instance.m_is_loaded) {
						return true;
					}
					if (___npcName.IsNullOrWhiteSpace()) {
						return false;
					}
					List<Sprite> value = null;
					PortraitKey key = get_force_portrait_key();
					if (key == PortraitKey.Wedding) {
						isMarriageBust = true;
					} else if (key == PortraitKey.Swimsuit) {
						isSwimsuitBust = true;
					}
					if (isMarriageBust) {
						if (____npcWeddingEmotes.TryGetValue(___npcName, out value) && value != null) {
							____bust.sprite = value[0];
						}
						return false;
					}
					if (isSwimsuitBust) {
						if (____npcSwimsuitEmotes.TryGetValue(___npcName, out value) && value != null) {
							____bust.sprite = value[0];
						}
						return false;
					}
					Season season = SingletonBehaviour<DayCycle>.Instance.Season;
					if (season == Season.Spring || key == PortraitKey.Normal) {
						if (!____npcEmotes.TryGetValue(___npcName, out value)) {
							return false;
						}
					} else if (season == Season.Summer || key == PortraitKey.Summer) {
						if (!____npcSummerEmotes.TryGetValue(___npcName, out value)) {
							return false;
						}
					} else if (season == Season.Fall || key == PortraitKey.Fall) {
						if (!____npcFallEmotes.TryGetValue(___npcName, out value)) {
							return false;
						}
					} else if (season == Season.Winter || key == PortraitKey.Winter) {
						if (!____npcWinterEmotes.TryGetValue(___npcName, out value)) {
							return false;
						}
					}
					if (value != null) {
						____bust.sprite = value[0];
					}
					return false;
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_SetInitialBust.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(DialogueController), "GetInitialBust")]
		class HarmonyPatch_DialogueController_GetInitialBust {

			private static bool Prefix(
				ref AssetReferenceSprite __result,
				ref bool isMarriageBust,
				ref bool isSwimsuitBust,
				string ___npcName,
				Dictionary<string, AssetReferenceSprite[]> ____npcWeddingEmotes2,
				Dictionary<string, AssetReferenceSprite[]> ____npcSwimsuitEmotes2,
				Dictionary<string, AssetReferenceSprite[]> ____npcEmotes2,
				Dictionary<string, AssetReferenceSprite[]> ____npcSummerEmotes2,
				Dictionary<string, AssetReferenceSprite[]> ____npcFallEmotes2,
				Dictionary<string, AssetReferenceSprite[]> ____npcWinterEmotes2
			) {
				try {

					bool try_get_sprite(AssetReferenceSprite[] sprites, ref 

					if (!m_enabled.Value || !SelfBustController.Instance.m_is_loaded) {
						return true;
					}
					if (___npcName.IsNullOrWhiteSpace()) {
						return false;
					}
					AssetReferenceSprite[] value = null;
					PortraitKey key = get_force_portrait_key();
					if (key == PortraitKey.Wedding) {
						isMarriageBust = true;
					} else if (key == PortraitKey.Swimsuit) {
						isSwimsuitBust = true;
					}
					if (isMarriageBust) {
						if (!____npcWeddingEmotes2.TryGetValue(___npcName, out value) || value == null || value.Length == 0) {
							__result = null;
							return false;
						}
						__result = value[0];
						return false;
					}
					if (isSwimsuitBust) {
						if (!____npcSwimsuitEmotes2.TryGetValue(___npcName, out value) || value == null || value.Length == 0) {
							__result = null;
							return false;
						}
						__result = value[0];
						return false;
					}
					Season season = SingletonBehaviour<DayCycle>.Instance.Season;
					if (season == Season.Spring || key == PortraitKey.Normal) {
						if (!____npcEmotes2.TryGetValue(___npcName, out value) || value == null || value.Length == 0) {
							__result = null;
							return false;
						}
						__result = value[0];
						return false;
					}
					return false;
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_GetInitialBust.Prefix ERROR - " + e);
				}
				return true;
			}
		}
	}
}