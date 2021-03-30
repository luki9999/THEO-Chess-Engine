using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using System;

using static ChessBoard;

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

    public const int shortCastlingWhite = 0, longCastlingWhite = 1, shortCastlingBlack = 2, longCastlingBlack = 3;

    public static readonly int[] rooksBeforeCastling = new int[] { 7, 0, 63, 56 };
    public static readonly int[] rooksAfterCastling = new int[] { 5, 3, 61, 59 };

    static readonly int[] slideDirections = new int[] { 8, -8, 1, -1, 9, 7, -7, -9 };
    //up, down, right, left, ur, ul, dr, dl
    static readonly int[][] pawnDirs = new int[][] { new int[] { 8, 9, 7 }, new int[] { -8, -7, -9} };
    static readonly int[][] knightDirs = new int[][] { new int[] { 15, 17 }, new int[] { -15, -17 },new int[] { -6, 10 },new int[] { 6, -10 } };
    //up, down, right, left
    static int[][] SpacesToEdge;

    public ChessGameData gameData;

    public BitBoard isSpaceAttackedByWhite, isSpaceAttackedByBlack;

    public int whiteKingPosition;
    public int blackKingPosition;


    public MoveGenerator()
    {
        board = new ChessBoard();
        gameData = new ChessGameData(white, 0, new bool[] { true, true, true, true });
        GenerateSpacesToEdgeData();
    }

    //getters and initalisation
    static void GenerateSpacesToEdgeData()
    {
        SpacesToEdge = new int[64][];
        for (int i = 0; i < 64; i++)
        {
            int x = SpaceX(i);
            int y = SpaceY(i);
            var edgeData = new int[] { 7 - y, y, 7 - x, x, Mathf.Min(7 - y, 7 - x), Mathf.Min(7 - y, x), Mathf.Min(y, 7 - x), Mathf.Min(y, x) };
            SpacesToEdge[i] = edgeData;
        }
    }

    public string MoveName(int startSpace, int endSpace, bool longNotation = false)
    {
        string output;
        if (board[startSpace] == 0) return "Tried to move from empty space";
        int piece = board[startSpace];
        int pieceType = PieceType(piece);
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
            output = PieceLetter(piece).ToString() + SpaceName(endSpace);
        }
        else
        {
            output = PieceLetter(piece).ToString() + "x" + SpaceName(endSpace);
        }
        if (leadsToCheck)
        {
            output += "+";
        }
        if (longNotation)
        {
            output = SpaceName(startSpace) + SpaceName(endSpace);
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
    private BitBoard GetSlideSpaces(int space, int pieceType, int range = 8, bool capturesOnly = false)
    {
        int pieceOnNewSpaceColor, newSpace;
        int movingPieceColor = PieceColor(board[space]); 
        int dirStart = (pieceType == bishop) ? 4 : 0;
        int dirEnd = (pieceType == rook) ? 4 : 8;
        BitBoard output = new BitBoard(0);
        for (int dirIndex = dirStart; dirIndex < dirEnd; dirIndex++)
        {
            for (int i = 0; i < SpacesToEdge[space][dirIndex]; i++)
            {
                newSpace = space + slideDirections[dirIndex] * (i + 1);
                pieceOnNewSpaceColor = PieceColor(board[newSpace]);
                if (pieceOnNewSpaceColor == movingPieceColor || (i+1) > range)
                {
                    break;
                } else
                { 
                    output[newSpace] = true && !capturesOnly; 
                    if (pieceOnNewSpaceColor != -1)
                    {
                        output[newSpace] = true;
                        break;
                    }
                } 
            }
        }
        return output;
    }


    private BitBoard GetPawnSpaces(int space, bool capturesOnly = false)
    {
        int newSpace, newSpacePieceColor, doublePushRow;
        var output = new BitBoard();
        int pieceColor = PieceColor(board[space]);
        newSpace = space + pawnDirs[pieceColor][0];
        newSpacePieceColor = PieceColor(board[newSpace]);
        if(newSpacePieceColor == -1 && !capturesOnly)
        {
            output[newSpace] = true;
            doublePushRow = (pieceColor == white) ? 1 : 6;
            if (SpaceY(space) == doublePushRow)
            {
                newSpace += pawnDirs[pieceColor][0];
                newSpacePieceColor = PieceColor(board[newSpace]);
                if (newSpacePieceColor == -1)
                {
                    output[newSpace] = true;
                }
            }
        }
        foreach (int possibleCapture in GetSpacesAttackedByPawn(space).GetActive())
        {
            if(PieceColor((board[possibleCapture])) == (pieceColor ^ 0b1) || possibleCapture == gameData.epSpace)
            {
                output[possibleCapture] = true;
            }
        }
        return output;
    }

    //useful for finding attacked spaces
    private BitBoard GetSpacesAttackedByPawn(int space)
    {
        int newSpace, newSpacePieceColor;
        var output = new BitBoard();
        int pieceColor = PieceColor(board[space]);
        for (int dirIndex = 1; dirIndex < 3; dirIndex++)
        {
            if (SpacesToEdge[space][dirIndex + 1] >= 1)
            {
                newSpace = space + pawnDirs[pieceColor][dirIndex];
                newSpacePieceColor = PieceColor(board[newSpace]);
                if (newSpacePieceColor != pieceColor)
                {
                    output[newSpace] = true;
                }
            }
        }
        return output;
    }

    private BitBoard GetKnightSpaces(int space, bool capturesOnly = false)
    {
        int newSpacePieceColor, newSpace, deltaX;
        var output = new BitBoard();
        int pieceColor = PieceColor(board[space]);
        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            if (SpacesToEdge[space][directionIndex] >= 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    newSpace = space + knightDirs[directionIndex][i];
                    if (newSpace >= 0 && newSpace < 64)
                    {
                        newSpacePieceColor = PieceColor(board[newSpace]);
                        deltaX = Mathf.Abs(SpaceX(newSpace) - SpaceX(space));
                        if (newSpacePieceColor != pieceColor && (deltaX == 1 || deltaX == 2))
                        {
                            output[newSpace] = ((newSpacePieceColor == -1) && (!capturesOnly)) || (newSpacePieceColor == (pieceColor ^ 1));
                        }
                    }
                }
            }
        }
        return output;
    }


    //completly useless wrappers, kept for completeness
    private BitBoard GetBishopSpaces(int space)
    {
        return GetSlideSpaces(space, 3);
    }

    private BitBoard GetRookSpaces(int space)
    {
        return GetSlideSpaces(space, 4);
    }

    private BitBoard GetQueenSpaces(int space)
    {
        return GetSlideSpaces(space, 5);
    }

    private BitBoard GetKingSpaces(int space)
    {
        return GetSlideSpaces(space, 6, 1);
    }

    public BitBoard GetPossibleSpacesForPiece(int space, bool possibleCapturesOnly = false)
    {
        int pieceType = PieceType(board[space]);
        if (pieceType == pawn) return GetPawnSpaces(space, capturesOnly: possibleCapturesOnly);
        else if (bishop <= pieceType && pieceType <= queen) return GetSlideSpaces(space, pieceType, capturesOnly: possibleCapturesOnly);
        else if (pieceType == knight) return GetKnightSpaces(space, capturesOnly: possibleCapturesOnly);
        else if (pieceType == king) return GetSlideSpaces(space, king, range: 1, capturesOnly: possibleCapturesOnly);
        else return new BitBoard(0);
    }

    //legal movegen and prerequisites

    public BitBoard GenerateAttackedSpaceBitboard(int player)
    {
        int currentPiece;
        var output = new BitBoard();
        foreach (int space in board.fullSpaces.GetActive())
        {
            currentPiece = board[space];
            if (PieceColor(currentPiece) == player)
            {
                if (PieceType(currentPiece) == pawn)
                {
                    output += GetSpacesAttackedByPawn(space);
                }
                else
                {
                    output += GetPossibleSpacesForPiece(space);
                }
            }
        }
        return output;
    }

    void SetAttackedSpaceData()
    {
        isSpaceAttackedByBlack = GenerateAttackedSpaceBitboard(black);
        isSpaceAttackedByWhite = GenerateAttackedSpaceBitboard(white);
    }

    void SetKingPositions()
    {
        for (int i = 0; i < 64; i++)
        {
            if (board.Contains(i, blackPiece | king))
            {
                blackKingPosition = i;
                break;
            }
        }
        for (int i = 0; i < 64; i++) // looks bad, but breaking the loop after finding the kings should be faster
        {
            if (board.Contains(i, whitePiece | king))
            {
                whiteKingPosition = i;
                break;
            }
        }
    }

    int KingPosition(int player)
    {
        return (player == white) ? whiteKingPosition : blackKingPosition;
    }

    
    public bool IsPlayerInCheck(int player)
    {
        var bitboardToCheck = (player == white) ? isSpaceAttackedByBlack : isSpaceAttackedByWhite;
        return bitboardToCheck[KingPosition(player)];
    }

    public BitBoard GetLegalMovesForPiece(int space)
    {
        //variables
        int piece = board[space];
        int player = PieceColor(piece);
        int pieceType = PieceType(piece);
        var output = GetPossibleSpacesForPiece(space);

        //castling
        int castlingRow = (player == black) ? 7 : 0;
        bool shortCastlingValid, longCastlingValid;
        if (pieceType == king && space == castlingRow * 8 + 4)
        {
            BitBoard attackedSpaces = GenerateAttackedSpaceBitboard(player ^ 1);
            var castlingSpaces = new int[] { 5 + 8 * castlingRow, 6 + 8 * castlingRow, 2 + 8 * castlingRow, 3 + 8 * castlingRow, 1 + 8 * castlingRow };
            bool castlingPossible = !IsPlayerInCheck(player);
            if (gameData.castling[2 * player] && castlingPossible)
            {
                shortCastlingValid = true;
                for (int i = 0; i < 2; i++) // short castling
                {
                    if (board.fullSpaces[castlingSpaces[i]]) shortCastlingValid = false; //cant castle is pieces are in the way
                    if (attackedSpaces[castlingSpaces[i]]) shortCastlingValid = false; //cant castle through check
                    if (!shortCastlingValid) break;
                }
                if (shortCastlingValid) output[space + 2] = true;
            }
            if (gameData.castling[2 * player + 1] && castlingPossible)
            {
                longCastlingValid = true;
                if (board.fullSpaces[castlingSpaces[4]]) longCastlingValid = false;
                for (int i = 2; i < 4; i++) // long castling
                {
                    if (!longCastlingValid) break;
                    if (board.fullSpaces[castlingSpaces[i]]) longCastlingValid = false;
                    if (attackedSpaces[castlingSpaces[i]]) longCastlingValid = false;
                }
                if (longCastlingValid) output[space - 2] = true;
            }
        }

        //checking legality
        foreach (int newSpace in output.GetActive())
        {
            bool invalidMove = false;
            List<int[]> move = MovePiece(space, newSpace);
            if (IsPlayerInCheck(player))
            {
                invalidMove = true;
            }
            UndoMovePiece(move);
            if (invalidMove)
            {
                output[newSpace] = false;
            }
        }
        //Debug.Log("There were " + output.Count.ToString() + " moves.");
        return output;
    }

    public BitBoard GetLegalCapturesForPiece(int space)
    {
        BitBoard output = GetPossibleSpacesForPiece(space, true);
        int piece = board[space];
        int player = PieceColor(piece);
        //no need for castling, thats not a capture :D
        foreach (int newSpace in output.GetActive())
        {
            bool invalidMove = false;
            List<int[]> move = MovePiece(space, newSpace);
            if (IsPlayerInCheck(player))
            {
                invalidMove = true;
            }
            UndoMovePiece(move);
            if (invalidMove)
            {
                output[newSpace] = false;
            }
        }
        return output;
    }

    //moving pieces and undoing moves

    public List<int[]> MovePiece(int start, int end)
    {
        //variables
        int piece = board[start];
        int color = PieceColor(piece);
        int type = PieceType(piece);
        int capture = board[end];
        bool isCapture = (capture != 0);
        int castlingIndex = color * 2;
        var previousPositions = new List<int[]>
        {
            new int[] { 0, 0, 0, 0, 0 }, // Bits for castling, last number is for ep
            new int[] { start, board[start] },
            new int[] { end, board[end] }
        };


        //castling + castling prevention when king moved
        if (type == king)
        { 
            int deltaX = end - start;
            if (!isCapture && (deltaX == 2 || deltaX == -2)) //keeps castling code from running all the time
            {
                int rookOld = 0, rookNew = 0;
                int rook = ChessBoard.rook | ((color == white) ? whitePiece : blackPiece);
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
            if (color == black) blackKingPosition = end;
            else if (color == white) whiteKingPosition = end;
        }

        //castling prevention after rook moved
        else if (type == rook)
        { 
            if (SpaceX(start) == 7 && gameData.castling[castlingIndex])
            {
                gameData.castling[castlingIndex] = false;
                previousPositions[0][castlingIndex] = 1;
            }
            else if (SpaceX(start) == 0 && gameData.castling[castlingIndex])
            {
                gameData.castling[castlingIndex + 1] = false;
                previousPositions[0][castlingIndex + 1] = 1;
            }
        }

        //taking the pawn in case of e.p.
        else if (type == pawn)
        { 
            if (end == gameData.epSpace && gameData.epSpace != 0)
            {
                int spaceToTake = board.TakeEPPawn(end, color);
                previousPositions.Add(new int[] { spaceToTake, board[spaceToTake] });
            }
        }

        //prventing castling when rook needed for it was taken
        if (isCapture && (end == 7 || end == 0 || end == 63 || end == 56))
        {
            if (board.Contains(end, whitePiece | rook)) 
            {
                previousPositions[0][castlingIndex] = (gameData.castling[castlingIndex]) ? 1 : 0;
                previousPositions[0][castlingIndex + 1] = (gameData.castling[castlingIndex + 1]) ? 1 : 0; 
                gameData.castling[castlingIndex] = (!(end == 7)) && gameData.castling[castlingIndex];
                gameData.castling[castlingIndex + 1] = (!(end == 0)) && gameData.castling[castlingIndex + 1];
            }
            else if (board.Contains(end, blackPiece | rook)) // ugly repetition
            {
                previousPositions[0][castlingIndex] = (gameData.castling[castlingIndex]) ? 1 : 0;
                previousPositions[0][castlingIndex + 1] = (gameData.castling[castlingIndex + 1]) ? 1 : 0;
                gameData.castling[castlingIndex] = (!(end == 63)) && gameData.castling[castlingIndex];
                gameData.castling[castlingIndex + 1] = (!(end == 56)) && gameData.castling[castlingIndex + 1];
            }
        }

        //leave that like this! dont put it the if clause up there. IT WILL BREAK
        
        //setting the e.p space for the next move
        previousPositions[0][4] = gameData.epSpace;
        if (!isCapture && Mathf.Abs(start / 8 - end / 8) == 2 && type == pawn)
        {
            gameData.epSpace = (color == black) ? end + 8 : end - 8;
        }
        else
        {
            gameData.epSpace = 0;
        }

        // making the actual move
        if (!isCapture) 
        {
            board.MovePieceToEmptySpace(start, end, piece);
        }
        else
        {
            board.MovePieceToFullSpace(start, end, piece, capture);
        }

        // turning pawns to queens on promotion
        if ((SpaceY(end) == 7 || SpaceY(end) == 0) && type == pawn) // pawn of own color never will go backwards
        {
            board.TurnPawnToQueen(end, color);
            //board.UpdateFullSpaces(); //for safety, bugs out when i dont
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