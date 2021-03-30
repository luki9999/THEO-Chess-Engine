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
    public int Eval;
    public Move(int piece, int start, int end, int eval = 0)
    {
        Piece = piece;
        Start = start;
        End = end;
        Eval = eval;
    }

    public static int CompareByEval(Move move1, Move move2)
    {
        return move1.Eval.CompareTo(move2.Eval);
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
    public bool evalReady = false;
    public SearchData currentSearch;
    Move currentBestMove;
    public Move nextFoundMove;

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
        currentSearch = new SearchData(0, 0, "");
    }

    public static int OtherPlayer(int player)
    {
        return (player == ChessBoard.white) ? ChessBoard.black : ChessBoard.white;
    }

    int MaterialValue()
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

    int MaterialSum()
    {
        int output = 0;
        for (int i = 0; i < 64; i++)
        {
            int currentPiece = moveGenerator.board[i];
            int currentValue = pieceValues[ChessBoard.PieceType(currentPiece)];
            output += currentValue;
        }
        return output;
    }

    int BonusValue()
    {
        int output = 0;
        bool endgame = MaterialSum() < endgameThreshold;
        foreach (int piece in ChessBoard.possiblePieces)
        {
            foreach (int space in moveGenerator.board.FindPieces(piece))
            {
                int colorSign = ChessBoard.PieceColor(piece) == ChessBoard.black ? -1 : 1;
                int spaceValue = PieceBonusTable.Read(piece, space, endgame);
                output += spaceValue * colorSign;
            }
        }
        return output;
    }

    int EndgameKingDistanceBonus()
    {
        int whiteBonus = 0, blackBonus = 0;
        foreach (int piece in ChessBoard.possiblePieces)
        {
            if (ChessBoard.PieceType(piece) == ChessBoard.pawn || ChessBoard.PieceType(piece) == ChessBoard.king) continue; //ignore pawns and kings
            foreach (int space in moveGenerator.board.FindPieces(piece))
            {
                if (ChessBoard.PieceColor(piece) == ChessBoard.white)
                {
                    whiteBonus += 7 - ChessBoard.Distance(piece, moveGenerator.blackKingPosition);
                } 
                else if(ChessBoard.PieceColor(piece) == ChessBoard.black)
                {
                    blackBonus += 7 - ChessBoard.Distance(piece, moveGenerator.whiteKingPosition);
                }
            }
        }
        whiteBonus = (whiteBonus * endgamePieceDistanceBonusMultiplier) / moveGenerator.board.WhitePieceCount();
        blackBonus = (blackBonus * endgamePieceDistanceBonusMultiplier) / moveGenerator.board.BlackPieceCount();
        int bonusForDistanceBetweenKings = (7 - ChessBoard.Distance(moveGenerator.blackKingPosition, moveGenerator.whiteKingPosition)) * endgamePieceDistanceBonusMultiplier;
        return ((whiteBonus - blackBonus)) + bonusForDistanceBetweenKings;
    }

    public int EvalPosition(int player)
    {
        int eval = MaterialValue();
        bool endgame = MaterialSum() <= endgameThreshold;
        if (moveGenerator.IsPlayerInCheck(player)) eval -= checkBonus;
        if (moveGenerator.IsPlayerInCheck(player ^ 1)) eval += checkBonus;
        eval += BonusValue();
        if (endgame) eval += EndgameKingDistanceBonus();
        return (player == ChessBoard.white) ? eval : -eval;
    }
     

    public int Search(int player, int depth, int alpha, int beta)
    {
        searchCount++;
        if (depth == 0) return CaptureSearch(player, alpha, beta);
        List<Move> moves = GetOrderedMoveset(player);
        if (moves.Count == 0) 
        {
            if(moveGenerator.IsPlayerInCheck(player)) return -10000; //Checkmate 
            return 0; // draw
        }
        foreach (Move move in moves)
        {
            var madeMove = moveGenerator.MovePiece(move.Start, move.End);
            int eval = -Search(player ^ 1, depth - 1, -beta, -alpha);
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

                    currentSearch.currentBestMove = moveGenerator.MoveName(move.Start, move.End); // usable for displaying in console
                    currentSearch.currentSearchCount = searchCount;
                    currentSearch.currentBestEval = alpha;
                    currentSearch.valuesChanged = true;
               }
            }
        }

        return alpha;
    }


    public int CaptureSearch(int player, int alpha, int beta)
    {
        searchCount++;
        int eval = EvalPosition(player);
        if (eval >= beta)
        {
            return beta;
        }
        if (eval > alpha)
        { //in case the pos is good but all captures are bad
            alpha = eval;
        }
        //searchCount++;
        List<Move> moves = GetCaptures(player);

        for (int i = 0; i < moves.Count; i++)
        {
            var madeMove = moveGenerator.MovePiece(moves[i].Start, moves[i].End);
            eval = -CaptureSearch(player ^ 1, -beta, -alpha);
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


    public Move ChooseMove(int player, int depth)
    {
        originalDepth = depth; //important
        searchCount = 0;
        Search(player, depth, negativeInfinity, positiveInfinity);
        currentSearch.currentSearchCount = searchCount;
        currentSearch.valuesChanged = true;

        return currentBestMove;
    }

    public void ThreadedMove()
    {
        Thread thread = new Thread(ChooseMove) { IsBackground = true };
        thread.Start();
    }

    public void ThreadedSearch()
    {
        Thread thread = new Thread(SearchEval) { IsBackground = true };
        thread.Start();
    }

    public void ChooseMove()
    {
        nextFoundMove = ChooseMove(manager.playerOnTurn, manager.engineDepth);
        moveReady = true;
    }

    public void SearchEval()
    {
        ChooseMove(manager.playerOnTurn, manager.engineDepth);
        evalReady = true;
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

    public List<Move> GetMoveset(int player) // no repetition avoidance
    {
        var output = new List<Move>();
        for (int space = 0; space<64;space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                foreach (int endSpace in moveGenerator.GetLegalMovesForPiece(space).GetActive())
                {
                    output.Add(new Move(currentPiece, space, endSpace));
                }
            }
        }
        return output;
    }

    public List<Move> GetOrderedMoveset(int player) // TODO make evalMove function to sort these
    {
        var quiets = new List<Move>();
        var captures = new List<Move>();
        for (int space = 0; space < 64; space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                BitBoard pieceMoveset = moveGenerator.GetLegalMovesForPiece(space);
                if ((ulong)pieceMoveset == 0) continue;
                foreach (int endSpace in pieceMoveset.GetActive())
                {
                    bool capture = moveGenerator.board.fullSpaces[endSpace];
                    //var testMove = moveGenerator.MovePiece(space, endSpace);
                    bool skipMove = manager.positionHistory.Contains(moveGenerator.board.Hash()); //avoids repeated positions completely
                    //int eval = -EvalPosition(player); // that minus is important
                    //moveGenerator.UndoMovePiece(testMove);
                    if (skipMove) continue;
                    if (capture)
                    {
                        captures.Add(new Move(currentPiece, space, endSpace));
                    }
                    else
                    {
                        quiets.Add(new Move(currentPiece, space, endSpace));
                    }
                }
            }
        }
        captures.AddRange(quiets);
        return captures;
    }

    public List<Move> GetCaptures(int player)
    {
        var output = new List<Move>();
        for (int space = 0; space < 64; space++)
        {
            int currentPiece = moveGenerator.board[space];
            if (ChessBoard.PieceColor(currentPiece) == player)
            {
                foreach (int endSpace in moveGenerator.GetLegalCapturesForPiece(space).GetActive())
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
            if (depth == originalDepth && console != null) console.Print(moveGenerator.MoveName(move.Start, move.End, true).PadRight(7) + (output - lastOutput).ToString("N0"));
        }

        return output;
    }

}
