using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ChessBoard;

public enum GameState
{
    Running,
    Mate,
    Draw
}

public class GameMngr : MonoBehaviour
{
    public MoveGenerator moveGenerator;
    public PieceHandler pieceHandler;
    public SpaceHandler spaceHandler;
    public BoardCreation boardCreation;
    public ConsoleBehaviour console;
    public bool boardExists = false;
    public bool dragAndDropRespectsTurns;
    public GameObject cursor;

    public bool boardFlipped;

    public bool gameOver;

    //sehr dumm bitte �ndern
    public bool theoIsBlack;
    public bool theoIsWhite;
    public int engineDepth;

    public int playerOnTurn;

    [HideInInspector] public List<UndoMoveData> moveHistory;
    [SerializeField] public List<ulong> positionHistory;

    public GameState currentState;

    public float moveAnimationTime;

    public int perftTestDepth;

    [HideInInspector]
    public static readonly string startingPosString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public static readonly string pawnTestPos = "8/8/6p1/7P/8/2p1p3/3P4/8 w - - 0 1";
    public static readonly string knightTestPos = "8/8/2p5/5p2/3N4/1P6/4P3/8 w - - 0 1";
    public static readonly string rookTestPos = "8/p2p2p1/8/8/3R4/8/1P1P1P2/8 w - - 0 1";
    public static readonly string bishopTestPos = "8/p2p2p1/8/8/3B4/8/3P1P2/8 w - - 0 1";
    public static readonly string queenTestPos = "8/p2p2p1/8/8/3Q4/8/1P1P1P2/8 w - - 0 1";
    public static readonly string kingTestPos = "8/8/8/3p4/2pKP3/3P4/8/8 w - - 0 1";
    public static readonly string middleGameTestPos = "r4rk1/p3bppp/b1pp1n2/P1q1p3/4P3/2N1B1P1/1PP2PBP/R2QR1K1 b - - 2 13";

    public static readonly string perftTest1 = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - ";
    public static readonly string perftTest2 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - ";
    public static readonly string perftTest3 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    public static readonly string perftTest4 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
    public static readonly string perftTest5 = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";


    UndoMoveData lastMove;
    public Engine engine;
    [HideInInspector]
    public UnityEvent moveMade = new UnityEvent();
    [HideInInspector]
    public UnityEvent gameEnd = new UnityEvent();

    //TODO make this a singleton

    void Awake()
    {
        moveGenerator = new MoveGenerator();
        ChessBoard test = new ChessBoard();
        moveMade.AddListener(OnMove);
        gameEnd.AddListener(OnGameOver);
    }

    void Start()
    {
        moveHistory = new List<UndoMoveData>();
        positionHistory = new List<ulong>();
        boardCreation.creationFinished.AddListener(OnBoardFinished);
        //SpeedTest.TestFunctionSpeed(() => moveGenerator.board[10] = whitePiece | rook, 10000000);
    }

