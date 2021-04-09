using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Depth Command", menuName = "Commands/Depth")]
public class DepthCommand : ConsoleCommand
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
        int depthValue;
        if (args.Length == 0)
        {
            console.Print("Current engine depth limit: " + manager.engineDepth.ToString());
            return true;
        }
        else if (!int.TryParse(args[0], out depthValue))
        {
            console.Print("Invalid arugment for depth command. Use an integer.");
            return false;
        }
        else if (depthValue < 2 || depthValue > 100)
        {
            console.Print("Engine depth limit has to be between 2 and 100.\nChoose a different value.");
            return false;
        }
        manager.engineDepth = depthValue;
        manager.engine.originalDepth = depthValue;
        console.Print("Engine depth limit was set to " + args[0] + ".");
        return true;
    }
}

