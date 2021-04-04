using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Engine Command", menuName = "Commands/Engine")]
public class EngineCommand : ConsoleCommand
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
        string playerText;
        if(args[0].Equals("stop", System.StringComparison.OrdinalIgnoreCase))
        {
            manager.engineState = EngineState.Off;
            console.Print("Engine playing stopped.");
            return true;
        }
        else if(args[0].Equals("black", System.StringComparison.OrdinalIgnoreCase)) { 
            manager.engineState = EngineState.Black;
            playerText = "black";
        }
        else if(args[0].Equals("white", System.StringComparison.OrdinalIgnoreCase)) {
            manager.engineState = EngineState.White;
            playerText = "white";
        } 
        else if(args[0].Equals("both", System.StringComparison.OrdinalIgnoreCase)) {
            manager.engineState = EngineState.Both;
            playerText = "both";
        }
        else {
            console.Print("Invalid input, use white, black, both or stop.");
            return false; 
        }
        console.Print("The engine plays " + playerText + " now. Use go to force it to move first.");
        return true;
    }
}
