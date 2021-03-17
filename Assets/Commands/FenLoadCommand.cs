using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName ="New FEN Loading Command", menuName ="Commands/FEN Loading")]
public class FenLoadCommand : ConsoleCommand
{
    public override bool Action(string[] args)
    {
        //uh I hate doing this
        GameMngr manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        ConsoleBehaviour console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();

        //do try catch or some other error handling here 
        string fen = string.Join(" ", args).Trim('"', ' ');
        console.Print("Loading FEN: " + fen);
        manager.LoadPosition(fen);
        return true;
    }
}