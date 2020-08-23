using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cauldron;

namespace CauldronCli
{
	class Program
	{
		static StreamWriter outJson;

		static void Main(string[] args)
		{

			// TODO: more responsible error checking and USAGE output if args are wrong
			string inputFile = args[0];
			string outputFile = args[1];

			StreamReader sr = new StreamReader(inputFile);
			outJson = new StreamWriter(outputFile);

			Processor c = new Processor();
			c.GameComplete += GameComplete;
			c.Process(sr);

			outJson.Close();

			int discards = c.TrackedGames.Values.Sum(x => x.Discards);
			int processed = c.TrackedGames.Values.Sum(x => x.Processed);
			int errors = c.TrackedGames.Values.Sum(x => x.Errors);
			IEnumerable<string> errorGameIds = c.TrackedGames.Values.Where(x => x.Errors > 0).Select(x => x.GameId);
			Console.WriteLine($"Error Games:");
			foreach (var game in errorGameIds)
				Console.WriteLine(game);
			Console.WriteLine("=========");
			Console.WriteLine($"Updates Processed: {processed}\nDuplicates Discarded: {discards}\nGames With Errors: {errorGameIds.Count()}\nErrors: {errors}\nGames Found: {c.TrackedGames.Keys.Count}");
		}

		static void GameComplete(object sender, GameCompleteEventArgs args)
		{
			Console.WriteLine($"Game {args.GameEvents.First().gameId} complete");
			foreach (var e in args.GameEvents)
			{
				// Write out the latest game event
				outJson.WriteLine(JsonSerializer.Serialize(e));
			}
		}


	}
}
