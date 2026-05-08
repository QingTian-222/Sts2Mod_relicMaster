using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace relicMaster.Cards
{
	[Pool(typeof(ColorlessCardPool))]
	public class RelicBarrage : CustomCardModel
	{
		private const int energyCost = 3;
		private const CardType type = CardType.Attack;
		private const CardRarity rarity = CardRarity.Rare;
		private const TargetType targetType = TargetType.AnyEnemy;
		private const bool shouldShowInCardLibrary = true;

		protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
		{
			new IntVar("RELICMASTER-TriggerTimes", 1m),

            new DamageVar(5, ValueProp.Move)

        };


		public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
		{
			CardKeyword.Exhaust,
			MyKeywords.Activate
		};
		public override string PortraitPath => $"res://relicMaster/images/cards/{nameof(RelicBarrage)}.png";



		public RelicBarrage() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
		{

		}

		public override int MaxUpgradeLevel => 300;

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int triggerTimes = (int)DynamicVars[TriggerTimesVar.Key].BaseValue;
            int relicCount = Owner.Relics.Count;
            var enemies = Owner.Creature.CombatState.Enemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            for (int i = 0; i < triggerTimes; i++)
            {

                if (relicCount > 0)
                {
                    var randomRelic = Owner.Relics.ElementAt(Owner.RunState.Rng.CombatCardGeneration.NextInt(relicCount));
                    randomRelic.Flash();

                    await RelicActivator.Activate(randomRelic, Owner, choiceContext, cardPlay.Target);
                }


                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .Execute(choiceContext);

                await Task.Delay(50);
            }
        }
        //protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        //{
        //    var relicTypes = Assembly.GetAssembly(typeof(RelicModel))
        //                .GetTypes()
        //                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RelicModel)))
        //                .ToList();

        //    GD.Print($"找到 {relicTypes.Count} 种遗物类型，开始激活...");

        //    foreach (var relicType in relicTypes)
        //    {
        //        try
        //        {
        //            var relicId = ModelDb.GetId(relicType);
        //            var canonicalRelic = ModelDb.GetById<RelicModel>(relicId);
        //            if (canonicalRelic == null)
        //            {
        //                GD.PrintErr($"无法获取遗物原型：{relicType.Name}");
        //                continue;
        //            }


        //            var mutableRelic = canonicalRelic.ToMutable();
        //            mutableRelic.Owner = Owner;


        //            await RelicActivator.Activate(mutableRelic, Owner, choiceContext, cardPlay.Target);
        //            GD.Print($"成功激活：{relicType.Name}");

        //        }
        //        catch (Exception ex)
        //        {
        //            GD.PrintErr($"激活遗物 {relicType.Name} 时出错：{ex.Message}");
        //        }
        //    }
        //}



        protected override void OnUpgrade()
		{
			DynamicVars["RELICMASTER-TriggerTimes"].UpgradeValueBy(1);
		}
	}
}
