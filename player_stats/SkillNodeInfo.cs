using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;

public class SkillNodeInfo {
    private static char[] CHARS = new char[] {'a', 'b', 'c', 'd'};
    private static char[] INVALID_NAME_CHARS = new char[] {'\n', '\t', '\\', '"', '\'', '[', ']'};
    public SkillTreeAsset m_tree;
    public int m_column;
    public int m_row;
    public string m_key;
    public SkillNodeAsset m_node;
    public ConfigEntry<float> m_multiplier;

    public SkillNodeInfo(SkillTreeAsset tree, int column, int row, EventHandler change_callback) {
        this.m_tree = tree;
        this.m_column = column;
        this.m_row = row;
        this.m_key = tree.profession.ToString() + column.ToString() + CHARS[row];
        this.m_node = tree.skillNodes[column, row];
        string name = $"Skills - {this.m_key} - {this.m_node.nodeTitle} - Multiplier";
        string fixed_name = "";
        foreach (char c in name) {
            if (!INVALID_NAME_CHARS.Contains(c)) {
                fixed_name += c;
            }
        }
        this.m_multiplier = Settings.Instance.create_entry<float>("Skills", fixed_name, 1f, $"[float] Multiplier applied to the numeric values of the '{this.m_node.nodeTitle}' skill.  Has no effect if the skill does not use numeric values or this setting is 1 or less than or equal to zero.  Applies only during gameplay. [default 1, no effect]", change_callback);
        this.update_description_items();
    }

    public float get_multiplier() {
        return (!Settings.m_enabled.Value || this.m_multiplier.Value <= 0 ? 1 : this.m_multiplier.Value);
    }

    public void update_description_items() {
        string update_item(string text) {
            bool is_float = true;
            bool is_int = true;
            foreach (char c in text) {
                if (c == '.') {
                    is_int = false;
                } else if (!char.IsDigit(c)) {
                    is_float = is_int = false;
                    break;
                }
            }
            return 
                is_float ?
                    (float.Parse(text) * this.get_multiplier()).ToString() :
                is_int ?
                    Mathf.CeilToInt((float) int.Parse(text) * this.get_multiplier()).ToString() :
                text;
        }
        
        for (int index = 0; index < this.m_node.skillDescriptionsItems.Count; index++) {
            SkillDescription item = this.m_node.skillDescriptionsItems[index];
            if (!string.IsNullOrEmpty(item.keyText)) {
                continue;
            }
            item.text = update_item(item.text);
            if (this.m_node.backupdescriptionItems.Count > index) {
                this.m_node.backupdescriptionItems[index] = update_item(this.m_node.backupdescriptionItems[index]);
            }
            if (this.m_node.thirdDescriptionItems.Count > index) {
                this.m_node.thirdDescriptionItems[index] = update_item(this.m_node.thirdDescriptionItems[index]);
            }
        }
    }
}
