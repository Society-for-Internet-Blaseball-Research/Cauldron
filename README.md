# Cauldron
Put in blaseball game update JSON, and pull out SIBR game event JSON

Run from the command line:

	Cauldron.exe [input file in newline-delimited JSON format] [output file in newline-delimited JSON format]

## TODO

* Handle partial games by ignoring them and printing a warning
* Ignore duplicate updates after the game is over
* Change to emit one event per plate appearance (roughly)
	* Steals should emit the current apperance so far, ended by the steal
	* We should then start a new event for the rest of that appearance

## Unimplemented Fields

* `eventType`
* `batterCount`
* `pitchesList`
* `isLeadoff`
* `lineupPosition`
* `battedBallType`
* `baseRunners`
	* This is an array of `GameEventBaseRunner` sub-objects that aren't yet implemented
* `isLastEventForAtBat`

## Known Wrong Fields

* `runsBattedIn` isn't accounting for stealing home
