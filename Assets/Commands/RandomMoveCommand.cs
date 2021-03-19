using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Random Move Command", menuName = "Commands/Random Move")]
public class RandomMoveCommand : ConsoleCommand
{
    GameMngr manager;
    ConsoleBehaviour console;
    //run by console object in start code
    public override void Init()
    {
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
    }
    //runs on command execution
    public override bool Action(string[] args)
    {
        Move move = manager.theo.ChooseRandomMove(manager.playerOnTurn);
        manager.MakeMoveAnimated(move.Start, move.End);
        console.Print("Made move " + manager.moveGenerator.MoveName(move.Start, move.End, true));
        return true;
    }
}

