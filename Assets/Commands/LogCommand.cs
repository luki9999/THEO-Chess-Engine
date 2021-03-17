using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Log Command", menuName = "Commands/Log")]
public class LogCommand : ConsoleCommand
{
    public override bool Action(string[] args)
    {
        var console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
        console.Print(string.Join(" ", args));
        return true;
    }
}