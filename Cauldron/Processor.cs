using System;
using System.Collections.Generic;
using System.IO;
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

		/// <summary>
		/// Constructor
		/// </summary>
		public Processor()
		{
			m_trackedGames = new Dictionary<string, GameEventParser>();
		}

		/// <summary>
		/// Process all the JSON objects on a given stream and write output to another stream
		/// </summary>
		/// <param name="newlineDelimitedJson">Incoming JSON objects, newline delimited, in blaseball game update format</param>
		/// <param name="outJson">SIBR Game Event schema JSON objects, newline delimited</param>
		public void Process(StreamReader newlineDelimitedJson, StreamWriter outJson)
		{
			// I like camel case for my C# properties, sue me
			JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
			serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			while (!newlineDelimitedJson.EndOfStream)
			{
				string obj = newlineDelimitedJson.ReadLine();
				Update update = JsonSerializer.Deserialize<Update>(obj, serializerOptions);

				// Currently we only care about the 'schedule' field that has the game updates
				foreach(var game in update.Schedule)
				{
					// Add new games if needed
					if (!m_trackedGames.ContainsKey(game._id))
					{
						GameEventParser parser = new GameEventParser();
						parser.StartNewGame(game);

						m_trackedGames[game._id] = parser;
					}
					else
					{
						// Update a current game
						GameEventParser parser = m_trackedGames[game._id];
						GameEvent latest = parser.ParseGameUpdate(game);

						if (latest != null)
						{
							// Write out the latest game event
							outJson.WriteLine(JsonSerializer.Serialize(latest));
						}
					}
				}
			}

			Console.WriteLine($"Processed updates for {m_trackedGames.Keys.Count} games.");
		}
	}
}
