using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static ChessBoard;

public class Evaluation
{
    MoveGenerator moveGenerator;

    readonly int[] pieceValues = new int[] {0, 100, 300, 300, 500, 900, 0};
    const int checkBonus = 30;
    const int endgamePieceDistanceBonusMultiplier = 5;
    const int endgameThreshold = 2 * 500 + 2 * 300 + 2 * 100; //two rooks, two pieces, two pawns or similar

    public Evaluation(MoveGenerator movegen){
        moveGenerator = movegen;
    }

    //static eval and prerequisites
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

    bool IsEndgame(){
        return MaterialSum() <= endgameThreshold;
    }

    int BonusValue()
    {
        int output = 0;
        bool endgame = IsEndgame();
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

    public int EvaluatePosition(int player) //static eval of given position from the players perspective
    {
        int eval = MaterialValue();
        bool endgame = IsEndgame();
        if (moveGenerator.IsPlayerInCheck(player)) eval -= checkBonus;
        if (moveGenerator.IsPlayerInCheck(player ^ 1)) eval += checkBonus; //SLOW: maybe test if this is worth it
        eval += BonusValue();
        if (endgame) eval += EndgameKingDistanceBonus();
        return (player == ChessBoard.white) ? eval : -eval;
    }

    //move evaluation, used for moveordering
    //move eval assumes the move has not been made yet
    public int CaptureDelta(EngineMove move) {
        int startValue = pieceValues[PieceType(move.Piece)];
        int capturedPiece = moveGenerator.board[move.End];
        int endValue = pieceValues[PieceType(capturedPiece)];
        if (endValue == 0) return -250; // base penalty for non capture moves, we first look at captures where we lose less than 2.5 pawns, then at non captures, then at the rest
        return endValue - startValue; // high values for taking good pieces with bad ones, negative for the reverse
    }

    public int PositionDelta(EngineMove move, bool endgame) {
        int before = PieceBonusTable.Read(move.Piece, move.Start,endgame);
        int after = PieceBonusTable.Read(move.Piece, move.End, endgame);
        return after - before; // high values for positioning pieces better, negative for worse positions
    }

    public int EvaluateMove(EngineMove move) {
        int eval = 0;
        bool endgame = IsEndgame();
        eval += CaptureDelta(move);
        eval += PositionDelta(move, endgame);
        return eval;
    }
}
