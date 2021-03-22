using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;

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

public struct SearchData
{
    public int currentSearchCount;
    public int currentBestEval;
    public string currentBestMove;
    public bool valuesChanged;

    public SearchData(int currentSearchCount, int currentBestEval, string currentBestMove)
    {
        this.currentSearchCount = currentSearchCount;
        this.currentBestEval = currentBestEval;
        this.currentBestMove = currentBestMove;
        valuesChanged = false;
    }
}

public class Engine
{
    GameMngr manager;
    MoveGenerator moveGenerator;
    ConsoleBehaviour console;
    int possibleMoveCount;
    public int originalDepth;
    public int searchCount;
    public bool moveReady = false;
    public SearchData currentSearch;
    Move currentBestMove;
    public Move nextFoundMove;

    readonly int[] pieceValues = new int[] {0, 100, 300, 300, 500, 900, 0};

    readonly int[] pawnBonusSpaces = new int[] { 27, 28, 35, 36 };

    const int pawnBonus = 20;
    const int checkBonus = 50;

    public Engine(MoveGenerator moveGenToUse)
    {
        moveGenerator = moveGenToUse;
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        console = manager.console;
        currentSearch = new SearchData(0, 0, "");
    }

    public static int OtherPlayer(int player)
    {
        return (player == ChessBoard.white) ? ChessBoard.black : ChessBoard.white;
    }

    public int MaterialValue()
    {
        int output = 0;
        for (int i = 0; i < 64; i++)
        {
            int currentPiece = moveGenerator.board[i];
            int valueSign = (ChessBoard.PieceColor(currentPiece) == ChessBoard.white) ? 1 : -1;
            int currentValue = pieceValues[ChessBoard.PieceType(currentPiece)] * valueSign;
            output += currentValue;
        }
        return output;
    }

    public int EvalPosition(int player)
    {
        int eval = MaterialValue();
        if (moveGenerator.IsPlayerInCheck(ChessBoard.white)) eval -= checkBonus;
        if (moveGenerator.IsPlayerInCheck(ChessBoard.black)) eval += checkBonus;
        foreach (int i in pawnBonusSpaces)
        {
            if (ChessBoard.PieceType(moveGenerator.board[i]) == ChessBoard.pawn) 
            {
                eval += (ChessBoard.PieceColor(moveGenerator.board[i]) == ChessBoard.white) ? pawnBonus : -pawnBonus;
            }
        }
        return (player == ChessBoard.white) ? eval : -eval;
    }

     

    public int Search(int player, int depth)
    {
        if (depth == 0) return EvalPosition(player);
        List<Move> moves = GetMoveset(player);
        if (moves.Count == 0) 
        {
            if(moveGenerator.IsPlayerInCheck(player)) return -10000; //Checkmate 
            return 0; // draw
        }

        int bestEval = -1000;

        foreach (Move move in moves)
        {
            var currentMoveName = moveGenerator.MoveName(move.Start, move.End);
            var madeMove = moveGenerator.MovePiece(move.Start, move.End);
            int eval = -Search(OtherPlayer(player), depth - 1);
            searchCount++;
            if (Mathf.Max(eval, bestEval) != bestEval)
            { //found new best move
                bestEval = Mathf.Max(eval, bestEval);
               if (depth == originalDepth)//dont forget to set this in wrapper
               {
                    currentBestMove = move;
                    currentSearch.currentBestMove = currentMoveName; // usable for displaying in console
                    currentSearch.currentSearchCount = searchCount;
                    currentSearch.currentBestEval = bestEval;
                    currentSearch.valuesChanged = true;
               }
            }
            moveGenerator.UndoMovePiece(madeMove);
        }

        return bestEval;
    }

    public Move ChooseMove(int player, int depth)
    {
        originalDepth = depth; //important
        searchCount = 0;
        Search(player, depth);
        currentSearch.currentSearchCount = searchCount;
        currentSearch.valuesChanged = true;
        return currentBestMove;
    }

    public void ThreadedMove()
    {
        Thread thread = new Thread(ChooseMove) { IsBackground = true };
        thread.Start();
    }

    public void ChooseMove()
    {
        nextFoundMove = ChooseMove(manager.playerOnTurn, manager.engineDepth);
        moveReady = true;
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
