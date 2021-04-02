using System.Collections;
using System.Collections.Generic;
using UnityEngine; //just for Debug.Log, remove eventually
using System;

[Serializable]
public class BitBoard
{
    [SerializeField]ulong boardInt;


    public BitBoard(ulong newBoardInt = 0)
    {
        boardInt = newBoardInt;
    }
    public static BitBoard operator +(BitBoard left, BitBoard right) // combines 2 boards
    {
        return new BitBoard(left.boardInt | right.boardInt);
    }
    public static BitBoard operator &(BitBoard left, BitBoard right) // usable for filtering
    {
        return new BitBoard(left.boardInt & right.boardInt);
    }
    public static BitBoard operator ~(BitBoard input) // inverts board
    {
        return new BitBoard(~input.boardInt);
    }

    public static explicit operator BitBoard(ulong input)
    {
        return new BitBoard(input);
    }

    public static explicit operator ulong(BitBoard input)
    {
        return input.boardInt;
    }

    public bool this[int index]
    {
        get => GetBoolAtSpace(index);
        set => SetBoolAtSpace(index, value);
    }

    bool GetBoolAtSpace(int index)
    {
        return ((boardInt >> index) & 1) == 1;
    }

    void SetBoolAtSpace(int index, bool input)
    {
        if (input)
        {
            boardInt |= ((ulong)1 << index);
        }
        else
        {
            boardInt &= ~((ulong)1 << index);
        }
    }

    public bool Equals(BitBoard otherBoard)
    {
        if (boardInt == otherBoard.boardInt) return true;
        else return false;
    }

    public override string ToString()
    {
        var fullString = Convert.ToString((long)boardInt, 2).PadLeft(64, '0');
        var partArray = new string[8];
        for (int i = 0; i < 8; i++)
        {
            var currentPart = fullString.Substring(i * 8, 8);
            partArray[i] = currentPart;
        }
        return string.Join("\n", partArray);
    }

    //this is a bit shitty but oh well...
    public override int GetHashCode()
    {
        return (int)boardInt;
    }

    public int CountActive()
    {
        int count = 0;
        for (int i = 0; i < 64; i++)
        {
            count += (int)(boardInt >> i) & 0b1;
        }
        return count;
    }

    public List<int> GetActive()
    {
        var output = new List<int>();
        ulong currentInt = boardInt;
        for (int i = 0; i < 64; i++)
        {
            if (currentInt == 0) break;
            if ((currentInt & 1) == 1) output.Add(i);
            currentInt >>= 1;
        }
        return output;
    }
}

[Serializable]
public class ChessBoard
{
    public BitBoard[] piecePositionBoards; //white pawns, white knights, white bishops, white rooks, white queens, white kings, after that same for black
    public BitBoard fullSpaces; // Every PiecePosBoard added together

    //use these instead of numbers!
    public const int pawn = 1, knight = 2, bishop = 3, rook = 4, queen = 5, king = 6;
    public const int whitePiece = 8, blackPiece = 16;
    public const int white = 0, black = 1;

    public static readonly int[] possiblePieces = new int[]
    {
        pawn | whitePiece, knight | whitePiece, bishop | whitePiece, rook | whitePiece, queen | whitePiece, king | whitePiece,
        pawn | blackPiece, knight | blackPiece, bishop | blackPiece, rook | blackPiece, queen | blackPiece, king | blackPiece
    };

    public static readonly char[] pieceNames = new char[] { ' ', 'N', 'B', 'R', 'Q', 'K' };
    public static readonly char[] fileLetters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
    public static readonly char[] rankNumbers = new char[] { '1', '2', '3', '4', '5', '6', '7', '8' };

    static readonly BitBoard whiteSpaces = (BitBoard)0b10101010_01010101_10101010_01010101_10101010_01010101_10101010_01010101;


    //castling data 
    //short white, long white, short black, short white
    public static readonly int[] rooksBefore = new int[] {7, 0, 63, 56};
    public static readonly int[] kingsBefore = new int[] {4, 4, 60, 60};
    public static readonly int[] rooksAfter = new int[] {5, 3, 61, 59};
    public static readonly int[] kingsAfter = new int[] {6, 2, 62, 58};


