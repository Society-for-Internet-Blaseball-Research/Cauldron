# Cauldron
Put in blaseball game update JSON, and pull out SIBR game event JSON

Run from the command line:

	CauldronCli.exe [input file in newline-delimited JSON format] [output file in newline-delimited JSON format]

## TODO

* Handle partial games by ignoring them and printing a warning

## Unimplemented Fields

* `isLeadoff`
* `lineupPosition`
* `battedBallType`
* `baseRunners`
	* This is an array of `GameEventBaseRunner` sub-objects that aren't yet implemented

## Partially implemented fields

* `eventType`
