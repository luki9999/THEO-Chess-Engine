using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Events;
using System.Linq;

public struct EngineMove
{
    public int Piece;
    public int Start;
    public int End;
    public int Eval;
    public EngineMove(int piece, int start, int end, int eval = 0)
    {
        Piece = piece;
        Start = start;
        End = end;
        Eval = eval;
    }

    public static int CompareByEval(EngineMove move1, EngineMove move2)
    {
        return -move1.Eval.CompareTo(move2.Eval); // that minus makes sure better moves come first in the search
    }
}

public struct SearchData
{
    public int currentSearchCount;
    public int currentBestEval;
    public int currentMoveScore;
    public string currentBestMoveName;
    public bool valuesChanged;
    public bool searchStarted;

    public SearchData(int currentSearchCount, int currentBestEval, string currentBestMove, int currentMoveScore)
    {
        this.currentSearchCount = currentSearchCount;
        this.currentBestEval = currentBestEval;
        this.currentBestMoveName = currentBestMove;
        this.currentMoveScore = currentMoveScore;
        valuesChanged = false;
        searchStarted = false;
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
    public bool evalReady = false;
    public SearchData currentSearch;
    EngineMove currentBestMove;
    public EngineMove nextFoundMove;
    Evaluation evaluator;
    public Evaluation Evaluation{get => evaluator; }

    readonly int[] pieceValues = new int[] {0, 100, 300, 300, 500, 900, 0};

    const int checkBonus = 30;
    const int endgamePieceDistanceBonusMultiplier = 5;

    const int endgameThreshold = 2 * 500 + 2 * 300 + 2 * 100; //two rooks, two pieces, two pawns or similar

    const int positiveInfinity = 9999999;
    const int negativeInfinity = -positiveInfinity;

    public Engine(MoveGenerator moveGenToUse)
    {
        moveGenerator = moveGenToUse;
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameMngr>();
        console = manager.console;
        currentSearch = new SearchData(0, 0, "", 0);
        evaluator = new Evaluation(moveGenerator);
    }     

    public int Search(int player, int depth, int alpha, int beta, int captureDepth = -1)
    {
        searchCount++;
        if (depth == 0) return CaptureSearch(player, captureDepth, alpha, beta);
        List<EngineMove> moves = GetOrderedMoveset(player);
        if (moves.Count == 0) 
        {
            if(moveGenerator.IsPlayerInCheck(player)) return -10000 + depth; //Checkmate in depth ply, favors earlier checkmate 
            return 0; // draw
        }
        foreach (EngineMove move in moves)
        {
            UndoMoveData madeMove = moveGenerator.MovePiece(move.Start, move.End);
            int eval = -Search(player ^ 1, depth - 1, -beta, -alpha, captureDepth);
            if(manager.positionHistory.Count(x => x == moveGenerator.board.Hash()) == 2) eval = 0; //draw after 3 fold repetion
            //if(manager.moveHistory.Count(x => (ChessBoard.PieceType(x.movedPiece) != ChessBoard.pawn)) == 49) eval = 0;  

            moveGenerator.UndoMovePiece(madeMove);

            if (eval >= beta)//prune the branch
            {
                return beta;
            }
            if (eval > alpha)
            { //found new best move
               alpha = eval;
               //Debug.Log("neues alpha: " + maxValue.ToString());
               if (depth == originalDepth)//dont forget to set this in wrapper
               {
                    currentBestMove = move;

                    currentSearch.currentBestMoveName = moveGenerator.MoveName(move.Start, move.End); // SLOW: i'm literally building a string in the most performance critical place... usable for displaying in console
                    currentSearch.currentSearchCount = searchCount;
                    currentSearch.currentBestEval = alpha;
                    currentSearch.currentMoveScore = evaluator.EvaluateMove(currentBestMove);
                    currentSearch.valuesChanged = true;
               }
            }
        }
        return alpha;
    }


    public int CaptureSearch(int player, int captureDepth, int alpha, int beta)
    {
        searchCount++;
        int eval = evaluator.EvaluatePosition(player);
        if (captureDepth == 0) return eval; //when captureDepth is negative this never happens
        if (eval >= beta)
        {
            return beta;
        }
        if (eval > alpha)
        { //in case the pos is good but all captures are bad
            alpha = eval;
        }
        //searchCount++;
        List<EngineMove> moves = GetCaptures(player);

        for (int i = 0; i < moves.Count; i++)
        {
            UndoMoveData madeMove = moveGenerator.MovePiece(moves[i].Start, moves[i].End);
            eval = -CaptureSearch(player ^ 1, captureDepth-1, -beta, -alpha);
            moveGenerator.UndoMovePiece(madeMove);
            if (eval >= beta) //prune the branch
            {
                return beta;
            }
            if (eval > alpha)
            { //found new best move
                alpha = eval;
                //Debug.Log("neues alpha: " + maxValue.ToString());
            }
        }
        return alpha;
    }


    public EngineMove ChooseMove(int player, int depth, int captureDepth = -1)
    {
        originalDepth = depth; //important
        searchCount = 0;
        Search(player, depth, negativeInfinity, positiveInfinity, captureDepth);
        currentSearch.currentSearchCount = searchCount;
        currentSearch.valuesChanged = true;
        return currentBestMove;
    }

    public void ThreadedMove()
    {
        Thread thread = new Thread(ChooseMove) { IsBackground = true };
        currentSearch.searchStarted = true;
        thread.Start();
    }

    public void ThreadedSearch()
    {
        Thread thread = new Thread(SearchEval) { IsBackground = true };
        thread.Start();
    }

    public void ChooseMove()
    {
        nextFoundMove = ChooseMove(manager.playerOnTurn, manager.engineDepth, manager.captureDepth);
        moveReady = true;
    }

    public void SearchEval()
    {
        ChooseMove(manager.playerOnTurn, manager.engineDepth);
        evalReady = true;
    }

    public EngineMove ChooseRandomMove(int player)
    {
        var allMoves = GetMoveset(player);
        if (allMoves.Count == 0) //checkmate or draw 
        {
            manager.gameEnd.Invoke();
            return new EngineMove(0, 0, 0); // maybe dont... hides bugs
        }
        int moveIndex = Random.Range(0, allMoves.Count);
        return allMoves[moveIndex];
    }

    public List<EngineMove> GetMoveset(int player) // no repetition avoidance
    {
        var output = new List<EngineMove>();
        for (int space = 0; space<64;space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                foreach (int endSpace in moveGenerator.GetLegalMovesForPiece(space).GetActive())
                {
                    output.Add(new EngineMove(currentPiece, space, endSpace));
                }
            }
        }
        return output;
    }