    public ChessBoard()
    {
        piecePositionBoards = new BitBoard[12];
        fullSpaces = new BitBoard();
        for (int i = 0; i < 12; i++)
        {
            piecePositionBoards[i] = new BitBoard();
        }
        //remove this once you are using the new stuff
        //SomeTests();
    }
    //operators and nice stuff

    public bool Equals(ChessBoard otherBoard)
    {
        for (int i = 0; i < 12; i++)
        {
            if (piecePositionBoards[i] != otherBoard.piecePositionBoards[i])
            {
                return false;
            }
        }
        return true;
    }

    public int this[int index]
    {
        get => GetPieceAtPos(index);
        set => SetPieceAtPos(index, value);
    }

    int GetPieceAtPos(int position)
    {
        if (!fullSpaces[position])
        {
            return 0;
        }
        for (int i = 0; i < 6; i++)
        {
            if(piecePositionBoards[i][position])
            {
                return i + 1 + whitePiece;
            } else if (piecePositionBoards[i + 6][position])
            {
                return i + 1 + blackPiece;
            }
        }
        throw new IndexOutOfRangeException("Full spaces are not updated correctly or there is a beer bottle on the board.");
    }

    /*void SetPieceAtPos(int position, int piece)
    {
        for (int i = 0; i < 12; i++)
        {
            piecePositionBoards[i][position] = false;
            fullSpaces[position] = false;
        }
        if (piece != 0)
        {
            int pieceIndex = PieceColor(piece) * 6 + (PieceType(piece) - 1);
            piecePositionBoards[pieceIndex][position] = true;
            fullSpaces[position] = true;
        }
    }*/

    void SetPieceAtPos(int position, int piece)
    {
        int prevPiece = GetPieceAtPos(position);
        if (piece == prevPiece) return; //piece is already there
        if (prevPiece != 0) piecePositionBoards[BitBoardIndex(prevPiece)][position] = false;
        if (piece != 0) piecePositionBoards[BitBoardIndex(piece)][position] = true;
        fullSpaces[position] = (piece != 0);
    }

    public ulong Hash()
    {
        ulong cutoffSum = 0, bitboardSum = 0;
        for (int i = 0; i < 12; i++)
        {
            ulong currentBoardInt = (ulong)piecePositionBoards[i];
            ulong currentCutoff = currentBoardInt & (ulong)0b11;
            bitboardSum += currentBoardInt >> 2;
            cutoffSum &= currentCutoff << (2 * i);
        }
        return bitboardSum + cutoffSum;
    }

    //some getters
    public static int PieceColor(int piece)
    {
        //ok that was important
        if (piece == 0) return -1;
        return (piece >> 4);
        //return ((piece & 0b11000) == whitePiece) ? white: black ;
    }

    public static int PieceType(int piece)
    {
        return piece & 0b111;
    }

    public static int PieceInt(int pieceType, int color){
        return pieceType | ((color + 1) << 3);
    }

    //TODO make function to move specific piece without looping through all boards, has to be very fast
    public static int BitBoardIndex(int piece)
    {
        return (piece & 0b111) - 1 + 6 * (piece >> 4);
    }

    public static int SpaceColor(int space)
    {
        return whiteSpaces[space] ? white : black;
    }

    public static int SpaceX(int space)
    {
        //return space % 8; but should be faster
        return space & 0b111;
    }

    public static int SpaceY(int space)
    {
        //return space / 8; but quicker
        return space >> 3;
    }

    public static int Distance(int start, int end)
    {
        int deltaX = Math.Abs(SpaceX(end) - SpaceX(start));
        int deltaY = Math.Abs(SpaceY(end) - SpaceY(start));
        return Math.Max(deltaY, deltaX);
    }

    public static string SpaceName(int space)
    {
        int x = SpaceX(space);
        int y = SpaceY(space);
        return fileLetters[x].ToString() + rankNumbers[y].ToString();
    }

