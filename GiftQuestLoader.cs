using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;

namespace WelcomeGifts;

internal sealed record GiftRecipeCatalog([property: JsonPropertyName("gifts")] List<GiftRecipe> Gifts);

internal sealed record GiftRecipe(
    [property: JsonPropertyName("id")] MongoId Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("requires")] MongoId? Requires,
    [property: JsonPropertyName("rewards")] List<GiftReward> Rewards);

internal sealed record GiftReward(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("template")] MongoId Template,
    [property: JsonPropertyName("quantity")] int Quantity);

internal sealed class GiftQuestLoader(
    TemplateTable templates,
    PresetHelper presets,
    ISptLogger<WelcomeGifts> logger)
{
    private const string Fence = "579dc571d53a0658a154fbec";

    public void Register(IReadOnlyList<GiftRecipe> recipes)
    {
        var quests = recipes.Select(MakeQuest).ToArray();
        foreach (var quest in quests)
        {
            templates.Quests.Add(quest.Id, quest);
        }
        logger.Info($"Welcome Gifts registered {quests.Length} gift quests from reward recipes.");
    }

    private Quest MakeQuest(GiftRecipe recipe)
    {
        var id = recipe.Id;
        var unlocks = new List<QuestCondition>();
        if (recipe.Requires is { } previousGift)
        {
            unlocks.Add(new QuestCondition
            {
                Id = MakeId($"{id}/prerequisite"),
                ConditionType = "Quest",
                Target = new ListOrT<string>(null, previousGift),
                DynamicLocale = false,
                Status = [QuestStatusEnum.Success],
                AvailableAfter = 0,
                Index = 0,
                VisibilityConditions = []
            });
        }

        return new Quest
        {
            Id = id,
            QuestName = recipe.Name,
            TraderId = Fence,
            Location = "any",
            Image = "/files/quest/icon/WelcomeGifts_unk_gifts.png",
            Type = QuestTypeEnum.Standing,
            Side = "Pmc",
            InstantComplete = true,
            Restartable = false,
            CanShowNotificationsInGame = true,
            Name = $"{id} name",
            Description = $"{id} description",
            Note = $"{id} note",
            StartedMessageText = $"{id} description",
            SuccessMessageText = $"{id} successMessageText",
            FailMessageText = $"{id} failMessageText",
            AcceptPlayerMessage = $"{id} description",
            CompletePlayerMessage = $"{id} successMessageText",
            DeclinePlayerMessage = $"{id} declinePlayerMessage",
            ChangeQuestMessageText = $"{id} changeQuestMessageText",
            Conditions = new QuestConditionTypes
            {
                AvailableForStart = unlocks,
                AvailableForFinish = [new QuestCondition
                {
                    Id = MakeId($"{id}/finish"),
                    ConditionType = "TraderLoyalty",
                    Target = new ListOrT<string>(null, Fence),
                    DynamicLocale = false,
                    CompareMethod = ">=",
                    Value = 1,
                    Index = 0,
                    VisibilityConditions = []
                }],
                Fail = []
            },
            Rewards = new Dictionary<string, List<Reward>>
            {
                ["Started"] = [],
                ["Success"] = MakeRewards(recipe),
                ["Fail"] = []
            }
        };
    }

    private List<Reward> MakeRewards(GiftRecipe recipe)
    {
        var rewards = new List<Reward>();
        foreach (var reward in recipe.Rewards)
        {
            if (!templates.Items.TryGetValue(reward.Template, out var template) || reward.Quantity <= 0)
            {
                throw new InvalidDataException($"Invalid reward for {recipe.Name}: {reward.Name}.");
            }

            var stackLimit = Math.Max(1, template.Properties?.StackMaxSize ?? 1);
            var remaining = reward.Quantity;
            while (remaining > 0)
            {
                var quantity = Math.Min(stackLimit, remaining);
                var key = $"{recipe.Id}/reward/{rewards.Count}";
                var items = MakeItems(reward.Template, quantity, key);
                rewards.Add(new Reward
                {
                    Id = MakeId(key),
                    Type = RewardType.Item,
                    Index = rewards.Count,
                    Target = items[0].Id,
                    Value = quantity,
                    FindInRaid = false,
                    AvailableInGameEditions = [],
                    Items = items
                });
                remaining -= quantity;
            }
        }

        return rewards;
    }

    private List<Item> MakeItems(MongoId template, int quantity, string key)
    {
        if (presets.GetDefaultPreset(template)?.Items is not { Count: > 0 } items)
        {
            return [new Item
            {
                Id = MakeId($"{key}/item/0"),
                Template = template,
                Upd = new Upd { StackObjectsCount = quantity }
            }];
        }

        // PresetHelper returns a clone. Give each item an ID local to this reward.
        var oldIds = new Dictionary<string, MongoId>(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            oldIds.Add(items[index].Id, MakeId($"{key}/item/{index}"));
        }

        var root = items.Single(item => item.Template == template
            && (item.ParentId is null || !oldIds.ContainsKey(item.ParentId)));
        foreach (var item in items)
        {
            item.Id = oldIds[item.Id];
            if (item.ParentId is { } parent && oldIds.TryGetValue(parent, out var newParent))
            {
                item.ParentId = newParent;
            }
        }

        root.ParentId = null;
        root.SlotId = null;
        root.Location = null;
        root.Upd ??= new Upd();
        root.Upd.StackObjectsCount = quantity;
        if (items[0] != root)
        {
            items.Remove(root);
            items.Insert(0, root);
        }

        return items;
    }

    private static MongoId MakeId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"online.efcl.spt.welcomegifts/{key}"));
        return Convert.ToHexStringLower(hash.AsSpan(0, 12));
    }
}
