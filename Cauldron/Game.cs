using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cauldron
{
	/// <summary>
	/// Serializable class that represents a game update in the gameDataUpdate structure
	/// </summary>
	public class Game : IEquatable<Game>
	{
		public List<int> basesOccupied { get; set; }
		public List<string> baseRunners { get; set; }
		public string _id { get; set; }
		public string awayTeamName { get; set; }
		public string awayTeamNickname { get; set; }
		public string awayTeam { get; set; }
		public string homeTeamName { get; set; }
		public string homeTeamNickname { get; set; }
		public string homeTeam { get; set; }
		public int awayScore { get; set; }
		public int homeScore { get; set; }
		public string lastUpdate { get; set; }
		public bool gameComplete { get; set; }
		public int inning { get; set; }
		public bool topOfInning { get; set; }
		public int halfInningOuts { get; set; }
		public int homeStrikes { get; set; }
		public int awayStrikes { get; set; }
		public int atBatBalls { get; set; }
		public int atBatStrikes { get; set; }
		public string homePitcher { get; set; }
		public string homePitcherName { get; set; }
		public string awayPitcher { get; set; }
		public string awayPitcherName { get; set; }
		public string homeBatter { get; set; }
		public string homeBatterName { get; set; }
		public string awayBatter { get; set; }
		public string awayBatterName { get; set; }
		public int season { get; set; } = 0;
		public int day { get; set; } = 0;

		
		public bool Equals([AllowNull] Game other)
		{
			// don't compare the lists
			return ((season == other.season) &&
				(day == other.day) &&
				(awayBatterName == other.awayBatterName) &&
				(awayBatter == other.awayBatter) &&
				(homeBatterName == other.homeBatterName) &&
				(homeBatter == other.homeBatter) &&
				(awayPitcherName == other.awayPitcherName) &&
				(awayPitcher == other.awayPitcher) &&
				(homePitcherName == other.homePitcherName) &&
				(homePitcher == other.homePitcher) &&
				(atBatStrikes == other.atBatStrikes) &&
				(atBatBalls == other.atBatBalls) &&
				(awayStrikes == other.awayStrikes) &&
				(homeStrikes == other.homeStrikes) &&
				(halfInningOuts == other.halfInningOuts) &&
				(topOfInning == other.topOfInning) &&
				(inning == other.inning) &&
				(gameComplete == other.gameComplete) &&
				(lastUpdate == other.lastUpdate) &&
				(homeScore == other.homeScore) &&
				(awayScore == other.awayScore) &&
				(homeTeam == other.homeTeam) &&
				(homeTeamNickname == other.homeTeamNickname) &&
				(homeTeamName == other.homeTeamName) &&
				(awayTeam == other.awayTeam) &&
				(awayTeamNickname == other.awayTeamNickname) &&
				(awayTeamName == other.awayTeamName) &&
				(_id == other._id));
		}
	}

}
