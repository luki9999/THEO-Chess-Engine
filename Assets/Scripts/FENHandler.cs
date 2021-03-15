using System.Collections;
using System.Collections.Generic;

//reads provided FEN strings and returns piece positions

public class FENHandler
{
    public static readonly char[] fenPieceLetters = new char[] { 'P', 'N', 'B', 'R', 'Q', 'K' };
    public static Dictionary<char, int> fenParsing = new Dictionary<char, int>();

    public static void FillFENDict()
    {
        if (fenParsing.Count == 0) {
            for (int pieceType = 0; pieceType < 6; pieceType++)
            {
                fenParsing.Add(fenPieceLetters[pieceType], pieceType + 1 + ChessBoard.whitePiece);
                fenParsing.Add(char.ToLower(fenPieceLetters[pieceType]), pieceType + 1 + ChessBoard.blackPiece);
            }
        }
    }

    public static ChessBoard ReadFEN(string fenNotation)
    {
        //TODO error handling this is quite dangerous
        int pieceToPlace;
        string[] fenGroups = fenNotation.Split(' ');
        string[] fenRows = fenGroups[0].Split('/');
        ChessBoard output = new ChessBoard();
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
                }
                else
                {
                    pieceToPlace = fenParsing[piece];
                    output[8 * (7 - y) + currentX] = pieceToPlace;
                    currentX++;
                }
            }
        }
        return output;
    }

    public static ChessGameData GameDataFromFEN(string fenNotation)
    {
        //TODO support for en passant space loading and error handling!
        var output = new ChessGameData(ChessBoard.white, 0, new bool[4]);
        string[] fenGroups = fenNotation.Split(' ');
        output.playerOnTurn = (fenGroups[1] == "w") ? ChessBoard.white : ChessBoard.black;
        string castlingStr = fenGroups[2];
        foreach (char currentLetter in castlingStr)
        {
            switch (currentLetter)
            {
                case 'k':
                    output.castling[0] = true;
                    break;
                case 'q':
                    output.castling[1] = true;
                    break;
                case 'K':
                    output.castling[2] = true;
                    break;
                case 'Q':
                    output.castling[3] = true;
                    break;
            }
        }
        return output;
    }
}
