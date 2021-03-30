using System.Collections;
using System.Collections.Generic;

using static ChessBoard;

//reads provided FEN strings and returns piece positions

public class FENHandler
{
    public static readonly char[] fenPieceLetters = new char[] { 'P', 'N', 'B', 'R', 'Q', 'K' };
    public static Dictionary<char, int> fenParsing = new Dictionary<char, int>
    {
        {'P', pawn | whitePiece },
        {'N', knight | whitePiece },
        {'B', bishop | whitePiece },
        {'R', rook | whitePiece },
        {'Q', queen | whitePiece },
        {'K', king | whitePiece },
        {'p', pawn | blackPiece },
        {'n', knight | blackPiece },
        {'b', bishop | blackPiece },
        {'r', rook | blackPiece },
        {'q', queen | blackPiece },
        {'k', king | blackPiece }
    };

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
        var output = new ChessGameData(white, 0, new bool[4]);
        string[] fenGroups = fenNotation.Split(' ');
        output.playerOnTurn = (fenGroups[1] == "w") ? white : black;
        string castlingStr = fenGroups[2];
        foreach (char currentLetter in castlingStr)
        {
            switch (currentLetter)
            {
                case 'K':
                    output.castling[MoveGenerator.shortCastlingWhite] = true;
                    break;
                case 'Q':
                    output.castling[MoveGenerator.longCastlingWhite] = true;
                    break;
                case 'k':
                    output.castling[MoveGenerator.shortCastlingBlack] = true;
                    break;
                case 'q':
                    output.castling[MoveGenerator.longCastlingBlack] = true;
                    break;
            }
        }
        return output;
    }
}
