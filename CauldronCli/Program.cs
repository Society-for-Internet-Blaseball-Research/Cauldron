using System;
using System.IO;
using System.Text.Json;
using Cauldron;

namespace CauldronCli
{
	class Program
	{
		static void Main(string[] args)
		{

			// TODO: more responsible error checking and USAGE output if args are wrong
			string inputFile = args[0];
			string outputFile = args[1];

			StreamReader sr = new StreamReader(inputFile);
			StreamWriter sw = new StreamWriter(outputFile);

			Processor c = new Processor();
			c.Process(sr, sw);

			sw.Close();

		}
	}
}
