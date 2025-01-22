using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wish;
using UnityEngine;
using I2.Loc;

public class SkillNodeDict {
    private static SkillNodeDict m_instance = null;
    public static SkillNodeDict Instance => m_instance;
    public Dictionary<string, SkillNodeInfo> m_skills;

    public SkillNodeDict(EventHandler change_callback) {
        m_instance = this;
        this.m_skills= new Dictionary<string, SkillNodeInfo>();
        foreach (SkillTreeAsset tree in Resources.FindObjectsOfTypeAll<SkillTreeAsset>()) {
            for (int column = 0; column < 10; column++) {
                for (int row = 0; row < tree.numRows; row++) {
                    SkillNodeInfo node = new SkillNodeInfo(tree, column, row, change_callback);
                    this.m_skills[node.m_key] = node;
                }
            }
		}
    }

    public void on_settings_changed() {
        foreach (SkillNodeInfo node in this.m_skills.Values) {
            node.update_description_items();
        }
    }

    private void get_node_amount(string node, ref int __result) {
        try {
            if (Settings.m_enabled.Value && this.m_skills.TryGetValue(node, out SkillNodeInfo item)) {
                //__result = Mathf.CeilToInt((float) __result * item.get_multiplier());
                if (__result != 0) {
                    DDPlugin._debug_log($"get_node_amount({item.m_node.nodeTitle}) = {__result}");
                }
            }
		} catch (Exception e) {
			DDPlugin._error_log("** SkillNodeDict.get_node_amount ERROR - " + e);
		}
    }
    
    //[HarmonyPatch(typeof(Profession), "GetNodeAmount")]
	class HarmonyPatch_Profession_GetNodeAmount {
		private static void Postfix(string node, ref int __result) {
			m_instance.get_node_amount(node, ref __result);
		}
	}
}