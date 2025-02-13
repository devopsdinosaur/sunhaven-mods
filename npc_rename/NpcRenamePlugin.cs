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

public static class PluginInfo {

    public const string TITLE = "Rename";
    public const string NAME = "npc_rename";
    public const string SHORT_DESCRIPTION = "Enables configurable run-time (non-permanent) name changes of player character and all NPCs!";
	public const string EXTRA_DETAILS = "This mod does not make any permanent changes to the game files.  It simply modifies the strings in memory for the duration of the game.  Removing the mod and restarting the game will revert everything to its default state.";

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
public class NpcRenamePlugin : DDPlugin {
	private static NpcRenamePlugin m_instance = null;
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
        logger = this.Logger;
        try {
			m_instance = this;
            Settings.Instance.early_load(m_instance);
            m_instance.create_nexus_page();
            this.m_plugin_info = PluginInfo.to_dict();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            _error_log("** Awake FATAL - " + e);
        }
    }

    private static string m_original_character_name = null;

    [HarmonyPatch(typeof(GameManager), "Awake")]
	class HarmonyPatch_GameManager_Awake {
		private static void Postfix() {
			try {
                Settings.Instance.late_load();
                PropertyInfo name_property = ReflectionUtils.get_property(GameSave.Instance.CurrentSave.characterData, "characterName");
                if (m_original_character_name == null) {
                    m_original_character_name = (string) name_property.GetValue(GameSave.Instance.CurrentSave.characterData);
                }
                if (m_original_character_name != null && Settings.m_enabled.Value && !string.IsNullOrEmpty(Settings.m_player_name.Value)) {
                    name_property.SetValue(GameSave.Instance.CurrentSave.characterData, Settings.m_player_name.Value);
                    _info_log($"** name: '{(string) name_property.GetValue(GameSave.Instance.CurrentSave.characterData)}'");
                }
            } catch (Exception e) {
				logger.LogError("** HarmonyPatch_GameManager_Awake.Postfix ERROR - " + e);
			}
		}
	}

    [HarmonyPatch(typeof(LocalizeText), "TranslateText")]
    class HarmonyPatch_LocalizeText_TranslateText {
        private static bool Prefix(string key, string defaultText, ref string __result) {
            try {
                if (Settings.m_enabled.Value && Settings.m_npc_names.TryGetValue(key, out ConfigEntry<string> name) && !string.IsNullOrEmpty(name.Value)) {
                    __result = name.Value;
                    return false;
                }
                return true;
            } catch {}
            return true;
        }
    }
    /*
    [HarmonyPatch(typeof(Player), "InitializeAsOwner")]
    class HarmonyPatch_Player_InializeAsOwner {
        private static void Postfix() {
            try {
                object name_instance = ReflectionUtils.get_field_value(GameSave.Instance.CurrentSave.characterData, "_characterName");
                PropertyInfo name_property = ReflectionUtils.get_property(name_instance, "Value");
                if (m_original_character_name == null) {
                    m_original_character_name = (string) name_property.GetValue(name_instance);
                }
                if (m_original_character_name != null && Settings.m_enabled.Value && !string.IsNullOrEmpty(Settings.m_player_name.Value)) {
                    name_property.SetValue(name_instance, Settings.m_player_name.Value);
                    _info_log($"** name: '{(string) name_property.GetValue(name_instance)}'");
                }
            } catch (Exception e) {
                _error_log("** HarmonyPatch_Player_InializeAsOwner.Postfix ERROR - " + e);
            }
        }
    }
    */
    [HarmonyPatch(typeof(CharacterData), "characterName", MethodType.Getter)]
    class HarmonyPatch_CharacterData_characterName_getter {
        private static void Postfix(ref string __result) {
            try {
                if (m_original_character_name != null) {
                    __result = m_original_character_name;
                }
            } catch (Exception e) {
                _error_log("** HarmonyPatch_CharacterData_characterName_getter.Postfix ERROR - " + e);
            }
        }
    }
}