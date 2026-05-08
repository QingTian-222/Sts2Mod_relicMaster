using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Exceptions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace relicMaster
{
    public static class RelicActivator
    {
        private static readonly Dictionary<Type, Func<RelicModel, Player, Creature?, PlayerChoiceContext, Task>> _activationMap = new();

        private static void Register<T>(Func<RelicModel, Player, Creature?, PlayerChoiceContext, Task> action) where T : RelicModel
        {
            _activationMap[typeof(T)] = action;
        }

        
        public static async Task Activate(RelicModel relic, Player player, PlayerChoiceContext choiceContext, Creature? target = null)
        {
            relic.Flash();
            if (_activationMap.TryGetValue(relic.GetType(), out var action))
            {
                await action(relic, player, target, choiceContext);
            }
            else
            {
                Log.Warn($"遗物 {relic.GetType().Name} 尚未注册激活效果，仅闪烁。");
            }
        }
        public static void RegisterAll()
        {
            Register<Akabeko>(async (relic, player, target, ctx) =>
            {
                //赤牛
                //在每场战斗开始时，获得[blue]{VigorPower}[/blue]点[gold]活力[/gold]。
                await PowerCmd.Apply<VigorPower>(relic.Owner.Creature, relic.DynamicVars["VigorPower"].IntValue, relic.Owner.Creature, null);
            });

            Register<AlchemicalCoffer>(async (relic, player, target, ctx) =>
            {
                //炼金箱
                //拾起时，获得[blue]{PotionSlots}[/blue]个放有随机药水的药水栏位。
                await relic.AfterObtained();
            });

            Register<AmethystAubergine>(async (relic, player, target, ctx) =>
            {
                //紫水晶茄子
                //敌人额外掉落[blue]{Gold}[/blue][gold]金币[/gold]。
                await PlayerCmd.GainGold(relic.DynamicVars.Gold.BaseValue, relic.Owner);
            });

            Register<Anchor>(async (relic, player, target, ctx) =>
            {
                //锚
                //每场战斗开始时获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<ArcaneScroll>(async (relic, player, target, ctx) =>
            {
                //奥术卷轴
                //拾起时，将一张随机[gold]稀有牌[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<ArchaicTooth>(async (relic, player, target, ctx) =>
            {
                //古老牙齿
                //拾起时，将{StarterCard.StringValue:cond:[gold]{StarterCard}[/gold][gold]变化[/gold]为[gold]{AncientCard}[/gold]|一张初始卡牌[gold]变化[/gold]为先古版本}。
            });

            Register<ArtOfWar>(async (relic, player, target, ctx) =>
            {
                //孙子兵法
                //如果你在本回合中没有打出过攻击牌，则在下一回合额外获得1点{Energy:energyIcons()}。
                await PowerCmd.Apply<EnergyNextTurnPower>(relic.Owner.Creature, relic.DynamicVars.Energy.IntValue, relic.Owner.Creature, null);

            });

            Register<Astrolabe>(async (relic, player, target, ctx) =>
            {
                //星盘
                //拾起时，选择[blue]{Cards}[/blue]张牌进行[gold]变化[/gold]，然后将这些牌[gold]升级[/gold]。
                await relic.AfterObtained();
            });

            Register<BagOfMarbles>(async (relic, player, target, ctx) =>
            {
                //弹珠袋
                //在每场战斗开始时，给予所有敌人[blue]{VulnerablePower}[/blue]层[gold]易伤[/gold]。
                await PowerCmd.Apply<VulnerablePower>(player.Creature.CombatState.HittableEnemies, relic.DynamicVars.Vulnerable.BaseValue, relic.Owner.Creature, null);
            });

            Register<BagOfPreparation>(async (relic, player, target, ctx) =>
            {
                //准备背包
                //在每场战斗开始时，额外抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);

            });

            Register<BeatingRemnant>(async (relic, player, target, ctx) =>
            {
                //律动残余
                //你在一回合内失去的生命值不会超过[blue]20[/blue]点。
            });

            Register<BeautifulBracelet>(async (relic, player, target, ctx) =>
            {
                //华美手镯
                //拾起时，从你的[gold]牌组[/gold]中选择[blue]{Cards}[/blue]张牌，[gold]附魔[/gold]：[purple]迅捷[/purple][blue]3[/blue]。
                await relic.AfterObtained();
            });

            Register<Bellows>(async (relic, player, target, ctx) =>
            {
                //风箱
                //你在每场战斗开始时的[gold]手牌[/gold]，将被[gold]升级[/gold]。
                CardCmd.Upgrade(PileType.Hand.GetPile(relic.Owner).Cards, CardPreviewStyle.HorizontalLayout);

            });

            Register<BeltBuckle>(async (relic, player, target, ctx) =>
            {
                //腰带扣
                //当你没有药水时，你额外拥有[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<DexterityPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.BaseValue, null, null);
            });

            Register<BigHat>(async (relic, player, target, ctx) =>
            {
                //大帽子
                //在每场战斗开始时，将[blue]{Cards}[/blue]张随机[gold]虚无[/gold]牌加入你的[gold]手牌[/gold]。
                IReadOnlyList<CardModel> readOnlyList = (from c in relic.Owner.Character.CardPool.GetUnlockedCards(relic.Owner.UnlockState, relic.Owner.RunState.CardMultiplayerConstraint)
                                                         where c.Keywords.Contains(CardKeyword.Ethereal)
                                                         select c).ToList();
                if (readOnlyList.Count > 0)
                {
                    List<CardModel> cards = CardFactory.GetDistinctForCombat(relic.Owner, readOnlyList, relic.DynamicVars.Cards.IntValue, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
                    await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
                }
            });

            Register<BigMushroom>(async (relic, player, target, ctx) =>
            {
                //大蘑菇
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。在每场战斗开始时，少抽[blue]{Cards}[/blue]张牌。

                await PowerCmd.Apply<DrawCardsNextTurnPower>(relic.Owner.Creature, -1, relic.Owner.Creature, null);
                await CreatureCmd.GainMaxHp(relic.Owner.Creature, relic.DynamicVars.MaxHp.BaseValue);
            });

            Register<BiiigHug>(async (relic, player, target, ctx) =>
            {
                //大～抱抱
                //拾起时，从你的[gold]牌组[/gold]中移除[blue]{Cards}[/blue]张牌。每当你的[gold]抽牌堆[/gold]打乱洗牌时，将一张[gold]煤灰[/gold]加入你的[gold]抽牌堆[/gold]。
                await relic.AfterObtained();
                CardModel soot = relic.Owner.Creature.CombatState.CreateCard<Soot>(relic.Owner);
                await CardPileCmd.AddGeneratedCardToCombat(soot, PileType.Draw, addedByPlayer: true, CardPilePosition.Random);
            });

            Register<BingBong>(async (relic, player, target, ctx) =>
            {
                //宾邦
                //每当你往[gold]牌组[/gold]中增添卡牌时，都将额外添加一张相同的牌。
            });

            Register<BlackBlood>(async (relic, player, target, ctx) =>
            {
                //黑暗之血
                //在战斗结束时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<BlackStar>(async (relic, player, target, ctx) =>
            {
                //黑星
                //[gold]精英[/gold]敌人在被打败时多掉落一件遗物。
                await RewardsCmd.OfferCustom(relic.Owner, new List<Reward>(1)
                {
                    new RelicReward(relic.Owner)
                });
            });

            Register<BlessedAntler>(async (relic, player, target, ctx) =>
            {
                //赐福鹿角
                //在每回合开始时获得{Energy:energyIcons()}。在战斗开始时，将[blue]{Cards}[/blue]张[gold]晕眩[/gold]放入你的[gold]抽牌堆[/gold]。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
                List<CardModel> list = new List<CardModel>();
                for (int i = 0; i < relic.DynamicVars.Cards.IntValue; i++)
                {
                    list.Add(relic.Owner.Creature.CombatState.CreateCard<Dazed>(relic.Owner));
                }
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));


            });

            Register<BloodSoakedRose>(async (relic, player, target, ctx) =>
            {
                //血染玫瑰
                //拾起时，将[blue]1[/blue]张[red]执迷[/red]加入你的[gold]牌组[/gold]。在回合开始时获得{Energy:energyIcons()}。
                await relic.AfterObtained();
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<BloodVial>(async (relic, player, target, ctx) =>
            {
                //小血瓶
                //在每场战斗开始时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<BoneFlute>(async (relic, player, target, ctx) =>
            {
                //骨笛
                //每当[gold]奥斯提[/gold]攻击时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<BoneTea>(async (relic, player, target, ctx) =>
            {
                //骨茶
                //在你接下来的[blue]{Combats}[/blue]场战斗开始时，[gold]升级[/gold]你的初始手牌。
                CardCmd.Upgrade(PileType.Hand.GetPile(relic.Owner).Cards, CardPreviewStyle.HorizontalLayout);
            });

            Register<Bookmark>(async (relic, player, target, ctx) =>
            {
                //书签
                //每回合结束时，随机一张被[gold]保留[/gold]的牌在被打出前的耗能减少[blue]1[/blue]。
                await relic.AfterTurnEnd(ctx, relic.Owner.Creature.Side);
            });

            Register<BookOfFiveRings>(async (relic, player, target, ctx) =>
            {
                //五轮书
                //你每将[blue]{Cards}[/blue]张牌加入你的[gold]牌组[/gold]时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<BookRepairKnife>(async (relic, player, target, ctx) =>
            {
                //修书小刀
                //每当有一名不是“爪牙”的敌人死于[gold]灾厄[/gold]时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<BoomingConch>(async (relic, player, target, ctx) =>
            {
                //轰鸣海螺
                //在[gold]精英[/gold]战的战斗开始时，额外抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<BoundPhylactery>(async (relic, player, target, ctx) =>
            {
                //缚魂命匣
                //在你的回合开始时，[gold]召唤[/gold][blue]{Summon}[/blue]
                await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), relic.Owner, relic.DynamicVars.Summon.BaseValue, null);


            });

            Register<BowlerHat>(async (relic, player, target, ctx) =>
            {
                //圆顶礼帽
                //额外获得[blue]{GoldIncrease:percentMore()}%[/blue]的[gold]金币[/gold]。

            });

            Register<Bread>(async (relic, player, target, ctx) =>
            {
                //面包
                //在你的第一个回合开始时，失去{LoseEnergy:energyIcons()}。在其余的回合开始时，获得{GainEnergy:energyIcons()}。
                await PlayerCmd.LoseEnergy(relic.DynamicVars["LoseEnergy"].BaseValue, relic.Owner);
                await PlayerCmd.GainEnergy(relic.DynamicVars["GainEnergy"].BaseValue, relic.Owner);

            });

            Register<BrilliantScarf>(async (relic, player, target, ctx) =>
            {
                //艳丽围巾
                //你每回合打出的第[blue]5[/blue]张牌可以被免费打出。
            });

            Register<Brimstone>(async (relic, player, target, ctx) =>
            {
                //硫磺
                //在你的每个回合开始时，你获得[blue]{SelfStrength}[/blue]点[gold]力量[/gold]，所有敌人获得[blue]{EnemyStrength}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars["SelfStrength"].BaseValue, relic.Owner.Creature, null);
                IEnumerable<Creature> targets = from c in player.Creature.CombatState.GetOpponentsOf(relic.Owner.Creature)
                                                where c.IsAlive
                                                select c;
                await PowerCmd.Apply<StrengthPower>(targets, relic.DynamicVars["EnemyStrength"].BaseValue, null, null);
            });

            Register<BronzeScales>(async (relic, player, target, ctx) =>
            {
                //铜质鳞片
                //在每场战斗开始时，获得[blue]{ThornsPower}[/blue]点[gold]荆棘[/gold]。
                await PowerCmd.Apply<ThornsPower>(relic.Owner.Creature, relic.DynamicVars["ThornsPower"].BaseValue, relic.Owner.Creature, null);
            });

            Register<BurningBlood>(async (relic, player, target, ctx) =>
            {
                //燃烧之血
                //在战斗结束时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<BurningSticks>(async (relic, player, target, ctx) =>
            {
                //燃烧木棍
                //每场战斗中你第一次[gold]消耗[/gold]技能牌时，将那张牌的复制品加入你的[gold]手牌[/gold]
            });

            Register<Byrdpip>(async (relic, player, target, ctx) =>
            {
                var byrdSwoopProto = ModelDb.Card<ByrdSwoop>();
                var byrdSwoop = player.RunState.CreateCard(byrdSwoopProto, player);
                await CardPileCmd.Add(byrdSwoop, PileType.Deck);
            });

            Register<CallingBell>(async (relic, player, target, ctx) =>
            {
                //召唤铃铛
                //拾起时，获得一个独特的[red]诅咒[/red]和[blue]{Relics}[/blue]件[gold]遗物[/gold]。
                await relic.AfterObtained();
            });

            Register<Candelabra>(async (relic, player, target, ctx) =>
            {
                //烛台
                //在你的[blue]第2[/blue]回合开始时，获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<CaptainsWheel>(async (relic, player, target, ctx) =>
            {
                //舵盘
                //在你的[blue]第三[/blue]回合开始时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<Cauldron>(async (relic, player, target, ctx) =>
            {
                //大锅
                //拾起时，制作[blue]{Potions}[/blue]瓶随机药水。
                await relic.AfterObtained();
            });

            Register<CentennialPuzzle>(async (relic, player, target, ctx) =>
            {
                //百年积木
                //你在每场战斗中第一次损失生命值时，抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<Chandelier>(async (relic, player, target, ctx) =>
            {
                //吊灯
                //在你的[blue]第三[/blue]回合开始时，获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<CharonsAshes>(async (relic, player, target, ctx) =>
            {
                //卡戎之灰
                //每当你[gold]消耗[/gold]一张牌，对所有敌人造成[blue]{Damage}[/blue]点伤害。
                DamageVar damage = relic.DynamicVars.Damage;
                await CreatureCmd.Damage(ctx, relic.Owner.Creature.CombatState.HittableEnemies, damage.BaseValue, damage.Props, relic.Owner.Creature, null);
            });

            Register<ChemicalX>(async (relic, player, target, ctx) =>
            {
                //化学物X
                //耗能为[blue]X[/blue]的牌的效果数值增加[blue]{Increase}[/blue]点。
            });

            Register<ChoicesParadox>(async (relic, player, target, ctx) =>
            {
                //选择悖论
                //在每场战斗开始时，从[blue]{Cards}[/blue]张随机牌中选择[blue]1[/blue]张放入你的[gold]手牌[/gold]。被选中的牌获得[gold]保留[/gold]。

                LocString prompt = new LocString("relics", "CHOICES_PARADOX.selectionScreenPrompt");
                prompt.Add("Amount", 1);
                var prefs = new CardSelectorPrefs(prompt, 1);

                List<CardModel> list = CardFactory.GetDistinctForCombat(relic.Owner, relic.Owner.Character.CardPool.GetUnlockedCards(relic.Owner.UnlockState, relic.Owner.RunState.CardMultiplayerConstraint), relic.DynamicVars.Cards.IntValue, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
                if (list.Count == 0)
                {
                    string text = "ChoicesParadox generated no cards for selection. Returning early to prevent softlock.";
                    Log.Error(text);
                    SentryService.CaptureException(new SoftlockException(text));
                    return;
                }

                foreach (CardModel item in list)
                {
                    CardCmd.ApplyKeyword(item, CardKeyword.Retain);
                }

                foreach (CardModel item2 in await CardSelectCmd.FromSimpleGrid(ctx, list, relic.Owner, prefs))
                {
                    await CardPileCmd.AddGeneratedCardToCombat(item2, PileType.Hand, addedByPlayer: true);
                }
            });

            Register<ChosenCheese>(async (relic, player, target, ctx) =>
            {
                //天选芝士
                //在战斗结束时，获得[blue]{MaxHp}[/blue]点最大生命值。
                await CreatureCmd.GainMaxHp(relic.Owner.Creature, relic.DynamicVars.MaxHp.BaseValue);
            });

            Register<Circlet>(async (relic, player, target, ctx) =>
            {
                //头环
                //这是一个头环。
            });

            Register<Claws>(async (relic, player, target, ctx) =>
            {
                //利爪
                //拾起时，将至多[blue]{Cards}[/blue]张牌[gold]变化[/gold]为[gold]撕咬[/gold]。
                //将一张撕咬加入牌组
            });

            Register<CloakClasp>(async (relic, player, target, ctx) =>
            {
                //斗篷扣
                //在你的回合结束时，每有一张[gold]手牌[/gold]，就获得[blue]{Block}[/blue]点[gold]格挡[/gold]
                IReadOnlyList<CardModel> cards = PileType.Hand.GetPile(relic.Owner).Cards;
                if (cards.Count != 0)
                {
                    int num = (int)((decimal)cards.Count * relic.DynamicVars.Block.BaseValue);
                    await CreatureCmd.GainBlock(relic.Owner.Creature, num, ValueProp.Unpowered, null);
                }
            });

            Register<CrackedCore>(async (relic, player, target, ctx) =>
            {
                //破损核心
                //在每场战斗开始时，[gold]生成[/gold][blue]{Lightning}[/blue]个[gold]闪电[/gold]充能球。
                await OrbCmd.Channel<LightningOrb>(new BlockingPlayerChoiceContext(), relic.Owner);

            });

            Register<Crossbow>(async (relic, player, target, ctx) =>
            {
                //十字弓
                //在你的回合开始时，将一张随机[gold]攻击牌[/gold]加入你的[gold]手牌[/gold]。这张牌在本回合可以免费打出。
                IReadOnlyList<CardModel> readOnlyList = (from c in relic.Owner.Character.CardPool.GetUnlockedCards(relic.Owner.UnlockState, relic.Owner.RunState.CardMultiplayerConstraint)
                                                         where c.Type == CardType.Attack
                                                         select c).ToList();
                if (readOnlyList.Count == 0)
                {
                    return;
                }

                List<CardModel> list = CardFactory.GetDistinctForCombat(relic.Owner, readOnlyList, 1, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
                foreach (CardModel item in list)
                {
                    item.SetToFreeThisTurn();
                }

                await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, addedByPlayer: true);
            });


            Register<CursedPearl>(async (relic, player, target, ctx) =>
            {
                //诅咒珍珠
                //拾起时，获得一张[red]贪婪[/red]，获得[blue]{Gold}[/blue][gold]金币[/gold]。
                await relic.AfterObtained();
            });

            Register<DarkstonePeriapt>(async (relic, player, target, ctx) =>
            {
                //黑石护符
                //每当你获得一张[red]诅咒[/red]，就将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await CreatureCmd.GainMaxHp(relic.Owner.Creature, relic.DynamicVars.MaxHp.BaseValue);
            });

            Register<DataDisk>(async (relic, player, target, ctx) =>
            {
                //数据磁盘
                //在每场战斗开始时，获得[blue]{FocusPower}[/blue]点[gold]集中[/gold]。
                await PowerCmd.Apply<FocusPower>(relic.Owner.Creature, relic.DynamicVars["FocusPower"].BaseValue, relic.Owner.Creature, null);
            });

            Register<DaughterOfTheWind>(async (relic, player, target, ctx) =>
            {
                //风的女儿
                //每当你打出一张攻击牌时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<DelicateFrond>(async (relic, player, target, ctx) =>
            {
                //娇嫩蕨草
                //在每场战斗开始时，用随机药水将你的空药水栏位填满。
                await relic.BeforeCombatStart();
            });

            Register<DemonTongue>(async (relic, player, target, ctx) =>
            {
                //恶魔之舌
                //当你第一次在自身回合内失去生命值时，回复等量的生命值。
            });

            Register<DeprecatedRelic>(async (relic, player, target, ctx) =>
            {
                //弃用遗物
                //这件遗物已经从游戏中被移除。它在这里仅做纪念意义。
            });

            Register<DiamondDiadem>(async (relic, player, target, ctx) =>
            {
                //钻石头冠
                //如果你在本回合打出的牌少于等于[blue]{CardThreshold}[/blue]张，则受到敌人的伤害减半。
                await PowerCmd.Apply<DiamondDiademPower>(relic.Owner.Creature, 1m, relic.Owner.Creature, null);
            });

            Register<DingyRug>(async (relic, player, target, ctx) =>
            {
                //肮脏地毯
                //卡牌奖励现在会包括无色牌。
            });

            Register<DistinguishedCape>(async (relic, player, target, ctx) =>
            {
                //卓越斗篷
                //拾起时，失去[red]9[/red]点最大生命值。将[blue]{Cards}[/blue]张[gold]灵体[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<DivineDestiny>(async (relic, player, target, ctx) =>
            {
                //天命所归
                //在每场战斗开始时，获得{Stars:starIcons()}。
                await PlayerCmd.GainStars(relic.DynamicVars.Stars.BaseValue, relic.Owner);
            });

            Register<DivineRight>(async (relic, player, target, ctx) =>
            {
                //天赋君权
                //在每场战斗开始时，获得{Stars:starIcons()}。
                await PlayerCmd.GainStars(relic.DynamicVars.Stars.BaseValue, relic.Owner);

            });

            Register<DollysMirror>(async (relic, player, target, ctx) =>
            {
                //多利之镜
                //拾起时，从你的[gold]牌组[/gold]中选择一张牌进行复制。
                await relic.AfterObtained();
            });

            Register<DragonFruit>(async (relic, player, target, ctx) =>
            {
                //火龙果
                //每当你获得[gold]金币[/gold]时，提升[blue]{MaxHp}[/blue]点你的最大生命值。
                await CreatureCmd.GainMaxHp(relic.Owner.Creature, relic.DynamicVars.MaxHp.BaseValue);
            });

            Register<DreamCatcher>(async (relic, player, target, ctx) =>
            {
                //捕梦网
                //每当你[gold]休息[/gold]时，可以添加一张牌到你的[gold]牌组[/gold]。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new CardReward(options, 3, relic.Owner));
                await RewardsCmd.OfferCustom(relic.Owner, list);

            });

            Register<Driftwood>(async (relic, player, target, ctx) =>
            {
                //浮木
                //你可以在每一个卡牌奖励中重掷一次。
            });

            Register<DustyTome>(async (relic, player, target, ctx) =>
            {
                //尘封魔典
                //拾起时，获得一张{AncientCard.StringValue:cond:[gold]{}+[/gold]|[gold]先古牌[/gold]}。
                await relic.AfterObtained();
            });

            Register<Ectoplasm>(async (relic, player, target, ctx) =>
            {
                //灵体外质
                //你不能再获得任何[gold]金币[/gold]。在回合开始时获得{Energy:energyIcons()}
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);

            });

            Register<ElectricShrymp>(async (relic, player, target, ctx) =>
            {
                //放电异虾
                //拾起时，选择一张技能牌为它[gold]附魔[/gold]：[purple]注能[/purple]。
                await relic.AfterObtained();
            });

            Register<EmberTea>(async (relic, player, target, ctx) =>
            {
                //余烬茶
                //在接下来的[blue]{Combats}[/blue]场战斗开始时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, null, null);
            });

            Register<EmotionChip>(async (relic, player, target, ctx) =>
            {
                //情感芯片
                //在每回合开始时，如果你在之前回合受到过伤害，则触发所有充能球的被动效果。
                foreach (OrbModel orb in relic.Owner.PlayerCombatState.OrbQueue.Orbs)
                {
                    await OrbCmd.Passive(ctx, orb, null);
                    await Cmd.Wait(0.25f);
                }
            });

            Register<EmptyCage>(async (relic, player, target, ctx) =>
            {
                //空鸟笼
                //拾起时，选择移除[gold]牌组[/gold]中的[blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

            

            Register<EternalFeather>(async (relic, player, target, ctx) =>
            {
                //永恒羽毛
                //你的[gold]牌组[/gold]中每有[blue]{Cards}[/blue]张牌，当你进入[gold]休息处[/gold]时就会回复[green]{Heal}[/green]点生命。
                int num = PileType.Deck.GetPile(relic.Owner).Cards.Count / relic.DynamicVars.Cards.IntValue;
                decimal healAmount = relic.DynamicVars.Heal.BaseValue * (decimal)num;
                await CreatureCmd.Heal(relic.Owner.Creature, healAmount);
                
            });

            Register<FakeAnchor>(async (relic, player, target, ctx) =>
            {
                //锚？？？
                //在每场战斗开始时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<FakeBloodVial>(async (relic, player, target, ctx) =>
            {
                //小血瓶？？？
                //在每场战斗开始时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<FakeHappyFlower>(async (relic, player, target, ctx) =>
            {
                //开心小花？？？
                //每[blue]{Turns}[/blue]个回合，获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<FakeLeesWaffle>(async (relic, player, target, ctx) =>
            {
                //李家华夫饼？？？
                //拾起时，回复你[green]{Heal}%[/green]的生命值。
                await relic.AfterObtained();
            });

            Register<FakeMango>(async (relic, player, target, ctx) =>
            {
                //芒果？？？
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<FakeMerchantsRug>(async (relic, player, target, ctx) =>
            {
                //商人的地毯？？？
                //低劣的仿制品。没有任何作用。
            });

            Register<FakeOrichalcum>(async (relic, player, target, ctx) =>
            {
                //奥利哈钢？？？
                //如果你在回合结束时没有任何[gold]格挡[/gold]，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<FakeSneckoEye>(async (relic, player, target, ctx) =>
            {
                //异蛇之眼？？？
                //每场战斗开始时获得[red]混乱[/red]效果。
            });

            Register<FakeStrikeDummy>(async (relic, player, target, ctx) =>
            {
                //打击木偶？？？
                //名字中有“打击”的卡牌造成[blue]{ExtraDamage}[/blue]点额外伤害。
                await PowerCmd.Apply<VigorPower>(relic.Owner.Creature, relic.DynamicVars["ExtraDamage"].BaseValue, relic.Owner.Creature, null);
            });

            Register<FakeVenerableTeaSet>(async (relic, player, target, ctx) =>
            {
                //古茶具套装？？？
                //到达[gold]休息处[/gold]后的下一场战斗开始时额外获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<FencingManual>(async (relic, player, target, ctx) =>
            {
                //击剑指南
                //在每场战斗开始时，[gold]铸造[/gold][blue]{Forge}[/blue]。
                await ForgeCmd.Forge(relic.DynamicVars.Forge.BaseValue, relic.Owner, relic);
            });

            Register<FestivePopper>(async (relic, player, target, ctx) =>
            {
                //节日拉炮
                //在每场战斗开始时，对所有敌人造成[blue]{Damage}[/blue]点伤害。
                CombatState combatState = player.Creature.CombatState;
                VfxCmd.PlayOnCreatureCenters(combatState.HittableEnemies, "vfx/vfx_attack_slash");
                await CreatureCmd.Damage(ctx, combatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);

            });

            Register<Fiddle>(async (relic, player, target, ctx) =>
            {
                //小提琴
                //在每个回合开始时，额外抽[blue]{Cards}[/blue]张牌。你在回合进行中不再能抽任何牌。
            });

            Register<ForgottenSoul>(async (relic, player, target, ctx) =>
            {
                //遗忘之魂
                //每当你[gold]消耗[/gold]一张牌，随机对一名敌人造成[blue]{Damage}[/blue]点伤害。
                Creature creature = relic.Owner.RunState.Rng.CombatTargets.NextItem(relic.Owner.Creature.CombatState.HittableEnemies);
                if (creature != null)
                {
                    VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_attack_blunt");
                    await CreatureCmd.Damage(ctx, creature, relic.DynamicVars.Damage, relic.Owner.Creature);
                }
            });

            Register<FragrantMushroom>(async (relic, player, target, ctx) =>
            {
                //芳香蘑菇
                //拾起时，失去[blue]{HpLoss}[/blue]点生命，然后随机[gold]升级[/gold][blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

            Register<FresnelLens>(async (relic, player, target, ctx) =>
            {
                //菲涅耳透镜
                //每当你将一张带有[gold]格挡[/gold]的牌加入你的[gold]牌组[/gold]时，为那张牌[gold]附魔[/gold]：[purple]灵巧[/purple][blue]{NimbleAmount}[/blue]
            });

            Register<FrozenEgg>(async (relic, player, target, ctx) =>
            {
                //冻结之蛋
                //每当你获得能力牌时，将其[gold]升级[/gold]。
            });

            Register<FuneraryMask>(async (relic, player, target, ctx) =>
            {
                //葬礼面具
                //在每场战斗开始时，将[blue]{Cards}[/blue]张[gold]灵魂[/gold]加入你的[gold]抽牌堆[/gold]。
                for (int i = 0; (decimal)i < relic.DynamicVars.Cards.BaseValue; i++)
                {
                    CardModel card = player.Creature.CombatState.CreateCard(ModelDb.Card<Soul>(), relic.Owner);
                    CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
                }
            });

            Register<FurCoat>(async (relic, player, target, ctx) =>
            {
                //皮草大衣
                //拾起时，随机标记[blue]{Combats}[/blue]处战斗。这些战斗中的敌人将只有[blue]1[/blue]点生命。
            });

            Register<GalacticDust>(async (relic, player, target, ctx) =>
            {
                //星系尘埃
                //每消耗[blue]{Stars}[/blue]点{singleStarIcon}，就获得[blue]{Block}[/blue][gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<GamblingChip>(async (relic, player, target, ctx) =>
            {
                //赌博筹码
                //在每场战斗开始时，丢弃任意张牌，然后抽相同数量张牌。
                List<CardModel> list = (await CardSelectCmd.FromHandForDiscard(ctx, relic.Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 999999999), null, null)).ToList();
                if (list.Count != 0)
                {
                    await CardCmd.DiscardAndDraw(ctx, list, list.Count);
                }
            });

            Register<GamePiece>(async (relic, player, target, ctx) =>
            {
                //棋子
                //每当你打出能力牌时，抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<GhostSeed>(async (relic, player, target, ctx) =>
            {
                //幽灵种子
                //[gold]打击[/gold]和[gold]防御[/gold]获得[gold]虚无[/gold]。
            });

            Register<Girya>(async (relic, player, target, ctx) =>
            {
                //壶铃
                //你现在能在[gold]休息处[/gold]获得[gold]力量[/gold]。（最多[blue]3[/blue]次）
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DisplayAmount, null, null);
            });

            Register<GlassEye>(async (relic, player, target, ctx) =>
            {
                //玻璃眼珠
                //拾起时，获得[blue]2[/blue]组[gold]普通[/gold]、[blue]2[/blue]组[gold]罕见[/gold]、和[blue]1[/blue]组[gold]稀有[/gold]卡牌奖励。
                await relic.AfterObtained();
            });

            Register<Glitter>(async (relic, player, target, ctx) =>
            {
                //亮片
                //为之后的所有卡牌奖励[gold]附魔[/gold]：[purple]华彩[/purple]。
            });

            

            Register<GnarledHammer>(async (relic, player, target, ctx) =>
            {
                //扭曲锤子
                //拾起时，从你的[gold]牌组[/gold]中选择至多[blue]{Cards}[/blue]张攻击牌，[gold]附魔[/gold]：[purple]锋利[/purple][blue]{SharpAmount:diff()}[/blue]。
                await relic.AfterObtained();
            });

            Register<GoldenCompass>(async (relic, player, target, ctx) =>
            {
                //黄金罗盘
                //拾起时，将第[blue]2[/blue][gold]阶段[/gold]的地图替换为一条特殊的直道。
            });

            Register<GoldenPearl>(async (relic, player, target, ctx) =>
            {
                //金色珍珠
                //拾起时，获得[blue]{Gold}[/blue][gold]金币[/gold]。
                await relic.AfterObtained();
            });

            Register<GoldPlatedCables>(async (relic, player, target, ctx) =>
            {
                //镀金缆线
                //你最右侧的充能球会额外触发一次被动效果。
                await PowerCmd.Apply<LoopPower>(relic.Owner.Creature, 1, relic.Owner.Creature, null);
            });

            Register<Gorget>(async (relic, player, target, ctx) =>
            {
                //护喉甲
                //在每场战斗开始时，获得[blue]{PlatingPower}[/blue]层[gold]覆甲[/gold]。
                await PowerCmd.Apply<PlatingPower>(relic.Owner.Creature, relic.DynamicVars["PlatingPower"].BaseValue, relic.Owner.Creature, null);
            });

            Register<GremlinHorn>(async (relic, player, target, ctx) =>
            {
                //地精之角
                //每当有一名敌人死亡时，获得{Energy:energyIcons()}并抽[blue]{Cards}[/blue]张牌。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<HandDrill>(async (relic, player, target, ctx) =>
            {
                //手钻
                //每当你突破敌人的[gold]格挡[/gold]时，给予其[blue]{VulnerablePower}[/blue]层[gold]易伤[/gold]。
                await PowerCmd.Apply<VulnerablePower>(target, relic.DynamicVars.Vulnerable.BaseValue, relic.Owner.Creature, null);
            });

            Register<HappyFlower>(async (relic, player, target, ctx) =>
            {
                //开心小花
                //每[blue]{Turns}[/blue]个回合，获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<HeftyTablet>(async (relic, player, target, ctx) =>
            {
                //沉重石板
                //拾起时，从[blue]{Cards}[/blue]张稀有牌中选择[blue]1[/blue]张加入你的[gold]牌组[/gold]，同时将[blue]1[/blue]张[red]受伤[/red]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<HelicalDart>(async (relic, player, target, ctx) =>
            {
                //螺线飞镖
                //你每打出一张[gold]小刀[/gold]，就在本回合获得[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<HelicalDartPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.IntValue, relic.Owner.Creature, null);
            });

            Register<HistoryCourse>(async (relic, player, target, ctx) =>
            {
                //历史课
                //在你的回合开始时，打出一张你上一回合最后打出的攻击牌或技能牌的复制品。
                CardModel? cardModel = CombatManager.Instance.History.CardPlaysFinished.LastOrDefault(delegate (CardPlayFinishedEntry e)
                {
                    bool flag = e.CardPlay.Card.Owner == relic.Owner && e.RoundNumber == relic.Owner.Creature.CombatState.RoundNumber - 1;
                    bool flag2 = flag;
                    if (flag2)
                    {
                        CardType type = e.CardPlay.Card.Type;
                        bool flag3 = (uint)(type - 1) <= 1u;
                        flag2 = flag3;
                    }

                    return flag2 && !e.CardPlay.Card.IsDupe;
                })?.CardPlay.Card;
                if (cardModel != null)
                {

                    await CardCmd.AutoPlay(ctx, cardModel.CreateDupe(), null);
                }
            });

            Register<HornCleat>(async (relic, player, target, ctx) =>
            {
                //船夹板
                //在你的[blue]第二[/blue]回合开始时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<IceCream>(async (relic, player, target, ctx) =>
            {
                //冰淇淋
                //多余的能量可以留到下一回合。

            });

            Register<InfusedCore>(async (relic, player, target, ctx) =>
            {
                //注能核心
                //在每场战斗开始时，[gold]生成[/gold][blue]{Lightning}[/blue]个[gold]闪电[/gold]充能球。
                for (int i = 0; (decimal)i < relic.DynamicVars["Lightning"].BaseValue; i++)
                {
                    await OrbCmd.Channel<LightningOrb>(new BlockingPlayerChoiceContext(), relic.Owner);
                }
            });

            

            Register<IntimidatingHelmet>(async (relic, player, target, ctx) =>
            {
                //骇人头盔
                //每当你打出一张耗能大于等于{Energy:energyIcons()}的牌，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<IronClub>(async (relic, player, target, ctx) =>
            {
                //铁棒
                //你每打出[blue]{Cards}[/blue]张牌，就抽[blue]1[/blue]张牌。
                await CardPileCmd.Draw(ctx, 1m, relic.Owner);
            });

            Register<IvoryTile>(async (relic, player, target, ctx) =>
            {
                //象牙麻将牌
                //每当你打出一张耗能大于等于{EnergyThreshold:energyIcons()}的牌时，获得{Energy:energyIcons()}。
                await PowerCmd.Apply<EnergyNextTurnPower>(relic.Owner.Creature, relic.DynamicVars.Energy.IntValue, relic.Owner.Creature, null);
            });

            Register<JeweledMask>(async (relic, player, target, ctx) =>
            {
                //宝石面具
                //在每场战斗开始时，将一张随机能力牌从你的[gold]抽牌堆[/gold]放入你的[gold]手牌[/gold]，这张牌可以免费打出。
                IReadOnlyList<CardModel> cards = PileType.Draw.GetPile(player).Cards;
                List<CardModel> list = cards.Where((CardModel c) => c.Type == CardType.Power).ToList();
                if (list.Count != 0)
                {
                    CardModel cardModel = player.RunState.Rng.CombatCardSelection.NextItem(list);
                    cardModel.SetToFreeThisTurn();
                    await CardPileCmd.Add(cardModel, PileType.Hand);
                }
            });

            Register<JewelryBox>(async (relic, player, target, ctx) =>
            {
                //珠宝盒
                //拾起时，将[blue]1[/blue]张[gold]神化[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<JossPaper>(async (relic, player, target, ctx) =>
            {
                //金纸
                //你每[gold]消耗[/gold][blue]{ExhaustAmount}[/blue]张牌，就抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<JuzuBracelet>(async (relic, player, target, ctx) =>
            {
                //佛珠手链
                //你在[gold]?[/gold]房间中不会再遭遇常规战斗。
            });

            Register<Kifuda>(async (relic, player, target, ctx) =>
            {
                //木札
                //拾起时，从你的[gold]牌组[/gold]中选择至多[blue]{Cards}[/blue]张牌，[gold]附魔[/gold]：[purple]伶俐[/purple]。
                await relic.AfterObtained();
            });

            Register<Kunai>(async (relic, player, target, ctx) =>
            {
                //苦无
                //你每在同一回合内打出[blue]{Cards}[/blue]张攻击牌，就获得[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<DexterityPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.BaseValue, relic.Owner.Creature, null);
            });

            Register<Kusarigama>(async (relic, player, target, ctx) =>
            {
                //锁镰
                //你每在同一回合内打出[blue]{Cards}[/blue]张攻击牌，就随机对一名敌人造成[blue]{Damage}[/blue]点伤害。
                Creature creature = relic.Owner.RunState.Rng.CombatTargets.NextItem(relic.Owner.Creature.CombatState.HittableEnemies);
                if (creature != null)
                {
                    await CreatureCmd.Damage(ctx, creature, relic.DynamicVars.Damage, relic.Owner.Creature);
                }
            });

            Register<Lantern>(async (relic, player, target, ctx) =>
            {
                //灯笼
                //在每场战斗的第一回合获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<LargeCapsule>(async (relic, player, target, ctx) =>
            {
                //巨大扭蛋
                //拾起时，获得[blue]{Relics}[/blue]件随机[gold]遗物[/gold]。额外将一对[gold]打击[/gold]和[gold]防御[/gold]，加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<LastingCandy>(async (relic, player, target, ctx) =>
            {
                //吃不完的糖
                //每两场战斗，你的卡牌奖励就会额外包含一张能力牌。
            });

            Register<LavaLamp>(async (relic, player, target, ctx) =>
            {
                //熔岩灯
                //在战斗结束时，如果你没有受伤，则[gold]升级[/gold]你的所有卡牌奖励。

            });

            Register<LavaRock>(async (relic, player, target, ctx) =>
            {
                //熔岩石
                //第一阶段的Boss敌人额外掉落[blue]{Relics}[/blue]件[gold]遗物[/gold]。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new RelicReward(relic.Owner));
                list.Add(new RelicReward(relic.Owner));
                await RewardsCmd.OfferCustom(relic.Owner, list);

            });

            Register<LeadPaperweight>(async (relic, player, target, ctx) =>
            {
                //铅制镇纸
                //拾起时，从[blue]2[/blue]张无色牌中选择[blue]1张[/blue]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<LeafyPoultice>(async (relic, player, target, ctx) =>
            {
                //树叶药膏
                //拾起时，[gold]变化[/gold]你的[blue]1[/blue]张[gold]打击[/gold]和[blue]1[/blue]张[gold]防御[/gold]，然后失去[blue]{MaxHp}[/blue]点最大生命。
                await relic.AfterObtained();
            });

            Register<LeesWaffle>(async (relic, player, target, ctx) =>
            {
                //李家华夫饼
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]点，并回复所有生命。
                await relic.AfterObtained();
            });

            Register<LetterOpener>(async (relic, player, target, ctx) =>
            {
                //开信刀
                //你每在同一回合内打出[blue]{Cards}[/blue]张技能牌，就对所有敌人造成[blue]{Damage}[/blue]点伤害。
                int intValue = relic.DynamicVars.Cards.IntValue;

                await CreatureCmd.Damage(ctx, relic.Owner.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);

            });

            Register<LizardTail>(async (relic, player, target, ctx) =>
            {
                //蜥蜴尾巴
                //当你的生命值将要降低至[blue]0[/blue]或以下时，回复到最大生命值的[green]{Heal}%[/green]（仅能起效一次）。
            });

            Register<LoomingFruit>(async (relic, player, target, ctx) =>
            {
                //布质果实
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<LordsParasol>(async (relic, player, target, ctx) =>
            {
                //领主阳伞
                //当你遇见[gold]商人[/gold]时，立刻获得他所出售的[red]所有[/red]物品。
            });

            Register<LostCoffer>(async (relic, player, target, ctx) =>
            {
                //失物盒
                //拾起时，获得[blue]1[/blue]次卡牌奖励和[blue]1[/blue]瓶随机药水。
                await relic.AfterObtained();
            });

            Register<LostWisp>(async (relic, player, target, ctx) =>
            {
                //迷失鬼火
                //你每打出一张[gold]能力牌[/gold]，就对所有敌人造成[blue]{Damage}[/blue]点伤害。
                await CreatureCmd.Damage(ctx, relic.Owner.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage.BaseValue, relic.DynamicVars.Damage.Props, relic.Owner.Creature, null);
            });

            Register<LuckyFysh>(async (relic, player, target, ctx) =>
            {
                //招财异鱼
                //每当你将一张卡牌加入你的[gold]牌组[/gold]时，获得[blue]{Gold}[/blue][gold]金币[/gold]。
                await PlayerCmd.GainGold(relic.DynamicVars.Gold.BaseValue, relic.Owner);
            });

            Register<LunarPastry>(async (relic, player, target, ctx) =>
            {
                //月亮糕点
                //在你的回合结束时，获得{Stars:starIcons()}。
                await PlayerCmd.GainStars(relic.DynamicVars.Stars.BaseValue, relic.Owner);
            });

            Register<Mango>(async (relic, player, target, ctx) =>
            {
                //芒果
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<MassiveScroll>(async (relic, player, target, ctx) =>
            {
                //巨大卷轴
                //拾起时，从[blue]3[/blue]张[gold]多人游戏牌[/gold]中选择[blue]1[/blue]张加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<MawBank>(async (relic, player, target, ctx) =>
            {
                //巨口储蓄罐
                //每攀爬一层楼层，就获得[blue]{Gold}[/blue][gold]金币[/gold]。一旦在商店中花费[gold]金币[/gold]就会使其失效。
                await PlayerCmd.GainGold(relic.DynamicVars.Gold.BaseValue, relic.Owner);
            });

            Register<MealTicket>(async (relic, player, target, ctx) =>
            {
                //餐券
                //每当你进入商店房间时，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<MeatCleaver>(async (relic, player, target, ctx) =>
            {
                //切肉刀
                //你可以在休息处进行[gold]烹饪[/gold]。
            });

            Register<MeatOnTheBone>(async (relic, player, target, ctx) =>
            {
                //带骨肉
                //如果你在战斗结束时生命值等于或低于[blue]{HpThreshold}%[/blue]，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<MembershipCard>(async (relic, player, target, ctx) =>
            {
                //会员卡
                //所有商品打折[blue]{Discount}%[/blue]！
            });

            Register<MercuryHourglass>(async (relic, player, target, ctx) =>
            {
                //水银沙漏
                //在你的回合开始时，对所有敌人造成[blue]{Damage}[/blue]点伤害。
                await CreatureCmd.Damage(ctx, player.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);
            });

            Register<Metronome>(async (relic, player, target, ctx) =>
            {
                //节拍器
                //每场战斗中你首次[gold]生成[/gold][blue]{OrbCount}[/blue]个[gold]充能球[/gold]时，对所有敌人造成[blue]{Damage}[/blue]点伤害。
                await CreatureCmd.Damage(ctx, player.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);
            });

            Register<MiniatureCannon>(async (relic, player, target, ctx) =>
            {
                //微型大炮
                //[gold]升级[/gold]的攻击牌额外造成[blue]{ExtraDamage}[/blue]点伤害。
                await PowerCmd.Apply<VigorPower>(relic.Owner.Creature, relic.DynamicVars["ExtraDamage"].BaseValue, relic.Owner.Creature, null);
            });

            Register<MiniatureTent>(async (relic, player, target, ctx) =>
            {
                //微型帐篷
                //你可以在[gold]休息处[/gold]选择任意数量的选项。
            });

            Register<MiniRegent>(async (relic, player, target, ctx) =>
            {
                //迷你储君
                //每回合你第一次花费{singleStarIcon}时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
            });

            Register<MoltenEgg>(async (relic, player, target, ctx) =>
            {
                //熔火之蛋
                //每当你获得[gold]攻击牌[/gold]时，将其[gold]升级[/gold]。
            });

            Register<MrStruggles>(async (relic, player, target, ctx) =>
            {
                //抱抱先生
                //在你的回合开始时，对所有敌人造成等量于当前回合数的伤害。
                CombatState combatState = player.Creature.CombatState;
                await CreatureCmd.Damage(ctx, combatState.HittableEnemies, combatState.RoundNumber, ValueProp.Unpowered, relic.Owner.Creature);
            });

            Register<MummifiedHand>(async (relic, player, target, ctx) =>
            {
                // 干瘪之手：手牌中一张随机牌本回合免费
                var hand = PileType.Hand.GetPile(player).Cards;
                var rng = player.RunState.Rng.CombatCardSelection;
                // 先找有费用（非0）的牌
                var candidates = hand.Where(c => c.CostsEnergyOrStars(includeGlobalModifiers: false)).ToList();
                if (candidates.Count == 0)
                {
                    // 如果没有，则找任何需要消耗资源的牌（包括0费但有星星等）
                    candidates = hand.Where(c => c.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList();
                }
                var card = rng.NextItem(candidates);
                card?.SetToFreeThisTurn();
            });

            Register<MusicBox>(async (relic, player, target, ctx) =>
            {
                //音乐盒
                //将你每回合打出的第一张攻击牌的一张[gold]虚无[/gold]复制品加入你的手牌。

            });

            

            Register<MysticLighter>(async (relic, player, target, ctx) =>
            {
                //神秘打火机
                //有[gold]附魔[/gold]的攻击牌额外造成[blue]{Damage}[/blue]点伤害。
                await PowerCmd.Apply<VigorPower>(relic.Owner.Creature, relic.DynamicVars.Damage.IntValue, relic.Owner.Creature, null);
            });

            Register<NeowsBones>(async (relic, player, target, ctx) =>
            {
                //涅奥的骨头
                //拾起时，获得[blue]{Relics}[/blue]件随机[gold]涅奥{Relics:plural:遗物|遗物}[/gold]。将[blue]{Curses}[/blue]张随机[red]{Curses:plural:诅咒|诅咒}[/red]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<NeowsTalisman>(async (relic, player, target, ctx) =>
            {
                //涅奥的护符
                //拾起时，[gold]升级[/gold]你的[blue]1[/blue]张[gold]打击[/gold]和[blue]1[/blue]张[gold]防御[/gold]。
                var deck = PileType.Deck.GetPile(player).Cards;
                var rng = player.RunState.Rng.CombatCardSelection;

                // 筛选未升级的基础打击
                var unupgradedStrikes = deck
                    .Where(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Strike) && !c.IsUpgraded)
                    .ToList();
                // 筛选未升级的基础防御
                var unupgradedDefends = deck
                    .Where(c => c.Rarity == CardRarity.Basic && c.Tags.Contains(CardTag.Defend) && !c.IsUpgraded)
                    .ToList();

                if (unupgradedStrikes.Any())
                {
                    var strike = rng.NextItem(unupgradedStrikes);
                    CardCmd.Upgrade(strike);
                }

                if (unupgradedDefends.Any())
                {
                    var defend = rng.NextItem(unupgradedDefends);
                    CardCmd.Upgrade(defend);
                }
            });

            Register<NeowsTorment>(async (relic, player, target, ctx) =>
            {
                //涅奥的苦痛
                //拾起时，将[blue]1[/blue]张[gold]涅奥之怒[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<NewLeaf>(async (relic, player, target, ctx) =>
            {
                //新叶
                //拾起时，[gold]变化[/gold][blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

            Register<NinjaScroll>(async (relic, player, target, ctx) =>
            {
                //忍术卷轴
                //每场战斗开始时，将[blue]{Shivs}[/blue]张[gold]小刀[/gold]加入你的[gold]手牌[/gold]。
                await Shiv.CreateInHand(relic.Owner, relic.DynamicVars["Shivs"].IntValue, player.Creature.CombatState);
            });

            Register<Nunchaku>(async (relic, player, target, ctx) =>
            {
                //双截棍
                //你每打出[blue]{Cards}[/blue]张攻击牌，获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<NutritiousOyster>(async (relic, player, target, ctx) =>
            {
                //营养牡蛎
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<NutritiousSoup>(async (relic, player, target, ctx) =>
            {
                //营养汤
                //拾起时，为你[gold]牌组[/gold]中的所有“[gold]打击[/gold]”[gold]附魔[/gold]：[purple]特兹卡塔拉的余烬[/purple]
                await relic.AfterObtained();
            });

            Register<OddlySmoothStone>(async (relic, player, target, ctx) =>
            {
                //意外光滑的石头
                //在每场战斗开始时，获得[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<DexterityPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.BaseValue, relic.Owner.Creature, null);
            });

            Register<OldCoin>(async (relic, player, target, ctx) =>
            {
                //古钱币
                //拾起时，获得[blue]{Gold}[/blue][gold]金币[/gold]。
                await relic.AfterObtained();
            });

            Register<OrangeDough>(async (relic, player, target, ctx) =>
            {
                //橙色团块
                //在每场战斗开始时，将[blue]{Cards}[/blue]张随机无色牌加入你的[gold]手牌[/gold]。
                List<CardModel> cards = CardFactory.GetDistinctForCombat(relic.Owner, ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(relic.Owner.UnlockState, relic.Owner.RunState.CardMultiplayerConstraint), relic.DynamicVars.Cards.IntValue, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
                await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
            });

            Register<Orichalcum>(async (relic, player, target, ctx) =>
            {
                //奥利哈钢
                //如果你在回合结束时没有任何[gold]格挡[/gold]，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<OrnamentalFan>(async (relic, player, target, ctx) =>
            {
                //精致折扇
                //你每在同一回合内打出[blue]{Cards}[/blue]张攻击牌，就获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<Orrery>(async (relic, player, target, ctx) =>
            {
                //星系仪
                //拾起时，获得[blue]{Cards}[/blue]次卡牌奖励
                await relic.AfterObtained();
            });

            Register<PaelsBlood>(async (relic, player, target, ctx) =>
            {
                //佩尔之血
                //在你的回合开始时，额外抽[blue]{Cards}[/blue]张牌
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<PaelsClaw>(async (relic, player, target, ctx) =>
            {
                //佩尔之爪
                //拾起时，为所有的“[gold]防御[/gold]”[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。
                await relic.AfterObtained();
            });

            Register<PaelsEye>(async (relic, player, target, ctx) =>
            {
                //佩尔之眼
                //你在每场战斗中第一次没有打出任何牌就结束回合时，[gold]消耗[/gold]所有[gold]手牌[/gold]然后进行一个额外回合。
            });

            Register<PaelsFlesh>(async (relic, player, target, ctx) =>
            {
                //佩尔之肉
                //从你的第[blue]3[/blue]回合开始，在回合开始时额外获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<PaelsGrowth>(async (relic, player, target, ctx) =>
            {
                //佩尔的增生组织
                //拾起时，从[gold]牌组[/gold]中选择一张牌，为其[gold]附魔[/gold]：[purple]{EnchantmentName}[/purple]。
                await relic.AfterObtained();
            });

            Register<PaelsHorn>(async (relic, player, target, ctx) =>
            {
                //佩尔之角
                //拾起时，将[blue]2[/blue]张[gold]放松[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<PaelsLegion>(async (relic, player, target, ctx) =>
            {
                //佩尔的士兵
                //将你从一张卡牌中获得的[gold]格挡[/gold]翻倍，然后此遗物会休眠[blue]{Turns}[/blue]回合。
            });

            Register<PaelsTears>(async (relic, player, target, ctx) =>
            {
                //佩尔之泪
                //如果你在拥有未花费的{energyPrefix:energyIcons(1)}情况下结束回合，则下个回合额外获得{Energy:energyIcons()}。
                await PowerCmd.Apply<EnergyNextTurnPower>(relic.Owner.Creature, relic.DynamicVars.Energy.IntValue, relic.Owner.Creature, null);

            });

            Register<PaelsTooth>(async (relic, player, target, ctx) =>
            {
                //佩尔之牙
                //拾起时，从你的[gold]牌组[/gold]中选择[blue]{Cards}[/blue]张牌移除。在每场战斗结束时，将其中随机[blue]1[/blue]张牌[gold]升级[/gold]然后返还。{CardTitles.StringValue:cond:

            });

            Register<PaelsWing>(async (relic, player, target, ctx) =>
            {
                //佩尔之翼
                //你可以将你的卡牌奖励献祭给佩尔。每献祭[blue]{Sacrifices}[/blue]次，就能获得一件[gold]遗物[/gold]。
                await RewardsCmd.OfferCustom(relic.Owner, new List<Reward>(1)
                {
                    new RelicReward(relic.Owner)
                });
            });

            Register<PandorasBox>(async (relic, player, target, ctx) =>
            {
                //潘多拉魔盒
                //[gold]变化[/gold]所有“[gold]打击[/gold]”和“[gold]防御[/gold]”。
                await relic.AfterObtained();
            });

            Register<Pantograph>(async (relic, player, target, ctx) =>
            {
                //缩放仪
                //在[gold]Boss[/gold]战开始时，回复[green]{Heal}[/green]点生命值。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<PaperKrane>(async (relic, player, target, ctx) =>
            {
                //纸鹤
                //有[gold]虚弱[/gold]状态的敌人造成的伤害降低[blue]40%[/blue]而非[blue]25%[/blue]。
            });

            Register<PaperPhrog>(async (relic, player, target, ctx) =>
            {
                //纸蛙
                //有[gold]易伤[/gold]状态的敌人受到的伤害增加[blue]75%[/blue]而非[blue]50%[/blue]。
            });

            Register<ParryingShield>(async (relic, player, target, ctx) =>
            {
                //招架盾
                //如果你在回合结束时拥有至少[blue]{Block}[/blue]点[gold]格挡[/gold]，则对随机敌人造成[blue]{Damage}[/blue]点伤害。
                Creature creature = relic.Owner.RunState.Rng.CombatTargets.NextItem(relic.Owner.Creature.CombatState.HittableEnemies);
                if (creature != null)
                {
                    VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_attack_blunt");
                    await CreatureCmd.Damage(ctx, creature, relic.DynamicVars.Damage, relic.Owner.Creature);
                }
            });

            Register<Pear>(async (relic, player, target, ctx) =>
            {
                //梨子
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<Pendulum>(async (relic, player, target, ctx) =>
            {
                //摆动球
                //每[blue]{Turns}[/blue]个回合，抽[blue]{Cards}[/blue]张牌
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<PenNib>(async (relic, player, target, ctx) =>
            {
                //钢笔尖
                //你每打出的第[blue]10[/blue]张攻击牌将会造成双倍伤害。

            });

            Register<Permafrost>(async (relic, player, target, ctx) =>
            {
                //永冻冰晶
                //当你在战斗中第一次打出能力牌时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<PetrifiedToad>(async (relic, player, target, ctx) =>
            {
                //石化蟾蜍
                //在每场战斗开始时，获得一瓶[gold]药水形状的石头[/gold]
                await PotionCmd.TryToProcure<PotionShapedRock>(relic.Owner);
            });

            Register<PhialHolster>(async (relic, player, target, ctx) =>
            {
                //药瓶皮套
                //拾起时，获得[blue]{PotionSlots}[/blue]个药水{PotionSlots:plural:栏位|栏位}并获得[blue]{Potions}[/blue]瓶随机{Potions:plural:药水|药水}。
                await relic.AfterObtained();
            });

            Register<PhilosophersStone>(async (relic, player, target, ctx) =>
            {
                //贤者之石
                //在每回合开始时获得{Energy:energyIcons()}。所有敌人初始获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
                IEnumerable<Creature> targets = from c in relic.Owner.Creature.CombatState.GetOpponentsOf(relic.Owner.Creature)
                                                where c.IsAlive
                                                select c;
                await PowerCmd.Apply<StrengthPower>(targets, relic.DynamicVars["StrengthPower"].BaseValue, null, null);

            });

            Register<PhylacteryUnbound>(async (relic, player, target, ctx) =>
            {
                //无界命匣
                //在每场战斗开始时，[gold]召唤[/gold][blue]{StartOfCombat}[/blue]。在你的回合开始时，[gold]召唤[/gold][blue]{StartOfTurn}[/blue]。
                await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), relic.Owner, relic.DynamicVars["StartOfCombat"].BaseValue, null);
                await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), relic.Owner, relic.DynamicVars["StartOfTurn"].BaseValue, null);
            });

            Register<Planisphere>(async (relic, player, target, ctx) =>
            {
                //活动星图
                //每当你进入[gold]？[/gold]房间的时候，回复[green]{Heal}[/green]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<Pocketwatch>(async (relic, player, target, ctx) =>
            {
                //怀表
                //当你在本回合打出的牌少于等于[blue]{CardThreshold}[/blue]张时，则在你的下个回合开始时额外抽[blue]{Cards}[/blue]张牌。
                await PowerCmd.Apply<DrawCardsNextTurnPower>(relic.Owner.Creature, relic.DynamicVars.Cards.BaseValue, relic.Owner.Creature, null);
            });

            Register<PollinousCore>(async (relic, player, target, ctx) =>
            {
                //花粉核心
                //每[blue]{Turns}[/blue]个回合，额外抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<Pomander>(async (relic, player, target, ctx) =>
            {
                //橙型香盒
                //拾起时，[gold]升级[/gold]一张牌。
                await relic.AfterObtained();
            });

            Register<PotionBelt>(async (relic, player, target, ctx) =>
            {
                //药水腰带
                //拾起时，获得[blue]{PotionSlots}[/blue]个药水栏位。
                await relic.AfterObtained();
            });

            Register<PowerCell>(async (relic, player, target, ctx) =>
            {
                //能量电池
                //在每场战斗开始时，将[blue]{Cards}[/blue]张耗能为0的卡牌从你的[gold]抽牌堆[/gold]放入你的[gold]手牌[/gold]。
                IEnumerable<CardModel> cards = PileType.Draw.GetPile(relic.Owner).Cards.Where((CardModel c) => !c.EnergyCost.CostsX && c.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0).ToList().StableShuffle(relic.Owner.RunState.Rng.CombatCardSelection)
                .Take(relic.DynamicVars.Cards.IntValue);
                await CardPileCmd.Add(cards, PileType.Hand);
            });

            Register<PrayerWheel>(async (relic, player, target, ctx) =>
            {
                //转经轮
                //普通敌人额外掉落一次卡牌奖励。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new CardReward(CardCreationOptions.ForRoom(player, RoomType.Monster), 3, player));
                await RewardsCmd.OfferCustom(relic.Owner, list);
            });

            Register<PrecariousShears>(async (relic, player, target, ctx) =>
            {
                //松动羊毛剪
                //拾起时，从你的[gold]牌组[/gold]中移除[blue]{Cards}[/blue]张牌并失去[blue]{Damage}[/blue]点生命。
                await relic.AfterObtained();
            });

            Register<PreciseScissors>(async (relic, player, target, ctx) =>
            {
                //精准剪刀
                //拾起时，从你的[gold]牌组[/gold]中移除[blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

            Register<PreservedFog>(async (relic, player, target, ctx) =>
            {
                //腌制活雾
                //拾起时，从你的[gold]牌组[/gold]中移除[blue]{Cards}[/blue]张牌。将一张[red]愚行[/red]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<PrismaticGem>(async (relic, player, target, ctx) =>
            {
                //棱彩宝石
                //在每个回合开始时获得{Energy:energyIcons()}。卡牌奖励现在会包含其他颜色的卡牌。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<PumpkinCandle>(async (relic, player, target, ctx) =>
            {
                //南瓜蜡烛
                //在每个回合开始时获得{Energy:energyIcons()}。这件遗物将在第[blue]3[/blue][gold]阶段[/gold]开始时熄灭。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<PunchDagger>(async (relic, player, target, ctx) =>
            {
                //拳刃
                //拾起时，选择一张攻击牌为它[gold]附魔[/gold]：[purple]动量[/purple][blue]{Momentum}[/blue]。
                await relic.AfterObtained();
            });

            Register<RadiantPearl>(async (relic, player, target, ctx) =>
            {
                //发光珍珠
                //在每场战斗开始时，将[blue]{Cards}[/blue]张[gold]冷光[/gold]加入你的[gold]手牌[/gold]。
                List<CardModel> list = new List<CardModel>();
                for (int i = 0; i < relic.DynamicVars.Cards.IntValue; i++)
                {
                    list.Add(relic.Owner.Creature.CombatState.CreateCard<Luminesce>(relic.Owner));
                }

                await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, addedByPlayer: true);
            });

            Register<RainbowRing>(async (relic, player, target, ctx) =>
            {
                //彩虹戒指
                //每回合，你第一次打出攻击牌、技能牌和能力牌各一张时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]和[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
                await PowerCmd.Apply<DexterityPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.BaseValue, relic.Owner.Creature, null);
            });

            Register<RazorTooth>(async (relic, player, target, ctx) =>
            {
                //剃刀牙
                //你打出攻击牌和技能牌时，将其在本场战斗内[gold]升级[/gold]。
            });

            Register<RedMask>(async (relic, player, target, ctx) =>
            {
                //红面具
                //在每场战斗开始时，给于所有敌人[blue]{WeakPower}[/blue]层[gold]虚弱[/gold]。
                await PowerCmd.Apply<WeakPower>(player.Creature.CombatState.HittableEnemies, relic.DynamicVars["WeakPower"].BaseValue, relic.Owner.Creature, null);
            });

            Register<RedSkull>(async (relic, player, target, ctx) =>
            {
                //红头骨
                //当你的生命值低于或等于[blue]{HpThreshold}%[/blue]时，你额外获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]
                Creature creature = relic.Owner.Creature;
                decimal baseValue = relic.DynamicVars.Strength.BaseValue;
                await PowerCmd.Apply<StrengthPower>(creature, baseValue, creature, null);
            });

            

            Register<Regalite>(async (relic, player, target, ctx) =>
            {
                //君王矿石
                //每当你生成一张牌时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<RegalPillow>(async (relic, player, target, ctx) =>
            {
                //皇家枕头
                //在[gold]休息[/gold]时，额外回复[blue]{Heal}[/blue]点生命。
                await CreatureCmd.Heal(relic.Owner.Creature, relic.DynamicVars.Heal.BaseValue);
            });

            Register<ReptileTrinket>(async (relic, player, target, ctx) =>
            {
                //爬行动物饰品
                //当你使用药水时，在本回合获得[blue]{StrengthPower}[/blue][gold]力量[/gold]。
                await PowerCmd.Apply<ReptileTrinketPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
            });

            Register<RingingTriangle>(async (relic, player, target, ctx) =>
            {
                //三角铃鼓
                //在每场战斗的第一回合[gold]保留[/gold]你的[gold]手牌[/gold]。
                await PowerCmd.Apply<RetainHandPower>(relic.Owner.Creature, 1, relic.Owner.Creature, null);
            });

            Register<RingOfTheDrake>(async (relic, player, target, ctx) =>
            {
                //长蛇戒指
                //在战斗开始时的前[blue]{Turns}[/blue]个回合，你额外抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<RingOfTheSnake>(async (relic, player, target, ctx) =>
            {
                //蛇之戒指
                //在每场战斗开始时，额外抽[blue]{Cards}[/blue]张牌。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<RippleBasin>(async (relic, player, target, ctx) =>
            {
                //波纹水盆
                //如果你在本回合中没有打出过攻击牌，则获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<RoyalPoison>(async (relic, player, target, ctx) =>
            {
                //王室猛毒
                //在每场战斗开始时，失去[blue]{Damage}[/blue]点生命。
                await CreatureCmd.Damage(ctx, relic.Owner.Creature, relic.DynamicVars.Damage, null, null);
            });

            Register<RoyalStamp>(async (relic, player, target, ctx) =>
            {
                //王室印章
                //拾起时，从[gold]牌组[/gold]中选择一张攻击牌或技能牌，为它[gold]附魔[/gold]：[purple]{Enchantment}[/purple]。
                await relic.AfterObtained();
            });

            Register<RuinedHelmet>(async (relic, player, target, ctx) =>
            {
                //损毁头盔
                //你在每场战斗中第一次获得的[gold]力量[/gold]值翻倍。
            });

            Register<RunicCapacitor>(async (relic, player, target, ctx) =>
            {
                //符文电容器
                //每场战斗开始时，获得[blue]{Repeat}[/blue]个额外[gold]充能球栏位[/gold]。
                await OrbCmd.AddSlots(relic.Owner, relic.DynamicVars.Repeat.IntValue);
            });

            Register<RunicPyramid>(async (relic, player, target, ctx) =>
            {
                //符文金字塔
                //你在回合结束时不再自动丢弃所有[gold]手牌[/gold]。
            });

            Register<Sai>(async (relic, player, target, ctx) =>
            {
                //钗
                //在你的回合开始时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<SandCastle>(async (relic, player, target, ctx) =>
            {
                //沙堡
                //拾起时，随机[gold]升级[/gold][blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

            Register<ScreamingFlagon>(async (relic, player, target, ctx) =>
            {
                //尖叫酒壶
                //如果你在回合结束时没有任何[gold]手牌[/gold]，则对所有敌人造成[blue]{Damage}[/blue]点伤害。
                await CreatureCmd.Damage(ctx, relic.Owner.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);
            });

            Register<ScrollBoxes>(async (relic, player, target, ctx) =>
            {
                //卷轴箱
                //拾起时，失去所有[gold]金币[/gold]，并从[blue]2[/blue]个卡牌包中选择[blue]1包[/blue]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<SealOfGold>(async (relic, player, target, ctx) =>
            {
                //黄金印
                //在你的回合开始时，花费[blue]{Gold}[/blue][gold]金币[/gold]来获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
                await PlayerCmd.LoseGold(relic.DynamicVars.Gold.IntValue, relic.Owner);
            });

            Register<SeaGlass>(async (relic, player, target, ctx) =>
            {
                //海玻璃
                //查看[blue]{Cards}[/blue]张来自{Character.StringValue:cond:[gold]{}[/gold]|其他角色}的牌。从中选择任意数量的卡牌加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<SelfFormingClay>(async (relic, player, target, ctx) =>
            {
                //自成型黏土
                //每当你在战斗中失去生命，就在下回合获得[blue]{BlockNextTurn}[/blue]点[gold]格挡[/gold]。
                await PowerCmd.Apply<SelfFormingClayPower>(relic.Owner.Creature, relic.DynamicVars["BlockNextTurn"].BaseValue, relic.Owner.Creature, null);
            });

            Register<SereTalon>(async (relic, player, target, ctx) =>
            {
                //原初之爪
                //拾起时，将[blue]{Curses}[/blue]张随机[red]诅咒[/red]牌和[blue]{Wishes}[/blue]张[purple]许愿[/purple]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<Shovel>(async (relic, player, target, ctx) =>
            {
                //铲子
                //现在你可以在[gold]休息处[/gold]挖掘[gold]遗物[/gold]。
            });

            Register<Shuriken>(async (relic, player, target, ctx) =>
            {
                //手里剑
                //你每在同一回合内打出[blue]{Cards}[/blue]张攻击牌，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
            });

            Register<SignetRing>(async (relic, player, target, ctx) =>
            {
                //图章戒指
                //拾起时，获得[blue]{Gold}[/blue][gold]金币[/gold]。
                await PlayerCmd.GainGold(relic.DynamicVars.Gold.BaseValue, relic.Owner);
            });

            Register<SilverCrucible>(async (relic, player, target, ctx) =>
            {
                //白银熔炉
                //你遇到的前[blue]{Cards}[/blue]次卡牌奖励将是被[gold]升级[/gold]过的。你打开的第一个宝箱将是[red]空的[/red]
            });

            Register<SlingOfCourage>(async (relic, player, target, ctx) =>
            {
                //勇气投石索
                //在与[gold]精英[/gold]敌人战斗时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
            });

            Register<SmallCapsule>(async (relic, player, target, ctx) =>
            {
                //小型扭蛋
                //拾起时，获得一件随机[gold]遗物[/gold]。
                await relic.AfterObtained();
            });

            Register<SneckoEye>(async (relic, player, target, ctx) =>
            {
                //异蛇之眼
                //每回合多抽[blue]{Cards}[/blue]张牌。每场战斗开始时获得[red]混乱[/red]效果。
                await CardPileCmd.Draw(ctx, relic.DynamicVars.Cards.BaseValue, relic.Owner);
            });

            Register<SneckoSkull>(async (relic, player, target, ctx) =>
            {
                //异蛇头骨
                //每当你给予敌人[gold]中毒[/gold]时，所给予的[gold]中毒[/gold]层数增加[blue]{PoisonPower}[/blue]层。
                await PowerCmd.Apply<PoisonPower>(target, relic.DynamicVars.Poison.IntValue, relic.Owner.Creature, null);
            });

            Register<Sozu>(async (relic, player, target, ctx) =>
            {
                //添水
                //你无法再获得药水。在每回合开始时获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<SparklingRouge>(async (relic, player, target, ctx) =>
            {
                //闪亮口红
                //在你的第[blue]3[/blue]回合开始时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]和[blue]{DexterityPower}[/blue]点[gold]敏捷[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
                await PowerCmd.Apply<DexterityPower>(relic.Owner.Creature, relic.DynamicVars.Dexterity.BaseValue, relic.Owner.Creature, null);
            });

            Register<SpikedGauntlets>(async (relic, player, target, ctx) =>
            {
                //带刺手甲
                //在每回合开始时获得{Energy:energyIcons()}。能力牌的耗能增加[blue]1[/blue]{energyPrefix:energyIcons(1)}。
                

            });

            Register<StoneCalendar>(async (relic, player, target, ctx) =>
            {
                //历石
                //在第[blue]{DamageTurn}[/blue]回合结束时，对所有敌人造成[blue]{Damage}[/blue]点伤害。
                int intValue = relic.DynamicVars["DamageTurn"].IntValue;
                await CreatureCmd.Damage(ctx, relic.Owner.Creature.CombatState.HittableEnemies, relic.DynamicVars.Damage, relic.Owner.Creature);
                    
                
            });

            Register<StoneCracker>(async (relic, player, target, ctx) =>
            {
                //碎石钻
                //在每场战斗开始时，在本场战斗中随机[gold]升级[/gold]你[gold]抽牌堆[/gold]中的[blue]{Cards}[/blue]张牌。
                List<CardModel> cards = PileType.Draw.GetPile(relic.Owner).Cards.Where((CardModel c) => c.IsUpgradable).ToList().StableShuffle(relic.Owner.RunState.Rng.CombatCardSelection)
                    .Take(relic.DynamicVars.Cards.IntValue)
                    .ToList();
                CardCmd.Upgrade(cards, CardPreviewStyle.HorizontalLayout);
                CardCmd.Preview(cards);
                await Cmd.CustomScaledWait(0.5f, 1f);
            });

            Register<StoneHumidifier>(async (relic, player, target, ctx) =>
            {
                //石炉加湿器
                //每当你在[gold]休息处[/gold][gold]休息[/gold]时，将你的最大生命值提升[blue]{MaxHp}[/blue]点。
                CreatureCmd.GainMaxHp(relic.Owner.Creature, relic.DynamicVars.MaxHp.BaseValue);
            });

            Register<Storybook>(async (relic, player, target, ctx) =>
            {
                //故事书
                //拾起时，将[blue]1[/blue]张[gold]至亮之焰[/gold]添加到你的[gold]牌组[/gold]中。
                await relic.AfterObtained();
            });

            Register<Strawberry>(async (relic, player, target, ctx) =>
            {
                //草莓
                //拾起时，将你的最大生命值提升[blue]{MaxHp}[/blue]。
                await relic.AfterObtained();
            });

            Register<StrikeDummy>(async (relic, player, target, ctx) =>
            {
                //打击木偶
                //名字中有“打击”的卡牌造成[blue]{ExtraDamage}[/blue]点额外伤害。
                await PowerCmd.Apply<VigorPower>(relic.Owner.Creature, relic.DynamicVars["ExtraDamage"].BaseValue, relic.Owner.Creature, null);
            });

            Register<SturdyClamp>(async (relic, player, target, ctx) =>
            {
                //坚固钳子
                //可以跨回合保留最多[blue]{Block}[/blue]点[gold]格挡[/gold]。
            });

            Register<SwordOfJade>(async (relic, player, target, ctx) =>
            {
                //玉之剑
                //在每场战斗开始时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, null, null);
            });

            Register<SwordOfStone>(async (relic, player, target, ctx) =>
            {
                //石之剑
                //在击败[blue]{Elites}[/blue]名[gold]精英[/gold]敌人之后将变化为一件强力[gold]遗物[/gold]。
                await RelicCmd.Replace(relic, ModelDb.Relic<SwordOfJade>().ToMutable());
            });

            Register<SymbioticVirus>(async (relic, player, target, ctx) =>
            {
                //共生病毒
                //在每场战斗开始时，[gold]生成[/gold][blue]{Dark}[/blue]个[gold]黑暗[/gold]充能球。
                for (int i = 0; (decimal)i < relic.DynamicVars["Dark"].BaseValue; i++)
                {
                    await OrbCmd.Channel<DarkOrb>(new BlockingPlayerChoiceContext(), relic.Owner);
                }
            });

            Register<TanxsWhistle>(async (relic, player, target, ctx) =>
            {
                //坦克斯的哨子
                //拾起时，将[blue]1[/blue]张[gold]吹哨[/gold]加入你的[gold]牌组[/gold]。
                await relic.AfterObtained();
            });

            Register<TeaOfDiscourtesy>(async (relic, player, target, ctx) =>
            {
                //无礼之茶
                //在下一场战斗开始时，将[blue]{DazedCount}[/blue]张[gold]晕眩[/gold]放入你的[gold]抽牌堆[/gold]。
                await CardPileCmd.AddToCombatAndPreview<Dazed>(relic.Owner.Creature, PileType.Draw, relic.DynamicVars["DazedCount"].IntValue, addedByPlayer: true, CardPilePosition.Random);
            });

            Register<TheAbacus>(async (relic, player, target, ctx) =>
            {
                //算盘
                //你每次将[gold]抽牌堆[/gold]洗牌时，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<TheBoot>(async (relic, player, target, ctx) =>
            {
                //发条靴
                //每当你造成小于等于[blue]{DamageThreshold}[/blue]点未被格挡的攻击伤害时，将伤害提升为[blue]{DamageMinimum}[/blue]。
            });

            Register<TheCourier>(async (relic, player, target, ctx) =>
            {
                //送货员
                //商人的卡牌、遗物和药水不再会卖光，并且所有商品打折[blue]{Discount}%[/blue]。
            });

            Register<ThrowingAxe>(async (relic, player, target, ctx) =>
            {
                //投斧
                //你在每场战斗中打出的第一张牌会多打出一次。

            });

            Register<Tingsha>(async (relic, player, target, ctx) =>
            {
                //铜钹
                //你每在你的回合丢弃一张牌，就对一名随机敌人造成[blue]{Damage}[/blue]点伤害。
                Creature creature = relic.Owner.RunState.Rng.CombatTargets.NextItem(relic.Owner.Creature.CombatState.HittableEnemies);
                VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_attack_blunt");
                await CreatureCmd.Damage(ctx, creature, relic.DynamicVars.Damage, relic.Owner.Creature);
                
            });

            Register<TinyMailbox>(async (relic, player, target, ctx) =>
            {
                //小邮箱
                //每当你[gold]休息[/gold]时，获得[blue]2[/blue]瓶随机药水。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new PotionReward(relic.Owner));
                list.Add(new PotionReward(relic.Owner));
                await RewardsCmd.OfferCustom(relic.Owner, list);

            });

            Register<ToastyMittens>(async (relic, player, target, ctx) =>
            {
                //烘焙手套
                //在你的回合开始时，[gold]消耗[/gold]你[gold]抽牌堆[/gold]顶部的牌并获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await CardPileCmd.ShuffleIfNecessary(ctx, relic.Owner);
                IReadOnlyList<CardModel> cards = PileType.Draw.GetPile(player).Cards;
                CardModel cardModel = null;

                if (cardModel == null)
                {
                    cardModel = cards.FirstOrDefault();
                }
                if (cardModel != null)
                {
                    await CardCmd.Exhaust(ctx, cardModel);
                }

                await PowerCmd.Apply<StrengthPower>(player.Creature, relic.DynamicVars.Strength.BaseValue, player.Creature, null);
            });

            Register<Toolbox>(async (relic, player, target, ctx) =>
            {
                //工具箱
                //在每场战斗开始时，从[blue]{Cards}[/blue]张随机无色牌中选择[blue]1[/blue]张加入你的[gold]手牌[/gold]。
                List<CardModel> cards = CardFactory.GetDistinctForCombat(relic.Owner, ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint), relic.DynamicVars.Cards.IntValue, relic.Owner.RunState.Rng.CombatCardGeneration).ToList();
                CardModel cardModel = await CardSelectCmd.FromChooseACardScreen(ctx, cards, relic.Owner);
                if (cardModel != null)
                {
                    await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, addedByPlayer: true);
                }
            });

            Register<TouchOfOrobas>(async (relic, player, target, ctx) =>
            {
                //欧洛巴斯之触
                //拾起时，将{StarterRelic.StringValue:cond:[gold]{StarterRelic}[/gold]替换为[gold]{UpgradedRelic}[/gold]|你的初始[gold]遗物[/gold]替换为[gold]先古[/gold]版本}。
                
            });

            Register<ToughBandages>(async (relic, player, target, ctx) =>
            {
                //结实绷带
                //你每在你的回合丢弃一张牌，就获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<ToxicEgg>(async (relic, player, target, ctx) =>
            {
                //毒素之蛋
                //每当你获得[gold]技能牌[/gold]时，将其[gold]升级[/gold]。
            });

            Register<ToyBox>(async (relic, player, target, ctx) =>
            {
                //玩具盒
                //拾起时，获得[blue]{Relics}[/blue]件[gold]蜡制遗物[/gold]。每经过[blue]{Combats}[/blue]场战斗，你最左侧的[gold]蜡制遗物[/gold]将会融化。
                List<Reward> list = new List<Reward>();
                for (int i = 0; i < relic.DynamicVars["Relics"].IntValue; i++)
                {
                    RelicModel relicModel = RelicFactory.PullNextRelicFromFront(relic.Owner).ToMutable();
                    relicModel.IsWax = true;
                    list.Add(new RelicReward(relicModel, relic.Owner));
                }

                await RewardsCmd.OfferCustom(relic.Owner, list);

                RelicModel R = relic.Owner.Relics.FirstOrDefault((RelicModel r) => r != null && r.IsWax && !r.IsMelted);
                if (R != null)
                {
                    await RelicCmd.Melt(R);
                    await Cmd.CustomScaledWait(0.5f, 0.75f);
                }
            });

            Register<TriBoomerang>(async (relic, player, target, ctx) =>
            {
                //三刃回旋镖
                //从你的[gold]牌组[/gold]中选择[blue]{Cards}[/blue]张攻击牌。为这些牌[gold]附魔[/gold]：[purple]本能[/purple]。
                await relic.AfterObtained();
            });

            Register<TungstenRod>(async (relic, player, target, ctx) =>
            {
                //钨合金棍
                //你每次失去生命时，减少失去的生命值[blue]{HpLossReduction}[/blue]点。

            });

            Register<TuningFork>(async (relic, player, target, ctx) =>
            {
                //音叉
                //你每打出[blue]{Cards}[/blue]张技能牌，获得[blue]{Block}[/blue]点[gold]格挡[/gold]。
                await CreatureCmd.GainBlock(relic.Owner.Creature, relic.DynamicVars.Block, null);
            });

            Register<TwistedFunnel>(async (relic, player, target, ctx) =>
            {
                //扭曲漏斗
                //在每场战斗开始时，给予所有敌人[blue]{PoisonPower}[/blue]层[gold]中毒[/gold]。
                foreach (Creature hittableEnemy in relic.Owner.Creature.CombatState.HittableEnemies)
                {
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NSmokePuffVfx.Create(hittableEnemy, NSmokePuffVfx.SmokePuffColor.Green));
                }

                await Cmd.CustomScaledWait(0.2f, 0.4f);
                foreach (Creature hittableEnemy2 in relic.Owner.Creature.CombatState.HittableEnemies)
                {
                    await PowerCmd.Apply<PoisonPower>(hittableEnemy2, relic.DynamicVars["PoisonPower"].IntValue, relic.Owner.Creature, null);
                }
            });

            Register<UnceasingTop>(async (relic, player, target, ctx) =>
            {
                //不休陀螺
                //在你的回合，当你没有[gold]手牌[/gold]时，抽一张牌。
                await CardPileCmd.Draw(ctx, 1, relic.Owner);
            });

            Register<UndyingSigil>(async (relic, player, target, ctx) =>
            {
                //不死符文
                //当敌人的[gold]灾厄[/gold]层数大于等于其生命值时，它造成的伤害降低[blue]50%[/blue]。

            });

            Register<UnsettlingLamp>(async (relic, player, target, ctx) =>
            {
                //不安油灯
                //你在每场战斗中第一次打出能给予敌人[gold]负面状态[/gold]的牌时，将其效果翻倍。

            });

            Register<Vajra>(async (relic, player, target, ctx) =>
            {
                //金刚杵
                //在每场战斗开始时，获得[blue]{StrengthPower}[/blue]点[gold]力量[/gold]。
                await PowerCmd.Apply<StrengthPower>(relic.Owner.Creature, relic.DynamicVars.Strength.BaseValue, relic.Owner.Creature, null);
            });

            Register<Vambrace>(async (relic, player, target, ctx) =>
            {
                //臂甲
                //每场战斗中，你第一次从卡牌中获得的[gold]格挡[/gold]值翻倍。

            });

            Register<VelvetChoker>(async (relic, player, target, ctx) =>
            {
                //天鹅绒颈圈
                //在每回合开始时获得{Energy:energyIcons()}。你每回合不能打出超过[blue]{Cards}[/blue]张牌。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<VenerableTeaSet>(async (relic, player, target, ctx) =>
            {
                //古茶具套装
                //到达[gold]休息处[/gold]后的下一场战斗开始时额外获得{Energy:energyIcons()}。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<VeryHotCocoa>(async (relic, player, target, ctx) =>
            {
                //烫嘴可可
                //在每场战斗的第一回合额外获得[blue]{Energy:energyIcons()}[/blue]。
                await PlayerCmd.GainEnergy(relic.DynamicVars.Energy.BaseValue, relic.Owner);
            });

            Register<VexingPuzzlebox>(async (relic, player, target, ctx) =>
            {
                //烦人机关盒
                //在每场战斗开始时，将一张随机卡牌加入你的[gold]手牌[/gold]。这张牌在本回合可以免费打出。
                CardModel cardModel = CardFactory.GetDistinctForCombat(relic.Owner, relic.Owner.Character.CardPool.GetUnlockedCards(relic.Owner.UnlockState, relic.Owner.RunState.CardMultiplayerConstraint), 1, relic.Owner.RunState.Rng.CombatCardGeneration).First();
                cardModel.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, addedByPlayer: true);
            });

            

            Register<VitruvianMinion>(async (relic, player, target, ctx) =>
            {
                //维特鲁威仆从
                //名字中有“仆从”的卡牌造成双倍的伤害与[gold]格挡[/gold]。
            });

            Register<WarHammer>(async (relic, player, target, ctx) =>
            {
                //战锤
                //每当你击败一名[gold]精英[/gold]敌人的时候，随机[gold]升级[/gold][blue]{Cards}[/blue]张牌。
                IEnumerable<CardModel> enumerable = PileType.Deck.GetPile(relic.Owner).Cards.Where((CardModel c) => c.IsUpgradable).ToList().StableShuffle(relic.Owner.RunState.Rng.Niche)
            .Take(relic.DynamicVars.Cards.IntValue);
                foreach (CardModel item in enumerable)
                {
                    CardCmd.Upgrade(item);
                }
            });

            Register<WarPaint>(async (relic, player, target, ctx) =>
            {
                //战纹涂料
                //拾起时，随机[gold]升级[/gold][blue]{Cards}[/blue]张技能牌。
                await relic.AfterObtained();
            });

            Register<Whetstone>(async (relic, player, target, ctx) =>
            {
                //磨刀石
                //拾起时，随机[gold]升级[/gold][blue]{Cards}[/blue]张攻击牌。
                await relic.AfterObtained();
            });

            Register<WhisperingEarring>(async (relic, player, target, ctx) =>
            {
                //低语耳环
                //在每个回合开始时，获得{Energy:energyIcons()}。[red]瓦库将接管你的第一回合。[/red]

            });

            Register<WhiteBeastStatue>(async (relic, player, target, ctx) =>
            {
                //白兽雕像
                //战斗结束后必定掉落药水。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new PotionReward(relic.Owner));
                await RewardsCmd.OfferCustom(relic.Owner, list);
            });

            Register<WhiteStar>(async (relic, player, target, ctx) =>
            {
                //白星
                //[gold]精英[/gold]敌人额外掉落一次[gold]稀有[/gold]卡牌奖励。
                List<Reward> list = new List<Reward>();
                var cardPools = new List<CardPoolModel> { relic.Owner.Character.CardPool };
                CardCreationOptions options = new CardCreationOptions(cardPools, CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
                list.Add(new CardReward(CardCreationOptions.ForRoom(relic.Owner, RoomType.Boss), 3, player));
                await RewardsCmd.OfferCustom(relic.Owner, list);

            });

            Register<WingedBoots>(async (relic, player, target, ctx) =>
            {
                //羽翼之靴
                //你在选择下一层的房间时有[blue]{Rooms}[/blue]次机会可以无视当前的路线。
            });

            //Register<WingCharm>(async (relic, player, target, ctx) =>
            //{
            //    //羽翼护符
            //    //每次卡牌奖励中，都会有随机一张牌被[gold]附魔[/gold]：[purple]迅捷[/purple][blue]{SwiftAmount}[/blue]。
            //});

            Register<WongosMysteryTicket>(async (relic, player, target, ctx) =>
            {
                //旺购神秘券
                //在[blue]{RemainingCombats}[/blue]场战斗后，获得随机[blue]{Repeat}[/blue]件[gold]遗物[/gold]。
                for (int i = 0; i < relic.DynamicVars.Repeat.IntValue; i++)
                {
                    RelicModel r = RelicFactory.PullNextRelicFromFront(relic.Owner).ToMutable();
                    await RelicCmd.Obtain(r, relic.Owner);
                }

            });

            Register<WongoCustomerAppreciationBadge>(async (relic, player, target, ctx) =>
            {
                //旺购客户感恩徽章
                //没有任何作用。
            });

            Register<YummyCookie>(async (relic, player, target, ctx) =>
            {
                //美味饼干
                //拾起时，[gold]升级[/gold][blue]{Cards}[/blue]张牌。
                await relic.AfterObtained();
            });

        }


    }
}