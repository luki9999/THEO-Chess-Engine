using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Perft Command", menuName = "Commands/Perft")]
public class PerftCommand : ConsoleCommand
{
    public override bool Action(string[] args)
    {
        int perftTestDepth;
        //uh I hate doing this
        GameMngr manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        ConsoleBehaviour console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
        try
        {
            perftTestDepth = int.Parse(args[0]);
        }
        catch
        {
            console.Print("Invalid Type: Use a number as the depth argument.");
            return false;
        }

        manager.theo.originalDepth = perftTestDepth;
        float startTime = Time.realtimeSinceStartup;
        int moveCount = manager.theo.MoveGenCountTest(perftTestDepth, manager.playerOnTurn, false, console);
        float timeElapsed = Time.realtimeSinceStartup - startTime;
        console.Print("Found " + moveCount.ToString("N0") + " moves with depth " + perftTestDepth.ToString());
        console.Print("It took " + timeElapsed.ToString() + " seconds.");

        return true;
    }
}