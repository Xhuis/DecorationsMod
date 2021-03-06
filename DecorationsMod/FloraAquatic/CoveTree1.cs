﻿using DecorationsMod.Controllers;
using SMLHelper;
using SMLHelper.Patchers;
using System.Collections.Generic;
using UnityEngine;

namespace DecorationsMod.FloraAquatic
{
    public class CoveTree1 : DecorationItem, ICustomFlora
    {
        public CustomFlora Config = new CustomFlora();
        CustomFlora ICustomFlora.Config
        {
            get => this.Config;
            set => this.Config = value;
        }

        public CoveTree1()
        {
            this.ClassID = "CoveTree1"; // 0e7cc3b9-cdf2-42d9-9c1f-c11b94277c19
            this.ResourcePath = "WorldEntities/Doodads/Lost_river/lost_river_cove_tree_01";

            this.GameObject = Resources.Load<GameObject>(this.ResourcePath);

            this.TechType = TechTypePatcher.AddTechType(this.ClassID,
                                                        LanguageHelper.GetFriendlyWord("CoveTreeName"),
                                                        LanguageHelper.GetFriendlyWord("CoveTreeDescription"),
                                                        true);

            this.Recipe = new TechDataHelper()
            {
                _craftAmount = 1,
                _ingredients = new List<IngredientHelper>(new IngredientHelper[1] {
                    new IngredientHelper(ConfigSwitcher.FloraRecipiesResource, 2)
                }),
                _techType = this.TechType
            };

            this.Config = ConfigSwitcher.config_CoveTree1;
        }

        public override void RegisterItem()
        {
            if (this.IsRegistered == false)
            {
                // Set item occupies 4 slots
                CraftDataPatcher.customItemSizes[this.TechType] = new Vector2int(2, 2);

                // Add the new TechType to Harvest types
                CraftDataPatcher.customHarvestTypeList.Add(this.TechType, HarvestType.DamageAlive);
                CraftDataPatcher.customHarvestOutputList.Add(this.TechType, this.TechType);

                // Set item background to normal (both land & water plant)
                // TODO: Replace with a call to SMLHelper when pull request gets released
                DecorationsMod.CustomBackgroundTypes.Add(this.TechType, CraftData.BackgroundType.Normal);

                // Set item bioreactor charge
                // TODO: Replace with a call to SMLHelper when pull request gets released
                DecorationsMod.CustomCharges.Add(this.TechType, this.Config.Charge);

                // Set the buildable prefab
                CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(this.ClassID, $"{DecorationItem.DefaultResourcePath}{this.ClassID}", this.TechType, this.GetPrefab));

                // Set the custom sprite
                CustomSpriteHandler.customSprites.Add(new CustomSprite(this.TechType, AssetsHelper.Assets.LoadAsset<Sprite>("covetreeicon")));

                // Associate recipe to the new TechType
                CraftDataPatcher.customTechData[this.TechType] = this.Recipe;

                this.IsRegistered = true;
            }
        }

        public override GameObject GetPrefab()
        {
            GameObject prefab = GameObject.Instantiate(this.GameObject);

            prefab.name = this.ClassID;
            
            // Get sub objects
            GameObject model = prefab.FindChild("lost_river_cove_tree_01");
            GameObject eggs = model.FindChild("lost_river_cove_tree_01_eggs");
            GameObject shells = model.FindChild("lost_river_cove_tree_01_eggs_shells");

            // Scale model
            model.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);

            // Disable colliders
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            // Hide eggs
            eggs.SetActive(false);
            shells.SetActive(false);

            // Now do the components dancing :^)

            // Update rigid body
            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            rb.mass = 40.0f;
            rb.drag = 1.0f;
            rb.angularDrag = 1.0f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.constraints = RigidbodyConstraints.None;

            // Add EntityTag
            EntityTag entityTag = prefab.AddComponent<EntityTag>();
            entityTag.slotType = EntitySlot.Type.Small;

            // Add TechTag
            TechTag techTag = prefab.AddComponent<TechTag>();
            techTag.type = this.TechType;

            // Update prefab identifier
            PrefabIdentifier prefabId = prefab.GetComponent<PrefabIdentifier>();
            prefabId.ClassId = this.ClassID;

            // Update large world entity
            LargeWorldEntity lwe = prefab.GetComponent<LargeWorldEntity>();
            lwe.cellLevel = LargeWorldEntity.CellLevel.Near;

            // Add world forces
            WorldForces worldForces = prefab.AddComponent<WorldForces>();
            worldForces.handleGravity = true;
            worldForces.aboveWaterGravity = 9.81f;
            worldForces.underwaterGravity = 1.0f;
            worldForces.handleDrag = true;
            worldForces.aboveWaterDrag = 0.0f;
            worldForces.underwaterDrag = 10.0f;
            worldForces.useRigidbody = rb;

            // Add pickupable
            Pickupable pickupable = prefab.AddComponent<Pickupable>();
            pickupable.isPickupable = false;
            pickupable.destroyOnDeath = true;
            pickupable.cubeOnPickup = false;
            pickupable.randomizeRotationWhenDropped = true;
            pickupable.usePackUpIcon = false;

            // Add plantable
            Plantable plantable = prefab.AddComponent<Plantable>();
            plantable.aboveWater = true;
            plantable.underwater = true;
            plantable.isSeedling = true;
            plantable.plantTechType = this.TechType;
            plantable.size = Plantable.PlantSize.Large;
            plantable.pickupable = pickupable;
            plantable.model = prefab;
            plantable.linkedGrownPlant = new GrownPlant();
            plantable.linkedGrownPlant.seed = plantable;
            plantable.linkedGrownPlant.seedUID = "CoveTree1";

            // Add cove tree controller (handles show/hide eggs capability)
            CoveTree1Controller coveTreeController = prefab.AddComponent<CoveTree1Controller>();

            // Add ghost leviatan spawner
            if (ConfigSwitcher.GhostLeviatan_enable)
                prefab.AddComponent<GhostLeviatanSpawner>();

            // Add generic plant controller (handles animation)
            PlantGenericController plantGenericController = prefab.AddComponent<PlantGenericController>();
            plantGenericController.GrowthDuration = Config.GrowthDuration;
            plantGenericController.Health = Config.Health;
            plantGenericController.Knifeable = Config.Knifeable;
            plantGenericController.EnableColliders = true; // Restore disabled colliders

            // Handles saving/restoring cove tree state
            CustomFloraSerializer customSerializer = prefab.AddComponent<CustomFloraSerializer>();

            // Add live mixin
            LiveMixin liveMixin = prefab.AddComponent<LiveMixin>();
            liveMixin.health = Config.Health;
            liveMixin.data = ScriptableObject.CreateInstance<LiveMixinData>();
            liveMixin.data.broadcastKillOnDeath = false;
            liveMixin.data.canResurrect = false;
            liveMixin.data.destroyOnDeath = true;
            liveMixin.data.explodeOnDestroy = false;
            liveMixin.data.invincibleInCreative = false;
            liveMixin.data.minDamageForSound = 10.0f;
            liveMixin.data.passDamageDataOnDeath = true;
            liveMixin.data.weldable = false;
            liveMixin.data.knifeable = false;
            liveMixin.data.maxHealth = Config.Health;
            //liveMixin.startHealthPercent = 1.0f;

            // Add atmosphere volume
            AtmosphereVolume aVolume = prefab.AddComponent<AtmosphereVolume>();
            aVolume.highDetail = true;
            aVolume.affectsVisuals = true;

            return prefab;
        }
    }
}
