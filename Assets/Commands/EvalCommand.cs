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
        int evalValue = manager.theo.EvalPosition(manager.playerOnTurn);
        console.Print("Current evaluation: " + evalValue.ToString());
        return true;
    }
}
