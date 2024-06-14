using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using QFSW.QC;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using ZeroFormatter;
using UnityEngine.SceneManagement;


[BepInPlugin("devopsdinosaur.sunhaven.testing", "Testing", "0.0.1")]
public class ActionSpeedPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.testing");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
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

	[HarmonyPatch(typeof(PlayerSettings), "SetCheatsEnabled")]
	class HarmonyPatch_PlayerSettings_SetCheatsEnabled {

		private static bool Prefix(ref bool enable) {
			enable = true;
			return false;
		}

		private static void Postfix() {
			
		}
	}

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

	/*
	[HarmonyPatch(typeof(Decoration), "SetRed")]
	class HarmonyPatch_GameManager_HasObjectSubTile {

		private static bool Prefix(
			Decoration __instance,
			Renderer[] ___renderers,
			bool ___setRedThisFrame,
			Tween ___redTween
		) {
			if (___renderers == null || ___setRedThisFrame) {
				return false;
			}
			___redTween?.Kill();
			Renderer[] array = ___renderers;
			foreach (Renderer rend in array) {
				if (!rend || !rend.material.HasProperty("_Color")) {
					continue;
				}
				//rend.material.SetColor("_Color", Color.Lerp(Color.white, Color.red, 0.4f));
				rend.material.SetColor("_Color", Color.Lerp(Color.white, Color.blue, 0.4f));
				___redTween = DOVirtual.Float(0.45f, 0f, 0.2f, delegate(float x) {
					if ((bool) rend) {
						rend.material.SetColor("_Color", Color.Lerp(Color.white, Color.blue, x));
					}
				}).SetEase(Ease.Linear);
			}
			___setRedThisFrame = true;
			if (!GameManager.SceneTransitioning && (bool) __instance && !__instance.Equals(null) && (bool) __instance.gameObject && __instance.isActiveAndEnabled) {
				___setRedThisFrame = false;
			}
			return false;
		}
	}
	*/

	/*
	[HarmonyPatch(typeof(Placeable), "LateUpdate")]
	class HarmonyPatch_Placeable_LateUpdate {

		private static bool Prefix(
			Placeable __instance,
			Player ___player,
			bool ___useAbleByPlayer,
			bool ___confirmPlacing,
			SpriteRenderer ____decorationPreview,
			float ___width,
			byte ___variation,
			Vector2Int ___roundedMousePos,
			ref bool ___canBePlaced,
			SpriteRenderer ____secondaryPreview,
			ref float ___placeTimer
		) {
			try {
				if (!(___player.IsOwner && ___useAbleByPlayer && (bool) SingletonBehaviour<GameManager>.Instance && !___confirmPlacing)) {
					return false;
				}
				if (!____decorationPreview) {
					__instance.GetType().GetMethod("MakeDecorationPreview", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {(byte) 0});
				}
				___width = (float) __instance._decoration.VariationSize(___variation).x / 6f / 2f;
				Vector2 vector = Utilities.MousePositionFloat() * 6f;
				vector.x = Mathf.Clamp(vector.x, (___player.ExactPosition.x - __instance.range) * 6f, (___player.ExactPosition.x + __instance.range) * 6f);
				vector.y = Mathf.Clamp(vector.y, (___player.ExactPosition.y - __instance.range) * 6f, (___player.ExactPosition.y + __instance.range) * 6f);
				___roundedMousePos = new Vector2Int((int) (vector.x - ___width * 6f), (int) vector.y);
				if (__instance.snapToTile || __instance.snapToX) {
					___roundedMousePos.x = (int) (((float) ___roundedMousePos.x + 3f) / 6f) * 6;
				}
				if (__instance.snapToTile || __instance.snapToY) {
					___roundedMousePos.y = ___roundedMousePos.y / 6 * 6;
				}
				____decorationPreview.transform.position = new Vector3((float) ___roundedMousePos.x / 6f + __instance.previewOffset.x, (float) ___roundedMousePos.y / 6f * 1.41421354f - 6f + __instance.previewOffset.y, -6f);
				Vector3Int position = new Vector3Int(___roundedMousePos.x, ___roundedMousePos.y, 0);
				___canBePlaced = true;
				//SingletonBehaviour<GameManager>.Instance.InvalidDecorationPlacement(ref position, _decoration, _decoration.VariationSize(variation), _decoration.PlacementSize, ignoreDataLayerPlacement: false, _canDestroyDecorations, ref canBePlaced);
				if ((bool) __instance.GetType().GetMethod("InvalidPlaceable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {})) {
					___canBePlaced = false;
				}
				logger.LogInfo("sdfsdfsdf");
				____decorationPreview.color = ((!___canBePlaced) ? new Color(0.33f, 1.0f, 0.33f, 0.67f) : new Color(1f, 1f, 1f, 0.67f));
				if ((bool) ____secondaryPreview) {
					____secondaryPreview.color = ____decorationPreview.color;
				}
				___placeTimer -= Time.deltaTime;
				return false;
			} catch (Exception e) {
				logger.LogInfo("** HarmonyPatch_Placeable_LateUpdate_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Placeable), "InvalidPlaceable")]
	class HarmonyPatch_Placeable_InvalidPlaceable {

		private static void Postfix(ref bool ___canBePlaced) {
			___canBePlaced = true;
		}
	}

	[HarmonyPatch(typeof(GameManager), "InvalidDecorationPlacement")]
	class HarmonyPatch_GameManager_InvalidDecorationPlacement {

		private static void Postfix(ref bool ___canBePlaced) {
			logger.LogInfo("HarmonyPatch_GameManager_InvalidDecorationPlacement");
		}
	}
	*/
	
	/*
	[HarmonyPatch(typeof(GameManager), "HasObjectSubTile")]
	class HarmonyPatch_GameManager_HasObjectSubTile {

		private static bool Prefix(
			GameManager __instance,
			Vector3Int position, 
			Vector2Int size, 
			Vector2Int placementSize, 
			Vector2Int offset, 
			out float zoffset, 
			bool canPlaceOnTable, 
			bool canPlaceOnWall, 
			bool ignoreDataLayerPlacement, 
			bool canDestroyDecorations, 
			bool setred, 
			bool canPlaceInWater, 
			bool checkForOtherDecorations,
			ref bool __result
		) {
			zoffset = 0f;
			checkForOtherDecorations = false;
			try {
				short activeSceneIndex = ScenePortalManager.ActiveSceneIndex;
				bool flag = false;
				if (!ignoreDataLayerPlacement) {
					for (int i = offset.x; i < placementSize.x + offset.x; i++) {
						for (int j = offset.y; j < placementSize.y + offset.y; j++) {
							Vector3Int position2 = position + new Vector3Int(i, j, 0);
							if (canPlaceOnWall) {
								if (!__instance.CanPlaceDecorationSubTile(position2, PlacementType.Wall)) {
									return true;
								}
							}
							else if (canPlaceInWater)
							{
								if (!__instance.CanPlaceDecorationSubTile(position2, PlacementType.Water)) {
									flag = true;
								}
							}
							else if (!__instance.CanPlaceDecorationSubTile(position2, PlacementType.Ground)) {
								flag = true;
							}
						}
					}
				}
				bool flag2 = false;
				for (int k = offset.x; k < Mathf.Max(size.x, placementSize.x) + offset.x; k++) {
					for (int l = offset.y; l < Mathf.Max(size.y, placementSize.y) + offset.y; l++) {
						Vector3Int vector3Int = new Vector3Int(position.x + k, position.y + l, position.z);
						Vector3Int vector3Int2 = new Vector3Int(position.x + k, position.y + l, 0);
						if (checkForOtherDecorations && __instance.objects.TryGetValue(vector3Int, out var value) && (!canDestroyDecorations || ((!(value is Rock rock) || !rock.canBeDestroyedFromDecoration) && !(value is Tree) && !(value is Wood) && !(value is HealthDecoration) && !(value is Forageable)))) {
							flag2 = true;
							if (setred) {
								logger.LogInfo("setting thingy to red");
								__instance.objects[vector3Int]?.SetRed();
							}
						}
						if (canPlaceOnTable && __instance.objects.ContainsKey(vector3Int2) && __instance.objects[vector3Int2] is Table table) {
							flag = false;
							zoffset = table.Height(vector3Int2);
							if (!table.CanPlaceDecoration(vector3Int2)) {
								table.SetRed();
								return true;
							}
							table.CancelRed();
							continue;
						}
						if (position.z == 1 && __instance.objects.ContainsKey(vector3Int2)) {
							flag2 = true;
							if (setred) {
								__instance.objects[vector3Int2].SetRed();
							}
						}
						if (checkForOtherDecorations && !canDestroyDecorations && SingletonBehaviour<GameSave>.Instance.CurrentWorld.Decorations.ContainsKey(activeSceneIndex) && SingletonBehaviour<GameSave>.Instance.CurrentWorld.Decorations[activeSceneIndex].ContainsKey(new KeyTuple<ushort, ushort, sbyte>((ushort)vector3Int.x, (ushort)vector3Int.y, (sbyte)vector3Int.z))) {
							flag2 = true;
						}
						if (!canPlaceOnWall) {
							continue;
						}
						RaycastHit2D raycastHit2D = Physics2D.BoxCast(new Vector2((float) vector3Int.x / 6f + 1f / 12f, (float) vector3Int.y * 1.41421354f / 6f), Vector2.one / 24f, 0f, Vector2.zero, 0f, SingletonBehaviour<Prefabs>.Instance.wall);
						if ((bool) raycastHit2D) {
							Wall component = raycastHit2D.collider.GetComponent<Wall>();
							if ((bool) component && zoffset == 0f)
							{
								zoffset = component.Height(vector3Int);
							}
						}
					}
				}
				__result = flag || flag2;
				return false;
			} catch (Exception e) {
				logger.LogInfo("** HarmonyPatch_GameManager_HasObjectSubTile_Prefix ERROR - " + e);
			}
			return true;
		}
	}
	*/

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		const float CHECK_FREQUENCY = 1.0f;
		static float m_elapsed = CHECK_FREQUENCY;
		static bool one_shot_done = false;

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
				if (one_shot_done) {
					return true;
				}
				one_shot_done = true;
				

			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Player_Update_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Water), "Start")]
	class HarmonyPatch_Water_Start {
		private static void Postfix(Water __instance, Material ____liquidMaterial, LiquidType ___liquidType) {
			GameObject.Destroy(__instance.transform.GetComponent<BoxCollider2D>());
		}
	}
}