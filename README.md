# Cauldron
Put in blaseball game update JSON, and pull out SIBR game event JSON

Run from the command line:

	CauldronCli.exe [input file in newline-delimited JSON format] [output file in newline-delimited JSON format]

If the input JSON doesn't contain a full game from start to finish, resulting stats may be wonky.

## Unimplemented Fields

## Partially implemented fields

* `lineupPosition` is getting -1s sometimes
