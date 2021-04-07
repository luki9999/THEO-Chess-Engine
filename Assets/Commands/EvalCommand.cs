using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Eval Command", menuName = "Commands/Evaluate")]
public class EvalCommand : ConsoleCommand
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
        if (manager.currentState != GameState.Running) return false;
        if (args.Length != 0 && args[0] == "search")
        {
            console.Line(" ");
            manager.engine.ThreadedSearch();
        }
        else
        {
            int evalValue = manager.engine.Evaluation.EvaluatePosition(manager.playerOnTurn);
            console.Print("Current static evaluation: " + evalValue.ToString());
        }
        return true;
    }
}
