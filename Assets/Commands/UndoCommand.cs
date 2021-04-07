using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Undo Command", menuName = "Commands/Undo")]
public class UndoCommand : ConsoleCommand
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
        if (manager.searching) return false;
        manager.spaceHandler.UnHighlightAll();
        manager.UndoLastMove();
        if (manager.moveHistory.Count != 0)
        {
            manager.spaceHandler.HighlightSpace(manager.lastMove.start, Color.yellow, 0.7f);
            manager.spaceHandler.HighlightSpace(manager.lastMove.end, Color.yellow, 0.7f);
        }
        console.Print("Last move was taken back.");
        return true;
    }
}
