using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Reflection;

[BepInPlugin("devopsdinosaur.sunhaven.no_logo", "No Logo", "0.0.1")]
public class NoLogoPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_logo");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.no_logo v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(LoadMainMenu), "LoadMasterContent")]
	class HarmonyPatch_LoadMainMenu_LoadMasterContent {

		class QuickLoader : MonoBehaviour {
			private static QuickLoader m_instance;
			public static QuickLoader Instance {
				get {
					return m_instance;
				}
			}
			private LoadMainMenu m_loader;

			public static QuickLoader create(LoadMainMenu loader) {
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
			}
		}

		private static bool Prefix(LoadMainMenu __instance) {
			try {
				if (!m_enabled.Value) {
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
