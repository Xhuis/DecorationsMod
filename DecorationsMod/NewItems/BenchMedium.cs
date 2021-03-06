﻿using SMLHelper;
using SMLHelper.Patchers;
using System.Collections.Generic;
using UnityEngine;

namespace DecorationsMod.NewItems
{
    public class BenchMedium : DecorationItem
    {
        public BenchMedium()
        {
            // Feed DecortionItem interface
            this.ClassID = "BenchMedium";
            this.ResourcePath = "Submarine/Build/Bench";

            this.GameObject = Resources.Load<GameObject>(this.ResourcePath);

            this.TechType = TechTypePatcher.AddTechType(this.ClassID,
                                                        LanguageHelper.GetFriendlyWord("BenchMediumName"),
                                                        LanguageHelper.GetFriendlyWord("BenchDescription"),
                                                        true);

            this.IsHabitatBuilder = true;

            this.Recipe = new TechDataHelper()
            {
                _craftAmount = 1,
                _ingredients = new List<IngredientHelper>(new IngredientHelper[1]
                    {
                        new IngredientHelper(TechType.Titanium, 1)
                    }),
                _techType = this.TechType
            };
        }

        public override void RegisterItem()
        {
            if (this.IsRegistered == false)
            {
                // Add new TechType to the buildables
                CraftDataPatcher.customBuildables.Add(this.TechType);
                CraftDataPatcher.AddToCustomGroup(TechGroup.Miscellaneous, TechCategory.Misc, this.TechType);

                // Set the buildable prefab
                CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(this.ClassID, DecorationItem.DefaultResourcePath + this.ClassID, this.TechType, this.GetPrefab));

                // Set the custom sprite
                CustomSpriteHandler.customSprites.Add(new CustomSprite(this.TechType, new Atlas.Sprite(ImageUtils.LoadTextureFromFile("./QMods/DecorationsMod/Assets/benchmediumicon.png"))));

                // Associate recipe to the new TechType
                CraftDataPatcher.customTechData[this.TechType] = this.Recipe;

                this.IsRegistered = true;
            }
        }

        public override GameObject GetPrefab()
        {
            GameObject prefab = GameObject.Instantiate(this.GameObject);

            prefab.name = this.ClassID;

            // Modify tech tag
            var techTag = prefab.GetComponent<TechTag>();
            techTag.type = this.TechType;

            // Modify prefab identifier
            var prefabId = prefab.GetComponent<PrefabIdentifier>();
            prefabId.ClassId = this.ClassID;

            // Retrieve model node
            GameObject model = prefab.FindChild("model");

            // Add large world entity
            var lwe = prefab.AddComponent<LargeWorldEntity>();
            lwe.cellLevel = LargeWorldEntity.CellLevel.Near;

            // Modify box colliders
            var collider = prefab.FindChild("Collider").GetComponent<BoxCollider>();
            collider.size = new Vector3(collider.size.x * 0.6f, collider.size.y, collider.size.z);
            var builderTrigger = prefab.FindChild("Builder Trigger").GetComponent<BoxCollider>();
            builderTrigger.size = new Vector3(builderTrigger.size.x * 0.6f, builderTrigger.size.y, builderTrigger.size.z);

            // Move bench parts
            GameObject benchStart = model.FindChild("Bench_01_start");
            benchStart.transform.localPosition = new Vector3(-0.408f, benchStart.transform.localPosition.y, benchStart.transform.localPosition.z);
            GameObject benchEnd = model.FindChild("Bench_01_end");
            benchEnd.transform.localPosition = new Vector3(0.408f, benchEnd.transform.localPosition.y, benchEnd.transform.localPosition.z);
            GameObject benchTile = model.FindChild("Bench_01_tile");
            benchTile.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f); //new Vector3(99.7f, 99.7f, 99.7f);

            // Update sky applier
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            var skyapplier = prefab.GetComponent<SkyApplier>();
            skyapplier.renderers = renderers;
            skyapplier.anchorSky = Skies.Auto;

            // Update contructable
            var constructible = prefab.GetComponent<Constructable>();
            constructible.allowedInBase = true;
            constructible.allowedInSub = true;
            constructible.allowedOutside = ConfigSwitcher.AllowBuildOutside;
            constructible.allowedOnCeiling = false;
            constructible.allowedOnGround = true;
            constructible.allowedOnConstructables = false;
            constructible.controlModelState = true;
            constructible.deconstructionAllowed = true;
            constructible.rotationEnabled = true;
            constructible.model = model;
            constructible.techType = this.TechType;
            constructible.enabled = true;

            // Update constructable bounds
            var constructableBounds = prefab.GetComponent<ConstructableBounds>();
            constructableBounds.bounds = new OrientedBounds(new Vector3(constructableBounds.bounds.position.x, constructableBounds.bounds.position.y, constructableBounds.bounds.position.z),
                new Quaternion(constructableBounds.bounds.rotation.x, constructableBounds.bounds.rotation.y, constructableBounds.bounds.rotation.z, constructableBounds.bounds.rotation.w),
                new Vector3(constructableBounds.bounds.extents.x * 0.6f, constructableBounds.bounds.extents.y, constructableBounds.bounds.extents.z));

            return prefab;
        }
    }
}
