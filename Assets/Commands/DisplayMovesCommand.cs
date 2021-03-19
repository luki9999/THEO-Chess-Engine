using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Moves Displaying Command", menuName = "Commands/Move Displaying")]
public class DisplayMovesCommand : ConsoleCommand
{
    public override bool Action(string[] args)
    {
        //uh I hate doing this
        GameMngr manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        ConsoleBehaviour console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();

        if (!args[0].Equals("white",System.StringComparison.OrdinalIgnoreCase) && !args[0].Equals("black", System.StringComparison.OrdinalIgnoreCase))
        { // sorting out wrong inputs
            console.Print("Can't highlight possible moves for color " + args[0] + ", please use white or black.");
            return false; 
        }

        int player = (args[0] == "white") ? ChessBoard.white : ChessBoard.black;
        List<Move> possibleMoves = manager.theo.GetMoveset(player);
        List<int> alreadyHighlightedSpaces = new List<int>();
        foreach (Move move in possibleMoves)
        {
            if (alreadyHighlightedSpaces.Contains(move.End)) continue;
            alreadyHighlightedSpaces.Add(move.End);
            manager.spaceHandler.HighlightSpace(move.End, Color.green, 0.5f);
        }

        console.Print("Showing possible moves for color " + args[0] + ".");

        return true;
    }
}
