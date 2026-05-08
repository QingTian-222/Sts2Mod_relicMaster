using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace relicMaster
{

	public class TriggerTimesVar : DynamicVar
	{

		public const string Key = "RELICMASTER-TriggerTimes";

		public static readonly string LocKey = Key.ToUpperInvariant();

		public TriggerTimesVar(decimal baseValue) : base(Key, baseValue)
		{

		}
	}
}
