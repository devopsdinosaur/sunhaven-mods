﻿using BepInEx;
using HarmonyLib;
using I2.Loc;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Wish;

public static class PluginInfo {

    public const string TITLE = "No Mo Logo";
    public const string NAME = "no_logo";
    public const string SHORT_DESCRIPTION = "Cuts out the ten-second logo animation at the start of the game.";
	public const string EXTRA_DETAILS = "This mod is *not* designed to diminish the efforts of Pixel Sprout studios, but simply to speed up the load time without the extra fanfare.  To be honest, like Continue Button, I just made this to speed up my debugging, but I thought others might want it.  Enjoy!";

	public const string VERSION = "0.0.2";

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
public class NoLogoPlugin : DDPlugin {
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
        logger = this.Logger;
        try {
            this.m_plugin_info = PluginInfo.to_dict();
            Settings.Instance.load(this);
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

	[HarmonyPatch(typeof(MainMenuLoader), "Start")]
	class HarmonyPatch_MainMenuLoader_Start {

		class QuickLoader : MonoBehaviour {
			private static QuickLoader m_instance;
			public static QuickLoader Instance {
				get {
					return m_instance;
				}
			}
			private MainMenuLoader m_loader;

			public static QuickLoader create(MainMenuLoader loader) {
				if (m_instance != null) {
					return m_instance;
				}
				m_instance = loader.gameObject.AddComponent<QuickLoader>();
				m_instance.m_loader = loader;
				return m_instance;
			}

			public void quick_load() {
				this.StartCoroutine(quick_load_coroutine());
			}

			private IEnumerator quick_load_coroutine() {
				int _assetsToPreloadCount = (int) ReflectionUtils.get_field_value(this.m_loader, "_assetsToPreloadCount");
				MethodInfo UpdateProgressDuringStep = ReflectionUtils.get_method(this.m_loader, "UpdateProgressDuringStep");
				if (_assetsToPreloadCount > 0) {
					_info_log($"Preloading {_assetsToPreloadCount} assets.");
					AssetReference[] array = (AssetReference[]) ReflectionUtils.get_field_value(this.m_loader, "assetReferencesToPreload");
					int _assetsPreloadedCount = 0;
					List<ClothingLayerSprites> _preloadedAssets = (List<ClothingLayerSprites>) ReflectionUtils.get_field_value(this.m_loader, "_preloadedAssets");
					for (int i = 0; i < array.Length; i++) {
						AsyncOperationHandle<ClothingLayerSprites> asyncOperationHandle = array[i].LoadAssetAsync<ClothingLayerSprites>();
						asyncOperationHandle.Completed += new Action<AsyncOperationHandle<ClothingLayerSprites>>(delegate (AsyncOperationHandle<ClothingLayerSprites> operationHandle) {
							if (operationHandle.Status == AsyncOperationStatus.Succeeded) {
								_preloadedAssets.Add(operationHandle.Result);
							}
							_assetsPreloadedCount++;
						});
					}
					while (_assetsPreloadedCount < _assetsToPreloadCount) {
						float stepProgress = (float) _assetsPreloadedCount / (float) _assetsToPreloadCount;
						UpdateProgressDuringStep.Invoke(this.m_loader, new object[] { stepProgress, 0f, 0.2f });
						yield return null;
					}
				}
				_info_log("Launching main menu scene loader async operation.");
				ReflectionUtils.get_field(this.m_loader, "_currentProgress").SetValue(this.m_loader, 0.2f);
				AsyncOperation operation = SceneManager.LoadSceneAsync(1);
				operation.allowSceneActivation = false;
				_info_log("Waiting for operation completion.");
				while (!operation.isDone) {
					float stepProgress = Mathf.Clamp01(operation.progress / 0.9f);
					UpdateProgressDuringStep.Invoke(this.m_loader, new object[] { stepProgress, 0.2f, 0.8f });
					if (operation.progress >= 0.8999f) {
						ReflectionUtils.get_field(this.m_loader, "_currentProgress").SetValue(this.m_loader, 1f);
						ReflectionUtils.get_field(this.m_loader, "_displayProgress").SetValue(this.m_loader, 1f);
						((UnityEngine.UI.Image) ReflectionUtils.get_field_value(this.m_loader, "fill")).fillAmount = 1f;
						_info_log("Main menu scene load complete.");
						yield return new WaitForSeconds(0.25f);
						operation.allowSceneActivation = true;
					}
					yield return null;
				}
				Application.runInBackground = GameManager.Multiplayer || Wish.Settings.RunInBackground;
			}
		}

		private static bool Prefix(MainMenuLoader __instance, GameObject ___logo, CanvasGroup ___animationCanvasGroup, CanvasGroup ___logoCanvasGroup, CanvasGroup ___loadingCanvasGroup, CanvasGroup ___logoNameCanvasGroup, ref bool ___enableDebugText, TextMeshProUGUI ___text, TextMeshProUGUI ___debugText, int ____assetsToPreloadCount, AssetReference[] ___assetReferencesToPreload) {
			try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				ReflectionUtils.invoke_method(__instance, "ResetProgressBar");
				___logo.SetActive(value: false);
				___animationCanvasGroup.alpha = 1f;
				___logoCanvasGroup.alpha = 0f;
				___loadingCanvasGroup.alpha = 0f;
				___logoNameCanvasGroup.alpha = 0f;
				ReflectionUtils.invoke_method(__instance, "EnsureValidScreenResolution");
				___enableDebugText = true;
				if (___enableDebugText && ___text != null) {
					___debugText = UnityEngine.Object.Instantiate(___text, ___text.transform.parent);
					___debugText.transform.localPosition += Vector3.up * 50f;
					___debugText.color = Color.yellow;
				}
				____assetsToPreloadCount = ___assetReferencesToPreload.Length;
				string systemLanguage = LocalizationManager.GetCurrentDeviceLanguage();
				LanguageLoader.LoadLanguageAsync(systemLanguage, delegate {
					if (LocalizationManager.HasLanguage(systemLanguage)) {
						LocalizationManager.CurrentLanguage = systemLanguage;
					}
					QuickLoader.create(__instance);
					QuickLoader.Instance.quick_load();
				});
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_MainMenuLoader_Start.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}
