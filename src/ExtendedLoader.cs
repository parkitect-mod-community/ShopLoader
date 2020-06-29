using System;
using System.IO;
using Parkitilities;
using Parkitilities.AssetPack;
using Parkitilities.ShopBuilder;
using UnityEngine;


namespace PMC.ExtendedLoader
{
    public class ExtendedLoader
    {
        public String Path { get; private set; }
        public bool IsLoaded { get; private set; }
        private readonly AssetManagerLoader _assetManagerLoader = new AssetManagerLoader();

        private AssetBundle _bundle;

        public ExtendedLoader(String path)
        {
            Path = path;
        }


        private void _bindIngredients<TTarget>(IngredientBuilder<TTarget> builder, ShopProduct product,
            ShopIngredient ingredient)
            where TTarget : class
        {
            var ingredientBuilder = builder
                .Cost(ingredient.Price)
                .Tweakable(ingredient.Tweakable)
                .DisplayName(ingredient.Name)
                .DefaultAmount(ingredient.Amount)
                .Id(product.Guid + "_" + ingredient.Name);

            foreach (var effect in ingredient.Effects)
            {
                ingredientBuilder.Effect(ProductShopUtility.ConvertEffectType(effect.Type), effect.Amount);
            }
        }

        public void onEnabled()
        {
            _bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                "assetPack"));
            if (_bundle == null)
                throw new Exception("Failed to load AssetBundle!");
            var pack = AssetPackUtilities.LoadAsset(File.ReadAllText(Path));
            Debug.Log("Loading asset pack for shop " + pack.Name + " with " + pack.Assets.Count +
                      " assets");

            IsLoaded = true;
            foreach (var asset in pack.Assets)
            {
                try
                {
                    Debug.Log("Loading Item " + asset.Name + " by type " + asset.TargetType);
                    switch (asset.TargetType)
                    {
                        case AssetType.Shop:
                            GameObject go = AssetPackUtilities.LoadAsset<GameObject>(_bundle, asset.Guid);
                            if (go == null)
                                throw new Exception("Can't find gameobject:" + asset.Guid);

                            var builder = Parkitility.CreateProductShop<ProductShop>(go)
                                .DisplayName(asset.Name)
                                .Id(asset.Guid)
                                .Price(asset.Price)
                                .WalkableFlag(Block.WalkableFlagType.FORWARD);

                            foreach (var box in AssetPackUtilities.ConvertBoundingBox(asset.BoundingBoxes.ToArray()))
                            {
                                builder.AddBoundingBox(box);
                            }

                            foreach (var product in asset.Products)
                            {
                                GameObject productGo = AssetPackUtilities.LoadAsset<GameObject>(_bundle, product.Guid);
                                if (productGo == null)
                                {
                                    Debug.Log("Can't find product game object for:" + product.Name);
                                    continue;
                                }

                                switch (product.ProductType)
                                {
                                    case ProductType.ON_GOING:
                                        var ongoingProductBuilder = Parkitility
                                            .CreateOnGoingProduct<OngoingEffectProduct>(productGo)
                                            .Id(product.Guid)
                                            .DisplayName(product.Name)
                                            .Duration(product.Duration)
                                            .DestroyWhenDepleted(product.DestroyWhenDepleted)
                                            .RemoveFromInventoryWhenDepleted(product.RemoveWhenDepleted)
                                            .TwoHanded(product.IsTwoHanded)
                                            .InterestingToLookAt(product.IsInterestingToLookAt)
                                            .DefaultPrice(product.Price)
                                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide));

                                        foreach (var shopIngredient in product.Ingredients)
                                        {
                                            _bindIngredients(ongoingProductBuilder.AddIngredient(_assetManagerLoader),
                                                product, shopIngredient);
                                        }

                                        builder.AddProduct(_assetManagerLoader, ongoingProductBuilder);
                                        break;
                                    case ProductType.WEARABLE:
                                        var wearableProductBuilder = Parkitility
                                            .CreateWearableProduct<WearableProduct>(productGo)
                                            .Id(product.Guid)
                                            .DisplayName(product.Name)
                                            .TwoHanded(product.IsTwoHanded)
                                            .InterestingToLookAt(product.IsInterestingToLookAt)
                                            .DefaultPrice(product.Price)
                                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide))
                                            .TemperaturePreference(
                                                ProductShopUtility.ConvertTemperaturePreference(
                                                    product.TemperaturePreference))
                                            .SeasonalPreference(
                                                ProductShopUtility.ConvertSeasonalPreference(
                                                    product.SeasonalPreference))
                                            .BodyLocation(ProductShopUtility.ConvertBodyLocation(product.BodyLocation))
                                            .HideHair(product.HideHair)
                                            .HideOnRide(product.HideOnRide);


                                        foreach (var shopIngredient in product.Ingredients)
                                        {
                                            _bindIngredients(wearableProductBuilder.AddIngredient(_assetManagerLoader),
                                                product, shopIngredient);
                                        }

                                        builder.AddProduct(_assetManagerLoader, wearableProductBuilder);
                                        break;
                                    case ProductType.CONSUMABLE:
                                        var consumableBuilder = Parkitility
                                            .CreateConsumableProduct<ConsumableProduct>(productGo)
                                            .Id(product.Guid)
                                            .DisplayName(product.Name)
                                            .TwoHanded(product.IsTwoHanded)
                                            .InterestingToLookAt(product.IsInterestingToLookAt)
                                            .TemperaturePreference(
                                                ProductShopUtility.ConvertTemperaturePreference(
                                                    product.TemperaturePreference))
                                            .ConsumeAnimation(
                                                ProductShopUtility.ConvertConsumeAnimation(product.ConsumeAnimation))
                                            .DefaultPrice(product.Price)
                                            .Trash(TrashType.ChipBagTrash)
                                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide));

                                        foreach (var shopIngredient in product.Ingredients)
                                        {
                                            _bindIngredients(consumableBuilder.AddIngredient(_assetManagerLoader),
                                                product,
                                                shopIngredient);
                                        }

                                        builder.AddProduct(_assetManagerLoader, consumableBuilder);
                                        break;
                                }
                            }

                            builder.Build(_assetManagerLoader);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            _bundle.Unload(false);
        }

        public void onDisabled()
        {
            IsLoaded = false;
            if (_bundle != null)
                _bundle.Unload(false);
            if (_assetManagerLoader != null)
                _assetManagerLoader.Unload();
        }
    }
}
