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
    public static readonly int[][] pawnDirs = new int[][] { new int[] { 8, 9, 7 }, new int[] { -8, -7, -9} };
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

    public string MoveName(int startSpace, int endSpace, bool longNotation = false)
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
        if (longNotation)
        {
            output = ChessBoard.SpaceName(startSpace) + ChessBoard.SpaceName(endSpace);
        }
        return output;
    }

    public void LoadFEN(string fenNotation)
    {
        board = FENHandler.ReadFEN(fenNotation);
        gameData = FENHandler.GameDataFromFEN(fenNotation);
        SetAttackedSpaceData();
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

    private List<int> GetSlideCaptures(int space, int pieceType, int range = 8)
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
                if (pieceOnNewSpaceColor == movingPieceColor || (i + 1) > range)
                {
                    break;
                }
                else if (pieceOnNewSpaceColor != -1)
                {
                    output.Add(newSpace);
                    break;
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
        foreach (int possibleCapture in GetSpacesAttackedByPawn(space))
        {
            if(ChessBoard.PieceColor((board[possibleCapture])) == (pieceColor ^ 0b1) || possibleCapture == gameData.epSpace)
            {
                output.Add(possibleCapture);
            }
        }
        return output;
    }

    private List<int> GetPawnCaptures(int space)
    {
        List<int> output = new List<int>();
        int pieceColor = ChessBoard.PieceColor(board[space]);
        foreach (int possibleCapture in GetSpacesAttackedByPawn(space))
        {
            if (ChessBoard.PieceColor((board[possibleCapture])) == (pieceColor ^ 0b1) || possibleCapture == gameData.epSpace)
            {
                output.Add(possibleCapture);
            }
        }
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

    private List<int> GetKnightCaptures(int space)
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
                        if (newSpacePieceColor == (pieceColor ^ 1) && (deltaX == 1 || deltaX == 2))
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
        else if (pieceType == ChessBoard.king) return GetSlideSpaces(space, ChessBoard.king, 1);
        else return new List<int>();
    }

    public List<int> GetPossibleCapturesForPiece(int space)
    {
        int pieceType = ChessBoard.PieceType(board[space]);
        if (pieceType == ChessBoard.pawn) return GetPawnCaptures(space);
        else if (ChessBoard.bishop <= pieceType && pieceType <= ChessBoard.queen) return GetSlideCaptures(space, pieceType);
        else if (pieceType == ChessBoard.knight) return GetKnightCaptures(space);
        else if (pieceType == ChessBoard.king) return GetSlideCaptures(space, ChessBoard.king, 1);
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
        isSpaceAttackedByBlack = GenerateAttackedSpaceBitboard(ChessBoard.black);
        isSpaceAttackedByWhite = GenerateAttackedSpaceBitboard(ChessBoard.white);
    }

    void SetKingPositions()
    {
        for (int i = 0; i < 64; i++)
        {
            if (board.Contains(i, ChessBoard.blackPiece | ChessBoard.king))
            {
                blackKingPosition = i;
                break;
            }
        }
        for (int i = 0; i < 64; i++) // looks bad, but breaking the loop after finding the kings should be faster
        {
            if (board.Contains(i, ChessBoard.whitePiece | ChessBoard.king))
            {
                whiteKingPosition = i;
                break;
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
        //Debug.Log("Getting moves for " + space.ToString());
        var output = new List<int>();
        List<int> possibleSpaces = GetPossibleSpacesForPiece(space);
        int piece = board[space];
        int player = ChessBoard.PieceColor(piece);
        int pieceType = ChessBoard.PieceType(piece);
        int castlingRow = (player == ChessBoard.black) ? 7 : 0;
        bool shortCastlingValid, longCastlingValid;
        if (pieceType == ChessBoard.king && space == castlingRow * 8 + 4)
        {
            var attackedSpaces = GetAttackedSpaces(player ^ 1);
            var castlingSpaces = new int[] { 5 + 8 * castlingRow, 6 + 8 * castlingRow, 2 + 8 * castlingRow, 3 + 8 * castlingRow, 1 + 8 * castlingRow };
            bool castlingPossible = !IsPlayerInCheck(player);
            if (gameData.castling[2 * player] && castlingPossible)
            {
                shortCastlingValid = true;
                for (int i = 0; i < 2; i++) // short castling
                {
                    if (board.fullSpaces[castlingSpaces[i]]) shortCastlingValid = false; //cant castle is pieces are in the way
                    if (attackedSpaces.Contains(castlingSpaces[i])) shortCastlingValid = false; //cant castle through check
                    if (!shortCastlingValid) break;
                }
                if (shortCastlingValid) possibleSpaces.Add(space + 2);
            }
            if (gameData.castling[2 * player + 1] && castlingPossible)
            {
                longCastlingValid = true;
                if (board.fullSpaces[castlingSpaces[4]]) longCastlingValid = false;
                for (int i = 2; i < 4; i++) // long castling
                {
                    if (!longCastlingValid) break;
                    if (board.fullSpaces[castlingSpaces[i]]) longCastlingValid = false;
                    if (attackedSpaces.Contains(castlingSpaces[i])) longCastlingValid = false;
                }
                if (longCastlingValid) possibleSpaces.Add(space - 2);
            }
        }
        foreach (int newSpace in possibleSpaces)
        {
            bool invalidMove = false;
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
        //Debug.Log("There were " + output.Count.ToString() + " moves.");
        return output;
    }

    public List<int> GetLegalCapturesForPiece(int space)
    {
        var output = new List<int>();
        List<int> possibleCaptures = GetPossibleCapturesForPiece(space);
        int piece = board[space];
        int player = ChessBoard.PieceColor(piece);
        //no need for castling, thats not a capture :D
        foreach (int newSpace in possibleCaptures)
        {
            bool invalidMove = false;
            var move = MovePiece(space, newSpace);
            if (IsPlayerInCheck(player))
            {
                invalidMove = true;
            }
            UndoMovePiece(move);
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
        int piece = board[start];
        int color = ChessBoard.PieceColor(piece);
        int type = ChessBoard.PieceType(piece);
        int capture = board[end];
        bool isCapture = (capture != 0);
        int castlingIndex = color * 2;
        var previousPositions = new List<int[]>
        {
            new int[] { 0, 0, 0, 0, 0 }, // Bits for castling, last number is for ep
            new int[] { start, board[start] },
            new int[] { end, board[end] }
        };

        if (type == ChessBoard.king)
        { // piece is a king
            int deltaX = end - start;
            if (!isCapture && (deltaX == 2 || deltaX == -2)) //keeps castling code from running all the time
            {
                int rookOld = 0, rookNew = 0;
                int rook = ChessBoard.rook | ((color == ChessBoard.white) ? ChessBoard.whitePiece : ChessBoard.blackPiece);
                if (deltaX == 2)
                { // short castling
                    rookOld = start + 3;
                    rookNew = start + 1;
                }
                else if (deltaX == -2)
                { // long castling
                    rookOld = start - 4;
                    rookNew = start - 1;
                }
                previousPositions.Add(new int[] { rookOld, board[rookOld] });
                previousPositions.Add(new int[] { rookNew, board[rookNew] });
                board.MovePieceToEmptySpace(rookOld, rookNew, rook);
            }
            foreach (int index in new int[] { castlingIndex, castlingIndex + 1 }) //no castling after moving kings
            {
                if (gameData.castling[index]) previousPositions[0][index] = 1;
                gameData.castling[index] = false;
            }
            if (color == ChessBoard.black) blackKingPosition = end;
            else if (color == ChessBoard.white) whiteKingPosition = end;
        }

        else if (type == ChessBoard.rook)
        { // piece is a rook
            if (ChessBoard.SpaceX(start) == 7 && gameData.castling[castlingIndex])
            {
                gameData.castling[castlingIndex] = false;
                previousPositions[0][castlingIndex] = 1;
            }
            else if (ChessBoard.SpaceX(start) == 0 && gameData.castling[castlingIndex])
            {
                gameData.castling[castlingIndex + 1] = false;
                previousPositions[0][castlingIndex + 1] = 1;
            }
        }

        else if (type == ChessBoard.pawn)
        { // piece is a pawn
            if (end == gameData.epSpace && gameData.epSpace != 0) // ep doesnt count as a capture
            {
                int spaceToTake = board.TakeEPPawn(end, color);
                previousPositions.Add(new int[] { spaceToTake, board[spaceToTake] });
            }
        }


        if (isCapture && (end == 7 || end == 0 || end == 63 || end == 56))
        {
            if (board.Contains(end, ChessBoard.whitePiece | ChessBoard.rook)) //rook needed for castling was taken 
            {
                previousPositions[0][castlingIndex] = (gameData.castling[castlingIndex]) ? 1 : 0;
                previousPositions[0][castlingIndex + 1] = (gameData.castling[castlingIndex + 1]) ? 1 : 0; 
                gameData.castling[castlingIndex] = (!(end == 7)) && gameData.castling[castlingIndex];
                gameData.castling[castlingIndex + 1] = (!(end == 0)) && gameData.castling[castlingIndex + 1];
            }
            else if (board.Contains(end, ChessBoard.blackPiece | ChessBoard.rook)) // ugly repetition
            {
                previousPositions[0][castlingIndex] = (gameData.castling[castlingIndex]) ? 1 : 0;
                previousPositions[0][castlingIndex + 1] = (gameData.castling[castlingIndex + 1]) ? 1 : 0;
                gameData.castling[castlingIndex] = (!(end == 63)) && gameData.castling[castlingIndex];
                gameData.castling[castlingIndex + 1] = (!(end == 56)) && gameData.castling[castlingIndex + 1];
            }
        }
        //leave that like this! dont put it the if clause up there. IT WILL BREAK

        previousPositions[0][4] = gameData.epSpace;
        if (!isCapture && Mathf.Abs(start / 8 - end / 8) == 2 && type == ChessBoard.pawn)
        {
            gameData.epSpace = (color == ChessBoard.black) ? end + 8 : end - 8;
        }
        else
        {
            gameData.epSpace = 0;
        }

        if (!isCapture) // making the actual move
        {
            board.MovePieceToEmptySpace(start, end, piece);
        }
        else
        {
            board.MovePieceToFullSpace(start, end, piece, capture);
        }


        if ((ChessBoard.SpaceY(end) == 7 || ChessBoard.SpaceY(end) == 0) && type == ChessBoard.pawn) // pawn of own color never will go backwards
        {
            board.TurnPawnToQueen(end, color);
            board.UpdateFullSpaces(); //for safety, bugs out when i dont
        }
        //ultra mega super slow
        SetAttackedSpaceData();
        //SetKingPositions();
        //board.UpdateFullSpaces();
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
        SetKingPositions();
        //dreckig und langsam
        board.UpdateFullSpaces();
    }


}