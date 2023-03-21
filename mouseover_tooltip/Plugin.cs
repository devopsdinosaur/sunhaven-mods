using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.mouseover_tooltip", "Mouseover Tooltip", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.mouseover_tooltip");
	public static ManualLogSource logger;


	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.mouseover_tooltip v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(Player), "Update")]
	class HarmonyPatch_Player_Update {

		private static void Postfix() {
			// EventSystem.current.IsPointerOverGameObject()		
		}
	}

}