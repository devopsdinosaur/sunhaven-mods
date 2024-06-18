using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;


[BepInPlugin("devopsdinosaur.sunhaven.no_more_deadlines", "No More Deadlines", "0.0.1")]
public class NoMoreDeadlinesPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.no_more_deadlines");
	public static ManualLogSource logger;

	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.no_more_deadlines v0.0.1 loaded.");
		m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
		this.m_harmony.PatchAll();
	}

	[HarmonyPatch(typeof(QuestManager), "Awake")]
    class HarmonyPatch_QuestManager_Awake {

        private static void Postfix() {
            if (!m_enabled.Value) {
				return;
			}
			foreach (QuestAsset quest in QuestManager.AllQuests) {
				quest.daysToDo = -1;
			}
        }
    }
}