using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Debug Command", menuName = "Commands/Debug Command")]
public class DebugCommand : ConsoleCommand
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
        manager.debugMode = !manager.debugMode;
        manager.spaceHandler.UnHighlightAll();
        if (manager.debugMode) manager.DebugOverlay();
        console.Print("Toggled debug overlay.");
        return true;
    }
}