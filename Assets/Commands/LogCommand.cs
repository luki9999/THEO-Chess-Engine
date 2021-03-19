using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Log Command", menuName = "Commands/Log")]
public class LogCommand : ConsoleCommand
{
    ConsoleBehaviour console;
    public override void Init()
    {
        console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
    }
    public override bool Action(string[] args)
    {
        console.Print(string.Join(" ", args));
        return true;
    }
}