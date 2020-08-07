using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cauldron
{
	/// <summary>
	/// Basic parser that takes Game json updates and emits GameEvent json objects
	/// PLEASE NOTE this is probably wrong; I'm currently emitting one GameEvent per game update but
	/// ultimately we want a Retrosheet-style condensed description of the whole "play" as a single GameEvent
	/// </summary>
	class GameEventParser
	{
		// Last state we saw, for comparison
		Game m_oldState;

		// State tracking for stats not tracked inherently in the state updates
		int m_eventIndex = 0;
		int m_batterCount = 0;

		GameEvent m_currEvent;

		public void StartNewGame(Game initState)
		{
			m_oldState = initState;
			m_eventIndex = 0;
			m_batterCount = 0;
			m_currEvent = CreateNewGameEvent(initState);
			m_currEvent.eventText.Add(initState.lastUpdate);
		}

		private string GetBatterId(Game state)
		{
			// Batters can sometimes be empty
			string batter = state.topOfInning ? state.awayBatter : state.homeBatter;
			return batter == string.Empty ? null : batter;
		}

		private string GetPitcherId(Game state)
		{
			return state.topOfInning ? state.awayPitcher : state.homePitcher;
		}

		private string GetBatterTeamId(Game state)
		{
			return state.topOfInning ? state.awayTeam : state.homeTeam;
		}

		private string GetPitcherTeamId(Game state)
		{
			return state.topOfInning ? state.homeTeam : state.awayTeam;
		}

		private GameEvent CreateNewGameEvent(Game newState)
		{
			GameEvent currEvent = new GameEvent();

			currEvent.gameId = newState._id;
			currEvent.eventIndex = m_eventIndex;
			currEvent.batterCount = m_batterCount;
			currEvent.inning = newState.inning;
			currEvent.outsBeforePlay = m_oldState.halfInningOuts;

			currEvent.homeStrikeCount = newState.homeStrikes;
			currEvent.awayStrikeCount = newState.awayStrikes;

			currEvent.homeScore = newState.homeScore;
			currEvent.awayScore = newState.awayScore;

			// Currently not supported by the cultural event of Blaseball
			currEvent.isPinchHit = false;
			currEvent.isWildPitch = false;
			currEvent.isBunt = false;
			currEvent.errorsOnPlay = 0;
			currEvent.isSacrificeFly = false; // I think we can't tell this

			currEvent.batterId = GetBatterId(newState);
			currEvent.batterTeamId = GetBatterTeamId(newState);
			currEvent.pitcherId = GetPitcherId(newState);
			currEvent.pitcherTeamId = GetPitcherTeamId(newState);

			currEvent.eventText = new List<string>();

			return currEvent;
		}

		public GameEvent ParseGameUpdate(Game newState)
		{
			if(newState.Equals(m_oldState))
			{
				Console.WriteLine($"Discarded update from game {newState._id} as a duplicate.");
				return null;
			}

			if(m_currEvent == null)
			{
				m_currEvent = CreateNewGameEvent(newState);
			}

			// If we haven't found the batter for this event yet, try again
			if (m_currEvent.batterId == null)
			{
				m_currEvent.batterId = GetBatterId(newState);
			}

			if (newState.atBatStrikes > m_currEvent.totalStrikes)
			{
				m_currEvent.totalStrikes = newState.atBatStrikes;
			}
			if (newState.atBatBalls > m_currEvent.totalBalls)
			{
				m_currEvent.totalBalls = newState.atBatBalls;
			}

			if (newState.lastUpdate.Contains("Foul Ball"))
			{
				m_currEvent.totalFouls++;
			}

			// If we had two outs but suddenly the inning changed, that means the 3rd out happened silently
			if (newState.topOfInning != m_oldState.topOfInning && m_oldState.halfInningOuts == 2)
			{
				m_currEvent.outsOnPlay = 1;
			}
			else
			{
				m_currEvent.outsOnPlay = Math.Max(0, newState.halfInningOuts - m_oldState.halfInningOuts);
			}


			// TODO: we need a better method to track RBIs
			// Stealing home can happen, for one thing
			m_currEvent.runsBattedIn = newState.topOfInning ? newState.awayScore - m_oldState.awayScore : newState.homeScore - m_oldState.homeScore;

			// Extremely basic single/double/triple/HR detection
			if (newState.lastUpdate.Contains("hits a Single"))
			{
				m_currEvent.basesHit = 1;
				m_currEvent.batterBaseAfterPlay = 1;
			}
			else if (newState.lastUpdate.Contains("hits a Double"))
			{
				m_currEvent.basesHit = 2;
				m_currEvent.batterBaseAfterPlay = 2;
			}
			else if (newState.lastUpdate.Contains("hits a Triple"))
			{
				m_currEvent.basesHit = 3;
				m_currEvent.batterBaseAfterPlay = 3;
			}
			else if (newState.lastUpdate.Contains("home run") || newState.lastUpdate.Contains("grand slam"))
			{
				m_currEvent.basesHit = 4;
				m_currEvent.batterBaseAfterPlay = 4;
			}

			// Sacrifice outs
			if (newState.lastUpdate.Contains("sacrifice"))
			{
				m_currEvent.isSacrificeHit = true;
			}

			// Double plays
			if (newState.lastUpdate.Contains("double play"))
			{
				m_currEvent.isDoublePlay = true;
			}

			// Triple plays
			if (newState.lastUpdate.Contains("triple play"))
			{
				m_currEvent.isTriplePlay = true;
			}

			// TODO steals
			m_currEvent.isLastEventForPlateAppearance = true;

			// TODO currEvent.pitchesList
			// TODO currEvent.isLeadoff
			// TODO currEvent.lineupPosition
			// TODO currEvent.battedBallType
			// TODO currEvent.baseRunners

			// Unsure if this is enough
			m_currEvent.isLastGameEvent = newState.gameComplete;

			// Store original update text for reference
			m_currEvent.eventText.Add(newState.lastUpdate);

			// Cycle state
			m_oldState = newState;

			// If we had outs or hits, emit
			// TODO: walks, steals
			if(m_currEvent.outsOnPlay > 0 || m_currEvent.basesHit > 0)
			{
				GameEvent emitted = m_currEvent;
				m_currEvent = null;
				m_eventIndex++;
				return emitted;
			}
			else
			{
				return null;
			}
		}


	}
}