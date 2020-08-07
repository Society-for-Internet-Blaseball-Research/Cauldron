# Cauldron
Put in blaseball game update JSON, and pull out SIBR game event JSON

Run from the command line:

	Cauldron.exe [input file in newline-delimited JSON format] [output file in newline-delimited JSON format]

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
