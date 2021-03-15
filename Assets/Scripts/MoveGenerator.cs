using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ChessGameData
{
    public int playerOnTurn;
    public int epSpace;
    public bool[] castling;

    public ChessGameData(int nextPlayer, int enPassantSpace, bool[] castlingData)
    {
        playerOnTurn = nextPlayer;
        epSpace = enPassantSpace;
        castling = castlingData;
    }
}

public class MoveGenerator
{
    public ChessBoard board;

    public static readonly int[] slideDirections = new int[] { 8, -8, 1, -1, 9, 7, -7, -9 };
    //up, down, right, left, ur, ul, dr, dl
    public static readonly int[][] pawnDirs = new int[][] { new int[] { 8, 7, 9 }, new int[] { -8, -9, -7} };
    public static readonly int[][] knightDirs = new int[][] { new int[] { 15, 17 }, new int[] { -15, -17 },new int[] { -6, 10 },new int[] { 6, -10 } };
    //up, down, right, left
    public static int[][] SpacesToEdge;

    public ChessGameData gameData;

    public BitBoard isSpaceAttackedByWhite, isSpaceAttackedByBlack;

    public int whiteKingPosition;
    public int blackKingPosition;

    public MoveGenerator()
    {
        board = new ChessBoard();
        gameData = new ChessGameData(ChessBoard.white, 0, new bool[] { true, true, true, true });
        GenerateSpacesToEdgeData();
        //just for testing in unity editor, does nothing on runtime
        FENHandler.FillFENDict();
    }

    //getters and initalisation
    static void GenerateSpacesToEdgeData()
    {
        SpacesToEdge = new int[64][];
        for (int i = 0; i < 64; i++)
        {
            int x = ChessBoard.SpaceX(i);
            int y = ChessBoard.SpaceY(i);
            var edgeData = new int[] { 7 - y, y, 7 - x, x, Mathf.Min(7 - y, 7 - x), Mathf.Min(7 - y, x), Mathf.Min(y, 7 - x), Mathf.Min(y, x) };
            SpacesToEdge[i] = edgeData;
        }
    }

    public string MoveName(int startSpace, int endSpace)
    {
        string output = "";
        if (board[startSpace] == 0) return "Tried to move from empty space";
        int piece = board[startSpace];
        int pieceType = ChessBoard.PieceType(piece);
        var testMoveForCheck = MovePiece(startSpace, endSpace);
        bool leadsToCheck = IsPlayerInCheck(gameData.playerOnTurn ^ 1);
        UndoMovePiece(testMoveForCheck);
        if (pieceType == 6)
        {
            if (startSpace - endSpace == -2) return "0-0";
            if (startSpace - endSpace == 2) return "0-0-0";
        }
        if (board[endSpace] == 0)
        {
            output = ChessBoard.PieceLetter(piece).ToString() + ChessBoard.SpaceName(endSpace);
        }
        else
        {
            output = ChessBoard.PieceLetter(piece).ToString() + "x" + ChessBoard.SpaceName(endSpace);
        }
        if (leadsToCheck)
        {
            output += "+";
        }
        return output;
    }

    public void LoadFEN(string fenNotation)
    {
        board = FENHandler.ReadFEN(fenNotation);
        gameData = FENHandler.GameDataFromFEN(fenNotation);
        //SetAttackedSpaceData();
        SetKingPositions();
    }

    //pseudo legal movegen
    private List<int> GetSlideSpaces(int space, int pieceType, int range = 8)
    {
        int pieceOnNewSpaceColor, newSpace;
        int movingPieceColor = ChessBoard.PieceColor(board[space]); 
        int dirStart = (pieceType == ChessBoard.bishop) ? 4 : 0;
        int dirEnd = (pieceType == ChessBoard.rook) ? 4 : 8;
        List<int> output = new List<int>();
        for (int dirIndex = dirStart; dirIndex < dirEnd; dirIndex++)
        {
            for (int i = 0; i < SpacesToEdge[space][dirIndex]; i++)
            {
                newSpace = space + slideDirections[dirIndex] * (i + 1);
                pieceOnNewSpaceColor = ChessBoard.PieceColor(board[newSpace]);
                if (pieceOnNewSpaceColor == movingPieceColor || (i+1) > range)
                {
                    break;
                } else
                { 
                    output.Add(newSpace); 
                    if (pieceOnNewSpaceColor != -1)
                    {
                        break;
                    }
                } 
            }
        }
        return output;
    }

