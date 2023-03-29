using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Wish;
using System;
using System.Collections.Generic;
using UnityEngine;


[BepInPlugin("devopsdinosaur.sunhaven.easy_animals", "Easy Animals", "0.0.1")]
public class EasyAnimalsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.easy_animals");
	public static ManualLogSource logger;
	
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_pet_relationship_inc;
	private static ConfigEntry<bool> m_never_hungry;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_pet_relationship_inc = this.Config.Bind<float>("General", "Relationship Increase on Pet", 20f, "The extra amount to increase your relationship with the animal when petting (float, set to 0 to disable, this is in addition to the game default of 1f [max relationship is 20f])");
			m_never_hungry = this.Config.Bind<bool>("General", "Never Hungry", true, "If true then animals never need food");
			this.m_harmony.PatchAll();	
			logger.LogInfo((object) "devopsdinosaur.sunhaven.easy_animals v0.0.1 loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Animal), "PetAnimal")]
	class HarmonyPatch_Animal_PetAnimal {

		private static bool Prefix(Animal __instance) {
			try {
				if (!m_enabled.Value) {
					return true;
				}
				__instance.animalItem.animalData.relationship = Mathf.Clamp(__instance.animalItem.animalData.relationship + m_pet_relationship_inc.Value, 0f, Animal.MaxRelationship);
				return true;
			} catch (Exception e) {
				logger.LogError("** Animal.PetAnimal_Prefix ERROR - " + e);
			}
			return true;
		}
	}
	
	[HarmonyPatch(typeof(Animal), "SendPetEvent")]
	class HarmonyPatch_Animal_SendPetEvent {

		private static bool Prefix(Animal __instance, AnimalEventType eventType) {
			try {
				if (!m_enabled.Value || eventType != AnimalEventType.Pet) {
					return true;
				}
				__instance.animalItem.animalData.relationship = Mathf.Clamp(__instance.animalItem.animalData.relationship + m_pet_relationship_inc.Value, 0f, Animal.MaxRelationship);
				return true;
			} catch (Exception e) {
				logger.LogError("** Animal.SendPetEvent_Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(NPCManager), "UpdateAnimalsOvernight")]
	class HarmonyPatch_NPCManager_UpdateAnimalsOvernight {

		private static bool Prefix(NPCManager __instance) {
			try {
				if (!(m_enabled.Value && m_never_hungry.Value)) {
					return true;
				}
				foreach (Animal animal in __instance.animals.Values) {
					animal.animalItem.animalData.hunger = 3;
				}
				return true;
			} catch (Exception e) {
				logger.LogError("** NPCManager.UpdateAnimalsOvernight_Prefix ERROR - " + e);
			}
			return true;
		}
	}
}