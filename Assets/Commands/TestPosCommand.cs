using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Test Position Command", menuName = "Commands/Test Position Loader")]
public class TestPosCommand : ConsoleCommand
{
    Dictionary<string, string> positionDict = new Dictionary<string, string>
    {
        {"start", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"},
        {"pawn", "8/8/6p1/7P/8/2p1p3/3P4/8 w - - 0 1"},
        {"knight", "8/8/2p5/5p2/3N4/1P6/4P3/8 w - - 0 1"},
        {"rook", "8/p2p2p1/8/8/3R4/8/1P1P1P2/8 w - - 0 1"},
        {"bishop", "8/p2p2p1/8/8/3B4/8/3P1P2/8 w - - 0 1"},
        {"queen", "8/p2p2p1/8/8/3Q4/8/1P1P1P2/8 w - - 0 1"},
        {"king", "8/8/8/3p4/2pKP3/3P4/8/8 w - - 0 1"},
        {"perft1", "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - "},
        {"perft2", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - "},
        {"perft3", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"},
        {"perft4", "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8"},
        {"perft5", "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10"}
    };

    ConsoleBehaviour console;
    GameMngr mngr;
    string validPosNames;

    public override void Init()
    {
        mngr = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        console = GameObject.FindGameObjectWithTag("Console").GetComponent<ConsoleBehaviour>();
        validPosNames = string.Join(" ", positionDict.Keys);
    }

    public override bool Action(string[] args)
    {
        if (args[0].Equals("help", System.StringComparison.OrdinalIgnoreCase))
        {
            console.Print("Possible positions to load:\n" + validPosNames);
            return true;
        }
        if(positionDict.ContainsKey(args[0]))
        {
            mngr.LoadPosition(positionDict[args[0]]);
            console.Print("Loading position: " + args[0]);
            return true;
        }
        console.Print("Invalid position pame, vaild names are:\n" + validPosNames);
        return false;
    }
}
