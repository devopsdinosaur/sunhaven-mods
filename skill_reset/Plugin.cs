
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System.Reflection;
using UnityEngine.Events;


[BepInPlugin("devopsdinosaur.sunhaven.skill_reset", "Skill Reset", "0.0.1")]
public class Plugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.skill_reset");
	public static ManualLogSource logger;
	public static Dictionary<ProfessionType, GameObject> m_reset_buttons = new Dictionary<ProfessionType, GameObject>();

	public Plugin() {
	}

	private void Awake() {
		Plugin.logger = this.Logger;
		logger.LogInfo((object) "devopsdinosaur.sunhaven.skill_reset v0.0.1 loaded.");
		this.m_harmony.PatchAll();
	}

	public static void reset_profession(Skills skills, ProfessionType profession_type) {
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
	}

	[HarmonyPatch(typeof(Skills), "SetupProfession")]
	class HarmonyPatch_Skills_SetupProfession {

		private static void Postfix(Skills __instance, ProfessionType profession, SkillTree panel) {
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
		}
	}
}