    private List<int> GetPawnSpaces(int space)
    {
        int newSpace, newSpacePieceColor, doublePushRow;
        List<int> output = new List<int>();
        int pieceColor = ChessBoard.PieceColor(board[space]);
        newSpace = space + pawnDirs[pieceColor][0];
        newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
        if(newSpacePieceColor == -1)
        {
            output.Add(newSpace);
            doublePushRow = (pieceColor == ChessBoard.white) ? 1 : 6;
            if (ChessBoard.SpaceY(space) == doublePushRow)
            {
                newSpace += pawnDirs[pieceColor][0];
                newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
                if (newSpacePieceColor == -1)
                {
                    output.Add(newSpace);
                }
            }
        }
        output.AddRange(GetSpacesAttackedByPawn(space));
        return output;
    }

    //useful for finding attacked spaces
    private List<int> GetSpacesAttackedByPawn(int space)
    {
        int newSpace, newSpacePieceColor;
        List<int> output = new List<int>();
        int pieceColor = ChessBoard.PieceColor(board[space]);
        for (int dirIndex = 1; dirIndex < 3; dirIndex++)
        {
            if (SpacesToEdge[space][dirIndex + 1] >= 1)
            {
                newSpace = space + pawnDirs[pieceColor][dirIndex];
                newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
                if (newSpacePieceColor != pieceColor)
                {
                    output.Add(newSpace);
                }
            }
        }
        return output;
    }

