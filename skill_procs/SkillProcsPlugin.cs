using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Wish;
using ZeroFormatter;
using DG.Tweening;

public static class PluginInfo {

    public const string TITLE = "Skill Procs";
    public const string NAME = "skill_procs";
    public const string SHORT_DESCRIPTION = "Configurable multipliers for skill procs.";
	public const string EXTRA_DETAILS = "";

	public const string VERSION = "0.0.1";

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
public class SkillProcsPlugin : DDPlugin {
	private static SkillProcsPlugin m_instance = null;
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
        logger = this.Logger;
        try {
			m_instance = this;
            this.m_plugin_info = PluginInfo.to_dict();
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

	[HarmonyPatch(typeof(GameManager), "Awake")]
	class HarmonyPatch_GameManager_Awake {
		private static void Postfix() {
			try {
				Settings.Instance.load(m_instance, null);
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_GameManager_Awake.Postfix ERROR - " + e);
			}
		}
	}

    [HarmonyPatch(typeof(DecorationUpdater), "FruitTree_UpdateMetaOvernight")]
    class HarmonyPatch_DecorationUpdater_FruitTree_UpdateMetaOvernight {
		private static void init_data() {
			DecorationUpdater.fruitTreeData = new ForageTreeSaveData {
				spot1 = check_fruit_spawn(),
				spot2 = check_fruit_spawn(),
				spot3 = check_fruit_spawn(),
				golden = golden_chance(),
				stage = 7
			};
		}
		
		private static bool check_fruit_spawn() {
			return Utilities.Chance(Settings.m_fruit_spawn_chance.Value);
		}

		private static bool golden_chance() {
			return (DecorationUpdater.fruitTreeData.stage == 7) && GameSave.Exploration.GetNode("Exploration10c") && Utilities.Chance(Settings.m_base_gold_fruit_chance.Value + (float) GameSave.Exploration.GetNodeAmount("Exploration10c") * Settings.m_midas_gold_fruit_chance.Value);
		}

		private static bool spawn_spot(bool spot) {
			return spot || DecorationUpdater.fruitTreeData.golden || ((DecorationUpdater.fruitTreeData.stage == 7) && check_fruit_spawn());
		}

		private static bool Prefix(ref DecorationPositionData decorationData) {
            try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				if (!DecorationUpdater.DeserializeMeta(decorationData.meta, ref DecorationUpdater.fruitTreeData)) {
					init_data();
				} else if (DecorationUpdater.fruitTreeData.stage <= 2 || Utilities.Chance(0.52f)) {
					DecorationUpdater.fruitTreeData.stage = Mathf.Min(DecorationUpdater.fruitTreeData.stage + 1, 7);
				}
				try {
					if (DecorationUpdater.fruitTreeData.stage <= 2 || Utilities.Chance(0.52f)) {
						DecorationUpdater.fruitTreeData.stage = Mathf.Min(DecorationUpdater.fruitTreeData.stage + 1, 7);
					}
					if (GameSave.Exploration.GetNode("Exploration10c") && !DecorationUpdater.fruitTreeData.spot1 && !DecorationUpdater.fruitTreeData.spot2 && !DecorationUpdater.fruitTreeData.spot3) {
						DecorationUpdater.fruitTreeData.golden = golden_chance();
					}
					DecorationUpdater.fruitTreeData.spot1 = spawn_spot(DecorationUpdater.fruitTreeData.spot1);
					DecorationUpdater.fruitTreeData.spot2 = spawn_spot(DecorationUpdater.fruitTreeData.spot2);
					DecorationUpdater.fruitTreeData.spot3 = spawn_spot(DecorationUpdater.fruitTreeData.spot3);
				} catch (Exception) {
					init_data();
				}
				decorationData.meta = ZeroFormatterSerializer.Serialize(DecorationUpdater.fruitTreeData);
				return false;
            } catch (Exception e) {
                _error_log("** HarmonyPatch_DecorationUpdater_FruitTree_UpdateMetaOvernight.Prefix ERROR - " + e);
            }
            return true;
        }
    }

