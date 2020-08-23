using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cauldron
{

	public static class BattedBallType
	{
		public static string UNKNOWN = "UNKNOWN";
		public static string GROUNDER = "GROUNDER";
		public static string BUNT = "BUNT";
		public static string FLY = "FLY";
	}

	public static class GameEventType
	{
		public static string UNKNOWN = "UNKNOWN";
		public static string NONE = "NONE";
		public static string OUT = "OUT";
		public static string STRIKEOUT = "STRIKEOUT";
		public static string STOLEN_BASE = "STOLEN_BASE";
		public static string CAUGHT_STEALING = "CAUGHT_STEALING";
		public static string PICKOFF = "PICKOFF";
		public static string WILD_PITCH = "WILD_PITCH";
		public static string BALK = "BALK";
		public static string OTHER_ADVANCE = "OTHER_ADVANCE";
		public static string WALK = "WALK";
		public static string INTENTIONAL_WALK = "INTENTIONAL_WALK";
		public static string HIT_BY_PITCH = "HIT_BY_PITCH";
		public static string FIELDERS_CHOICE = "FIELDERS_CHOICE";
		public static string SINGLE = "SINGLE";
		public static string DOUBLE = "DOUBLE";
		public static string TRIPLE = "TRIPLE";
		public static string HOME_RUN = "HOME_RUN";
	}

	public static class PlayerEventType
	{
		public static string INCINERATION = "INCINERATION";
		public static string PEANUT_GOOD = "PEANUT_GOOD";
		public static string PEANUT_BAD = "PEANUT_BAD";
	}

	/// <summary>
	/// Player events
	/// </summary>
	public class PlayerEvent
	{
		public string playerId { get; set; }
		public string eventType { get; set; }
	}

	/// <summary>
	/// Serializable class following DB schema from SIBR for baserunning info
	/// </summary>
	public class GameEventBaseRunner
	{
		public string runnerId { get; set; }
		public string responsiblePitcherId { get; set; }
		public int baseBeforePlay { get; set; }
		public int baseAfterPlay { get; set; }
		public bool wasBaseStolen { get; set; }
		public bool wasCaughtStealing { get; set; }
		public bool wasPickedOff { get; set; }
	}

	/// <summary>
	/// Serializable class following the DB schema from SIBR for game events
	/// </summary>
	public class GameEvent
	{
		public string gameId { get; set; }
		public string eventType { get; set; }
		public int eventIndex { get; set; }
		public int inning { get; set; }
		public int outsBeforePlay { get; set; }
		public string batterId { get; set; }
		public string batterTeamId { get; set; }
		public string pitcherId { get; set; }
		public string pitcherTeamId { get; set; }
		public float homeScore { get; set; }
		public float awayScore { get; set; }
		public int homeStrikeCount { get; set; }
		public int awayStrikeCount { get; set; }
		public int batterCount { get; set; }
		public List<char> pitchesList { get; set; }
		public int totalStrikes { get; set; }
		public int totalBalls { get; set; }
		public int totalFouls { get; set; }
		public bool isLeadoff { get; set; }
		public bool isPinchHit { get; set; }
		public int lineupPosition { get; set; }
		public bool isLastEventForPlateAppearance { get; set; }
		public int basesHit { get; set; }
		public int runsBattedIn { get; set; }
		public bool isSacrificeHit { get; set; }
		public bool isSacrificeFly { get; set; }
		public int outsOnPlay { get; set; }
		public bool isDoublePlay { get; set; }
		public bool isTriplePlay { get; set; }
		public bool isWildPitch { get; set; }
		public string battedBallType { get; set; }
		public bool isBunt { get; set; }
		public int errorsOnPlay { get; set; }
		public int batterBaseAfterPlay { get; set; }
		public List<GameEventBaseRunner> baseRunners { get; set; }
		public bool isLastGameEvent { get; set; }
		public string additionalContext { get; set; }
		public bool topOfInning { get; set; }
		public List<string> eventText { get; set; }
		public bool isSteal { get; set; }
		public bool isWalk{ get; set; }
		public List<PlayerEvent> playerEvents { get; set; }
		[JsonConverter(typeof(TimestampConverter))]
		public DateTime firstPerceivedAt { get; set; }
		[JsonConverter(typeof(TimestampConverter))]
		public DateTime lastPerceivedAt { get; set; }

		public int season { get; set; }
		public override string ToString()
		{
			return $"[{eventIndex}] OB: {outsBeforePlay}\tO: {outsOnPlay}\tCount {totalBalls}-{totalStrikes}\tFouls: {totalFouls}\tBases: {basesHit}\tRBIs: {runsBattedIn}\t\"{additionalContext}\": {pitcherId} pitching to {batterId}";
		}

		public bool parsingError { get; set; }
		public List<string> parsingErrorList { get; set; }
		public bool fixedError { get; set; }
		public List<string> fixedErrorList { get; set; }
	}
}