    public List<EngineMove> GetOrderedMoveset(int player) // TODO make evalMove function to sort these
    {
        var moves = new List<EngineMove>();
        for (int space = 0; space < 64; space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                BitBoard pieceMoveset = moveGenerator.GetLegalMovesForPiece(space);
                if ((ulong)pieceMoveset == 0) continue;
                foreach (int endSpace in pieceMoveset.GetActive())
                {
                    var newMove = new EngineMove(currentPiece, space, endSpace);
                    bool capture = moveGenerator.board.fullSpaces[endSpace];
                    int eval = evaluator.EvaluateMove(newMove);
                    newMove.Eval = eval;
                    moves.Add(newMove);
                }
            }
        }
        moves.Sort(EngineMove.CompareByEval);
        return moves;
    }

    public List<EngineMove> GetCaptures(int player)
    {
        var output = new List<EngineMove>();
        for (int space = 0; space < 64; space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                foreach (int endSpace in moveGenerator.GetLegalCapturesForPiece(space).GetActive())
                {
                    EngineMove nextMove = new EngineMove(currentPiece, space, endSpace);
                    nextMove.Eval = evaluator.EvaluateMove(nextMove);
                    output.Add(new EngineMove(currentPiece, space, endSpace));
                }
            }
        }
        output.Sort(EngineMove.CompareByEval);
        return output;
    }

    public int MoveGenCountTest(int depth, int playerToStart, bool reportMoves = false, ConsoleBehaviour console = null)
    {
        if (depth == 0) return 1;
        List<EngineMove> moves = GetMoveset(playerToStart);
        int output = 0, lastOutput = 0;
        foreach (EngineMove move in moves)
        {
            if(reportMoves) Debug.Log("I just made " + moveGenerator.MoveName(move.Start, move.End));
            UndoMoveData thisMove = moveGenerator.MovePiece(move.Start, move.End);
            lastOutput = output;
            output += MoveGenCountTest(depth - 1, playerToStart ^ 1);
            moveGenerator.UndoMovePiece(thisMove);
            if (depth == originalDepth) Debug.Log(moveGenerator.MoveName(move.Start, move.End, true) + "   " + (output-lastOutput).ToString("N0"));
            if (depth == originalDepth && console != null) console.Print(moveGenerator.MoveName(move.Start, move.End, true).PadRight(7) + (output - lastOutput).ToString("N0"));
        }

        return output;
    }

}