    public static int SpaceNumberFromString(string spaceName)
    {
        int x=0, y=0;
        for (int i = 0; i < 8; i++)
        {
            if(fileLetters[i] == spaceName[0])
            {
                x = i;
            } 
            if(rankNumbers[i] == spaceName[1])
            {
                y = i;
            }
        }
        return 8 * y + x;
    }

    public static char PieceLetter(int piece)
    {
        return pieceNames[PieceType(piece) - 1];
    }

    public int PieceCount(int piece)
    {
        int index = BitBoardIndex(piece);
        return piecePositionBoards[index].CountActive();
    }

    public int WhitePieceCount()
    {
        BitBoard whitePieces = new BitBoard();
        for (int i = 0; i < 6; i++)
        {
            whitePieces += piecePositionBoards[i];
        }
        return whitePieces.CountActive();
    }

    public int BlackPieceCount()
    {
        BitBoard blackPieces = new BitBoard();
        for (int i = 6; i < 12; i++)
        {
            blackPieces += piecePositionBoards[i];
        }
        return blackPieces.CountActive();
    }

    public bool Contains(int space, int piece)
    {
        return piecePositionBoards[BitBoardIndex(piece)][space];
    }

    public List<int> FindPieces(int piece) //returns list of spaces where the piece is
    {
        var output = new List<int>();
        int index = BitBoardIndex(piece);
        for (int i = 0; i < 64; i++)
        {
            if (piecePositionBoards[index][i]) output.Add(i);
        }
        return output;
    }

    //methods

    public void UpdateFullSpaces()
    {
        for (int i = 0; i < 12; i++)
        {
            fullSpaces += piecePositionBoards[i];
        }
    }

    public void CreatePiece(int position, int piece){
        piecePositionBoards[BitBoardIndex(piece)][position] = true;
        fullSpaces[position] = true;
    }

    public void MovePieceToEmptySpace(int start, int end, int piece)
    {
        piecePositionBoards[BitBoardIndex(piece)][start] = false;
        fullSpaces[start] = false;
        piecePositionBoards[BitBoardIndex(piece)][end] = true;
        fullSpaces[end] = true;
    }

    public void MovePieceToFullSpace(int start, int end, int piece, int takenPiece)
    {
        piecePositionBoards[BitBoardIndex(takenPiece)][end] = false;
        piecePositionBoards[BitBoardIndex(piece)][start] = false;
        fullSpaces[start] = false;
        piecePositionBoards[BitBoardIndex(piece)][end] = true; // no need to update full spaces at end, there will still be a piece
    }

    public void TurnPawnToQueen(int pos, int color)
    {
        piecePositionBoards[queen - 1 + (6 * color)][pos] = true; //queen = true
        piecePositionBoards[6 * color][pos] = false; //pawn = false
        //fullSpaces[pos] = true; 
    }

    public void TurnQueenToPawn(int pos, int color) //for undoing promotions
    {
        piecePositionBoards[queen - 1 + (6 * color)][pos] = false; 
        piecePositionBoards[6 * color][pos] = true; 
        //fullSpaces[pos] = true; 
    }

    public int TakeEPPawn(int pos, int color)
    {
        piecePositionBoards[-6*(color-1)][(-8 + 16 * color) + pos] = false; // 6 for white (black pawn), 0 for black (white pawn) and -8 for white, 8 for black 
        fullSpaces[(-8 + 16 * color) + pos] = false;
        return (-8 + 16 * color) + pos;
    }
    //tests and shit

    void SomeTests()
    {
        Debug.Log("The piece 12 is of color " + PieceColor(12).ToString() + " and of type " + PieceType(12).ToString());
        piecePositionBoards[3][10] = true;
        Debug.Log(piecePositionBoards[3][10].ToString());
        Debug.Log(piecePositionBoards[3].ToString());
        BitBoard test = (BitBoard)0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101;
        BitBoard empty = new BitBoard();
        Debug.Log(test);
        Debug.Log(empty + ~test);
        Debug.Log(test + ~test);
    }
}
