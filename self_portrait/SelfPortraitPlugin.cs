using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private static ConfigEntry<string> m_hotkey_modifier;
    private static ConfigEntry<string> m_hotkey_reload;
	private static ConfigEntry<string> m_force_outfit;

    private const int HOTKEY_MODIFIER = 0;
    private const int HOTKEY_RELOAD = 1;
	private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

    private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_subdir = this.Config.Bind<string>("General", "Subfolder", "self_portrait", "Subfolder under 'plugins' in which per-user self portrait folders will be located.");
			m_default_username = this.Config.Bind<string>("General", "Default Username", "default", "Fallback self portrait directory to use if there is none for current user.");
            m_hotkey_modifier = this.Config.Bind<string>("General", "Hotkey Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
            m_hotkey_reload = this.Config.Bind<string>("General", "Reload Hotkey", "F2", "Comma-separated list of Unity Keycodes, any of which (in combination with modifier key [if not blank]) will reload portrait images.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
            m_force_outfit = this.Config.Bind<string>("General", "Force Outfit", "", "Specify one of (Summer, Fall, Winter, Wedding, or Swimsuit) to override the game logic for portrait outfit selection for self and NPC (set to empty or invalid string to disable this setting).");
            m_hotkeys = new Dictionary<int, List<KeyCode>>();
            set_hotkey(m_hotkey_modifier.Value, HOTKEY_MODIFIER);
            set_hotkey(m_hotkey_reload.Value, HOTKEY_RELOAD);
            this.m_harmony.PatchAll();
			logger.LogInfo("devopsdinosaur.sunhaven.self_portrait v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

    private static void debug_log(object text) {
        logger.LogInfo(text);
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
		private Dictionary<PortraitKey, Dictionary<string, List<Sprite>>> m_npc_emotes = new Dictionary<PortraitKey, Dictionary<string, List<Sprite>>>();
		private Dictionary<PortraitKey, Dictionary<string, AssetReferenceSprite[]>> m_npc_emotes2 = new Dictionary<PortraitKey, Dictionary<string, AssetReferenceSprite[]>>();

		private void initialize() {
			try {
				if (this.m_bust != null) {
					return;
				}
				Transform bust_offset = this.gameObject.transform.Find("BustOffset");
				this.m_bust = GameObject.Instantiate<GameObject>(bust_offset.gameObject, this.gameObject.transform).transform.GetChild(0).gameObject;
				this.m_bust.name = "SelfPortrait_Bust";
				this.m_rect_transform = this.m_bust.GetComponent<RectTransform>();
				this.m_image = this.m_bust.GetComponent<Image>();
                this.m_image.gameObject.SetActive(false);
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
					logger.LogInfo($"Directory does not exist; creating empty.");
					Directory.CreateDirectory(player_files_dir);
					string original_player_files_dir = player_files_dir;
                    player_files_dir = Path.Combine(root_dir, m_default_username.Value);
                    logger.LogInfo($"Attempting to load default (from mod config) bust portrait files from directory, '{player_files_dir}'.");
					if (!Directory.Exists(player_files_dir)) {
                        logger.LogInfo($"Directory does not exist; creating empty.");
                        Directory.CreateDirectory(player_files_dir);
						logger.LogInfo($"* Self portraits disabled.  Add images to '{original_player_files_dir}' and use the reload hotkey (see mod config) or reload the savegame.");
                        return;
					}
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
                    this.m_image.gameObject.SetActive(false);
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

			private static void Postfix(DialogueController __instance, GameObject ____dialoguePanel) {
				try {
					____dialoguePanel.AddComponent<SelfBustController>().load_images(
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), m_subdir.Value),
                        SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.characterName
                    );
					SelfBustController controller = SelfBustController.Instance;
					controller.m_npc_emotes[PortraitKey.Normal] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcEmotes");
                    controller.m_npc_emotes[PortraitKey.Summer] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcSummerEmotes");
                    controller.m_npc_emotes[PortraitKey.Fall] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcFallEmotes");
                    controller.m_npc_emotes[PortraitKey.Winter] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcWinterEmotes");
                    controller.m_npc_emotes[PortraitKey.Wedding] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcWeddingEmotes");
                    controller.m_npc_emotes[PortraitKey.Swimsuit] = (Dictionary<string, List<Sprite>>) ReflectionUtils.get_field_value(__instance, "_npcSwimsuitEmotes");
                    controller.m_npc_emotes2[PortraitKey.Normal] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcEmotes2");
                    controller.m_npc_emotes2[PortraitKey.Summer] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcSummerEmotes2");
                    controller.m_npc_emotes2[PortraitKey.Fall] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcFallEmotes2");
                    controller.m_npc_emotes2[PortraitKey.Winter] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcWinterEmotes2");
                    controller.m_npc_emotes2[PortraitKey.Wedding] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcWeddingEmotes2");
                    controller.m_npc_emotes2[PortraitKey.Swimsuit] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcSwimsuitEmotes2");
                } catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_Awake.Postfix ERROR - " + e);
				}
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

		private static PortraitKey get_force_season_key(PortraitKey key) {
			switch (key) {
			case PortraitKey.Summer:
			case PortraitKey.Fall:
			case PortraitKey.Winter:
				return key;
			}
			switch (SingletonBehaviour<DayCycle>.Instance.Season) {
			case Season.Summer: return PortraitKey.Summer;
			case Season.Fall: return PortraitKey.Fall;
			case Season.Winter: return PortraitKey.Winter;
			}
			return PortraitKey.Normal;
        }

		[HarmonyPatch(typeof(DialogueController), "SetDialogueBustVisualsOptimized", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
		class HarmonyPatch_DialogueController_SetDialogueBustVisualsOptimized {

			private static bool Prefix(string name, bool small, ref bool isMarriageBust, ref bool isSwimsuitBust, bool hideName, bool isRefreshBust) {
				try {
					if (!m_enabled.Value) {
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
		
		[HarmonyPatch(typeof(DialogueController), "SetInitialBust")]
		class HarmonyPatch_DialogueController_SetInitialBust {

			private static bool Prefix(ref bool isMarriageBust, ref bool isSwimsuitBust, string ___npcName, Image ____bust) {
				try {
					bool set_bust(PortraitKey _key) {
                        List<Sprite> value = null;
						if (SelfBustController.Instance.m_npc_emotes[_key].TryGetValue(___npcName, out value) && value != null) {
							____bust.sprite = value[0];
						}
						return false;
					}

					if (!m_enabled.Value) {
						return true;
					}
					if (___npcName.IsNullOrWhiteSpace()) {
						return false;
					}
					PortraitKey key = get_force_portrait_key();
					if (isMarriageBust || key == PortraitKey.Wedding) {
						isMarriageBust = true;
                        return set_bust(PortraitKey.Wedding);
                    }
					if (isSwimsuitBust || key == PortraitKey.Swimsuit) {
						isSwimsuitBust = true;
                        return set_bust(PortraitKey.Swimsuit);
                    }
					return set_bust(get_force_season_key(key));
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_SetInitialBust.Prefix ERROR - " + e);
				}
				return true;
			}
		}
		
		[HarmonyPatch(typeof(DialogueController), "GetInitialBust")]
		class HarmonyPatch_DialogueController_GetInitialBust {

            private static bool get_sprite(PortraitKey _key, string ___npcName, ref AssetReferenceSprite __result) {
                AssetReferenceSprite[] value;
                if (!SelfBustController.Instance.m_npc_emotes2[_key].TryGetValue(___npcName, out value) || value == null || value.Length == 0) {
                    __result = null;
                    return false;
                }
				__result = value[0];
                //debug_log($"get_sprite - key: {_key}, name: {___npcName}, result: {__result}");
                return false;
            }

            private static bool Prefix(ref AssetReferenceSprite __result, ref bool isMarriageBust, ref bool isSwimsuitBust, string ___npcName) {
				try {
					if (!m_enabled.Value) {
                        return true;
					}
					if (___npcName.IsNullOrWhiteSpace()) {
						return false;
					}
					PortraitKey key = get_force_portrait_key();
					if (isMarriageBust || key == PortraitKey.Wedding) {
						isMarriageBust = true;
                        return get_sprite(PortraitKey.Wedding, ___npcName, ref __result);
                    }
					if (isSwimsuitBust || key == PortraitKey.Swimsuit) {
						isSwimsuitBust = true;
                        return get_sprite(PortraitKey.Swimsuit, ___npcName, ref __result);
                    }
					return get_sprite(get_force_season_key(key), ___npcName, ref __result);
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_GetInitialBust.Prefix ERROR - " + e);
				}
				return true;
			}
		}

        [HarmonyPatch(typeof(NPCAI), "LoadSeasonAnimators")]
        class HarmonyPatch_NPCAI_LoadSeasonAnimators {

            private static bool Prefix(
				NPCAI __instance, 
				NPCAnimatorLoader ___seasonAnimatorLoader, 
				bool ___hasSeasonalSprites, 
				int ___currentAnimatorLayer,
                MeshGenerator ____meshGenerator,
                Direction ____facingDirection
            ) {
                try {
					if (!m_enabled.Value) {
						return true;
					}
                    __instance.GetSluggedAnimator();
                    if (!(___seasonAnimatorLoader != null)) {
                        return false;
                    }
                    int index = 0;
                    if (___hasSeasonalSprites) {
						switch (get_force_season_key(get_force_portrait_key())) {
						case PortraitKey.Summer: index = 1; break;
						case PortraitKey.Fall: index = 2; break;
						case PortraitKey.Winter: index = 3; break;
						}
					}
                    ___seasonAnimatorLoader.LoadAnimator(index, delegate {
                        __instance.DelayOneFrame(delegate {
                            if ((bool) __instance.animator && (bool) __instance.animator.runtimeAnimatorController && __instance.animator.gameObject.activeSelf && __instance.animator.gameObject.activeInHierarchy) {
                                for (int j = 0; j < __instance.animator.layerCount; j++) {
                                    __instance.animator.SetLayerWeight(j, (j == ___currentAnimatorLayer) ? 1 : 0);
                                }
                                __instance.DelayOneFrame(delegate {
                                    ____meshGenerator?.SetDefault();
                                });
                                __instance.animator.SetInteger("Direction", (int) ____facingDirection);
                            }
                        });
                    });
                    return false;
                } catch (Exception e) {
                    logger.LogError("** HarmonyPatch_NPCAI_LoadSeasonAnimators.Prefix ERROR - " + e);
                }
                return true;
            }
        }
    }
}