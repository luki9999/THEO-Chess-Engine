using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Exit Command", menuName = "Commands/Exit")]
public class ExitCommand : ConsoleCommand
{
    //run by console object in start code
    public override void Init()
    {

    }
    //runs on command execution
    public override bool Action(string[] args)
    {
        Application.Quit();
        return true;
    }
}