
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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
using Mirror;
using UnityEngine.UI;
using QFSW.QC;


[BepInPlugin("devopsdinosaur.sunhaven.debugging", "DEBUGGING", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.debugging");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enable_cheats;


	public Plugin() {
	}

	private static bool enum_descendants_callback(Transform transform) {
		logger.LogInfo(transform);
		return true;
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.debugging v0.0.1 loaded.");
		this.m_harmony.PatchAll();
		m_enable_cheats = this.Config.Bind<bool>("General", "Enable Cheats", true, "Determines whether console cheats are enabled (without that weird key combination thingy)");
		
		foreach (string key in BepInEx.Bootstrap.Chainloader.PluginInfos.Keys) {
			PluginInfo plugin_info = BepInEx.Bootstrap.Chainloader.PluginInfos[key];
			logger.LogInfo(key + " - " + plugin_info.ToString());
		}

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

    [HarmonyPatch(typeof(PlayerSettings), "Initialize")]
    class HarmonyPatch_PlayerSettings_Initialize {

        private static void Postfix(PlayerSettings __instance) {
            __instance.SetCheatsEnabled(true);
        }
    }

    [HarmonyPatch(typeof(Player), "RequestSleep")]
	class HarmonyPatch_Player_RequestSleep {

		private static bool Prefix(Player __instance, Bed bed, ref bool ____paused, ref UnityAction ___OnUnpausePlayer) {
			DialogueController.Instance.SetDefaultBox();
			DialogueController.Instance.PushDialogue(new DialogueNode {
				dialogueText = new List<string> { "Would you like to sleep?" },
				responses = new Dictionary<int, Response> {{
					0,
					new Response
					{
						responseText = () => "Yes",
						action = delegate {
							__instance.GetType().GetTypeInfo().GetMethod("StartSleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {bed});
						}
					}
				}, {
					1,
					new Response
					{
						responseText = () => "No",
						action = delegate {
							DialogueController.Instance.CancelDialogue(animate: true, null, showActionBar: true);
						}
					}
				}
			}});
			____paused = true;
			___OnUnpausePlayer = (UnityAction) Delegate.Combine(___OnUnpausePlayer, (UnityAction) delegate {
				DialogueController.Instance.CancelDialogue();
			});
			return false;
		}
	}

	// ======================================================================================
	// Stuff for granting all race implicits
	// ======================================================================================

	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static bool Prefix(ref float ___craftSpeedMultiplier) {
			// the CraftSpeedMultiplier property multiplies this by 1.2 for humans,
			// so this grants the 20% to the other races
			___craftSpeedMultiplier = (GameSave.CurrentCharacter.race != (int) Race.Human ? 1.2f : 1f);
			return true;
		}
	}

	[HarmonyPatch(typeof(SkillStats), "GetStat")]
	class HarmonyPatch_SkillStats_GetStat {

		private static bool Prefix(StatType stat, ref float __result, ref float ___lastJumpTimer) {
			if (stat == StatType.CrossbowPower) {
				__result = 0f;
				if (GameSave.Combat.GetNode("Combat3b") && Time.time <= ___lastJumpTimer + 2f) {
					__result += (float) GameSave.Combat.GetNodeAmount("Combat3b") * 0.05f;
				}
				__result += 0.1f;
				return false;
			}
			return true;
		}
	}

	// ======================================================================================
	// ======================================================================================

	[HarmonyPatch(typeof(LiamWheat), "ReceiveDamage")]
	class HarmonyPatch_LiamWheat_ReceiveDamage {

		private static bool Prefix(ref LiamWheat __instance, ref DamageHit __result) {
			AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.cropHit, __instance.transform.position);
			UnityEngine.Object.Destroy(__instance.gameObject);
			__result = new DamageHit {
				hit = true,
				damageTaken = 1f
			};
			Pickup.Spawn(
				__instance.transform.position.x + 0.5f, 
				__instance.transform.position.y + 0.707106769f, 
				__instance.transform.position.z, 
				ItemID.Wheat
			);
			return false;
		}
	}

	public class SimulationClock {

		private static SimulationClock m_instance = null;
		public static SimulationClock Instance {
			get {
				if (m_instance == null) {
					m_instance = new SimulationClock();
				}
				return m_instance;
			}
		}
		public const float TICK = 0.05f;
		public const float MAX_VALUE = 20f;
		public const float MAX_TICK = MAX_VALUE / TICK;

		//public void create_settings_slider

		public void adjust_dayspeed_slider(float value, Slider slider, TextMeshProUGUI label) {
			slider.minValue = 0f;
			slider.maxValue = MAX_TICK;
			slider.value = (value < TICK ? 0f : value / MAX_VALUE);
			label.text = "Blah";
		}
	}

	[HarmonyPatch(typeof(Pickaxe), "Action")]
	class HarmonyPatch_Pickaxe_Action {

		private const float POWER_MULTIPLIER = 5f;
		private const float AOE_RANGE_MULTIPLIER = 2f;

		private static bool Prefix(
			ref Pickaxe __instance,
			ref Decoration ____currentDecoration,
			ref Player ___player,
			ref float ____power,
			ref PickaxeType ___pickaxeType,
			ref int ____breakingPower
		) {
			if (!((bool) ____currentDecoration && ____currentDecoration is Rock rock)) {
				return true;
			}
			bool is_crit = Utilities.Chance(___player.GetStat(StatType.MiningCrit));
			float damage = (is_crit ? (____power * 2f) : ____power);
			damage *= UnityEngine.Random.Range(0.75f, 1.25f);
			damage *= 1f + ___player.GetStat(StatType.MiningDamage);
			if (SceneSettingsManager.Instance.GetCurrentSceneSettings != null && SceneSettingsManager.Instance.GetCurrentSceneSettings.townType == TownType.Nelvari && ___pickaxeType == PickaxeType.Nelvari) {
				damage *= 1.3f;
			}
			if (SceneSettingsManager.Instance.GetCurrentSceneSettings != null && SceneSettingsManager.Instance.GetCurrentSceneSettings.townType == TownType.Withergate && ___pickaxeType == PickaxeType.Withergate) {
				damage *= 1.3f;
			}
			foreach (Rock rock2 in Utilities.CircleCast<Rock>(rock.Center, 3f * AOE_RANGE_MULTIPLIER)) {
				if (rock2 != null) {
					hit_rock(rock2, damage, ____breakingPower, is_crit);
				}
			}
			return true;
		}

		private static void hit_rock(Rock rock, float damage, float _breakingPower, bool is_crit) {
			Vector3 position = rock.Position + new Vector3(0.5f, -0.15f, -1f);
			ParticleManager.Instance.InstantiateParticle(((ParticleSystem) rock.GetType().GetTypeInfo().GetField("_breakParticle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rock)), position);
			((Transform) rock.GetType().GetTypeInfo().GetField("graphics", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rock)).DOScale(new Vector3(1.1f, 0.9f, 1f), 0.35f).From().SetEase(Ease.OutBounce);
			if (_breakingPower < (float) rock.GetType().GetTypeInfo().GetField("requiredPower", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rock)) {
				return;
			}
			FieldInfo current_health_info = rock.GetType().GetTypeInfo().GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
			return;
			float current_health = (float) current_health_info.GetValue(rock) - damage;
			current_health_info.SetValue(rock, current_health);
			FieldInfo heal_tween_info = rock.GetType().GetTypeInfo().GetField("healTween", BindingFlags.Instance | BindingFlags.NonPublic);
			Tween heal_tween = (Tween) heal_tween_info.GetValue(rock);
			heal_tween.Kill();
			FloatingTextManager.Instance.SpawnFloatingDamageText((int) damage, position, DamageType.Player, is_crit);
			Slider _healthSlider = (Slider) rock.GetType().GetTypeInfo().GetField("_healthSlider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rock);
			if ((bool) _healthSlider) {
				_healthSlider.gameObject.SetActive(value: true);
				_healthSlider.DOValue(Mathf.Clamp(current_health / (float) rock.GetType().GetTypeInfo().GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rock), 0f, 1f), 0.125f);
				DOVirtual.DelayedCall(20f, delegate {
					if ((bool) _healthSlider) {
						_healthSlider.gameObject.SetActive(value: false);
					}
				});
			}
			/*
				healTween = DOVirtual.DelayedCall(20f, delegate
				{
					if ((bool) _healthSlider) {
						_healthSlider.gameObject.SetActive(value: false);
					}
				});
			} else if (isHeavystone) {
				if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Heavystone")) {
					SingletonBehaviour<HelpTooltips>.Instance.SendNotification("Heavystone", "<color=#39CCFF>Heavystone</color> Deposits are a lot tougher than normal stone! You'll need <color=#39CCFF>a stronger tool</color> to break them!", new List<(Transform, Vector3, Direction)>(), 35, delegate
					{
						SingletonBehaviour<HelpTooltips>.Instance.CompleteNotification(35);
						SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Heavystone", value: true);
					});
				}
				SingletonBehaviour<NotificationStack>.Instance.SendNotification("You'll need <color=#39CCFF>a stronger tool</color> to break <color=#39CCFF>Heavystone</color> Deposits!");
			}
			if (_currentHealth <= 0f) {
				Die(hitFromLocalPlayer, homeIn, rustyKeyDropMultiplier, brokeUsingPickaxe);
			} else if ((bool) _rockHitSound) {
				AudioManager.Instance.PlayOneShot(_rockHitSound, base.transform.position);
			}
			*/
		}
	}

	[HarmonyPatch(typeof(Rock), "Die")]
	class HarmonyPatch_Rock_Die {

		private static bool Prefix(
			ref bool hitFromLocalPlayer,
			ref bool homeIn, 
			ref float rustyKeyDropMultiplier, 
			ref bool brokeUsingPickaxe,
			ref Rock __instance,
			ref AudioClip ____rockBreakSound
		) {
			logger.LogInfo("Rock.Die - pos: " + __instance.Position);
			if ((bool) ____rockBreakSound) {
				AudioManager.Instance.PlayOneShot(____rockBreakSound, __instance.transform.position);
			}
			if (hitFromLocalPlayer) {
				__instance.GetType().GetTypeInfo().GetMethod("HandleRockDrop", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {homeIn, rustyKeyDropMultiplier, brokeUsingPickaxe});
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerAnimationLayers), "LateUpdate")]
	class HarmonyPatch_PlayerAnimationLayers_LateUpdate {

		private static PlayerAnimationLayers m_instance;
		private static MethodInfo m_method_info_UpdateBodyPart;
		private static Dictionary<int, Sprite[]> m_morphed_chest_sprites = new Dictionary<int, Sprite[]>();
		
		private static Sprite[] morph_chest_sprites(Sprite[] original_sprites) {
			try {
				int hash = original_sprites.GetHashCode();
				if (m_morphed_chest_sprites.ContainsKey(hash)) {
					return m_morphed_chest_sprites[hash];
				}
				Sprite[] sprites = new Sprite[4];
				Texture2D texture = null;
				Sprite original_sprite = null;
				Texture2D original_texture = null;
				RenderTexture render_texture = null;
				sprites[0] = original_sprites[0];
				original_sprite = original_sprites[1];
				original_texture = original_sprite.texture;
				render_texture = new RenderTexture(original_texture.width, original_texture.height, 32, RenderTextureFormat.ARGB32);
				texture = new Texture2D(original_texture.width, original_texture.height);
				Graphics.Blit(original_texture, render_texture);
				for (int y = 0; y < original_texture.height; y++) {
					for (int x = 0; x < original_texture.width; x++) {
						texture.SetPixel(x, y, Color.red);
					}
				}
				RenderTexture.active = render_texture;
				texture.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);
				texture.Apply();
				
				sprites[1] = Sprite.Create(texture, original_sprite.rect, new Vector2(0.5f, 0.5f));
				//sprites[1] = original_sprites[0];
				sprites[2] = original_sprites[2];
				sprites[3] = original_sprites[3];
				m_morphed_chest_sprites[hash] = sprites;
				m_morphed_chest_sprites[sprites.GetHashCode()] = sprites;
				return sprites;
			} catch (Exception e) {
				logger.LogInfo(e);
			}
			return original_sprites;
		}

		private static void UpdateBodyPart(MeshGenerator renderer, Sprite[] sprites, int index, Vector2 offset, float sortingOrder, float northSortingOrder, bool useChestDirection = false, bool is_chest_sprites = false) {
			//if (is_chest_sprites) {
			//	sprites = morph_chest_sprites(sprites);
			//}
			m_method_info_UpdateBodyPart.Invoke(m_instance, new object[] {renderer, sprites, index, offset, sortingOrder, northSortingOrder, useChestDirection});
		}

		private static int GetAnimatedHatIndex(int headIndex, float randomTimeOffset, Player player) {
			int num = (int) ((Time.timeSinceLevelLoad + randomTimeOffset + 1.25f) * 5f) % ((player.Velocity.magnitude > 0.1f || !player.Grounded) ? 4 : 20);
			if (num >= 4) {
				num = 0;
			}
			if ((player.Velocity.magnitude > 0.1f || !player.Grounded) && num == 0) {
				num = 2;
			}
			return headIndex * 4 + num;
		}

		private static int GetAnimatedWingIndex(int spriteCount, int chestIndex, float randomTimeOffset, Player player) {
			if (spriteCount == 12) {
				if (player.Grounded) {
					int num = (int) ((Time.timeSinceLevelLoad + randomTimeOffset) * 5f) % 20;
					if (num == 3) {
						num = 1;
					}
					if (num >= 4) {
						num = 0;
					}
					return chestIndex * 3 + num;
				}
				return chestIndex * 3 + 2;
			}
			if (player.Grounded) {
				int num2 = (int) ((Time.timeSinceLevelLoad + randomTimeOffset) * 5f) % 20;
				if (num2 >= 4) {
					num2 = 0;
				}
				return chestIndex * 4 + num2;
			}
			return chestIndex * 4 + 2;
		}

		private static void UpdateTransform(Transform trans, Vector2 offset, float sortingOrder, float northSortingOrder, Player player) {
			trans.gameObject.SetActive(value: true);
			trans.localPosition = new Vector3(offset.x, offset.y / 1.41421354f, (0f - offset.y) / 1.41421354f) * (1f / 24f);
			float num = (0f - ((player.facingDirection == Direction.North) ? northSortingOrder : sortingOrder)) * 0.0001f;
			trans.localPosition += new Vector3(0f, num, num);
		}

		private static bool Prefix(
			PlayerAnimationLayers __instance,
			AnimationIndex ____topArmAnimations,
			AnimationIndex ____bottomArmAnimations,
			AnimationIndex ____headAnimations,
			AnimationIndex ____chestAnimations,
			AnimationIndex ____legAnimations,
			AnimationIndex ____eyeAnimations,
			AnimationIndex ____mouthAnimations,
			MeshGenerator ____chest,
			ref Sprite[] ____chestSprites,
			MeshGenerator ____leg,
			Sprite[] ____legSprites,
			MeshGenerator ____head,
			Sprite[] ____headSprites,
			MeshGenerator ____topArms,
			Sprite[] ____topArmSprites,
			MeshGenerator ____bottomArm,
			Sprite[] ____bottomArmSprites,
			MeshGenerator ____mouth,
			Sprite[] ____mouthSprites,
			MeshGenerator ____eye,
			Sprite[] ____eyeEmoteSprites,
			Sprite[] ____eyeSprites,
			MeshGenerator ____eyeGlow,
			MeshGenerator ____gloves,
			Sprite[] ____glovesSprites,
			MeshGenerator ____backGloves,
			Sprite[] ____backGlovesSprites,
			MeshGenerator ____sleeves,
			Sprite[] ____sleevesSprites,
			MeshGenerator ____backSleeves,
			Sprite[] ____backSleevesSprites,
			MeshGenerator ____hair,
			Sprite[] ____hairSprites,
			MeshGenerator ____hairGlow,
			MeshGenerator ____hat,
			Sprite[] ____hatSprites,
			MeshGenerator ____hatGlow,
			MeshGenerator ____ears,
			Sprite[] ____earsSprites,
			MeshGenerator ____chestArmor,
			ref Sprite[] ____chestArmorSprites,
			MeshGenerator ____pants,
			Sprite[] ____pantsSprites,
			MeshGenerator ____back,
			Sprite[] ____backSprites,
			MeshGenerator ____wingGlow,
			MeshGenerator ____tail,
			Sprite[] ____tailSprites,
			MeshGenerator ____overlay,
			Sprite[] ____overlaySprites,
			MeshGenerator ____face,
			Sprite[] ____faceSprites,
			float ___randomTimeOffset,
			Player ____player,
			HatType ____hatType,
			Transform ____useItem
		) {
			m_instance = __instance;
			m_method_info_UpdateBodyPart = __instance.GetType().GetTypeInfo().GetMethod("UpdateBodyPart", BindingFlags.Instance | BindingFlags.NonPublic);
			__instance.transform.localPosition = new Vector3(__instance.Offset.x, __instance.Offset.y / 1.41421354f, (0f - __instance.Offset.y) / 1.41421354f);
			__instance.topArmIndex = ____topArmAnimations.index;
			__instance.bottomArmIndex = ____bottomArmAnimations.index;
			__instance.armOffset = ____topArmAnimations.offset;
			__instance.headIndex = ____headAnimations.index;
			__instance.headOffset = ____headAnimations.offset;
			__instance.chestIndex = ____chestAnimations.index;
			__instance.chestOffset = ____chestAnimations.offset;
			__instance.legIndex = ____legAnimations.index;
			__instance.legOffset = ____legAnimations.offset;
			__instance.eyeIndex = ____eyeAnimations.index;
			__instance.eyeOffset = ____eyeAnimations.offset;
			__instance.mouthIndex = ____mouthAnimations.index;
			__instance.mouthOffset = ____mouthAnimations.offset;
			if (__instance.rotateChestDirection) {
				switch (__instance.headIndex) {
				case 0: { __instance.headIndex = 3; break; }
				case 1: { __instance.headIndex = 0; break; }
				case 2: { __instance.headIndex = 1; break; }
				case 3: { __instance.headIndex = 2; break; }
				}
			}
			if (__instance.grabAnimation) {
				switch (__instance.headIndex) {
				case 0: { __instance.topArmIndex = 10; __instance.legIndex = 2; break; }
				case 1: { __instance.topArmIndex = 25; __instance.legIndex = 8; break; }
				case 2: { __instance.topArmIndex = 40; __instance.legIndex = 14; break; }
				case 3: { __instance.topArmIndex = 55; __instance.legIndex = 20; break; }
				}
			}
			if (__instance.race == 3) {
				__instance.GetType().GetTypeInfo().GetMethod("HandleNageAnimations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {});
				UpdateBodyPart(____chest, ____chestSprites, __instance.chestIndex, __instance.chestOffset, 8f, 8f, true);
				UpdateBodyPart(____leg, ____legSprites, __instance.legIndex, __instance.legOffset, 9f, 9f);
			} else {
				____leg.Squares = new List<Square> {
					new Square {
						TopLeft = new Vector3(-20f, 52f, 52f),
						TopRight = new Vector3(20f, 52f, 52f),
						BotLeft = new Vector3(-20f, 0f, 0f),
						BotRight = new Vector3(20f, 0f, 0f)
					}
				};
				UpdateBodyPart(____chest, ____chestSprites, __instance.chestIndex, __instance.chestOffset, 10f, 10f, false, true);
				UpdateBodyPart(____leg, ____legSprites, __instance.legIndex, __instance.legOffset, 9f, 9f);
			}
			UpdateBodyPart(____head, ____headSprites, __instance.headIndex, __instance.headOffset, 14f, 14f);
			UpdateBodyPart(____topArms, ____topArmSprites, __instance.topArmIndex, __instance.chestOffset, 21f, 21f);
			UpdateBodyPart(____bottomArm, ____bottomArmSprites, __instance.bottomArmIndex, __instance.chestOffset, 6f, 6f);
			UpdateBodyPart(____mouth, ____mouthSprites, __instance.mouthIndex, __instance.headOffset, 17f, 17f);
			if (__instance.eyeIndex >= 12) {
				UpdateBodyPart(____eye, ____eyeEmoteSprites, __instance.eyeIndex - 12, __instance.headOffset, (__instance.race == 4) ? 33 : 18, 18f);
			} else {
				UpdateBodyPart(____eye, ____eyeSprites, __instance.eyeIndex, __instance.headOffset, (__instance.race == 4) ? 33 : 18, 18f);
			}
			UpdateBodyPart(____eyeGlow, ____eyeSprites, (__instance.eyeIndex == -1) ? (-1) : (__instance.eyeIndex + 12), __instance.headOffset, 21f, 21f);
			UpdateBodyPart(____gloves, ____glovesSprites, __instance.topArmIndex, __instance.chestOffset, 22f, 22f);
			UpdateBodyPart(____backGloves, ____backGlovesSprites, __instance.bottomArmIndex, __instance.chestOffset, 8f, 8f);
			UpdateBodyPart(____sleeves, ____sleevesSprites, __instance.topArmIndex, __instance.chestOffset, 23f, 23f);
			UpdateBodyPart(____backSleeves, ____backSleevesSprites, __instance.bottomArmIndex, __instance.chestOffset, 12f, 12f);
			int animatedHairIndex = __instance.headIndex;
			if (__instance.race == 4) {
				UpdateBodyPart(____hair, ____hairSprites, __instance.headIndex * 5 + (int) (Time.timeSinceLevelLoad * 6f) % 5, __instance.headOffset, 19f, 28f);
				UpdateBodyPart(____hairGlow, ____hairSprites, __instance.headIndex * 5 + (int) (Time.timeSinceLevelLoad * 6f) % 5 + 20, __instance.headOffset, 22f, 29f);
			} else {
				if (____hairSprites != null) {
					if (____hairSprites.Length == 24 || ____hairSprites.Length == 20) {
						animatedHairIndex = GetAnimatedHatIndex(__instance.headIndex, ___randomTimeOffset, ____player);
					}
					switch (____hatType) {
					default:				{ UpdateBodyPart(____hair, ____hairSprites, animatedHairIndex, __instance.headOffset, 19f, 28f); break; }
					case HatType.Hat:		{ UpdateBodyPart(____hair, ____hairSprites, __instance.headIndex + 16, __instance.headOffset, 19f, 28f); break; }
					case HatType.Helmet:	{ UpdateBodyPart(____hair, null, __instance.headIndex, __instance.headOffset, 19f, 28f); break; }
					}
				}
				UpdateBodyPart(____hairGlow, ____hairSprites, -1, __instance.headOffset, 22f, 29f);
			}
			____hair.SetDefault();
			UpdateBodyPart(____hat, ____hatSprites, (____hatSprites.Length == 16) ? GetAnimatedHatIndex(__instance.headIndex, ___randomTimeOffset, ____player) : __instance.headIndex, __instance.headOffset, 20f, (____hatType == HatType.Horns) ? 18 : 29);
			____hat.SetDefault();
			UpdateBodyPart(____hatGlow, ____hatSprites, (____hatSprites.Length == 8) ? (__instance.chestIndex + 4) : (-1), __instance.headOffset, 21f, 30f);
			____hatGlow.SetDefault();
			int index = __instance.headIndex;
			if (____hatType == HatType.Helmet) {
				index = -1;
			}
			if (__instance.race == 1 || __instance.race == 2) {
				UpdateBodyPart(____ears, ____earsSprites, index, __instance.headOffset, 21f, 15f);
			} else {
				UpdateBodyPart(____ears, ____earsSprites, index, __instance.headOffset, 15f, 15f);
			}
			UpdateBodyPart(____chestArmor, ____chestArmorSprites, __instance.chestIndex, __instance.chestOffset, 13f, 13f, false, true);
			if (__instance.race == 3) {
				UpdateBodyPart(____pants, null, __instance.legIndex, __instance.legOffset, 11f, 11f);
			} else {
				UpdateBodyPart(____pants, ____pantsSprites, __instance.legIndex, __instance.legOffset, 11f, 11f);
			}
			int animatedWingIndex = GetAnimatedWingIndex(____backSprites.Length, __instance.chestIndex, ___randomTimeOffset, ____player);
			UpdateBodyPart(____back, ____backSprites, animatedWingIndex, __instance.chestOffset, 1f, 31f, useChestDirection: true);
			____back.SetDefault();
			UpdateBodyPart(____wingGlow, ____backSprites, animatedWingIndex + 16, __instance.chestOffset, 2f, 32f, useChestDirection: true);
			UpdateBodyPart(____tail, ____tailSprites, (____tailSprites != null && ____tailSprites.Length > 4) ? (__instance.chestIndex * 4 + (int) (Time.timeSinceLevelLoad * 4f) % 4) : __instance.chestIndex, __instance.legOffset, 2f, 27.5f, useChestDirection: true);
			UpdateBodyPart(____overlay, ____overlaySprites, 0, __instance.legOffset, 50f, 50f);
			UpdateBodyPart(____face, (__instance.faceOnTopOfHair && ____hatType == HatType.Helmet) ? null : ____faceSprites, __instance.headIndex, __instance.headOffset, __instance.faceOnTopOfHair ? 20 : 16, __instance.faceBehindHairWhenFacingNorth ? 29.5f : 16f);
			if (____useItem != null) {
				UpdateTransform(____useItem, __instance.chestOffset + new Vector2(0f, 15f), 20.5f, 0f, ____player);
			}
			return false;
		}
	}
}