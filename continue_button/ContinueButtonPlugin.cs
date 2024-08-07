using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using TMPro;
using I2.Loc;

public static class PluginInfo {

    public const string TITLE = "Continue Button";
    public const string NAME = "continue_button";

    public const string VERSION = "0.0.7";
    public static string[] CHANGELOG = new string[] {
        "v0.0.7 - Updates from p1xel8ted to fix misc UI glitches with the button"
    };

    public const string AUTHOR = "devopsdinosaur";
    public const string GAME = "sunhaven";
    public const string GUID = AUTHOR + "." + GAME + "." + NAME;
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class ContinueButtonPlugin :BaseUnityPlugin {

    private Harmony m_harmony = new Harmony(PluginInfo.GUID);
    public static ManualLogSource logger;
    private static ConfigEntry<bool> m_enabled;
    
    private void Awake() {
        logger = this.Logger;
        try {
            m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            logger.LogError("** Awake FATAL - " + e);
        }
    }

    private static string GetLocalizedContinue()
    {
        var language = LocalizationManager.CurrentLanguageCode;
        return language switch
        {
            "da" => "Fortsæt",
            "de" => "Fortsetzen",
            "en" => "Continue",
            "es" => "Continuar",
            "fr" => "Continuer",
            "it" => "Continua",
            "ja" => "続行",
            "ko" => "계속",
            "nl" => "Doorgaan",
            "pt" => "Continuar",
            "pt-BR" => "Continuar",
            "ru" => "Продолжить",
            "sv" => "Fortsätt",
            "uk" => "Продовжити",
            "zh-CN" => "继续",
            "zh-TW" => "繼續",
            _ => "Continue"
        };
    }

    [HarmonyPatch(typeof(MainMenuController), "HomeMenu")]
    [HarmonyBefore("p1xel8ted.sunhaven.keepalive")]
    class HarmonyPatch_MainMenuController_HomeMenu {

        private static GameObject m_continue_button = null;
        private static bool m_is_first_load = true;

        private static void Postfix(MainMenuController __instance, ref GameObject ___homeMenu) {
            try {
                string saves_dir = Path.Combine(Application.persistentDataPath, "Saves");
            
                Transform playButtonsHolder = ___homeMenu.transform.Find("PlayButtons");
                Transform play_button = playButtonsHolder.GetChild(0);

                if (m_continue_button != null || !Directory.Exists(saves_dir)) {
                    return;
                }
            
                //get last modified save
                var lastModifiedSave = SingletonBehaviour<GameSave>.Instance.Saves
                    .OrderByDescending(save => File.GetLastWriteTime(Path.Combine(saves_dir, save.fileName)))
                    .FirstOrDefault();
            
                if (lastModifiedSave == null) return;
            
                //get index of last modified save
                var characterIndex = SingletonBehaviour<GameSave>.Instance.Saves.IndexOf(lastModifiedSave);
         
                //resize menu border to fit button
                var menuRect = playButtonsHolder.GetComponent<RectTransform>();
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, 310);
            
                m_continue_button = GameObject.Instantiate<GameObject>(play_button.gameObject, play_button.parent);
            
                //get localized continue text
                m_continue_button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = GetLocalizedContinue();
                m_continue_button.transform.SetAsFirstSibling();
            
                //set name for easier locating by other mods
                m_continue_button.name = "ContinueButton";

                //this gets overridden somewhere
                //deselct play button (Red highlight)
                //var playHighlight = play_button.GetComponent<HighlightButton>();
                //playHighlight.Deselect();

                //highlight continue button
                //var continueHighlight = m_continue_button.GetComponent<HighlightButton>();
                //continueHighlight.Select();
            
                GameObject.Destroy(m_continue_button.transform?.GetChild(0)?.gameObject?.GetComponent<Localize>());
                m_continue_button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate {
                    __instance.PlayGame(characterIndex);
                });
                if (m_is_first_load) {
                    m_is_first_load = false;
                    foreach (string arg in System.Environment.GetCommandLineArgs()) {
                        if (arg.ToLower() == "--continue") {
                            __instance.PlayGame(characterIndex);
                        }
                    }
                }
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_MainMenuController_HomeMenu.Postfix ERROR - " + e);
            }
        }
    }
}
