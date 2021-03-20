using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Move
{
    public int Piece;
    public int Start;
    public int End;
    public Move(int piece, int start, int end)
    {
        Piece = piece;
        Start = start;
        End = end;
    }
}

public class Engine
{
    GameMngr manager;
    MoveGenerator moveGenerator;
    ChessBoard board;
    int possibleMoveCount;
    public int originalDepth;

    int[] pieceValues = new int[] { 100, 300, 300, 500, 900 };

    public Engine(MoveGenerator moveGenToUse)
    {
        moveGenerator = moveGenToUse;
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        board = moveGenerator.board;
    }

    public int EvalPosition()
    {
        int output = 0;
        return output;
    }

    public Move ChooseMove(int player, int depth)
    {
        return new Move(0, 0, 0);
    }

    public Move ChooseRandomMove(int player)
    {
        var allMoves = GetMoveset(player);
        if (allMoves.Count == 0) //checkmate or draw 
        {
            manager.gameEnd.Invoke();
            return new Move(0, 0, 0);
        }
        int moveIndex = Random.Range(0, allMoves.Count);
        return allMoves[moveIndex];
    }

    public List<Move> GetMoveset(int player)
    {
        var output = new List<Move>();
        for (int space = 0; space<64;space++)
        {
            if (ChessBoard.PieceColor(moveGenerator.board[space]) == player)
            {
                int currentPiece = moveGenerator.board[space];
                foreach (int endSpace in moveGenerator.GetLegalMovesForPiece(space))
                {
                    output.Add(new Move(currentPiece, space, endSpace));
                }
            }
        }
        return output;
    }

    public int MoveGenCountTest(int depth, int playerToStart, bool reportMoves = false, ConsoleBehaviour console = null)
    {
        if (depth == 0) return 1;
        List<Move> moves = GetMoveset(playerToStart);
        int output = 0, lastOutput = 0;
        foreach (Move move in moves)
        {
            if(reportMoves) Debug.Log("I just made " + moveGenerator.MoveName(move.Start, move.End));
            var thisMove = moveGenerator.MovePiece(move.Start, move.End);
            lastOutput = output;
            output += MoveGenCountTest(depth - 1, playerToStart ^ 1);
            moveGenerator.UndoMovePiece(thisMove);
            if (depth == originalDepth) Debug.Log(moveGenerator.MoveName(move.Start, move.End, true) + "   " + (output-lastOutput).ToString("N0"));
            if (depth == originalDepth && console != null) console.Print(moveGenerator.MoveName(move.Start, move.End, true) + "   " + (output - lastOutput).ToString("N0"));
        }
        return output;
    }

}