    private List<int> GetKnightSpaces(int space)
    {
        int newSpacePieceColor, newSpace, deltaX;
        List<int> output = new List<int>();
        int pieceColor = ChessBoard.PieceColor(board[space]);
        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            if (SpacesToEdge[space][directionIndex] >= 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    newSpace = space + knightDirs[directionIndex][i];
                    if (newSpace >= 0 && newSpace < 64)
                    {
                        newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
                        deltaX = Mathf.Abs(ChessBoard.SpaceX(newSpace) - ChessBoard.SpaceX(space));
                        if (newSpacePieceColor != pieceColor && (deltaX == 1 || deltaX == 2))
                        {
                            output.Add(newSpace);
                        }
                    }
                }
            }
        }
        return output;
    }

    //completly useless wrappers, kept for completeness
    private List<int> GetBishopSpaces(int space)
    {
        return GetSlideSpaces(space, 3);
    }

    private List<int> GetRookSpaces(int space)
    {
        return GetSlideSpaces(space, 4);
    }

    private List<int> GetQueenSpaces(int space)
    {
        return GetSlideSpaces(space, 5);
    }

    private List<int> GetKingSpaces(int space)
    {
        return GetSlideSpaces(space, 6, 1);
    }

    public List<int> GetPossibleSpacesForPiece(int space)
    {
        int pieceType = ChessBoard.PieceType(board[space]);
        if (pieceType == ChessBoard.pawn) return GetPawnSpaces(space);
        else if (ChessBoard.bishop <= pieceType && pieceType <= ChessBoard.queen) return GetSlideSpaces(space, pieceType);
        else if (pieceType == ChessBoard.knight) return GetKnightSpaces(space);
        else if (pieceType == ChessBoard.king) return (GetSlideSpaces(space, ChessBoard.king, 1));
        else return new List<int>();
    }

    //legal movegen and prerequisites

    public List<int> GetAttackedSpaces(int player)
    {
        int currentPiece;
        var output = new List<int>();
        //Maybe you shold make this faster...
        for (int space = 0; space < 64; space++)
        {
            if (board.fullSpaces[space])
            {
                currentPiece = board[space];
                if (ChessBoard.PieceColor(currentPiece) == player)
                {
                    if (ChessBoard.PieceType(currentPiece) == ChessBoard.pawn)
                    {
                        output.AddRange(GetSpacesAttackedByPawn(space));
                    }
                    else if (currentPiece != 0)
                    {
                        output.AddRange(GetPossibleSpacesForPiece(space));
                    }
                }
            }
        }
        return output;
    }

    BitBoard GenerateAttackedSpaceBitboard(int player)
    {
        var output = new BitBoard();
        foreach (int attackedSpace in GetAttackedSpaces(player))
        {
            //Debug.Log(attackedSpace.ToString() + " is attacked");
            output[attackedSpace] = true;
        }
        return output;
    }

    void SetAttackedSpaceData()
    {
        for (int player = 0; player < 2; player++)
        {
            if (player == 0)
            {
                isSpaceAttackedByBlack = GenerateAttackedSpaceBitboard(0);
            }
            else if (player == 1)
            {
                isSpaceAttackedByWhite = GenerateAttackedSpaceBitboard(1);
            }
        }
    }

    void SetKingPositions()
    {
        for (int i = 0; i < 64; i++)
        {
            if (board[i] == ChessBoard.blackPiece + ChessBoard.king)
            {
                blackKingPosition = i;
            }
            else if (board[i] == ChessBoard.whitePiece + ChessBoard.king)
            {
                whiteKingPosition = i;
            }
        }
    }



    public bool IsPlayerInCheck(int player)
    {
        var bitboardToCheck = (player == ChessBoard.white) ? isSpaceAttackedByBlack : isSpaceAttackedByWhite;
        int kingPos = (player == ChessBoard.white) ? whiteKingPosition : blackKingPosition;
        return bitboardToCheck[kingPos];
    }

    public List<int> GetLegalMovesForPiece(int space)
    {
        var output = new List<int>();
        List<int> possibleSpaces = GetPossibleSpacesForPiece(space);
        int piece = board[space];
        int player = ChessBoard.PieceColor(piece);
        int pieceType = ChessBoard.PieceType(piece);
        int castlingRow = (player == ChessBoard.black) ? 7 : 0;
        bool shortCastlingValid, longCastlingValid;
        //Am I trying to do this as slow as possible???
        if (pieceType == ChessBoard.king && space == castlingRow * 8 + 4)
        {
            var attackedSpaces = GetAttackedSpaces(player ^ 1);
            var castlingSpaces = new int[] { 5 + 8 * castlingRow, 6 + 8 * castlingRow, 2 + 8 * castlingRow, 3 + 8 * castlingRow, 1 + 8 * castlingRow };
            if (gameData.castling[2 * player])
            {
                shortCastlingValid = true;
                for (int i = 0; i < 2; i++) // short castling
                {
                    if (board[castlingSpaces[i]] != 0) shortCastlingValid = false;
                    if (attackedSpaces.Contains(castlingSpaces[i])) shortCastlingValid = false;
                }
                if (shortCastlingValid) possibleSpaces.Add(space + 2);
            }
            if (gameData.castling[2 * player + 1])
            {
                longCastlingValid = true;
                if (board[castlingSpaces[4]] != 0) longCastlingValid = false;
                for (int i = 2; i < 4; i++) // long castling
                {
                    if (board[castlingSpaces[i]] != 0) longCastlingValid = false;
                    if (attackedSpaces.Contains(castlingSpaces[i])) longCastlingValid = false;
                }
                if (longCastlingValid) possibleSpaces.Add(space - 2);
            }
        }
        foreach (int newSpace in possibleSpaces)
        {
            bool invalidMove = false;
            if (pieceType == 1)
            {
                if (ChessBoard.SpaceX(space) != ChessBoard.SpaceX(newSpace) && board[newSpace] == 0)
                {
                    invalidMove = true;
                    if (newSpace == gameData.epSpace)
                    {
                        invalidMove = false;
                    }
                }
            }
            if (!invalidMove)
            {
                var move = MovePiece(space, newSpace);
                if (IsPlayerInCheck(player))
                {
                    invalidMove = true;
                }
                UndoMovePiece(move);
            }
            if (!invalidMove)
            {
                output.Add(newSpace);
            }
        }
        return output;
    }

    //moving pieces and undoing moves

    public List<int[]> MovePiece(int start, int end)
    {
        int color = ChessBoard.PieceColor(board[start]);
        var previousPositions = new List<int[]>
        {
            new int[] { 0, 0, 0, 0, 0 }, // Bits for castling, last number is for ep
            new int[] { start, board[start] },
            new int[] { end, board[end] }
        };
        int castlingIndex = color * 2;
        int currentPieceType = ChessBoard.PieceType(board[start]);
        gameData.epSpace = 0;
        if (currentPieceType == ChessBoard.king)
        { // piece is a king

            int castlingRow = ChessBoard.SpaceY(end);
            int deltaX = end - start;
            int rookOld, rookNew;
            if (color == ChessBoard.black) blackKingPosition = end;
            else if (color == ChessBoard.white) whiteKingPosition = end;
            if (Mathf.Abs(deltaX) == 2) // castling
            {//TODO THERE IS THE BUG IM NOT SETTING CASTLING POS      fuck thats not true, just read over that
                if (deltaX == 2)
                { // short castling
                    rookOld = 7 + 8 * castlingRow;
                    rookNew = 5 + 8 * castlingRow;
                }
                else
                { // long castling
                    rookOld = 0 + 8 * castlingRow;
                    rookNew = 3 + 8 * castlingRow;
                }
                previousPositions.Add(new int[] { rookOld, board[rookOld] });
                previousPositions.Add(new int[] { rookNew, board[rookNew] });
                //moves rook while castling, faster than just board[rookNew]
                if (color == ChessBoard.white) { 
                    board.piecePositionBoards[ChessBoard.rook - 1][rookNew] = true;
                    board.piecePositionBoards[ChessBoard.rook - 1][rookOld] = false;
                    board.fullSpaces[rookNew] = true;
                    board.fullSpaces[rookOld] = false;
                } else
                {
                    board.piecePositionBoards[ChessBoard.rook - 1 + 6][rookNew] = true;
                    board.piecePositionBoards[ChessBoard.rook - 1 + 6][rookOld] = false;
                    board.fullSpaces[rookNew] = true;
                    board.fullSpaces[rookOld] = false;
                }
            }
            foreach (int index in new int[] { castlingIndex, castlingIndex + 1 })
            {
                gameData.castling[index] = false;
                previousPositions[0][index] = 1;
            }
        }
        else if (currentPieceType == ChessBoard.rook)
        { // piece is a rook
            if (ChessBoard.SpaceX(start) == 7)
            {
                gameData.castling[castlingIndex] = false;
                previousPositions[0][castlingIndex] = 1;
            }
            else if (ChessBoard.SpaceX(start) == 0)
            {
                gameData.castling[castlingIndex + 1] = false;
                previousPositions[0][castlingIndex] = 1;
            }
        }
        else if (currentPieceType == ChessBoard.pawn)
        { // piece is a pawn
            if (end == gameData.epSpace && gameData.epSpace != 0)
            {
                int spaceToTake = (ChessBoard.PieceColor(board[start]) == ChessBoard.black) ? end + 8 : end - 8;
                previousPositions.Add(new int[] { spaceToTake, board[spaceToTake] });
                board[spaceToTake] = 0;
            }
            if (Mathf.Abs(start / 8 - end / 8) == 2)
            {
                gameData.epSpace = (ChessBoard.PieceColor(board[start]) == ChessBoard.black) ? end + 8 : end - 8;
            }
        }
        previousPositions[0][4] = gameData.epSpace;

        board[end] = board[start];
        board[start] = 0; // Making the actual move

        if (end / 8 == 7 && currentPieceType == ChessBoard.pawn)
        {
            board[end] = ChessBoard.white + ChessBoard.queen; // White pawn becomes white queen
        }
        else if (end / 8 == 0 && currentPieceType == ChessBoard.pawn)
        {
            board[end] = ChessBoard.black + ChessBoard.queen; // Black pawn becomes black queen
        }
        //ultra mega super slow
        SetAttackedSpaceData();
        SetKingPositions();
        return previousPositions;
    }

    public void UndoMovePiece(List<int[]> prevPos)
    {
        int prevPosCount = prevPos.Count;
        int movedPiece = prevPos[1][1];
        gameData.epSpace = prevPos[0][4];
        for (int i = 0; i < 4; i++)
        {
            if (prevPos[0][i] == 1)
            {
                gameData.castling[i] = true;
            }
        }
        for (int i = 1; i < prevPosCount; i++)
        {
            board[prevPos[i][0]] = prevPos[i][1];
        }
        //if (movedPiece == ChessBoard.white + ChessBoard.king) whiteKingPosition = prevPos[1][0]; //white king
        //else if (movedPiece == ChessBoard.black + ChessBoard.king) blackKingPosition = prevPos[1][0]; // black king
        SetKingPositions();
    }


}