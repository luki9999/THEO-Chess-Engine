using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Capture Depth Command", menuName = "Commands/Capture Depth")]
public class CaptureDepthCommand : ConsoleCommand
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
        int depthValue;
        if (args.Length == 0)
        {
            string capDepthStr = (manager.captureDepth <= -1) ? "infinity" :((manager.captureDepth == 0) ? "off" :  manager.captureDepth.ToString());
            console.Print("Current capture search depth: " + capDepthStr);
            return true;
        }
        else if (args[0].Equals("infinity", System.StringComparison.OrdinalIgnoreCase))
        {
            manager.captureDepth = -1;
            console.Print("Capture search will run until no captures are possible.");
            return true;
        }
        else if (args[0].Equals("off", System.StringComparison.OrdinalIgnoreCase))
        {
            manager.captureDepth = 0;
            console.Print("Capture search disabled.");
            return true;
        }
        else if (!int.TryParse(args[0], out depthValue))
        {
            console.Print("Invalid arugment for capturedepth command. Use an integer.");
            return false;
        }
        else if (depthValue < 1 || depthValue > 32) //TODO make this not hardcoded, you donut
        {
            console.Print("Capture depth has to be between 1 and 32.\nChoose a different value.");
            return false;
        }
        manager.captureDepth = depthValue;
        console.Print("Capture search depth was set to " + args[0] + ".");
        return true;
    }
}
