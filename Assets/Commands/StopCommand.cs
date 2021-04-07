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
        if (!manager.searching) return false;
        manager.engine.abortSearch = true;
        manager.engineState = EngineState.Off;
        console.Print("Aborting current search and playing last found move.");
        return true;
    }
}
