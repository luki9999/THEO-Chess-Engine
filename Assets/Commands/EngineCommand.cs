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
        if(args[0].Equals("stop", System.StringComparison.OrdinalIgnoreCase))
        {
            manager.theoIsBlack = false;
            manager.theoIsWhite = false;
            console.Print("Engine playing stopped.");
            return true;
        }
        manager.theoIsBlack = args[0].Equals("black", System.StringComparison.OrdinalIgnoreCase);
        manager.theoIsWhite = args[0].Equals("white", System.StringComparison.OrdinalIgnoreCase);
        if (!manager.theoIsBlack && !manager.theoIsWhite) 
        {
            console.Print("Invalid color name, use white or black.");
            return false; 
        }
        string enginePlayer = manager.theoIsWhite ? "white" : (manager.theoIsBlack ? "black" : "invalid");
        console.Print("The engine plays " + enginePlayer + " now. Use go to force it to move first.\n");
        return true;
    }
}
