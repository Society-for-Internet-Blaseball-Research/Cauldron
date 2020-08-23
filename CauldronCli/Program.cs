using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cauldron;
using CommandLine;

namespace CauldronCli
{
	class Program
	{
		public class Options
		{
			[Option('i', "inputFile", HelpText = "Single file to process (newline-delimited JSON)")]
			public string InputFile { get; set; }

			[Option(HelpText ="Folder of files to process (newline-delimited JSON)")]
			public string InputFolder { get; set; }

			[Option('o', "outputFile", HelpText = "Single file to write output to (newline-delimited JSON)")]
			public string OutputFile { get; set; }

			[Option(HelpText = "Folder to output single-game files (newline-delimited JSON)")]
			public string OutputFolder { get; set; }
		}

		static IGameWriter s_writer;


		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(RunWithOptions)
				.WithNotParsed(HandleParseError);
		}

		static void HandleParseError(IEnumerable<Error> errors)
		{
			Console.WriteLine("Error - Unknown args:");
			foreach(var err in errors)
			{
				Console.WriteLine(err.ToString());
			}
		}

		static void RunWithOptions(Options opt)
		{
			if(opt.InputFile == null && opt.InputFolder == null)
			{
				Console.WriteLine("ERROR: Input file OR folder must be provided");
				return;
			}
			if (opt.OutputFile == null && opt.OutputFolder == null)
			{
				Console.WriteLine("ERROR: Input file OR folder must be provided");
				return;
			}

			if(opt.OutputFile != null)
			{
				s_writer = new SingleFileGameWriter(opt.OutputFile);
			}
			else if(opt.OutputFolder != null)
			{
				s_writer = new MultiFileGameWriter(opt.OutputFolder);
			}

			if(opt.InputFile != null)
			{
				SingleFileProcessor sfp = new SingleFileProcessor(opt.InputFile);
				sfp.GameComplete += GameComplete;
				sfp.Run();
				sfp.GameComplete -= GameComplete;
			}
			else if(opt.InputFolder != null)
			{
				MultiFileProcessor mfp = new MultiFileProcessor(opt.InputFolder);
				mfp.GameComplete += GameComplete;
				mfp.Run();
				mfp.GameComplete -= GameComplete;
			}

			s_writer.Finish();

			//int discards = c.TrackedGames.Values.Sum(x => x.Discards);
			//int processed = c.TrackedGames.Values.Sum(x => x.Processed);
			//int errors = c.TrackedGames.Values.Sum(x => x.Errors);
			//IEnumerable<string> errorGameIds = c.TrackedGames.Values.Where(x => x.Errors > 0).Select(x => x.GameId);
			//Console.WriteLine($"Error Games:");
			//foreach (var game in errorGameIds)
			//	Console.WriteLine(game);
			//Console.WriteLine("=========");
			//Console.WriteLine($"Updates Processed: {processed}\nDuplicates Discarded: {discards}\nGames With Errors: {errorGameIds.Count()}\nErrors: {errors}\nGames Found: {c.TrackedGames.Keys.Count}");
		}

		static void GameComplete(object sender, GameCompleteEventArgs args)
		{
			s_writer.WriteGame(args.GameEvents);
		}


	}
}
