using System;
using System.IO;
using System.Text.Json;

namespace Cauldron
{
	class Program
	{
		static void Main(string[] args)
		{

			string inputFile = args[0];
			string outputFile = args[1];

			StreamReader sr = new StreamReader(inputFile);
			StreamWriter sw = new StreamWriter(outputFile);

			Cauldron c = new Cauldron();
			c.Process(sr, sw);

			sw.Close();

		}
	}
}