    public void OnBoardFinished()
    {
        boardExists = true;
        StartChessGame();
        
        engine = new Engine(moveGenerator);
        //MakeMoveAnimated(4, 1, 4, 3);
    }

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0) return;
        moveGenerator.UndoMovePiece(moveHistory.Last());
        moveHistory.RemoveAt(moveHistory.Count - 1);
        positionHistory.RemoveAt(positionHistory.Count - 1);
        pieceHandler.ReloadPieces();
        playerOnTurn = (playerOnTurn == white) ? black : white;
    }

    void OnMove()
    {
        currentState = CurrentState();
        moveHistory.Add(lastMove);
        positionHistory.Add(moveGenerator.board.Hash());
        console.Print(moveGenerator.gameData.castling[0].ToString() + " " + moveGenerator.gameData.castling[1].ToString() + " | " + moveGenerator.gameData.castling[2].ToString() + " " + moveGenerator.gameData.castling[3].ToString() + " ");
        console.Print(SpaceName(moveGenerator.gameData.epSpace));
        if(currentState == GameState.Mate || currentState == GameState.Draw)
        {
            gameEnd.Invoke();
            gameOver = true;
        }
        if (gameOver) return;
        if (theoIsBlack && playerOnTurn == black)
        {
            engine.ThreadedMove();
        } else if (theoIsWhite && playerOnTurn == white)
        {
            engine.ThreadedMove();
        }
    }

    void OnGameOver()
    {
        console.Print("Game over.");
        string playerStr = (playerOnTurn == white) ? "White" : "Black";
        if (moveGenerator.IsPlayerInCheck(playerOnTurn))
        {

            console.Print(playerStr + " is checkmate. Type restart to reset board.");
        }
        else
        {
            console.Print(playerStr + " can't move any more, this is a draw. Type restart to reset board.");
        }
    }

    public void MakeMoveNoGraphics(int start, int end)
    {
        if (gameOver) return;
        lastMove = moveGenerator.MovePiece(start, end);
        if (lastMove.castlingIndex != -1) // Castling!
        {
            pieceHandler.MovePieceSprite(rooksBefore[lastMove.castlingIndex], rooksAfter[lastMove.castlingIndex]);
        } else if (lastMove.end == lastMove.epSpaceBefore) // en passant
        {
            int epOffset = (PieceColor(lastMove.movedPiece) == white) ? -8 : 8;
            pieceHandler.DisablePiece(lastMove.end + epOffset);
        }
        playerOnTurn = (playerOnTurn == white) ? black : white;
        moveMade.Invoke();
    }

    public void MakeMove(int start, int end)
    {
        if (gameOver) return;
        if (pieceHandler.GetPieceAtPos(end) != null)
        {
            pieceHandler.DisablePiece(end);
        }
        MakeMoveNoGraphics(start, end);
        pieceHandler.MovePieceSprite(start, end);
    }

    public void MakeMoveAnimated(int start, int end)
    {
        if (gameOver) return;
        if (pieceHandler.GetPieceAtPos(end) != null)
        {
            pieceHandler.DisablePiece(end);
        }
        MakeMoveNoGraphics(start, end);
        pieceHandler.MovePieceSpriteAnimated(start, end, moveAnimationTime);
    }

    public void MakeMoveFromString(string moveString)
    {
        //TODO error handling
        string startString = moveString.Substring(0, 2);
        string endString = moveString.Substring(2, 2);
        MakeMove(SpaceNumberFromString(startString), SpaceNumberFromString(endString));
    }

    public GameState CurrentState()
    {
        //TODO add all the other posibilities for draws (50 moves, repetition...)
        List<EngineMove> moveset = engine.GetMoveset(playerOnTurn);
        if(moveset.Count == 0)
        {
            if (moveGenerator.IsPlayerInCheck(playerOnTurn)) return GameState.Mate;
            else return GameState.Draw;
        }
        return GameState.Running;
    }

    void Update()
    {
        if (!cursor.activeSelf) cursor.SetActive(true);
        if (engine.currentSearch.valuesChanged)
        {
            console.ReplaceLast(engine.currentSearch.currentBestMove.PadRight(6)
                + "| Eval: " + engine.currentSearch.currentBestEval.ToString().PadRight(8) 
                + "| Count: " + engine.currentSearch.currentSearchCount.ToString().PadRight(12));
            engine.currentSearch.valuesChanged = false;
        }
        if (engine.moveReady)
        {
            OnEngineMoveReady();
            if (theoIsBlack || theoIsWhite) console.Print("");
            engine.moveReady = false;
        }
        if (engine.evalReady)
        {
            console.Print("Searched Eval: " + engine.currentSearch.currentBestEval.ToString());
            engine.evalReady = false;
        }
    }

    void OnEngineMoveReady()
    {
        MakeMoveAnimated(engine.nextFoundMove.Start, engine.nextFoundMove.End);
    }

    public void MoveGenerationTest(int piece)
    {
        switch (piece)
        {
            case 1:
                LoadPosition(pawnTestPos);
                break;
            case 2:
                LoadPosition(knightTestPos);
                break;
            case 3:
                LoadPosition(bishopTestPos);
                break;
            case 4:
                LoadPosition(rookTestPos);
                break;
            case 5:
                LoadPosition(queenTestPos);
                break;
            case 6:
                LoadPosition(kingTestPos);
                break;
        }
        if (piece != 1)
        {
            spaceHandler.UnHighlightAll();
            List<int> possibleMoves = moveGenerator.GetPossibleSpacesForPiece(3+ 3*8).GetActive();
            spaceHandler.HighlightMoveList(possibleMoves, Color.cyan, 0.5f);
            spaceHandler.HighlightSpace(27, Color.green, 0.5f);
        }
        else
        {
            spaceHandler.UnHighlightAll();
            List<int> possibleMoves1 = moveGenerator.GetPossibleSpacesForPiece(3+ 1*8).GetActive();
            List<int> possibleMoves2 = moveGenerator.GetPossibleSpacesForPiece(6+ 5*8).GetActive();
            spaceHandler.HighlightMoveList(possibleMoves1, Color.cyan, 0.5f);
            spaceHandler.HighlightMoveList(possibleMoves2, Color.magenta, 0.5f);
            spaceHandler.HighlightSpace(11, Color.green, 0.5f);
            spaceHandler.HighlightSpace(46, Color.red, 0.5f);
        }
    }

    public void AttackedSpaceGenerationTest()
    {
        LoadPosition(middleGameTestPos);
        ShowAttackedSpaces();
    }

    public void ShowAttackedSpaces()
    {
        spaceHandler.UnHighlightAll();
        var attackedSpacesBlack = moveGenerator.GenerateAttackedSpaceBitboard(black).GetActive();
        spaceHandler.HighlightMoveList(attackedSpacesBlack, Color.red, 0.25f);
        var attackedSpacesWhite = moveGenerator.GenerateAttackedSpaceBitboard(white).GetActive();
        spaceHandler.HighlightMoveList(attackedSpacesWhite, Color.green, 0.25f);
    }

    public void StartChessGame()
    {
        LoadPosition(startingPosString);
        theoIsBlack = false;
        theoIsWhite = false;
    }

    public void LoadPosition(string fen)
    {
        pieceHandler.ClearBoard();
        spaceHandler.UnHighlightAll();
        moveGenerator.LoadFEN(fen);
        gameOver = false;
        moveHistory = new List<UndoMoveData>();
        positionHistory = new List<ulong>() { moveGenerator.board.Hash() };
        playerOnTurn = moveGenerator.gameData.playerOnTurn;
        pieceHandler.LayOutPieces(moveGenerator.board);
        /*for (int i = 0; i < 12; i++)
        {
            print(moveGenerator.board.piecePositionBoards[i]);
        }*/
    }

    public void FlipBoard()
    {
        pieceHandler.ClearBoard();
        boardFlipped = !boardFlipped;
        pieceHandler.LayOutPieces(moveGenerator.board);
    }

    public void EngineMoveCountTest()
    {
        for (int i = 1; i <= perftTestDepth; i++)
        {
            engine.originalDepth = i;
            float startTime = Time.realtimeSinceStartup;
            int moveCount = engine.MoveGenCountTest(i, playerOnTurn);
            float timeElapsed = Time.realtimeSinceStartup - startTime;
            print("Found " + moveCount.ToString("N0") + " moves with depth " + i.ToString());
            print("It took " + timeElapsed.ToString() + " seconds.");
        }
    }
}