	[HarmonyPatch(typeof(ForageTree), "Shake")]
	class HarmonyPatch_ForageTree_Shake {
		private static bool Prefix(bool fromLocalPlayer, ForageTree __instance, bool ___requireRelationship, ParticleSystem ____treeHitParticle, float ___forageEXP, ref bool ___canShake) {
			try {
				if (___requireRelationship && (!SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships.ContainsKey("Claude") || SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships["Claude"] < 10f)) {
					Player.Instance.AddPauseObject("forageTree");
					DialogueController.Instance.SetDefaultBox();
					DialogueController.Instance.PushDialogue(new DialogueNode {
						dialogueText = new List<string> { "Best not to steal from Claude's garden... Maybe if I get to know him better, he'll let me pick fruit from his garden." }
					}, delegate {
						Player.Instance.RemovePauseObject("forageTree");
					}, animateOnComplete: true, ignoreDialogueOnGoing: true);
					return false;
				}
				ParticleManager.Instance.InstantiateParticle(____treeHitParticle, __instance.transform.position + new Vector3(0.65f, 2.25f, -2.25f));
				AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.shakeTreeSound, __instance.transform.position);
				float x2 = (__instance.transform.position + Vector3.right * 0.5f - Player.Instance.transform.position).x;
				float num = 0.95f;
				float direction = Mathf.Sign(x2) * num * 0.85f;
				DOVirtual.Float(0f, 0.45f * direction, 0.16f, delegate (float x)
				{
					__instance.Renderer.material.SetFloat("_Sway", x);
				}).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutSine)
					.OnComplete(delegate {
						DOVirtual.Float(0f, -0.35f * direction, 0.125f, delegate (float x) {
							__instance.Renderer.material.SetFloat("_Sway", x);
						}).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear)
							.OnComplete(delegate {
								__instance.Renderer.material.SetFloat("_Sway", 0f);
							})
							.SetUpdate(isIndependentUpdate: false);
					})
					.SetUpdate(isIndependentUpdate: false);
				DOVirtual.Float(0f, 0.25f * direction, 0.1f, delegate (float x)
				{
					__instance.Renderer.material.SetFloat("_Sway", x);
				}).SetDelay(0.57f).SetLoops(2, LoopType.Yoyo)
					.SetEase(Ease.OutSine)
					.OnComplete(delegate {
						DOVirtual.Float(0f, -0.15f * direction, 0.08f, delegate (float x) {
							__instance.Renderer.material.SetFloat("_Sway", x);
						}).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear)
							.OnComplete(delegate {
								__instance.Renderer.material.SetFloat("_Sway", 0f);
							})
							.SetUpdate(isIndependentUpdate: false);
					})
					.SetUpdate(isIndependentUpdate: false);
				if (fromLocalPlayer) {
					Player.Instance.AddPauseObject("forage");
					Player.Instance.SetPlayerAnimation(new PlayerAnimation {
						facingDirection = Player.Instance.facingDirection,
						hold = WeaponHold.NoGrip,
						swing = SwingAnimation.Push
					});
					DOVirtual.DelayedCall(0.3f, delegate {
						Player.Instance.RemovePauseObject("forage");
						Player.Instance.CancelPlayerAnimation();
					}).SetUpdate(isIndependentUpdate: false);
					for (int i = 0; i < __instance.spots.Length; i++) {
						switch (i) {
							case 0:
								if (!__instance.data.spot1) {
									continue;
								}
								break;
							case 1:
								if (!__instance.data.spot2) {
									continue;
								}
								break;
							case 2:
								if (!__instance.data.spot3) {
									continue;
								}
								break;
						}
						ReflectionUtils.invoke_method(__instance, "SetFruit", new object[] { i, false });
						Vector3 position = __instance.spots[i].transform.position;
						int fruit = (int) ReflectionUtils.invoke_method(__instance, "GetFruit", new object[] { i });
						Pickup.Spawn(position.x, position.y, position.z, fruit, 1, homeIn: false, 0.4f, Pickup.BounceAnimation.Fall, 1.1f, 125f);
						for (int counter = 0; counter < Settings.m_horn_of_plenty_checks.Value; counter++) {
							if (Utilities.Chance((float) GameSave.Exploration.GetNodeAmount("Exploration7c", 2) * Settings.m_horn_of_plenty_chance.Value)) {
								Pickup.Spawn(position.x + 0.1f, position.y, position.z, fruit, 1, homeIn: false, 0.4f, Pickup.BounceAnimation.Fall, 1.1f, 125f);
							}
						}
						if (Utilities.Chance(0.004f)) {
							int num2 = Wish.Tree.explorationMuseumItems.RandomItem();
							Pickup.Spawn(position.x - 0.1f, position.y, position.z, num2, 1, homeIn: false, 0.4f, Pickup.BounceAnimation.Fall, 1.1f, 125f);
						}
						Player.Instance.AddEXP(ProfessionType.Exploration, ___forageEXP);
					}
					__instance.data.golden = false;
					__instance.SaveMeta();
					ForageTree.onShakeTree?.Invoke(__instance.Position, __instance.sceneID, __instance.meta);
				}
				__instance.data.golden = false;
				___canShake = false;
				DOVirtual.DelayedCall(1f, delegate {
					ReflectionUtils.get_field(__instance, "canShake").SetValue(__instance, true);
				});
				return false;
			} catch (Exception e) {
				_error_log("** HarmonyPatch_ForageTree_Shake.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}