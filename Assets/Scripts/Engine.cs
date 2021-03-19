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
    MoveGenerator moveGenerator;
    int possibleMoveCount;
    public int originalDepth;
    public Engine(MoveGenerator boardToPlayAt)
    {
        moveGenerator = boardToPlayAt;
    }

    public float EvalPosition(int[] position)
    {
        float output = 0;
        return output;
    }

    public float EvalPosition()
    {
        float output = 0;
        return output;
    }

    void Search()
    {

    }

    public Move ChooseDumbMove(int player)
    {
        var allMoves = GetMoveset(player);
        return allMoves[0];
    }

    public Move ChooseRandomMove(int player)
    {
        var allMoves = GetMoveset(player);
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
