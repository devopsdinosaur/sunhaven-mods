using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Wish;
using TMPro;
using System.Reflection;
using UnityEngine.Events;

public static class PluginInfo {

    public const string TITLE = "Skill Reset";
    public const string NAME = "skill_reset";
    public const string SHORT_DESCRIPTION = "Regretting that \"Pen Pal\" thing?  Or decided crossbow is better than sword?  How about a skill reset!  This mod adds a \"Reset\" text button to each skill tab to enable instant reset of skills for free and as often as you need.";

    public const string VERSION = "0.0.3";

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
public class SelfPortraitPlugin:DDPlugin {
    private Harmony m_harmony = new Harmony(PluginInfo.GUID);

    private void Awake() {
        logger = this.Logger;
        try {
            this.m_plugin_info = PluginInfo.to_dict();
            Settings.Instance.load(this);
            DDPlugin.set_log_level(Settings.m_log_level.Value);
            this.create_nexus_page();
            this.m_harmony.PatchAll();
            logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
        } catch (Exception e) {
            logger.LogError("** Awake FATAL - " + e);
        }
    }

    public static void reset_profession(Skills skills, ProfessionType profession_type) {
		try {
			Profession profession = GameSave.Instance.CurrentSave.characterData.Professions[profession_type];
			string profession_string = profession_type.ToString();
			string column_string;
			string key;

			for (int column = 1; column <= 10; column++) {
				column_string = column.ToString();
				foreach (char letter in "abcd") {
					if (profession_type == ProfessionType.Fishing && letter == 'd') {
						continue;
					}
					key = profession_string + column_string + letter;
					profession.nodes[key.GetStableHashCode()] = 0;
				}
			}
			Skills.skillPointsUsed[profession_type] = 0;
			skills.EnablePanelWithAvailableSkillPoint();
		} catch (Exception e) {
			logger.LogError("** reset_profession ERROR - " + e);
		}
	}

	[HarmonyPatch(typeof(Skills), "SetupProfession")]
	class HarmonyPatch_Skills_SetupProfession {

		private static void Postfix(Skills __instance, ProfessionType profession, SkillTree panel) {
			try {
				TextMeshProUGUI _skillPointsTMP = (TextMeshProUGUI) panel.
					GetType().
					GetTypeInfo().
					GetField("_skillPointsTMP", BindingFlags.Instance | BindingFlags.NonPublic).
					GetValue(panel);
				GameObject reset_button = GameObject.Instantiate<GameObject>(_skillPointsTMP.gameObject, _skillPointsTMP.transform.parent);
				TextMeshProUGUI label = reset_button.GetComponent<TextMeshProUGUI>();
				reset_button.transform.position = _skillPointsTMP.transform.position + Vector3.down * _skillPointsTMP.GetComponent<RectTransform>().rect.height;
				label.fontSize = 12;
				label.text = "[Reset]";
				reset_button.AddComponent<UnityEngine.UI.Button>().onClick.AddListener((UnityAction) delegate {
					reset_profession(__instance, profession);
				});
				m_reset_buttons[profession] = reset_button;
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_Skills_SetupProfession.Postfix ERROR - " + e);
            }
        }
	}
}