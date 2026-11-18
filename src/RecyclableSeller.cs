using System;
using System.Collections.Generic;

namespace RecyclableSign
{
    /// <summary>
    /// The scavenger attracted by the sign. Half of the visits are a seller modelled on the vanilla
    /// generic scav sellers (StoreClientListSupplier.CreateScavGenericSeller); the other half are a
    /// barterer modelled on the vanilla food barterer (StoreClientListBarter.CreateNormalFoodBarter).
    /// Both share one client id, so the display case lists and rolls a single visitor.
    /// </summary>
    internal static class RecyclableSeller
    {
        private const string WineBottle = "wine_bottle";            // empty 750 ml winemaking bottle
        private const string SmallBottle = "small_bottled_water";   // 450 ml
        private const string NormalBottle = "bottled_water";        // 750 ml
        private const string LargeBottle = "large_bottled_water";   // 2000 ml
        private const string WaterJug = "water_jug";                // 6000 ml

        /// <summary>Chance that the visitor wants to barter instead of selling, when the player allows both.</summary>
        private const int BarterChance = 50;

        public static StoreClient Create()
        {
            switch (VisitorSettings.Mode)
            {
                case VisitorMode.SellerOnly:
                    return CreateSeller();
                case VisitorMode.BarterOnly:
                    return CreateBarterer();
                default:
                    return RNG.Roll(BarterChance) ? CreateBarterer() : CreateSeller();
            }
        }

        /// <summary>
        /// Trades one page of empties for any one food item. The pages are browsed with the arrows in
        /// the negotiation tablet and the visitor leaves after one trade, exactly like vanilla barter.
        /// </summary>
        private static StoreClient CreateBarterer()
        {
            StoreClient client = new StoreClient();
            client.identifier = ModEntry.ClientId;
            client.displayName = LocHelper.GetLocalizedClientName("name_scavenger");
            client.spriteName = SpriteDict.GetRandomScavSpriteName();
            client.clientFaction = "FACTION_LOWER_LEVEL";
            StoreClientFactionSetup.InitScav(client);
            client.SetBarterOffer(CreateBarterOffer());

            string name = client.displayName;
            client.mainDialogue.SetText(name, Line("barter_intro_1")).NextDialogue().SetText(name, Line("barter_intro_2"))
                .SetEndAction(() =>
                {
                    client.barterOffer.InstanciateOffer();
                });
            client.repeatDialogue = new Dialogue();
            client.repeatDialogue.SetText(name, Line("barter_repeat"));
            client.acceptDealDialogue = new Dialogue().SetText(name, Line("barter_accept"));
            client.allDoneDialogue = new Dialogue().SetText(name, Line("barter_done")).SetEndAction(() =>
            {
                DialogUIManager.Instance.CurrentClientLeave();
            });
            client.CompleteClientCreation();
            return client;
        }

        private static BarterOffer CreateBarterOffer()
        {
            BarterOffer offer = new BarterOffer();
            offer.barterTextBody = LocHelper.GetLocalizedDialogue("dialog_barter_for", TypeHelper.GetTypeDisplayName("FOOD"));

            // A page is rebuilt every time it is shown, so anything random is rolled here, once.
            offer.AddOfferSet(() => OfferMany(WineBottle, 3));
            offer.AddOfferSet(() =>
            {
                OfferMany(SmallBottle, 2);
                OfferMany(NormalBottle, 2);
            });
            offer.AddOfferSet(() => OfferMany(LargeBottle, 2));
            int jugPageChance = Clamp(20 + StoreReputation.GetLLReputation() / 3, 10, 40);
            if (IsJugGuaranteed() || RNG.Roll(jugPageChance))
            {
                offer.AddOfferSet(() => OfferMany(WaterJug, 1));
            }

            // One food item of any kind pays for the page on display.
            BarterCondition food = new BarterCondition();
            food.requiredItemTag = "FOOD";
            offer.conditions.Add(food);
            return offer;
        }

        private static void OfferMany(string itemId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Offer(itemId);
            }
        }

        private static string Line(string key)
        {
            return ModHelper.GetLocalized(ModEntry.ModId, key);
        }

