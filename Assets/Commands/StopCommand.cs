using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stop Command", menuName = "Commands/Stop")]
public class StopCommand : ConsoleCommand
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
    { // TODO test if engine in search
        manager.engine.abortSearch = true;
        manager.engineState = EngineState.Off;
        console.Print("Aborted current search. Playing last found move.");
        return true;
    }
}
