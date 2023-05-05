
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Wish;
using TMPro;
using System;
using System.Reflection;
using UnityEngine.Events;


[BepInPlugin("devopsdinosaur.sunhaven.consolidated_crafting", "Consolidated Crafting", "0.0.1")]
public class ConsolidatedCraftingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.consolidated_crafting");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static List<Recipe> m_all_recipes = null;
	private static Dictionary<string, RecipeList> m_table_recipes = new Dictionary<string, RecipeList>();
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.consolidated_crafting v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_descendants(child, callback);
		}
		return true;
	}
	
	[HarmonyPatch(typeof(CraftingTable), "Awake")]
	class HarmonyPatch_CraftingTable_Awake {

		private static bool Prefix(CraftingTable __instance, GameObject ___ui, RecipeList ___recipeList) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				if (m_all_recipes == null) {
					m_all_recipes = new List<Recipe>(Resources.FindObjectsOfTypeAll<Recipe>());
					foreach (RecipeList recipes in Resources.FindObjectsOfTypeAll<RecipeList>()) {
						m_table_recipes[recipes.name.Replace("RecipeList_", "").Replace('_', ' ').Replace("RecipeList", "").Trim()] = recipes;
					}
					if (!m_table_recipes.ContainsKey("Jam Maker")) {
						// jam maker has no RecipeList, for some reason
						RecipeList recipes = new RecipeList();
						foreach (Recipe recipe in m_all_recipes) {
							//logger.LogInfo(recipe.__name);
						}
					}
				}
				Transform adjacent_transform = null;
				RectTransform adjacent_rect = null;
				
				bool find_high_to_low_price_toggle(Transform transform) {
					if (transform.name == "HighToLowPriceToggle") {
						adjacent_transform = transform;
						adjacent_rect = transform.GetComponent<RectTransform>();
						return false;
					}
					return true;
				}
				
				enum_descendants(___ui.transform, find_high_to_low_price_toggle);
				if (adjacent_rect == null) {
					logger.LogError("** HarmonyPatch_CraftingTable_Awake ERROR - unable to locate 'HighToLowPriceToggle' object; cannot create dropdown.");
					return true;
				}
				TMP_Dropdown dropdown = GameObject.Instantiate<TMP_Dropdown>(Resources.FindObjectsOfTypeAll<TMP_Dropdown>()[0], adjacent_transform.parent);
				RectTransform dropdown_rect = dropdown.GetComponent<RectTransform>();
				dropdown_rect.localPosition = adjacent_rect.localPosition + Vector3.down * (adjacent_rect.rect.height / 2 + 20f);
				dropdown.onValueChanged.AddListener(new UnityAction<int>(delegate {
					FieldInfo field_info = __instance.GetType().GetField("_craftingRecipes", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field_info == null) {
						logger.LogError("** CraftingTable.TableTypeDropdown.onValueChanged ERROR - unable to access '_craftingRecipes' member and cannot set recipes.");
						return;
					}
					field_info.SetValue(__instance, m_table_recipes[dropdown.options[dropdown.value].text].craftingRecipes);
					__instance.Initialize();
				}));
				dropdown.options.Clear();
				int index = 0;
				int selected_index = -1;
				foreach (string name in m_table_recipes.Keys) {
					dropdown.options.Add(new TMP_Dropdown.OptionData(name));
					if (m_table_recipes[name] == ___recipeList) {
						selected_index = index;
					}
					index++;
				}
				dropdown.value = selected_index;
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_CraftingTable_Awake ERROR - " + e);
			}
			return true;
		}
	}
}