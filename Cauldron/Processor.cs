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

		private readonly JsonSerializerOptions m_serializerOptions;

		private bool m_batchStarted;
		private bool m_batchUpdating;
		private List<GameEvent> m_events;

		/// <summary>
		/// Constructor
		/// </summary>
		public Processor()
		{
			// I like camel case for my C# properties, sue me
			m_serializerOptions = new JsonSerializerOptions();
			m_serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			m_trackedGames = new Dictionary<string, GameEventParser>();
			m_batchStarted = false;
			m_batchUpdating = false;
			m_events = new List<GameEvent>();
		}

		#region Batching version of the API
		public void BatchStart()
		{
			m_batchStarted = true;
		}

		public void BatchProcess(StreamReader newlineDelimitedJson)
		{
			if (!m_batchStarted)
				throw new InvalidOperationException("Called BatchProcessUpdate without calling BatchStart.");

			m_batchUpdating = true;
			Process(newlineDelimitedJson);
			m_batchUpdating = false;
		}
		
		public void BatchProcess(string newlineDelimitedJson)
		{
			if (!m_batchStarted)
				throw new InvalidOperationException("Called BatchProcess without calling BatchStart.");

			m_batchUpdating = true;
			Process(newlineDelimitedJson);
			m_batchUpdating = false;
		}

		public void BatchProcess(Game game, DateTime timestamp)
		{
			if (!m_batchStarted)
				throw new InvalidOperationException("Called BatchProcess without calling BatchStart.");

			m_batchUpdating = true;
			ProcessGame(game, timestamp);
			m_batchUpdating = false;
		}

		public IEnumerable<GameEvent> BatchEnd()
		{
			if (!m_batchStarted)
				throw new InvalidOperationException("Called BatchEnd without calling BatchStart.");

			m_batchStarted = false;
			return m_events;
		}
		#endregion


		public GameEvent ProcessGame(Game game, DateTime timestamp)
		{
			if (m_batchStarted && !m_batchUpdating)
				throw new InvalidOperationException("Called ProcessGame after BatchStart - use BatchProcess instead.");

			// Add new games if needed
			if (!m_trackedGames.ContainsKey(game._id))
			{
				GameEventParser parser = new GameEventParser();
				parser.StartNewGame(game, timestamp);

				m_trackedGames[game._id] = parser;
			}
			else
			{
				// Update a current game
				GameEventParser parser = m_trackedGames[game._id];
				GameEvent latest = parser.ParseGameUpdate(game, timestamp);

				if (latest != null)
				{
					return latest;
				}
			}

			return null;
		}

		private IEnumerable<GameEvent> ProcessUpdate(string obj)
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
				yield break;
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

				var latest = ProcessGame(game, timestamp.Value);
				if (latest != null)
					yield return latest;
			}
		}

		public IEnumerable<GameEvent> Process(StreamReader newlineDelimitedJson)
		{
			if (m_batchStarted && !m_batchUpdating)
				throw new InvalidOperationException("Called Process after BatchStart - use BatchProcess instead.");

			while (!newlineDelimitedJson.EndOfStream)
			{
				string obj = newlineDelimitedJson.ReadLine();

				IEnumerable<GameEvent> newEvents = ProcessUpdate(obj);
				m_events.AddRange(newEvents);
			}

			return m_events;
		}

		public IEnumerable<GameEvent> Process(string newlineDelimitedJson)
		{
			if (m_batchStarted && !m_batchUpdating)
				throw new InvalidOperationException("Called Process after BatchStart - use BatchProcess instead.");

			StringReader sr = new StringReader(newlineDelimitedJson);
			string line = sr.ReadLine();
			while (line != null)
			{
				IEnumerable<GameEvent> newEvents = ProcessUpdate(line);
				m_events.AddRange(newEvents);

				line = sr.ReadLine();
			}

			return m_events;
		}

		/// <summary>
		/// Process all the JSON objects on a given stream and write output to another stream
		/// </summary>
		/// <param name="newlineDelimitedJson">Incoming JSON objects, newline delimited, in blaseball game update format</param>
		/// <param name="outJson">SIBR Game Event schema JSON objects, newline delimited</param>
		public void Process(StreamReader newlineDelimitedJson, StreamWriter outJson)
		{
			int linesRead = 0;
			while (!newlineDelimitedJson.EndOfStream)
			{
				string obj = newlineDelimitedJson.ReadLine();
				linesRead++;
				IEnumerable<GameEvent> newEvents = ProcessUpdate(obj);

				foreach(var e in newEvents)
				{
					// Write out the latest game event
					outJson.WriteLine(JsonSerializer.Serialize(e));
				}
			}

			int discards = m_trackedGames.Values.Sum(x => x.Discards);
			int processed = m_trackedGames.Values.Sum(x => x.Processed);
			int errors = m_trackedGames.Values.Sum(x => x.Errors);
			int fixes = m_trackedGames.Values.Sum(x => x.Fixes);
			IEnumerable<string> errorGameIds = m_trackedGames.Values.Where(x => x.Errors > 0).Select(x => x.GameId);
			Console.WriteLine($"Error Games:");
			foreach(var game in errorGameIds)
				Console.WriteLine(game);
			Console.WriteLine("=========");
			Console.WriteLine($"Lines Read: {linesRead}\nUpdates Processed: {processed}\nDuplicates Discarded: {discards}\nGames With Errors: {errorGameIds.Count()}\nErrors: {errors}\nFixed: {fixes}\nGames Found: {m_trackedGames.Keys.Count}");

		}
	}
}
