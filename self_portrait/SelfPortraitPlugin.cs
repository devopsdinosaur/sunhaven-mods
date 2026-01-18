using BepInEx;
using HarmonyLib;
using Wish;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

public static class PluginInfo {

	public const string TITLE = "Self Portrait";
	public const string NAME = "self_portrait";
	public const string SHORT_DESCRIPTION = "Add custom bust portraits for your player for all seasons, wedding, and swimsuit just by dropping PNG files in a folder.  Additionally, can use config option to force NPC outfits regardless of season.";

	public const string VERSION = "0.0.3";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "Sun Haven";
	public const string GAME = "sunhaven";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "sunhaven-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class SelfPortraitPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			Hotkeys.load();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}
	
	private static void notify(string message) {
		logger.LogInfo(message);
		NotificationStack.Instance.SendNotification(message);
	}

    public class SelfBustController : MonoBehaviour {
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
			Halloween,
			None
		};
		private bool m_is_loaded = false;
		private static Dictionary<PortraitKey, string> portrait_map = new Dictionary<PortraitKey, string>() {
			{PortraitKey.Normal, "Normal"},
			{PortraitKey.Summer, "Summer"},
			{PortraitKey.Fall, "Fall"},
			{PortraitKey.Winter, "Winter"},
			{PortraitKey.Wedding, "Wedding"},
			{PortraitKey.Swimsuit, "Swimsuit"},
			{PortraitKey.Halloween, "Halloween"}
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

		public void load_images(bool do_notify = false) {
			string result = "";
			try {
				if (m_instance == null) {
					m_instance = this;
					this.initialize();
				}
				this.m_is_loaded = false;
				string root_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value);
				foreach (string player_name in new string[] {SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.characterName,  Settings.m_default_username.Value}) {
					string player_files_dir = Path.Combine(root_dir, player_name);
					result = $"Images loaded from '{player_files_dir}'.";
					logger.LogInfo($"Loading bust portrait files from directory, '{player_files_dir}'.");
					if (!Directory.Exists(player_files_dir)) {
						logger.LogInfo($"Directory does not exist; creating empty.");
						Directory.CreateDirectory(player_files_dir);
						continue;
					}
					Sprite fallback = null;
					foreach (KeyValuePair<PortraitKey, string> item in portrait_map) {
						string file_name = $"{player_name}_{item.Value}.png";
						string full_path = Path.Combine(player_files_dir, file_name);
						if (!File.Exists(full_path)) {
							logger.LogInfo($"'{file_name}' does not exist.");
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
						logger.LogInfo("Directory contains no portrait files.");
						continue;
					}
					foreach (KeyValuePair<PortraitKey, string> item in portrait_map) {
						if (this.m_portrait_sprites[item.Key] == null) {
							logger.LogInfo($"Using fallback portrait for {item.Value}.");
							this.m_portrait_sprites[item.Key] = fallback;
						}
					}
					this.m_is_loaded = true;
					return;
				}
				if (!this.m_is_loaded) {
					result = "No valid portrait files found.";
				}
			} catch (Exception e) {
				logger.LogError("** SelfBustController.load_images ERROR - " + e);
			} finally {
				if (do_notify) {
					notify("[Self Portrait] " + result);
				}
			}
		}

		public void show_self_portrait(bool isMarriageBust, bool isSwimsuitBust, bool hideName, bool isRefreshBust, bool isHalloweenBust) {
			try {
				if (!Settings.m_enabled.Value || !this.m_is_loaded) {
                    this.m_image.gameObject.SetActive(false);
                    return;
				}
				this.m_image.gameObject.SetActive(!isRefreshBust);
				if (isMarriageBust) {
					this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Wedding];
				} else if (isSwimsuitBust) {
					this.m_image.sprite = this.m_portrait_sprites[PortraitKey.Swimsuit];
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
					____dialoguePanel.AddComponent<SelfBustController>().load_images();
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
					controller.m_npc_emotes2[PortraitKey.Halloween] = (Dictionary<string, AssetReferenceSprite[]>) ReflectionUtils.get_field_value(__instance, "_npcHalloweenEmotes2");
				} catch (Exception e) {
					logger.LogError("** HarmonyPatch_DialogueController_Awake.Postfix ERROR - " + e);
				}
			}
		}

		private static PortraitKey get_force_portrait_key() {
			string val = Settings.m_force_outfit.Value;
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
		//																						  string          bool          bool          bool          bool          bool          bool isHalloweenBust = false
		[HarmonyPatch(typeof(DialogueController), "SetDialogueBustVisualsOptimized", new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
		class HarmonyPatch_DialogueController_SetDialogueBustVisualsOptimized {

			private static bool Prefix(string name, bool small, ref bool isMarriageBust, ref bool isSwimsuitBust, bool hideName, bool isRefreshBust, bool isHalloweenBust) {
				try {
					if (!Settings.m_enabled.Value) {
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

			private static void Postfix(string name, bool small, bool isMarriageBust, bool isSwimsuitBust, bool hideName, bool isRefreshBust, bool isHalloweenBust) {
				try {
					SelfBustController.Instance.show_self_portrait(isMarriageBust, isSwimsuitBust, hideName, isRefreshBust, isHalloweenBust);
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

					if (!Settings.m_enabled.Value) {
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

            private static bool Prefix(ref AssetReferenceSprite __result, ref bool isMarriageBust, ref bool isSwimsuitBust, ref bool isHalloweenBust, string ___npcName) {
				try {
					if (!Settings.m_enabled.Value) {
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
					if (isHalloweenBust || key == PortraitKey.Halloween) {
						isHalloweenBust = true;
						return get_sprite(PortraitKey.Halloween, ___npcName, ref __result);
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
                Direction ____facingDirection,
				bool ___isBSDSluggedNPC
			) {
                try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					if (___isBSDSluggedNPC) {
						__instance.GetSluggedAnimator();
					}
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