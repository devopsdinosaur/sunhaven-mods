
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using System.IO;
using TMPro;


[BepInPlugin("devopsdinosaur.sunhaven.continue_button", "Continue Button", "0.0.2")]
public class ContinueButtonPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.continue_button");
	public static ManualLogSource logger;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.continue_button v0.0.2 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(MainMenuController), "HomeMenu")]
	class HarmonyPatch_MainMenuController_HomeMenu {

		private static GameObject m_continue_button = null;
		private static bool m_is_first_load = true;

		private static void Postfix(MainMenuController __instance, ref GameObject ___homeMenu) {
			string saves_dir = Path.Combine(Application.persistentDataPath, "Saves");
			int latest_save_index = -1;
			DateTime latest_timestamp = DateTime.Now;
			string full_path;
			Transform play_button = ___homeMenu.transform.Find("Buttons").GetChild(0);

			if (m_continue_button != null || !Directory.Exists(saves_dir)) {
				return;
			}
			for (int index = 0; index < GameSave.Instance.Saves.Count; index++) {
				full_path = Path.Combine(saves_dir, GameSave.Instance.Saves[index].fileName);
				if (latest_save_index == -1 || File.GetLastWriteTime(full_path) > latest_timestamp) {
					latest_save_index = index;
					latest_timestamp = File.GetLastWriteTime(full_path);
				}
			}
			if (latest_save_index == -1 || play_button == null) {
				return;
			}
			m_continue_button = GameObject.Instantiate<GameObject>(play_button.gameObject, play_button.parent);
			m_continue_button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Continue";
			m_continue_button.transform.SetAsFirstSibling();
			
			//enables using the keyboard or a controller to use the up/down keys/buttons to select the continue button
			NavigationElement navElement = play_button.gameObject.GetComponent<NavigationElement>();
        		navElement.up = m_continue_button.GetComponent<NavigationElement>();
        		m_continue_button.GetComponent<NavigationElement>().down = navElement;
			
			m_continue_button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate {
				__instance.PlayGame(latest_save_index);
			});
			if (m_is_first_load) {
				m_is_first_load = false;
				foreach (string arg in System.Environment.GetCommandLineArgs()) {
					if (arg.ToLower() == "--continue") {
						__instance.PlayGame(latest_save_index);
					}
				}
			}
		}
	}
}
