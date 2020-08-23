using Cauldron;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CauldronCli
{
	class MultiFileProcessor
	{
		public event EventHandler<GameCompleteEventArgs> GameComplete;

		string m_folder;

		public MultiFileProcessor(string folderName)
		{
			m_folder = folderName;
		}

		public void Run()
		{
			Console.WriteLine($"Reading events from folder {m_folder}...");
			Processor p = new Processor();

			p.GameComplete += InternalGameComplete;
			foreach(var fileName in Directory.GetFiles(m_folder))
			{
				Console.WriteLine($"  File {fileName}...");
				using (StreamReader sr = new StreamReader(fileName))
				{
					p.Process(sr);
				}
			}
			p.GameComplete -= InternalGameComplete;
		}

		private void InternalGameComplete(object sender, GameCompleteEventArgs e)
		{
			GameComplete?.Invoke(this, e);
		}

	}
}
