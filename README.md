# Cauldron
Put in blaseball game update JSON, and pull out SIBR game event JSON

# CauldronCli
Command-line frontend for the Cauldron library.

	-i, --inputFile	: Single file to process (newline-delimited JSON)
	--inputFolder	: Folder of files to process (newline-delimited JSON)
	-o, --outputFile : Single file to write output to (newline-delimited JSON)
	--outputFolder	 : Folder to output single-game files (newline-delimited JSON)
	
You must specify one input method and one output method.

# CauldronVisualizer
WPF frontend for the Cauldron library.

* Load updates from .json
* Load updates directly from S3 bucket
* Save updates to .json
* Convert updates to SIBR Game Events
* Load events from .json
* Save events to .json
* Filter to a single game
