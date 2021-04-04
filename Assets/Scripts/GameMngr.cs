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

public enum EngineState{
    Off,
    White,
    Black,
    Both
}

public class GameMngr : MonoBehaviour
{
    //other scripts
    public MoveGenerator moveGenerator;
    public PieceHandler pieceHandler;
    public SpaceHandler spaceHandler;
    public BoardCreation boardCreation;
    public ConsoleBehaviour console;

    //graphics + init
    public bool boardExists = false;
    public bool boardFlipped;
    public float moveAnimationTime;

    //game flow
    public GameState currentState;
    public int playerOnTurn;
    public UndoMoveData lastMove;
    public bool dragAndDropRespectsTurns;
    public bool gameOver;
    public int movesWithoutPawn = 0;

    //events
    [HideInInspector] public UnityEvent moveMade = new UnityEvent();
    [HideInInspector] public UnityEvent gameEnd = new UnityEvent();

    //engine
    public Engine engine;
    public EngineState engineState;
    public int engineDepth;
    public int captureDepth;
    public int perftTestDepth;

    
    //history
    [HideInInspector] public List<UndoMoveData> moveHistory;
    [SerializeField] public List<ulong> positionHistory;

    //test positions
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


    //init and game starting
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
        engineState = EngineState.Off;
    }

    public void OnBoardFinished()
    {
        boardExists = true;
        StartChessGame();
        engine = new Engine(moveGenerator);
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
        engineState = EngineState.Off;
        pieceHandler.LayOutPieces(moveGenerator.board);
    }

    public void StartChessGame()
    {
        LoadPosition(startingPosString);
    }

    // move making (sync between board and graphics)
    public void MakeMoveNoGraphics(int start, int end)
    {
        if (gameOver) return;
        if(engineState != EngineState.Both){        
            if(playerOnTurn == white && !(engineState == EngineState.White)) console.Print("W: " + moveGenerator.MoveName(start, end));
            if(playerOnTurn == black && !(engineState == EngineState.Black)) console.Print("B: " + moveGenerator.MoveName(start, end));
        }
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

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0) return;
        moveGenerator.UndoMovePiece(moveHistory.Last());
        moveHistory.RemoveAt(moveHistory.Count - 1);
        positionHistory.RemoveAt(positionHistory.Count - 1);
        pieceHandler.ReloadPieces();
        playerOnTurn = (playerOnTurn == white) ? black : white;
    }

    public void FlipBoard()
    {
        pieceHandler.ClearBoard();
        boardFlipped = !boardFlipped;
        pieceHandler.LayOutPieces(moveGenerator.board);
    }

    //event handling (what happens after moves)
    public GameState CurrentState()
    {
        if(positionHistory.Count(x => x == moveGenerator.board.Hash()) == 3) return GameState.Draw; //draw after 3 fold repetion
        if(movesWithoutPawn == 50) return GameState.Draw; //draw after 50 moves without pawn move
        List<EngineMove> moveset = engine.GetMoveset(playerOnTurn);
        if(moveset.Count == 0)
        {
            if (moveGenerator.IsPlayerInCheck(playerOnTurn)) return GameState.Mate;
            else return GameState.Draw;
        }
        return GameState.Running;
    }

    void OnMove()
    {
        currentState = CurrentState();
        spaceHandler.UnHighlightAll();
        if(moveGenerator.IsPlayerInCheck(playerOnTurn)) { 
            int kingSpace = (playerOnTurn == white) ? moveGenerator.whiteKingPosition : moveGenerator.blackKingPosition;
            spaceHandler.HighlightSpace(kingSpace, Color.red, 0.5f);
        }
        moveHistory.Add(lastMove);
        spaceHandler.HighlightSpace(lastMove.start, Color.yellow, 0.7f);
        spaceHandler.HighlightSpace(lastMove.end, Color.yellow, 0.7f);
        positionHistory.Add(moveGenerator.board.Hash());
        if (PieceType(lastMove.movedPiece) == pawn){
            movesWithoutPawn = 0;
        } else{
            movesWithoutPawn ++;
        }
        if(currentState == GameState.Mate || currentState == GameState.Draw)
        {
            gameEnd.Invoke();
            gameOver = true;
        }
        if (gameOver) return;
        if (engineState == EngineState.Black && playerOnTurn == black)
        {
            engine.ThreadedMove();
        } else if (engineState == EngineState.White && playerOnTurn == white)
        {
            engine.ThreadedMove();
        }
    }

    void OnGameOver()
    {
        console.Print("The game ended.");
        engineState = EngineState.Off;
        string playerStr = (playerOnTurn == white) ? "White" : "Black";
        if (moveGenerator.IsPlayerInCheck(playerOnTurn))
        {
            console.Print(playerStr + " is checkmate. Type restart to reset board.");
        }
        else
        {
            console.Print("This was a draw. Type restart to reset board.");
        }
    }

    //data sharing with engine and making engine moves
    void Update()
    {
        if(engine.currentSearch.searchStarted){
            console.Print(" ");
            engine.currentSearch.searchStarted = false;
        }
        if (engine.currentSearch.valuesChanged)
        {
            string playerStr = (playerOnTurn == white) ? "W: " : "B: ";
            console.ReplaceLast(playerStr + engine.currentSearch.currentBestMoveName.PadRight(6)
                + " | Static: " + engine.currentSearch.currentBestEval.ToString().PadLeft(7) 
                + " | Delta: " + engine.currentSearch.currentMoveScore.ToString().PadLeft(7)
                + " | Count: " + engine.currentSearch.currentSearchCount.ToString().PadLeft(12));
            engine.currentSearch.valuesChanged = false;
        }
        if (engine.moveReady)
        {
            OnEngineMoveReady();
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
        if(engineState == EngineState.Both){
            engine.ThreadedMove();
        }
    }

    //tests and stuff
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

    public void ShowAttackedSpaces()
    {
        spaceHandler.UnHighlightAll();
        var attackedSpacesBlack = moveGenerator.GenerateAttackedSpaceBitboard(black).GetActive();
        spaceHandler.HighlightMoveList(attackedSpacesBlack, Color.red, 0.25f);
        var attackedSpacesWhite = moveGenerator.GenerateAttackedSpaceBitboard(white).GetActive();
        spaceHandler.HighlightMoveList(attackedSpacesWhite, Color.green, 0.25f);
    }

    public void AttackedSpaceGenerationTest()
    {
        LoadPosition(middleGameTestPos);
        ShowAttackedSpaces();
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
