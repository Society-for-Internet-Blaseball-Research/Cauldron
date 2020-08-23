using Cauldron;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CauldronCli
{
	interface IGameWriter
	{
		public void WriteGame(IEnumerable<GameEvent> events);
		public void Finish();
	}

	class SingleFileGameWriter : IGameWriter
	{
		string m_file;
		StreamWriter m_writer;

		public SingleFileGameWriter(string fileName)
		{
			m_file = fileName;
			m_writer = new StreamWriter(fileName);
		}

		public void Finish()
		{
			m_writer.Close();
		}

		public void WriteGame(IEnumerable<GameEvent> events)
		{
			string gameId = events.First().gameId;
			Console.WriteLine($"Writing game {gameId} to file {m_file}...");

			foreach (var e in events)
			{
				// Write out each game event
				m_writer.WriteLine(JsonSerializer.Serialize(e));
			}
		}
	}

	class MultiFileGameWriter : IGameWriter
	{
		string m_folder;
		public MultiFileGameWriter(string folder)
		{
			m_folder = folder;
			if (!Directory.Exists(m_folder))
				Directory.CreateDirectory(m_folder);
		}

		public void Finish()
		{
		}

		public void WriteGame(IEnumerable<GameEvent> events)
		{
			string gameId = events.First().gameId;
			string path = Path.Combine(m_folder, gameId) + ".sibr";
			Console.WriteLine($"Writing game {gameId} to {path}");
			using (StreamWriter writer = new StreamWriter(path))
			{
				foreach(var ev in events)
				{
					writer.WriteLine(JsonSerializer.Serialize(ev));
				}
			}
		}
	}
}
