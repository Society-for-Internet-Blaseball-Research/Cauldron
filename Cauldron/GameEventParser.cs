using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		// Map of pitcher IDs indexed by batter ID; used in attributing baserunners to pitchers
		Dictionary<string, string> m_responsiblePitchers;
		// Keep of track of whether we've had a valid batter for this inning
		HashSet<string> m_startedInnings;

		// Properties for metrics
		public int Discards => m_discards;
		int m_discards = 0;
		public int Processed => m_processed;
		int m_processed = 0;
		public int Errors => m_errors;
		int m_errors = 0;
        public int Fixes => m_fixes;
        int m_fixes = 0;
		public string GameId => m_gameId;
		string m_gameId;

		// Event currently being appended to
		GameEvent m_currEvent;

		// Map of player IDs indexed by name; used in looking up players who were incinerated or ate a peanut
		Dictionary<string, string> m_playerNameToId;
		
		public void StartNewGame(Game initState, DateTime timeStamp)
		{
			m_playerNameToId = new Dictionary<string, string>();
			m_oldState = initState;
			m_eventIndex = 0;
			m_batterCount = 0;
			m_discards = 0;
			m_processed = 0;
			m_errors = 0;
			m_gameId = initState._id;
			m_responsiblePitchers = new Dictionary<string, string>();
			m_startedInnings = new HashSet<string>();

			m_currEvent = CreateNewGameEvent(initState, timeStamp);
			m_currEvent.eventText.Add(initState.lastUpdate);
		}

		#region Inning tracking
		private string MakeInningKey(Game newState)
		{
			return newState.topOfInning ? $"T{newState.inning}" : $"B{newState.inning}";
		}

		private bool CanStartInning(Game newState)
		{
			return !m_startedInnings.Contains(MakeInningKey(newState)) && newState.BatterId != null;
		}

		private void StartInning(Game newState)
		{
			m_startedInnings.Add(MakeInningKey(newState));
		}
        #endregion

		private void AddParsingError(string message) {
			m_currEvent.parsingError = true;
			m_currEvent.parsingErrorList.Add(message);
			m_errors++;
            Console.WriteLine(message);
		}

        private void AddFixedError(string message)
        {
            m_currEvent.fixedError = true;
            m_currEvent.fixedErrorList.Add(message);
            m_fixes++;
        }
	
		private bool IsNextHalfInning(Game oldState, Game newState)
		{
			// Assumes no gaps
			return ((newState.topOfInning != oldState.topOfInning) &&
					(newState.inning - oldState.inning <= 1));
		}

        private bool IsStartOfNextAtBat(Game oldState, Game newState)
        {
			// Assumes no gaps
			bool sameTeam = (((newState.homeTeamBatterCount - oldState.homeTeamBatterCount == 1) ^
                     	     (newState.awayTeamBatterCount - oldState.awayTeamBatterCount == 1)) &&
                             (newState.atBatStrikes == 0 && newState.atBatBalls == 0));
		
			bool diffTeam = (IsNextHalfInning(oldState, newState) &&
							 (newState.homeTeamBatterCount == oldState.homeTeamBatterCount) &&
                             (newState.awayTeamBatterCount == oldState.awayTeamBatterCount) &&
                             (newState.atBatStrikes == 0 && newState.atBatBalls == 0));
            return sameTeam || diffTeam;
        }

		private bool IsEndOfCurrentAtBat(Game oldState, Game newState) {
            // Assumes no gaps
            return ((newState.atBatBalls <= oldState.atBatBalls) && 
					(newState.atBatStrikes <= oldState.atBatStrikes) &&
					(newState.atBatStrikes == 0 && newState.atBatBalls == 0));
		}

		private bool IsSameAtBat(Game oldState, Game newState)
		{
			return ((oldState.inning == newState.inning) &&
					(oldState.topOfInning == newState.topOfInning) &&
                    (oldState.atBatBalls <= newState.atBatBalls) &&
                    (oldState.atBatStrikes <= newState.atBatStrikes) &&
					(oldState.homeTeamBatterCount == newState.homeTeamBatterCount) &&
					(oldState.awayTeamBatterCount == newState.awayTeamBatterCount));
		}


		// If the Id and name are valid, store them in our map
		private void CapturePlayerId(string id, string name)
		{
			if(id != null && name != null && id != "" && name != "")
			{
				m_playerNameToId[name] = id;
			}
		}

		// Capture available player IDs and names from the state
		private void CapturePlayerIds(Game state)
		{
			CapturePlayerId(state.awayBatter, state.awayBatterName);
			CapturePlayerId(state.awayPitcher, state.awayPitcherName);
			CapturePlayerId(state.homeBatter, state.homeBatterName);
			CapturePlayerId(state.homePitcher, state.homePitcherName);
		}



		private GameEvent CreateNewGameEvent(Game newState, DateTime timeStamp)
		{
			CapturePlayerIds(newState);

			GameEvent currEvent = new GameEvent();
			currEvent.parsingError = false;
			currEvent.parsingErrorList = new List<string>();
			currEvent.fixedError = false;
			currEvent.fixedErrorList = new List<string>();

			currEvent.firstPerceivedAt = timeStamp;

			currEvent.gameId = newState._id;
			currEvent.season = newState.season;
			currEvent.eventIndex = m_eventIndex;
			currEvent.batterCount = m_batterCount;
			currEvent.inning = newState.inning;
			currEvent.topOfInning = newState.topOfInning;
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

			currEvent.batterId = newState.BatterId;
			currEvent.batterTeamId = newState.BatterTeamId;
			currEvent.pitcherId = newState.PitcherId;
			currEvent.pitcherTeamId = newState.PitcherTeamId;

			currEvent.eventText = new List<string>();
			currEvent.pitchesList = new List<char>();
			currEvent.playerEvents = new List<PlayerEvent>();

			// Might be incorrect
			currEvent.totalStrikes = newState.atBatStrikes;
			currEvent.totalBalls = newState.atBatBalls;

			return currEvent;
		}

		/// <summary>
		/// Logic for updating balls and strikes, including foul balls
		/// </summary>
		private void UpdateBallsAndStrikes(Game newState)
		{
			int newStrikes = 0;
			int newBalls = 0;
			if(IsSameAtBat(m_oldState, newState))
			{
                newStrikes = newState.atBatStrikes - m_currEvent.totalStrikes;
                newBalls = newState.atBatBalls - m_currEvent.totalBalls;
                m_currEvent.totalBalls = newState.atBatBalls;
                m_currEvent.totalStrikes = newState.atBatStrikes;
			}
			else if(IsEndOfCurrentAtBat(m_oldState, newState))
			{
                // If a batter strikes out we never get an update with 3 strikes on it
                // so check the play text
                if (newState.lastUpdate.Contains("struck out") || newState.lastUpdate.Contains("strikes out"))
				{
					// Set the strikes to the total for the team that WAS batting
					m_currEvent.totalStrikes = m_oldState.topOfInning ? m_oldState.awayStrikes : m_oldState.homeStrikes;
					newStrikes = m_currEvent.totalStrikes - m_oldState.atBatStrikes;
				}
                else if (newState.lastUpdate.Contains("walk"))
                {
                    m_currEvent.totalBalls = 4;
                    m_currEvent.eventType = GameEventType.WALK;
                    m_currEvent.isWalk = true;
					newBalls = m_currEvent.totalBalls - m_oldState.atBatBalls;
                }
			}
			else if(IsStartOfNextAtBat(m_oldState, newState))
			{
				// Nothing to see here, just changing batters
			}
			// This else case should return so we can assume we are only covering one event below
			else
			{
				AddParsingError($"Event jumped to processing a different batter unexpectedly");
				return;
			}

			// Oops, we hit a gap, lets see if we can fill it in
			if(newStrikes + newBalls > 1)
			{
				// Error: We skipped *something*, we should log it
				AddFixedError($"A single update had more than one pitch, but we fixed it");
				// We can know for sure the state of the last strike.
                if (newState.lastUpdate.Contains("struck out") || newState.lastUpdate.Contains("strikes out"))
                {
                    if (newState.lastUpdate.Contains("looking"))
                    {
                        m_currEvent.pitchesList.Add('C');
						newStrikes -= 1;
                    }
                    else if (newState.lastUpdate.Contains("swinging"))
                    {
                        m_currEvent.pitchesList.Add('S');
						newStrikes -= 1;
                    }
                }
				// We can know for sure that the last pitch was a ball
                else if (newState.lastUpdate.Contains("walk"))
                {
                    m_currEvent.pitchesList.Add('B');
                    newBalls -= 1;
                }

				// Add the rest as unknowns
                for (int i = 0; i < newStrikes; i++)
                {
                    m_currEvent.pitchesList.Add('K');
                }
                for (int i = 0; i < newBalls; i++)
                {
                    m_currEvent.pitchesList.Add('A');
                }
			}
			else if (newStrikes == 1)
			{
				if (newState.lastUpdate.Contains("looking"))
				{
					m_currEvent.pitchesList.Add('C');
				}
				else if (newState.lastUpdate.Contains("swinging"))
				{
					m_currEvent.pitchesList.Add('S');
				}
				else if (newState.lastUpdate.Contains("Foul Ball"))
				{
					// Do nothing, fouls are handled at the end
				}
				else
				{
					m_currEvent.pitchesList.Add('K');
                    AddFixedError($"A we missed a single strike, but we fixed it");
				}
			} 
			else if (newBalls == 1)
			{
				m_currEvent.pitchesList.Add('B');
				if (!(newState.lastUpdate.Contains("Ball.") || newState.lastUpdate.Contains("walk.")))
				{
                    AddFixedError($"A we missed a single ball, but we fixed it");
				}
			}

			if (newState.lastUpdate.Contains("Foul Ball"))
			{
				m_currEvent.totalFouls++;
				m_currEvent.pitchesList.Add('F');
			}
		}

		/// <summary>
		/// Update outs (they're annoying)
		/// </summary>
		private void UpdateOuts(Game newState)
		{
			// If the inning suddenly changed, that means this play got all the rest of the outs
			// TODO: triple plays if implemented
			if (newState.topOfInning != m_oldState.topOfInning && m_oldState.halfInningOuts > 0)
			{
				m_currEvent.outsOnPlay = 3 - m_oldState.halfInningOuts;
			}
			else
			{
				m_currEvent.outsOnPlay = Math.Max(0, newState.halfInningOuts - m_oldState.halfInningOuts);
			}

			// Types of outs
			if (newState.lastUpdate.Contains("out") || newState.lastUpdate.Contains("sacrifice") || newState.lastUpdate.Contains("hit into a double play"))
			{
				if (newState.lastUpdate.Contains("strikes out") || newState.lastUpdate.Contains("struck out"))
				{
					m_currEvent.eventType = GameEventType.STRIKEOUT;
				}
				else
				{
					m_currEvent.eventType = GameEventType.OUT;
				}
			}

		}

		/// <summary>
		/// Update hit information
		/// </summary>
		private void UpdateHits(Game newState)
		{
			// Handle RBIs
			if (!m_oldState.lastUpdate.Contains("steals"))
			{
				m_currEvent.runsBattedIn = newState.topOfInning ? newState.awayScore - m_oldState.awayScore : newState.homeScore - m_oldState.homeScore;
			}

			// Mark any kind of hit
			if (newState.lastUpdate.Contains("hits a") || newState.lastUpdate.Contains("hit a"))
			{
				m_currEvent.pitchesList.Add('X');
			}

			// Extremely basic single/double/triple/HR detection
			if (newState.lastUpdate.Contains("hits a Single"))
			{
				m_currEvent.basesHit = 1;
				m_currEvent.batterBaseAfterPlay = 1;
				m_currEvent.eventType = GameEventType.SINGLE;
			}
			else if (newState.lastUpdate.Contains("hits a Double"))
			{
				m_currEvent.basesHit = 2;
				m_currEvent.batterBaseAfterPlay = 2;
				m_currEvent.eventType = GameEventType.DOUBLE;
			}
			else if (newState.lastUpdate.Contains("hits a Triple"))
			{
				m_currEvent.basesHit = 3;
				m_currEvent.batterBaseAfterPlay = 3;
				m_currEvent.eventType = GameEventType.TRIPLE;
			}
			else if (newState.lastUpdate.Contains("home run") || newState.lastUpdate.Contains("grand slam"))
			{
				m_currEvent.basesHit = 4;
				m_currEvent.batterBaseAfterPlay = 4;
				m_currEvent.eventType = GameEventType.HOME_RUN;
			}

			// TODO currEvent.battedBallType
		}

		/// <summary>
		/// Should be called after UpdateOuts because fielder's choice overrides the generic OUT type
		/// </summary>
		private void UpdateFielding(Game newState)
		{
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

			// Fielder's choice
			// This has to go after out because it overrides it in case
			// a different batter was out.
			if (newState.lastUpdate.Contains("fielder's choice"))
			{
				m_currEvent.eventType = GameEventType.FIELDERS_CHOICE;
			}

			// Caught Stealing
			if (newState.lastUpdate.Contains("caught stealing"))
			{
				m_currEvent.eventType = GameEventType.CAUGHT_STEALING;
			}
		}

		/// <summary>
		/// Update stuff around baserunning
		/// </summary>
		private void UpdateBaserunning(Game newState)
		{

			// Steals
			if (newState.lastUpdate.Contains("steals"))
			{
				m_currEvent.eventType = GameEventType.STOLEN_BASE;
				m_currEvent.isSteal = true;
				m_currEvent.isLastEventForPlateAppearance = false;
			}

			// Clear to a new list every time we parse an update
			// Since runners can only move in cases where we emit, the last state should be correct
			// TODO: when the 3rd out happens, newState will not have any runners
			// If we want to show the runners as still stranded in the 3rd out event, we'll have to
			// dig back into the m_oldState to find them
			m_currEvent.baseRunners = new List<GameEventBaseRunner>();

			// Handle runners present in the new state and probably the old state too
			for(int i=0; i < newState.baseRunners.Count; i++)
			{
				string runnerId = newState.baseRunners[i];
				int baseIndex = newState.basesOccupied[i];

				GameEventBaseRunner runner = new GameEventBaseRunner();
				runner.runnerId = runnerId;

				// Add a new entry for this new baserunner
				if(!m_responsiblePitchers.ContainsKey(runnerId))
				{
					// Pitcher from the previous state must be responsible for this new baserunner
					m_responsiblePitchers[runnerId] = m_oldState.PitcherId;
				}

				runner.responsiblePitcherId = m_responsiblePitchers[runnerId];

				// We number home = 0, first = 1, second = 2, third = 3
				// Game updates have first = 0, second = 1, third = 2
				runner.baseAfterPlay = baseIndex + 1;

				// Find this runner's previous base in the old state
				bool found = false;
				for(int j=0; j < m_oldState.baseRunners.Count; j++)
				{
					if(m_oldState.baseRunners[j] == runnerId)
					{
						runner.baseBeforePlay = m_oldState.basesOccupied[j] + 1;
						found = true;
						if(runner.baseBeforePlay != runner.baseAfterPlay)
						{
							if(newState.lastUpdate.Contains("steals"))
							{
								runner.wasBaseStolen = true;
							}
							if(newState.lastUpdate.Contains("caught"))
							{
								runner.wasCaughtStealing = true;
							}
						}
					}
				}
				if(!found)
				{
					runner.baseBeforePlay = 0;
				}
				
				m_currEvent.baseRunners.Add(runner);
			}

			// Handle runners present in the old state but possibly not in the new ('cuz they scored)
			for(int i=0; i < m_oldState.baseRunners.Count; i++)
			{
				string runnerId = m_oldState.baseRunners[i];
				int baseIndex = m_oldState.basesOccupied[i];

				bool found = false;
				for(int j=0; j < newState.baseRunners.Count; j++)
				{
					if(newState.baseRunners[j] == runnerId)
					{
						found = true;
					}
				}
				// If we didn't find a runner from last state, and the outs are the same, they must have scored
				// newState might already have jumped to the next inning, so check old
				int newScore = m_oldState.topOfInning ? newState.awayScore : newState.homeScore;
				int oldScore = m_oldState.topOfInning ? m_oldState.awayScore : m_oldState.homeScore;
				if (!found && (m_currEvent.outsOnPlay == 0 || newScore > oldScore))
				{
					GameEventBaseRunner runner = new GameEventBaseRunner();
					runner.runnerId = runnerId;
					if(m_responsiblePitchers.ContainsKey(runnerId))
					{
						runner.responsiblePitcherId = m_responsiblePitchers[runnerId];
					}
					else
					{
						AddParsingError($"Couldn't find responsible pitcher for runner {runnerId} in update '{newState.lastUpdate}'");
					}
					runner.baseBeforePlay = baseIndex + 1;
					runner.baseAfterPlay = 4;
					if (newState.lastUpdate.Contains("steals"))
					{
						runner.wasBaseStolen = true;
					}
					m_currEvent.baseRunners.Add(runner);
				}
				else if (!found && m_currEvent.outsOnPlay > 0)
				{
					// Fine, he was out
				}
				else if(found)
				{
					// Fine, he was found
				}
				else
				{ 
					// What the hell else could have happened?
					AddParsingError($"Baserunner {runnerId} missing from base {baseIndex + 1}, but there were no outs and score went from {oldScore} to {newScore}");
				}
			}

			// Last thing - if we just changed innings, clear the responsible pitcher list
			// Note that we do this AFTER attributing baserunners who may have just done something on this play
			// and whose pitcher was from this old inning
			if (newState.inning != m_oldState.inning)
			{
				m_responsiblePitchers.Clear();
			}

		}

		/// <summary>
		/// Update metadata like the leadoff flag and lineupPosition
		/// </summary>
		private void UpdateLineupInfo(Game newState)
		{
			// Always attribute the event to the last pitcher involved
			m_currEvent.pitcherId = newState.PitcherId;

			// Track first batter in each inning
			if(CanStartInning(newState))
			{
				StartInning(newState);
				m_currEvent.isLeadoff = true;
			}

			// Game updates have a batter count per team, so the lineup position is that % 9
			if (newState.topOfInning)
			{
				m_currEvent.lineupPosition = newState.awayTeamBatterCount % 9;
			}
			else 
			{
				m_currEvent.lineupPosition = newState.homeTeamBatterCount % 9;
			}
		}


		private void TryPopulatePlayerId(PlayerEvent p, string name)
		{
			string id;
			if (m_playerNameToId.TryGetValue(name, out id))
			{
				p.playerId = id;
			}
		}

		private static Regex incineRegex = new Regex(@".*incinerated.*er (\w+ \w+)! Replaced by (\w+ \w+)");
		private static Regex peanutRegex = new Regex(@".*er (\w+ \w+) swallowed.*had an? (\w+) reaction!");

		private void UpdatePlayerEvents(Game newState)
		{

			var match = incineRegex.Match(newState.lastUpdate);
			if (match.Success)
			{
				PlayerEvent newEvent = new PlayerEvent();
				newEvent.eventType = PlayerEventType.INCINERATION;
				TryPopulatePlayerId(newEvent, match.Groups[1].Value);
				m_currEvent.playerEvents.Add(newEvent);
			}

			match = peanutRegex.Match(newState.lastUpdate);
			if (match.Success)
			{
				string playerName = match.Groups[1].Value;

				if (match.Groups[2].Value == "yummy")
				{
					PlayerEvent newEvent = new PlayerEvent();
					newEvent.eventType = PlayerEventType.PEANUT_GOOD;
					TryPopulatePlayerId(newEvent, playerName);
					m_currEvent.playerEvents.Add(newEvent);
				}
				else if (match.Groups[2].Value == "allergic")
				{
					PlayerEvent newEvent = new PlayerEvent();
					newEvent.eventType = PlayerEventType.PEANUT_BAD;
					TryPopulatePlayerId(newEvent, playerName);
					m_currEvent.playerEvents.Add(newEvent);
				}
			}

		}

		/// <summary>
		/// Final check for obvious errors in the event we're about to emit
		/// </summary>
		private void ErrorCheckBeforeEmit(GameEvent toEmit)
		{
			if(toEmit.batterId == null)
			{
				AddParsingError($"Emitted an event with NULL batterId");
			}
			if (toEmit.pitcherId == null)
			{
				AddParsingError($"Emitted an event with NULL pitcherId");
			}
		}

		/// <summary>
		/// Call this with every game update for the game this parser is handling
		/// </summary>
		/// <param name="newState"></param>
		/// <param name="timeStamp"></param>
		/// <returns></returns>
		public GameEvent ParseGameUpdate(Game newState, DateTime timeStamp)
		{
			if(newState.Equals(m_oldState))
			{
				//Console.WriteLine($"Discarded update from game {newState._id} as a duplicate.");
				m_discards++;
				return null;
			}
			else if(newState._id != m_oldState._id)
			{
				Console.WriteLine("ERROR: GameEventParser got an update for the wrong game!");
				m_discards++;
				return null;
			}
			else
			{
				m_processed++;
			}

			if(m_currEvent == null)
			{
				m_currEvent = CreateNewGameEvent(newState, timeStamp);
			}

			CapturePlayerIds(newState);
			m_currEvent.lastPerceivedAt = timeStamp;

			// If we haven't found the batter for this event yet, try again
			if (m_currEvent.batterId == null)
			{
				m_currEvent.batterId = newState.BatterId;
			}

			// Presume this event will be last; steals can set this to false later
			m_currEvent.isLastEventForPlateAppearance = true;

			UpdateLineupInfo(newState);

			UpdateBallsAndStrikes(newState);

			UpdateOuts(newState);

			UpdateHits(newState);

			// Call after UpdateOuts
			UpdateFielding(newState);

			// Call after UpdateOuts
			UpdateBaserunning(newState);

			UpdatePlayerEvents(newState);

			// Unknown or not currently handled event
			if(m_currEvent.eventType == null)
			{
				m_currEvent.eventType = GameEventType.UNKNOWN;
			}

			// Unsure if this is enough
			m_currEvent.isLastGameEvent = newState.gameComplete;

			// Store original update text for reference
			m_currEvent.eventText.Add(newState.lastUpdate);

			// Cycle state
			m_oldState = newState;

			// If we had outs or hits or a walk or a steal, emit
			// OR IF THE GAME IS OVER, duh
			if(m_currEvent.outsOnPlay > 0 || m_currEvent.basesHit > 0 || m_currEvent.isSteal || m_currEvent.isWalk || m_currEvent.isLastGameEvent)
			{
				GameEvent emitted = m_currEvent;
				m_eventIndex++;

				if (m_currEvent.isSteal || m_currEvent.eventType == GameEventType.CAUGHT_STEALING)
				{
					// Start the next event in this state
					m_currEvent = CreateNewGameEvent(newState, timeStamp);
				}
				else
				{
					// Start the next event in the next state
					m_currEvent = null;
				}

				ErrorCheckBeforeEmit(emitted);
				return emitted;
			}
			else
			{
				return null;
			}
		}
	}
}