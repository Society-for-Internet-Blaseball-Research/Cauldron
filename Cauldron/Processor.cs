using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Cauldron
{
	/// <summary>
	/// Basic processor that reads game update JSON objects from a stream and writes GameEvent JSON objects out
	/// </summary>
	public class Processor
	{
		/// <summary>
		/// Parser has state, so store one per game we're tracking
		/// </summary>
		Dictionary<string, GameEventParser> m_trackedGames;
		public Dictionary<string, GameEventParser> TrackedGames => m_trackedGames;

		private readonly JsonSerializerOptions m_serializerOptions;

		/// <summary>
		/// Constructor
		/// </summary>
		public Processor()
		{
			// I like camel case for my C# properties, sue me
			m_serializerOptions = new JsonSerializerOptions();
			m_serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			m_trackedGames = new Dictionary<string, GameEventParser>();
		}

		#region Event-based API

		/// <summary>
		/// Prophesizer registers for GameCompleted event
		/// Prophesizer sends chunks of updates to Processor
		/// Processor waits for each GameEventParser to be finished, then fires GameCompleted with the IEnumerable<GameEvent> for that game
		/// </summary>
		public event EventHandler<GameCompleteEventArgs> GameComplete;

		private void GameCompleteInternal(object sender, GameCompleteEventArgs e)
		{
			GameComplete?.Invoke(this, e);
		}
		#endregion


		/// <summary>
		/// Process a single game update object
		/// </summary>
		/// <param name="game">update object</param>
		/// <param name="timestamp">time this object was perceived</param>
		/// <param name="complete">event handler to register for game completion events</param>
		public void ProcessGameObject(Game game, DateTime timestamp)
		{

			// Add new games if needed
			if (!m_trackedGames.ContainsKey(game._id))
			{
				GameEventParser parser = new GameEventParser();
				parser.GameComplete += GameCompleteInternal;
				parser.StartNewGame(game, timestamp);

				m_trackedGames[game._id] = parser;
			}
			else
			{
				// Update a current game
				GameEventParser parser = m_trackedGames[game._id];
				parser.ParseGameUpdate(game, timestamp);
			}
		}

		private void ProcessUpdateString(string obj)
		{
			Update update = null;
			try
			{
				update = JsonSerializer.Deserialize<Update>(obj, m_serializerOptions);
			}
			catch(System.Text.Json.JsonException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine($"While processing: {obj}");
			}

			// Currently we only care about the 'schedule' field that has the game updates
			foreach (var game in update.Schedule)
			{
				var timestamp = update?.clientMeta?.timestamp;

				// If timestamp is missing, just set to unix time 0
				if(timestamp == null)
				{
					timestamp = TimestampConverter.unixEpoch;
				}

				ProcessGameObject(game, timestamp.Value);
			}
		}

		public void Process(StreamReader newlineDelimitedJson)
		{
			if (GameComplete == null)
				throw new InvalidOperationException("Please listen for the GameComplete event before calling Process()");
			while (!newlineDelimitedJson.EndOfStream)
			{
				string obj = newlineDelimitedJson.ReadLine();

				ProcessUpdateString(obj);
			}
		}

		public void Process(string newlineDelimitedJson)
		{
			StringReader sr = new StringReader(newlineDelimitedJson);
			string line = sr.ReadLine();
			while (line != null)
			{
				ProcessUpdateString(line);
				line = sr.ReadLine();
			}
		}

	}
}
