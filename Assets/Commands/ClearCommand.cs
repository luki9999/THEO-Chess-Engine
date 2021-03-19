using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Clear Command", menuName = "Commands/Clear")]
public class ClearCommand : ConsoleCommand
{
    ConsoleBehaviour console;
    //run by console object in start code
    public override void Init()
    {
        console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
    }
    //runs on command execution
    public override bool Action(string[] args)
    {
        console.Clear();
        return true;
    }
}
