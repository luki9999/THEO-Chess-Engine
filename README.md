# THEO-Chess-Engine
Meet THEO, the Tiny Helpless Erroneus Open chess engine, a little open-source chess AI I created to beat my friend at chess.
It currently plays kinda ok, is really slow and basically made of bugs and atrocious code. Very easy to beat for experienced players.

To get started, type "help" or "?" in the console.

Features:
  - Very slow engine
  - Human vs Human, AI vs Human and AI vs AI mode
  - FEN reading
  - Graphics in Unity, engine in C#

Console commands:
  - clear:        Clears the console
  - showmoves:    Displays all possible moves for a given color. Syntax: showmoves white|black
  - perft:        Runs a performance test on the engine with a specified depth. Syntax: perft (int)
  - fen:          Loads a specified FEN string. Syntax: fen "string"
  - testpos:      Loads a predefined FEN for testing. Enter "testpos help" to show options.
  - exit:         Exits the application.
  - flip:         Flips the board.
  - go:           Searches and makes a move for the player currenly on turn.
  - engine:       Makes the engine play a certain side. Use go to force it to move first. Syntax: engine white|black|stop|both
  - restart:      Restarts the game.
  - eval:         Evaluates the current position. eval search to run search.
  - depth:        Sets or displays engine depth. 
  - reload:       Reloads pieces. Useful in case of bugs.
  - undo:         Undoes the last move.
  - showcaptures: Shows possible captures. Syntax: showcaptures white|black
  - capturedepth: Sets or shows the depth for capture only searching. Syntax: capturedepth off|infinity|(int)
