﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using SMLHelper;
using SMLHelper.Patchers;
using Harmony;
using DecorationsMod.Fixers;

namespace DecorationsMod
{
    public class DecorationsMod
    {
        // Harmony stuff
        internal static HarmonyInstance HarmonyInstance = null;
        internal static Dictionary<TechType, CraftData.BackgroundType> CustomBackgroundTypes = new Dictionary<TechType, CraftData.BackgroundType>(TechTypeExtensions.sTechTypeComparer);
        internal static Dictionary<TechType, float> CustomCharges = new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer);
        internal static Dictionary<TechType, int> CustomFinalCutBonusList = new Dictionary<TechType, int>(TechTypeExtensions.sTechTypeComparer);

        public static void Patch()
        {
            // 1) INITIALIZE HARMONY
            HarmonyInstance = HarmonyInstance.Create("com.osubmarin.decorationsmod");

            // 2) LOAD CONFIGURATION
            ConfigSwitcher.LoadConfiguration();

            // 3) REGISTER DECORATION ITEMS
            List<IDecorationItem> decorationItems = RegisterDecorationItems();

            // 4) MAKE SOME EXISTING ITEMS PICKUPABLE & POSITIONABLE
            if (ConfigSwitcher.EnablePlaceItems)
                PlaceToolItems.MakeItemsPlaceable();
            
            // 5) REGISTER DECORATIONS FABRICATOR
            CustomFabricator.RegisterDecorationsFabricator(decorationItems);

            // 6) REGISTER FLORA FABRICATOR
            CustomFabricator.RegisterFloraFabricator(decorationItems);
            
            // 7) HARMONY PATCHING
            Logger.Log("Patching with Harmony...");
            // Patch dictionaries
            Utility.PatchDictionary(typeof(CraftData), "backgroundTypes", CustomBackgroundTypes);
            Utility.PatchDictionary(typeof(CraftData), "harvestFinalCutBonusList", CustomFinalCutBonusList);
            Utility.PatchDictionary(typeof(BaseBioReactor), "charge", CustomCharges);
            // Give salt when purple pinecone is cut
            var giveResourceOnDamageMethod = typeof(Knife).GetMethod("GiveResourceOnDamage", BindingFlags.NonPublic | BindingFlags.Instance);
            var giveResourceOnDamagePostfix = typeof(KnifeFixer).GetMethod("GiveResourceOnDamage_Postfix", BindingFlags.Public | BindingFlags.Static);
            HarmonyInstance.Patch(giveResourceOnDamageMethod, null, new HarmonyMethod(giveResourceOnDamagePostfix));
            // Make plants undropable
            var canDropItemHereMethod = typeof(Inventory).GetMethod("CanDropItemHere", BindingFlags.Public | BindingFlags.Static);
            var canDropItemHerePrefix = typeof(InventoryFixer).GetMethod("CanDropItemHere_Prefix", BindingFlags.Public | BindingFlags.Static);
            HarmonyInstance.Patch(canDropItemHereMethod, new HarmonyMethod(canDropItemHerePrefix), null);
            // Change custom plants tooltips
            var onHandHoverMethod = typeof(GrownPlant).GetMethod("OnHandHover", BindingFlags.Public | BindingFlags.Instance);
            var onHandHoverPostfix = typeof(GrownPlantFixer).GetMethod("OnHandHover_Postfix", BindingFlags.Public | BindingFlags.Static);
            HarmonyInstance.Patch(onHandHoverMethod, null, new HarmonyMethod(onHandHoverPostfix));
            // Fix cargo crates items-containers
            var onProtoDeserializeObjectTreeMethod = typeof(StorageContainer).GetMethod("OnProtoDeserializeObjectTree", BindingFlags.Public | BindingFlags.Instance);
            var onProtoDeserializeObjectTreePostfix = typeof(StorageContainerFixer).GetMethod("OnProtoDeserializeObjectTree_Postfix", BindingFlags.Public | BindingFlags.Static);
            HarmonyInstance.Patch(onProtoDeserializeObjectTreeMethod, null, new HarmonyMethod(onProtoDeserializeObjectTreePostfix));
            // Failsafe on lockers and cargo crates deconstruction
            var canDeconstructMethod = typeof(Constructable).GetMethod("CanDeconstruct", BindingFlags.Public | BindingFlags.Instance);
            var canDeconstructPrefix = typeof(ConstructableFixer).GetMethod("CanDeconstruct_Prefix", BindingFlags.Public | BindingFlags.Static);
            HarmonyInstance.Patch(canDeconstructMethod, new HarmonyMethod(canDeconstructPrefix), null);
            // Fix equipment types for batteries, power cells, and their ion versions
            if (ConfigSwitcher.EnablePlaceBatteries)
            {
                var allowedToAddMethod = typeof(Equipment).GetMethod("AllowedToAdd", BindingFlags.Public | BindingFlags.Instance);
                var allowedToAddPrefix = typeof(EquipmentFixer).GetMethod("AllowedToAdd_Prefix", BindingFlags.Public | BindingFlags.Static);
                HarmonyInstance.Patch(allowedToAddMethod, new HarmonyMethod(allowedToAddPrefix), null);
                var addOrSwapMethod = typeof(Inventory).GetMethod("AddOrSwap", new Type[] { typeof(InventoryItem), typeof(Equipment), typeof(string) }); //, BindingFlags.Public | BindingFlags.Static);
                var addOrSwapPrefix = typeof(InventoryFixer).GetMethod("AddOrSwap_Prefix", BindingFlags.Public | BindingFlags.Static);
                HarmonyInstance.Patch(addOrSwapMethod, new HarmonyMethod(addOrSwapPrefix), null);
                var canSwitchOrSwapMethod = typeof(uGUI_Equipment).GetMethod("CanSwitchOrSwap", BindingFlags.Public | BindingFlags.Instance);
                var canSwitchOrSwapPrefix = typeof(uGUI_EquipmentFixer).GetMethod("CanSwitchOrSwap_Prefix", BindingFlags.Public | BindingFlags.Static);
                HarmonyInstance.Patch(canSwitchOrSwapMethod, new HarmonyMethod(canSwitchOrSwapPrefix), null);
            }
        }

