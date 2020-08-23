using Cauldron;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CauldronCli
{
	class SingleFileProcessor
	{
		public event EventHandler<GameCompleteEventArgs> GameComplete;

		string m_file;

		public SingleFileProcessor(string file)
		{
			m_file = file;
		}

		public void Run()
		{
			Console.WriteLine($"Reading events from file {m_file}...");
			StreamReader sr = new StreamReader(m_file);
			Processor c = new Processor();
			c.GameComplete += InternalGameComplete;
			c.Process(sr);
			c.GameComplete -= InternalGameComplete;
		}

		private void InternalGameComplete(object sender, GameCompleteEventArgs e)
		{
			GameComplete?.Invoke(this, e);
		}
	}
}
