using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName ="New FEN Loading Command", menuName ="Commands/FEN Loading")]
public class FenLoadCommand : ConsoleCommand
{
    GameMngr manager;
    ConsoleBehaviour console;
    public override void Init()
    {
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
    }
    public override bool Action(string[] args)
    {
        //do try catch or some other error handling here 
        string fen = string.Join(" ", args).Trim('"', ' ');
        console.Print("Loading FEN: " + fen);
        manager.LoadPosition(fen);
        return true;
    }
}