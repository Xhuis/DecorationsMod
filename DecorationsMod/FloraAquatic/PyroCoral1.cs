﻿using DecorationsMod.Controllers;
using SMLHelper;
using SMLHelper.Patchers;
using System.Collections.Generic;
using UnityEngine;

namespace DecorationsMod.FloraAquatic
{
    public class PyroCoral1 : DecorationItem, ICustomFlora
    {
        public CustomFlora Config = new CustomFlora();
        CustomFlora ICustomFlora.Config
        {
            get => this.Config;
            set => this.Config = value;
        }

        public PyroCoral1()
        {
            this.ClassID = "PyroCoral1"; // 9d7d3f7a-fbb9-41ab-84fa-6f40c1ed2524
            this.ResourcePath = "WorldEntities/Doodads/Lost_river/lost_river_pillar_01";

            this.GameObject = Resources.Load<GameObject>(this.ResourcePath);

            this.TechType = TechTypePatcher.AddTechType(this.ClassID,
                                                        LanguageHelper.GetFriendlyWord("PyroCoralName") + " (A)",
                                                        LanguageHelper.GetFriendlyWord("PyroCoralDescription"),
                                                        true);

            this.Recipe = new TechDataHelper()
            {
                _craftAmount = 1,
                _ingredients = new List<IngredientHelper>(new IngredientHelper[1] {
                    new IngredientHelper(ConfigSwitcher.FloraRecipiesResource, 1)
                }),
                _techType = this.TechType
            };

            this.Config = ConfigSwitcher.config_PyroCoral1;
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

                // Change item background to water-plant seed
                // TODO: Replace with a call to SMLHelper when pull request gets released
                DecorationsMod.CustomBackgroundTypes.Add(this.TechType, CraftData.BackgroundType.PlantWaterSeed);

                // Set item bioreactor charge
                // TODO: Replace with a call to SMLHelper when pull request gets released
                DecorationsMod.CustomCharges.Add(this.TechType, this.Config.Charge);

                // Set the buildable prefab
                CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(this.ClassID, $"{DecorationItem.DefaultResourcePath}{this.ClassID}", this.TechType, this.GetPrefab));

                // Set the custom sprite
                CustomSpriteHandler.customSprites.Add(new CustomSprite(this.TechType, AssetsHelper.Assets.LoadAsset<Sprite>("pyrocoral1icon")));

                // Associate recipe to the new TechType
                CraftDataPatcher.customTechData[this.TechType] = this.Recipe;

                this.IsRegistered = true;
            }
        }

        public override GameObject GetPrefab()
        {
            GameObject prefab = GameObject.Instantiate(this.GameObject);

            prefab.name = this.ClassID;

            // Remove light
            Light light = prefab.GetComponentInChildren<Light>();
            if (light != null)
                GameObject.DestroyImmediate(light);

            // Scale prefab
            prefab.FindChild("lost_river_pillar_01").transform.localScale *= 0.15f;

            // Shrink colliders
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                foreach (Collider collider in colliders)
                {
                    collider.transform.localScale *= 0.001f;
                }
            }

            // Update rigid body
            var rb = prefab.GetComponent<Rigidbody>();
            rb.mass = 10.0f;
            rb.drag = 1.0f;
            rb.angularDrag = 1.0f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.constraints = RigidbodyConstraints.None;

            // Add EntityTag
            var entityTag = prefab.AddComponent<EntityTag>();
            entityTag.slotType = EntitySlot.Type.Small;

            // Add TechTag
            var techTag = prefab.AddComponent<TechTag>();
            techTag.type = this.TechType;

            // Update prefab identifier
            var prefabId = prefab.GetComponent<PrefabIdentifier>();
            prefabId.ClassId = this.ClassID;
            
            // Update large world entity
            var lwe = prefab.GetComponent<LargeWorldEntity>();
            lwe.cellLevel = LargeWorldEntity.CellLevel.Near;

            // Add world forces
            var worldForces = prefab.AddComponent<WorldForces>();
            worldForces.handleGravity = true;
            worldForces.aboveWaterGravity = 9.81f;
            worldForces.underwaterGravity = 1.0f;
            worldForces.handleDrag = true;
            worldForces.aboveWaterDrag = 0.0f;
            worldForces.underwaterDrag = 10.0f;
            worldForces.useRigidbody = rb;

            // Add pickupable
            var pickupable = prefab.AddComponent<Pickupable>();
            pickupable.isPickupable = false;
            pickupable.destroyOnDeath = true;
            pickupable.cubeOnPickup = false;
            pickupable.randomizeRotationWhenDropped = true;
            pickupable.usePackUpIcon = false;

            // Add plantable
            var plantable = prefab.AddComponent<Plantable>();
            plantable.aboveWater = false;
            plantable.underwater = true;
            plantable.isSeedling = true;
            plantable.plantTechType = this.TechType;
            plantable.size = Plantable.PlantSize.Large;
            plantable.pickupable = pickupable;
            plantable.model = prefab;
            plantable.linkedGrownPlant = new GrownPlant();
            plantable.linkedGrownPlant.seed = plantable;
            plantable.linkedGrownPlant.seedUID = "PyroCoral1";

            // Add generic plant controller
            PlantGenericController landPlant1Controller = prefab.AddComponent<PlantGenericController>();
            landPlant1Controller.GrowthDuration = Config.GrowthDuration;
            landPlant1Controller.Health = Config.Health;
            landPlant1Controller.Knifeable = Config.Knifeable;
            landPlant1Controller.RestoreColliders = true;

            CustomFloraSerializer customSerializer = prefab.AddComponent<CustomFloraSerializer>();

            // Add live mixin
            var liveMixin = prefab.AddComponent<LiveMixin>();
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
            
            return prefab;
        }
    }
}
