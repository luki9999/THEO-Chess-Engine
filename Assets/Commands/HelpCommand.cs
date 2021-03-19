using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Help Command", menuName = "Commands/Help")]
public class HelpCommand : ConsoleCommand
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
        console.Help();
        return true;
    }
}
