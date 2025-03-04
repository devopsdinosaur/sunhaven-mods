using BepInEx;
using HarmonyLib;
using Wish;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using I2.Loc;

public static class PluginInfo {

    public const string TITLE = "Continue Button";
    public const string NAME = "continue_button";
    public const string SHORT_DESCRIPTION = "Single Player only. Adds a 'Continue' option to the main menu to jump straight to the last save written. Also adds a '--continue' option to the command-line to auto load your game right at launch!";
	public const string EXTRA_DETAILS = "";

	public const string VERSION = "0.0.9";

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
public class TestingPlugin:DDPlugin {
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
                if (!Settings.m_enabled.Value) {
                    return;
                }
                string saves_dir = Path.Combine(Application.persistentDataPath, "Saves");
                if (m_continue_button != null || !Directory.Exists(saves_dir)) {
                    return;
                }
                Transform play_buttons_container = null;
                foreach (Transform child in ___homeMenu.transform) {
                    if (child.gameObject.activeSelf) {
                        if ((play_buttons_container = child.Find("PlayButtons")) == null) {
                            continue;
                        }
                        break;
                    }
                }
                Transform play_button = null;
                if (play_buttons_container == null || (play_button = play_buttons_container.Find("PlayButton")) == null) {
                    logger.LogError("** HarmonyPatch_MainMenuController_HomeMenu.Postfix ERROR - unable to locate platform menu container => PlayButtons.");
                    return;
                }
                var lastModifiedSave = SingletonBehaviour<GameSave>.Instance.Saves
                    .OrderByDescending(save => File.GetLastWriteTime(Path.Combine(saves_dir, save.fileName)))
                    .FirstOrDefault();
                if (lastModifiedSave == null) {
                    return;
                }
                var characterIndex = SingletonBehaviour<GameSave>.Instance.Saves.IndexOf(lastModifiedSave);
                var menuRect = play_buttons_container.GetComponent<RectTransform>();
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, 310);
                m_continue_button = GameObject.Instantiate<GameObject>(play_button.gameObject, play_button.parent);
                m_continue_button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = GetLocalizedContinue();
                m_continue_button.transform.SetAsFirstSibling();
                m_continue_button.name = "ContinueButton";
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
