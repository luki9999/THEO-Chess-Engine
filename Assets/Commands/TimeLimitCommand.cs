using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Time Limit Command", menuName = "Commands/Time Limit")]
public class TimeLimitCommand : ConsoleCommand
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
        float newTimeLimit = 10f;

        if (args.Length > 0 && float.TryParse(args[0], out newTimeLimit)) manager.timeLimit = newTimeLimit;
        console.Print("Set search time limit to " + newTimeLimit.ToString() + " seconds. ");
        return true;
    }
}