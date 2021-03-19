using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Flip Command", menuName = "Commands/Flip")]
public class BoardFlipCommand : ConsoleCommand
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
        manager.FlipBoard();
        console.Print("Board flipped.");
        return true;
    }
}
