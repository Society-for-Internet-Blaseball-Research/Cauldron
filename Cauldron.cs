using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Cauldron
{

	class Cauldron
	{
		Dictionary<string, GameEventParser> m_trackedGames;

		public Cauldron()
		{
			m_trackedGames = new Dictionary<string, GameEventParser>();
		}

		public void Process(StreamReader newlineDelimitedJson, StreamWriter outJson)
		{
			JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
			serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			while (!newlineDelimitedJson.EndOfStream)
			{
				string obj = newlineDelimitedJson.ReadLine();
				Update update = JsonSerializer.Deserialize<Update>(obj, serializerOptions);
				foreach(var game in update.Schedule)
				{
					if (!m_trackedGames.ContainsKey(game._id))
					{
						GameEventParser parser = new GameEventParser();
						parser.StartNewGame(game);

						m_trackedGames[game._id] = parser;
					}
					else
					{
						GameEventParser parser = m_trackedGames[game._id];
						GameEvent latest = parser.ParseGameUpdate(game);

						outJson.WriteLine(JsonSerializer.Serialize(latest));
					}
				}
			}

			Console.WriteLine($"Processed updates for {m_trackedGames.Keys.Count} games.");
		}
	}
}
