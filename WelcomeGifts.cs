using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text.Json.Serialization;
using Reward = SPTarkov.Server.Core.Models.Eft.Common.Tables.Reward;

namespace WelcomeGifts
{
    public record ModMetaData : IModMetadata
    {
        public string ModGuid { get; init; } = "online.efcl.spt.welcomegifts";
        public string Name { get; init; } = "Welcome Gifts";
        public string Author { get; init; } = "Inmortuae";
        public List<string>? Contributors { get; init; } = ["ShadowXtrex (EFCL)", "Senko"];
        public SemanticVersioning.Version Version { get; init; } = new("4.3.3");
        public SemanticVersioning.Range SptVersion { get; init; } = new("4.1.6");
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
        {
            ["com.wtt.commonlib"] = new("~3.0.8")
        };
        public string? Url { get; init; } = "https://gitlab.com/ShadowXtrex/welcomegifts";
        public bool HasPrepatcher { get; init; }
        public string License { get; init; } = "MIT";
    }

    // Content Backport completes its item registration and base game edits at Preload + 3.
    [Injectable(TypePriority = OnLoadOrder.Preload + 4)]
    public class WelcomeGifts(
        WTTServerCommonLib.WTTServerCommonLib wttCommon,
        ModHelper modHelper,
        JsonUtil jsonUtil,
        TemplateTable templates,
        LocaleTable locales,
        PresetHelper presets,
        ISptLogger<WelcomeGifts> logger) : IOnLoad
    {
        public async Task OnLoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assembly = Assembly.GetExecutingAssembly();
            await wttCommon.CustomQuestService.CreateCustomQuests(assembly);

            var db = Path.Combine(modHelper.GetAbsolutePathToModFolder(assembly), "db");
            var recipes = await jsonUtil.DeserializeFromFileAsync<GiftRecipeCatalog>(
                Path.Combine(db, "GiftRecipes.json"), cancellationToken);
            new GiftQuestLoader(templates, presets, logger).Register(recipes?.Gifts ?? []);

            var path = Path.Combine(db, "BackportGifts.json");
            var variants = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, BackportGift>>(path, cancellationToken);
            if (variants is null)
            {
                return;
            }

            var descriptions = new Dictionary<string, string>();
            foreach (var (questId, variant) in variants)
            {
                if (!templates.Quests.TryGetValue(questId, out var quest) || quest.Rewards is null
                    || variant.Rewards.Any(reward => reward.Items is not { Count: > 0 }
                        || reward.Items.Any(item => !templates.Items.ContainsKey(item.Template))))
                {
                    continue;
                }

                quest.Rewards["Success"] = variant.Rewards;
                if (quest.Description is { } descriptionKey)
                {
                    descriptions[descriptionKey] = variant.Description;
                }
            }

            if (descriptions.Count == 0)
            {
                return;
            }

            foreach (var locale in locales.Global.Values)
            {
                locale.AddTransformer(dictionary =>
                {
                    if (dictionary is null)
                    {
                        return null;
                    }
                    foreach (var (key, description) in descriptions)
                    {
                        dictionary[key] = description;
                    }
                    return dictionary;
                });
            }
        }
    }

    internal sealed record BackportGift(
        [property: JsonPropertyName("rewards")] List<Reward> Rewards,
        [property: JsonPropertyName("description")] string Description);
}
