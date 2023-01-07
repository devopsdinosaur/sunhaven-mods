
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System.Reflection;
using UnityEngine.Events;


[BepInPlugin("devopsdinosaur.sunhaven.designated_driver", "Designated Driver", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.designated_driver");
	public static ManualLogSource logger;
	

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.designated_driver v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	private static void notify(string message) {
		logger.LogInfo(message);
		NotificationStack.Instance.SendNotification(message);
	}
	
	[HarmonyPatch(typeof(Player), "PassOut")]
	class HarmonyPatch_Player_PassOut {

		private static bool Prefix(ref Player __instance) {
			__instance.
				GetType().
				GetTypeInfo().
				GetDeclaredMethod("CheckIfAllPlayersSleeping").
				Invoke(__instance, new object[] {});
			notify("You passed out, but your invisible best friend drove you home.  What a pal!");
			return false;
		}
	}
}