        private static void RegisterRecipeForTechType(TechType techType, TechType resource, int resourceAmount = 1, int craftAmount = 1)
        {
            // Associate recipe to the new TechType
            CraftDataPatcher.customTechData[techType] = new TechDataHelper()
            {
                _craftAmount = craftAmount,
                _ingredients = new List<IngredientHelper>(new IngredientHelper[1] {
                    new IngredientHelper(resource, resourceAmount)
                }),
                _techType = techType
            };
        }

        private static List<IDecorationItem> RegisterDecorationItems()
        {
            List<IDecorationItem> result = new List<IDecorationItem>();

            Logger.Log("Registering items...");

            // Get the list of modified existing items
            var existingItems = from t in Assembly.GetExecutingAssembly().GetTypes() 
                                where t.IsClass && t.Namespace == "DecorationsMod.ExistingItems" 
                                select t;
            // Register modified existing items
            foreach (Type existingItemType in existingItems)
            {
                // Get item
                DecorationItem existingItem = (DecorationItem)(Activator.CreateInstance(existingItemType));
                // Register item
                existingItem.RegisterItem();
                // Unlock item at game start
                KnownTechPatcher.unlockedAtStart.Add(existingItem.TechType);
                // Store item in the list
                result.Add(existingItem);
            }

            // Get the list of new items
            var newItems = from t in Assembly.GetExecutingAssembly().GetTypes() 
                           where t.IsClass && t.Namespace == "DecorationsMod.NewItems" 
                           select t;
            // Register new items
            foreach (Type newItemType in newItems)
            {
                DecorationItem newItem = (DecorationItem)(Activator.CreateInstance(newItemType));

                // If current item is not a nutrient block continue, otherwise if that is a nutrient block
                // we continue only if nutrient blocks are enabled in Config.txt file.
                if (newItem.TechType != TechType.NutrientBlock || (newItem.TechType == TechType.NutrientBlock && ConfigSwitcher.EnableNutrientBlock))
                {
                    // If decoration items from habitat builder are enabled, add everything.
                    // Otherwise add only items that are not from habitat builder.
                    if (ConfigSwitcher.EnableSpecialItems || (!ConfigSwitcher.EnableSpecialItems && !newItem.IsHabitatBuilder))
                    {
                        newItem.RegisterItem();
                        result.Add(newItem);
                    }
                }
            }

            // Get the list of new land flora
            var newFlora = from t in Assembly.GetExecutingAssembly().GetTypes()
                           where t.IsClass && t.Namespace == "DecorationsMod.Flora"
                           select t;
            // Register new land flora
            foreach (Type newItemType in newFlora)
            {
                DecorationItem newItem = (DecorationItem)(Activator.CreateInstance(newItemType));
                
                newItem.RegisterItem();
                result.Add(newItem);
            }

            // Get the list of new water flora
            var newWaterFlora = from t in Assembly.GetExecutingAssembly().GetTypes()
                           where t.IsClass && t.Namespace == "DecorationsMod.FloraAquatic"
                           select t;
            // Register new water flora
            foreach (Type newItemType in newWaterFlora)
            {
                DecorationItem newItem = (DecorationItem)(Activator.CreateInstance(newItemType));

                newItem.RegisterItem();
                result.Add(newItem);
            }
            
            // Register existing air seeds recipes
            if (ConfigSwitcher.EnableRegularAirSeeds)
            {
                RegisterRecipeForTechType(TechType.BulboTreePiece, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleVegetable, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.HangingFruit, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.MelonSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.FernPalmSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.OrangePetalsPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleVasePlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.OrangeMushroomSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PinkMushroomSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleRattleSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PinkFlowerSeed, ConfigSwitcher.FloraRecipiesResource);
            }
            
            // Register existing water seeds recipes
            if (ConfigSwitcher.EnableRegularWaterSeeds)
            {
                RegisterRecipeForTechType(TechType.GabeSFeatherSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.RedGreenTentacleSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.SeaCrownSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.ShellGrassSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleBranchesSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.RedRollPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.RedBushSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleStalkSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.SpottedLeavesPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.AcidMushroomSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.WhiteMushroomSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.JellyPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.SmallFanSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleFanSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleTentacleSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.BluePalmSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.EyesPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.MembrainTreeSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.RedConePlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.RedBasketPlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.SnakeMushroomSpore, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.SpikePlantSeed, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.CreepvinePiece, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.CreepvineSeedCluster, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.BloodOil, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.PurpleBrainCoralPiece, ConfigSwitcher.FloraRecipiesResource);
                RegisterRecipeForTechType(TechType.KooshChunk, ConfigSwitcher.FloraRecipiesResource);
            }

            // Register lamp tooltip
            LanguagePatcher.customLines.Add("ToggleLamp", LanguageHelper.GetFriendlyWord("LampTooltip"));
            // Register seamoth doll tooltip
            LanguagePatcher.customLines.Add("SwitchSeamothModel", LanguageHelper.GetFriendlyWord("SwitchSeamothModel"));
            // Register exosuit doll tooltip
            LanguagePatcher.customLines.Add("SwitchExosuitModel", LanguageHelper.GetFriendlyWord("SwitchExosuitModel"));
            // Register cargo boxes tooltip
            LanguagePatcher.customLines.Add("AdjustCargoBoxSize", LanguageHelper.GetFriendlyWord("AdjustCargoBoxSize"));
            // Register forklift tooltip
            LanguagePatcher.customLines.Add("AdjustForkliftSize", LanguageHelper.GetFriendlyWord("AdjustForkliftSize"));
            // Register cove tree tooltip
            LanguagePatcher.customLines.Add("DisplayCoveTreeEggs", LanguageHelper.GetFriendlyWord("DisplayCoveTreeEggs"));

            return result;
        }
    }
}