        private static StoreClient CreateSeller()
        {
            StoreClient client = new StoreClient();
            client.identifier = ModEntry.ClientId;
            client.displayName = LocHelper.GetLocalizedClientName("name_scavenger");
            client.spriteName = SpriteDict.GetRandomScavSpriteName();
            client.clientFaction = "FACTION_LOWER_LEVEL";
            StoreClientFactionSetup.InitScav(client);
            client.clientIntent = StoreClient.ClientIntent.SELL;
            client.isOfferWholesale = true;
            client.wholesaleDiscount = -RNG.GetRandomInt(15, 25);

            List<string> greetings = StoreClientList.GetScavGreetingLines();
            string greeting = greetings[RNG.GetRandomInt(0, greetings.Count - 1)];
            string offer = ModHelper.GetLocalized(ModEntry.ModId, "seller_offer_" + RNG.GetRandomInt(1, 3));

            client.mainDialogue.SetText(client.displayName, greeting).NextDialogue().SetText(client.displayName, offer)
                .SetEndAction(() =>
                {
                    AddStock();
                    client.OnIntroduced();
                });
            client.AddBasicDialog();
            client.CompleteClientCreation();
            return client;
        }

        /// <summary>
        /// 2 wine bottles + 2 water bottles guaranteed, then two extra rolls per kind.
        /// All chances follow Lower Level reputation; large bottles, extra wine bottles and
        /// the rare jug lean on it the most. Roughly 5-6 items at neutral reputation, 7-8 at +50.
        /// </summary>
        private static void AddStock()
        {
            int rep = StoreReputation.GetLLReputation();
            int baseChance = Clamp(25 + rep / 2, 10, 75);   // same curve as the vanilla scav sellers
            int largeChance = Clamp(10 + rep / 2, 5, 60);
            int wineChance = Clamp(15 + rep / 2, 5, 60);
            int jugChance = Clamp(5 + rep / 5, 3, 20);

            // Roll everything first, then lay the goods out grouped by kind. Items land on the counter
            // in the order they are added, so adding them as rolled left wine and water bottles mixed.
            List<string> stock = new List<string>();
            stock.Add(WineBottle);
            stock.Add(WineBottle);
            stock.Add(PickWaterBottle(largeChance));
            stock.Add(PickWaterBottle(largeChance));

            for (int i = 0; i < 2; i++)
            {
                if (RNG.Roll(baseChance)) stock.Add(SmallBottle);
                if (RNG.Roll(baseChance)) stock.Add(NormalBottle);
                if (RNG.Roll(largeChance)) stock.Add(LargeBottle);
                if (RNG.Roll(wineChance)) stock.Add(WineBottle);
            }
            if (IsJugGuaranteed() || RNG.Roll(jugChance)) stock.Add(WaterJug);

            foreach (string kind in DisplayOrder)
            {
                foreach (string itemId in stock)
                {
                    if (itemId == kind) Offer(itemId);
                }
            }
        }

        /// <summary>
        /// Counter layout: wine bottles, then water containers from largest to smallest. Items that do not fit
        /// on the counter are dropped from the end, so the big jug goes early and only small bottles risk it.
        /// </summary>
        private static readonly string[] DisplayOrder = { WineBottle, WaterJug, LargeBottle, NormalBottle, SmallBottle };

        /// <summary>Above this Lower Level reputation the 6000 ml jug is no longer left to chance.</summary>
        private const int JugGuaranteedAboveRep = 50;

        /// <summary>
        /// True when Lower Level reputation is above 50: the seller always brings one water jug and the
        /// barterer always offers the jug page. Below that, both keep their reputation-scaled chance.
        /// </summary>
        private static bool IsJugGuaranteed()
        {
            return StoreReputation.GetLLReputation() > JugGuaranteedAboveRep;
        }

        /// <summary>A guaranteed water bottle: small or standard, with a reputation-scaled chance of a large one.</summary>
        private static string PickWaterBottle(int largeChance)
        {
            if (RNG.Roll(largeChance))
            {
                return LargeBottle;
            }
            return RNG.Roll(50) ? SmallBottle : NormalBottle;
        }

        /// <summary>Puts one empty container on the counter as the scavenger's goods.</summary>
        private static void Offer(string itemId)
        {
            PlayerStore.Instance.AddDirectSellingItemToTable(itemId);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
