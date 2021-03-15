using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MoveGenerator
{
    public ChessBoard board;

    public static readonly int pawn = 1, knight = 2, bishop = 3, rook = 4, queen = 5, king = 6;
    public static readonly int white = 8, black = 16;
    public static readonly char[] pieceNames = new char[] {' ', 'N', 'B', 'R', 'Q', 'K' };

    public static readonly int[] slideDirections = new int[] { 8, -8, 1, -1, 9, 7, -7, -9 };
    //up, down, right, left, ur, ul, dr, dl
    public static readonly int[][] pawnDirs = new int[][] { new int[] { -8, -7, -9 }, new int[] { 8, 9, 7} };
    public static readonly int[][] knightDirs = new int[][] { new int[] { 15, 17 }, new int[] { -15, -17 },new int[] { -6, 10 },new int[] { 6, -10 } };
    //up, down, right, left
    public static int[][] SpacesToEdge;

    public int epSpace;

    public static Dictionary<string, int> fenParsing = new Dictionary<string, int>();
    public static bool fenParsingReady = false; //Oh no
    public static readonly char[] fileLetters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
    public static readonly char[] rankNumbers = new char[] { '1', '2', '3', '4', '5', '6', '7', '8' };

    public bool[] isSpaceAttackedByWhite = new bool[64];
    public bool[] isSpaceAttackedByBlack = new bool[64];


    public static int boardsMade;

    public int playerOnTurn = 1;

    public bool[] castling = new bool[4];

    public int whiteKingPosition;
    public int blackKingPosition;

    public MoveGenerator()
    {
        board = new ChessBoard();
        GenerateSpacesToEdgeData();
        FillFENDict();
    }

    //stays
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

    // -> ChessBoard
    public static string SpaceName(int space)
    {
        int x = ChessBoard.SpaceX(space);
        int y = ChessBoard.SpaceY(space);
        return fileLetters[x].ToString() + rankNumbers[y].ToString();
    }

    // -> ChessBoard
    public static char PieceLetter(int piece)
    {
        return pieceNames[ChessBoard.PieceType(piece)-1];
    }

    public string MoveName(int startSpace, int endSpace)
    {
        string output = "";
        if (board[startSpace] == 0) return "Tried to move from empty space";
        int piece = board[startSpace];
        int pieceType = ChessBoard.PieceType(piece);
        var testMoveForCheck = MovePiece(startSpace, endSpace);
        bool leadsToCheck = IsPlayerInCheck(OtherPlayer(playerOnTurn));
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
        return output;
    }
    // -> ChessBoard
    public static int OtherPlayer(int player)
    {
        return (player == 1) ? 0 : 1;
    }
    // -> ChessBoard
    public static int KoordsToInt(int x, int y)
    {
        return 8 * y + x;
    }

    public static int[] ReadFEN(string fenNotation)
    {
        int pieceToPlace;
        string[] fenGroups = fenNotation.Split(' ');
        string[] fenRows = fenGroups[0].Split('/');
        int[] output = new int[64];
        string currentRow;
        for (int y = 0; y < 8; y++)
        {
            currentRow = fenRows[y];
            int currentX = 0;
            foreach (char piece in currentRow)
            {
                if (char.IsDigit(piece))
                {
                    currentX += int.Parse(piece.ToString());
                    if (currentX >= 8)
                    {
                        break;
                    }

                } else
                {
                    pieceToPlace = fenParsing[piece.ToString()];
                    output[8 * (7 - y) + currentX] = pieceToPlace;
                    // Debug.Log(fenParsing[piece.ToString()].ToString() + " was placed at " + currentX.ToString() + " " + y.ToString());
                    currentX++;
                }
            }
        }
        return output;
    }

    public void SetGameDataFromFEN(string fenNotation)
    {
        string[] fenGroups = fenNotation.Split(' ');
        playerOnTurn = (fenGroups[1] == "w") ? 1 : 0;
        string castlingStr = fenGroups[2];
        foreach (char currentLetter in castlingStr)
        {
            switch (currentLetter){
                case 'k':
                    castling[0] = true;
                    break;
                case 'q':
                    castling[1] = true;
                    break;
                case 'K':
                    castling[2] = true;
                    break;
                case 'Q':
                    castling[3] = true;
                    break;
            }
        }
    }

    public void LoadFEN(string fenNotation)
    {
        int[] piecesToLoad = ReadFEN(fenNotation);
        for (int i = 0; i < 64; i++)
        {

            board[i] = piecesToLoad[i];
        }
        SetGameDataFromFEN(fenNotation);
        //SetPiecePositionList();
        //SetAttackedSpaceData();
        SetKingPositions();
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
            if (board[i] == ChessBoard.black + ChessBoard.king)
            {
                blackKingPosition = i;
            } else if (board[i] == ChessBoard.white + ChessBoard.king)
            {
                whiteKingPosition = i;
            }
        }
    }

    bool[] GenerateAttackedSpaceBitboard(int player)
    {
        var output = new bool[64];
        foreach (int attackedSpace in GetAttackedSpaces(player))
        {
            //Debug.Log(attackedSpace.ToString() + " is attacked");
            output[attackedSpace] = true;
        }
        return output;
    }

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
        epSpace = 0;
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
                } else
                { // long castling
                    rookOld = 0 + 8 * castlingRow;
                    rookNew = 3 + 8 * castlingRow;
                }
                previousPositions.Add(new int[] { rookOld, board[rookOld] });
                previousPositions.Add(new int[] { rookNew, board[rookNew] });
                board[rookNew] = board[rookOld];
                board[rookOld] = 0;
            }
            foreach (int index in new int[] { castlingIndex, castlingIndex + 1 })
            {
                castling[index] = false;
                previousPositions[0][index] = 1;
            }
        } else if (currentPieceType == ChessBoard.rook)
        { // piece is a rook
            if(ChessBoard.SpaceX(start) == 7)
            {
                castling[castlingIndex] = false;
                previousPositions[0][castlingIndex] = 1;
            } else if (ChessBoard.SpaceX(start) == 0)
            {
                castling[castlingIndex + 1] = false;
                previousPositions[0][castlingIndex] = 1;
            }
        } else if (currentPieceType == ChessBoard.pawn)
        { // piece is a pawn
            if (end == epSpace && epSpace != 0)
            {
                int spaceToTake = (ChessBoard.PieceColor(board[start]) == 0) ? end + 8 : end - 8;
                previousPositions.Add(new int[] { spaceToTake, board[spaceToTake] });
                board[spaceToTake] = 0;
            }
            if (Mathf.Abs(start / 8 - end / 8) == 2)
            {
                epSpace = (ChessBoard.PieceColor(board[start]) == 0) ? end + 8 : end - 8;
            }
        }
        previousPositions[0][4] = epSpace;

        board[end] = board[start];
        board[start] = 0; // Making the actual move

        if (ChessBoard.PieceType(board[end]) == ChessBoard.pawn && end / 8 == 7)
        {
            board[end] = ChessBoard.white + ChessBoard.queen; // White pawn becomes white queen
        }
        else if (ChessBoard.PieceType(board[end]) == ChessBoard.pawn && end / 8 == 0)
        {
            board[end] = ChessBoard.black + ChessBoard.queen; // Black pawn becomes black queen
        }
        SetAttackedSpaceData();
        return previousPositions;
    }

    public void UndoMovePiece(List<int[]> prevPos)
    {
        int prevPosCount = prevPos.Count;
        int movedPiece = prevPos[1][1];
        epSpace = prevPos[0][4];
        for (int i = 0; i < 4; i++)
        {
            if (prevPos[0][i] == 1)
            {
                castling[i] = true;
            }
        }
        for (int i = 1; i < prevPosCount; i++)
        {
            board[prevPos[i][0]] = prevPos[i][1];
        }
        if (movedPiece == ChessBoard.white + ChessBoard.king) whiteKingPosition = prevPos[1][0]; //white king
        else if (movedPiece == ChessBoard.black + ChessBoard.king) blackKingPosition = prevPos[1][0]; // black king
    }



    public List<int> GetPossibleSpacesForPiece(int space)
    {
        List<int> output;
        int pieceType = ChessBoard.PieceType(board[space]);
        //oh no
        switch (pieceType)
        {
            case 1:
                output = GetPawnSpaces(space);
                break;
            case 2:
                output = GetKnightSpaces(space);
                break;
            case 3:
                output = GetBishopSpaces(space);
                break;
            case 4:
                output = GetRookSpaces(space);
                break;
            case 5:
                output = GetQueenSpaces(space);
                break;
            case 6:
                output = GetKingSpaces(space);
                break;
            default:
                output = new List<int>();
                break;
        }
        return output;
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
            var attackedSpaces = GetAttackedSpaces(OtherPlayer(player));
            var castlingSpaces = new int[] { 5 + 8 * castlingRow, 6 + 8 * castlingRow, 2 + 8 * castlingRow, 3 + 8 * castlingRow, 1 + 8 * castlingRow };
            if (castling[2 * player])
            {
                shortCastlingValid = true;
                for (int i = 0; i < 2; i++) // short castling
                {
                    if (board[castlingSpaces[i]] != 0) shortCastlingValid = false;
                    if (attackedSpaces.Contains(castlingSpaces[i])) shortCastlingValid = false;
                }
                if (shortCastlingValid) possibleSpaces.Add(space + 2);
            } 
            if (castling[2* player + 1])
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
                    if (newSpace == epSpace)
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
        int newSpace, newSpacePieceColor;
        List<int> output = new List<int>();
        int pieceColor = ChessBoard.PieceColor(board[space]);
        newSpace = space + pawnDirs[pieceColor][0];
        newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
        if(newSpacePieceColor == -1)
        {
            output.Add(newSpace);
            if (ChessBoard.SpaceY(space) == (6 - (5 * pieceColor)))
            {
                newSpace = newSpace + pawnDirs[pieceColor][0];
                newSpacePieceColor = ChessBoard.PieceColor(board[newSpace]);
                if (newSpacePieceColor == -1)
                {
                    output.Add(newSpace);
                }
            }
        }
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

    public bool IsPlayerInCheck(int player)
    {
        var bitboardToCheck = (player == ChessBoard.white) ? isSpaceAttackedByBlack : isSpaceAttackedByWhite;
        int kingPos = (player == ChessBoard.white) ? whiteKingPosition : blackKingPosition;
        return bitboardToCheck[kingPos];
    }

    public static void FillFENDict()
    {
        if (!fenParsingReady) //Oh dont do shit like this oh god man
        {
            fenParsing.Add("p", ChessBoard.pawn + ChessBoard.blackPiece);
            fenParsing.Add("P", ChessBoard.pawn + ChessBoard.whitePiece);
            fenParsing.Add("n", ChessBoard.knight + ChessBoard.blackPiece);
            fenParsing.Add("N", ChessBoard.knight + ChessBoard.whitePiece);
            fenParsing.Add("b", ChessBoard.bishop + ChessBoard.blackPiece);
            fenParsing.Add("B", ChessBoard.bishop + ChessBoard.whitePiece);
            fenParsing.Add("r", ChessBoard.rook + ChessBoard.blackPiece);
            fenParsing.Add("R", ChessBoard.rook + ChessBoard.whitePiece);
            fenParsing.Add("q", ChessBoard.queen + ChessBoard.blackPiece);
            fenParsing.Add("Q", ChessBoard.queen + ChessBoard.whitePiece);
            fenParsing.Add("k", ChessBoard.king + ChessBoard.blackPiece);
            fenParsing.Add("K", ChessBoard.king + ChessBoard.whitePiece);
            fenParsingReady = true;
        }
    }
}