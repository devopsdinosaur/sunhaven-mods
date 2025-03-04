using BepInEx;
using HarmonyLib;
using PSS;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

public static class PluginInfo {

    public const string TITLE = "No Mo Logo";
    public const string NAME = "no_logo";
    public const string SHORT_DESCRIPTION = "Cuts out the ten-second logo animation at the start of the game, simply displaying the final Pixel Sprout emblem for the quick initial load.";
	public const string EXTRA_DETAILS = "Cuts out the ten-second logo animation at the start of the game, simply displaying the final Pixel Sprout emblem for the quick initial load.  This mod is *not* designed to diminish the efforts of Pixel Sprout studios, but simply to speed up the load time without the extra fanfare.  To be honest, like Continue Button, I just made this to speed up my debugging, but I thought others might want it.  Enjoy!";

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

	[HarmonyPatch(typeof(GameLoader), "LateUpdate")]
	class HarmonyPatch_GameLoader_LateUpdate {

		class QuickLoader : MonoBehaviour {
			private static QuickLoader m_instance;
			public static QuickLoader Instance {
				get {
					return m_instance;
				}
			}
			private GameLoader m_loader;

			public static QuickLoader create(GameLoader loader) {
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
				for (;;) {
					bool found = false;
					foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
						if (obj.name == "UI_SplashScreen") {
							obj.SetActive(false);
							found = true;
							break;
						}
					}
					if (found) {
						break;
					}
					yield return new WaitForSeconds(0.1f);
				}
				
				/*
				foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
					if (obj.name == "UI_SplashScreen") {
						obj.SetActive(false);
						break;
					}
				}
				AsyncOperation loadOperation = SceneManager.LoadSceneAsync("Content", LoadSceneMode.Additive);
				yield return new WaitUntil(() => loadOperation.isDone);
				GameObject[] rootGameObjects = SceneManager.GetSceneByName("Content").GetRootGameObjects();
				MasterScriptableObjectContainer masterScriptableObjectContainer = null;
				GameObject[] array = rootGameObjects;
				for (int i = 0; i < array.Length; i++) {
					masterScriptableObjectContainer = array[i].GetComponent<MasterScriptableObjectContainer>();
					if (masterScriptableObjectContainer != null) {
						break;
					}
				}
				if (masterScriptableObjectContainer != null) {
					this.m_loader.GetType().GetField("masterContent", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this.m_loader, masterScriptableObjectContainer.masterContent);
				}
				AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync("Content");
				yield return new WaitUntil(() => unloadOperation.isDone);
				this.m_loader.LoadLevel(1);
				*/
			}
		}

		private static bool Prefix(GameLoader __instance) {
			try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				QuickLoader.create(__instance);
				QuickLoader.Instance.quick_load();
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_LoadMainMenu_LoadMasterContent.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}
