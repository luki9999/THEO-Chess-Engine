using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "__New Command__", menuName = "Commands/__TEMPLATE, DONT USE__")]
public class __CommandName__ : ConsoleCommand
{
    //run by console object in start code
    public override void Init()
    {

    }
    //runs on command execution
    public override bool Action(string[] args)
    {
        return true;
    }
}
