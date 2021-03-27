using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static ChessBoard;

public class PieceHandler : MonoBehaviour
{
    [SerializeField]
    GameObject[] pieceObjects;

    [SerializeField]
    Dictionary<int, GameObject> piecePositions;

    public HoverPieceCursor cursor;
    public SpaceHandler spaceHandler;
    public GameMngr manager;

    GameObject selectedPiece;
    Vector3 transformDelta;
    List<int> possibleMovesForClickedPiece;
     
    MoveGenerator moveGenerator;

    int startSpace;
    int endSpace;

    float pieceSize;

    bool respectTurn;
    void Awake()
    {
        moveGenerator = manager.moveGenerator;
        respectTurn = manager.dragAndDropRespectsTurns;
        pieceSize = pieceObjects[0].transform.localScale.x;
        piecePositions = new Dictionary<int, GameObject>();
    }

    void Update()
    {
        DragDrop(respectTurn);
    }

    void DragDrop(bool respectTurn = false)
    {
        if (Input.GetMouseButtonDown(0) && cursor.hoveredPiece != null)
        {
            selectedPiece = cursor.hoveredPiece;
            startSpace = spaceHandler.WorldSpaceToChessSpace(selectedPiece.transform.position);

            if (!respectTurn || manager.playerOnTurn == ChessBoard.PieceColor(moveGenerator.board[startSpace])){

                possibleMovesForClickedPiece = moveGenerator.GetLegalMovesForPiece(startSpace);
                spaceHandler.HighlightMoveList(possibleMovesForClickedPiece, Color.cyan, 0.5f);

                transformDelta = selectedPiece.transform.position - cursor.transform.position;
                selectedPiece.GetComponent<BoxCollider2D>().enabled = false;
                selectedPiece.GetComponent<SpriteRenderer>().sortingOrder = 1;

                cursor.inDrag = true; }
            //Oh god please no one else write shit like this this is an embarassment
            else
            {
                selectedPiece = null;
            }
        }
        else if (Input.GetMouseButton(0) && selectedPiece != null)
        {
            selectedPiece.transform.position = cursor.transform.position + transformDelta;
        }
        else if (Input.GetMouseButtonUp(0) && selectedPiece != null)
        {
            endSpace = SnapToSpace(selectedPiece, startSpace, possibleMovesForClickedPiece);
            spaceHandler.UnHighlightAll(); // Doesnt need to be fast :D
            selectedPiece.GetComponent<BoxCollider2D>().enabled = true;
            selectedPiece.GetComponent<SpriteRenderer>().sortingOrder = 0;

            if (startSpace != endSpace)
            {
                manager.MakeMoveNoGraphics(startSpace, endSpace);
            }

            selectedPiece = null;
            cursor.inDrag = false;
        }
    }

    int SnapToSpace(GameObject piece, int startSpace, List<int> possibleSpaces)
    {
        int space = spaceHandler.WorldSpaceToChessSpace(piece.transform.position);
        //resetting the piece in case of an invalid move
        if (space < 0 || space > 63 || space == startSpace || !possibleSpaces.Contains(space))
        {
            piece.transform.position = spaceHandler.ChessSpaceToWorldSpace(startSpace);
            return startSpace;
        }
        //taking a piece, only one piece per space
        if (GetPieceAtPos(space) != null)
        {
            DisablePiece(space);
        }
        //actual snapping
        piece.transform.position = spaceHandler.ChessSpaceToWorldSpace(space);
        piecePositions.Remove(startSpace);
        piecePositions[space] = piece;
        return space;
    }

    public void ClearBoard()
    {
        piecePositions = new Dictionary<int, GameObject>();
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
        foreach (GameObject piece in pieces)
        {
            DestroyImmediate(piece);
        }
    }

    public void LayOutPieces(ChessBoard board)
    {
        piecePositions = new Dictionary<int, GameObject>();
        for (int i = 0; i < 12; i++)
        {
            for (int space = 0; space < 64; space++)
            {
                if (board.piecePositionBoards[i][space])
                {
                    GameObject newPiece = Instantiate(pieceObjects[i], spaceHandler.ChessSpaceToWorldSpace(space), Quaternion.identity, gameObject.transform);
                    piecePositions.Add(space, newPiece);
                }
            }
        }
    }

    public void ReloadPieces()
    {
        ClearBoard();
        LayOutPieces(moveGenerator.board);
    }

    public void MovePieceSprite(int oldSpace, int newSpace)
    {
        GameObject pieceToMove = GetPieceAtPos(oldSpace);
        pieceToMove.transform.position = spaceHandler.ChessSpaceToWorldSpace(newSpace);
        GameObject takenPiece = GetPieceAtPos(newSpace);
        if (takenPiece != null)
        {
            DisablePiece(newSpace);
        }
        piecePositions.Remove(oldSpace);
        piecePositions[newSpace] = pieceToMove;
        if ((SpaceY(newSpace) == 0 || SpaceY(newSpace) == 7)) //pawn got maybe promoted
        {
            if (moveGenerator.board.Contains(newSpace, whitePiece | queen)) ChangePieceToQueen(newSpace, white);
            if (moveGenerator.board.Contains(newSpace, blackPiece | queen)) ChangePieceToQueen(newSpace, black);
        }
    }

    IEnumerator PieceAnimationCoroutine(int oldSpace, int newSpace, float animTime)
    {
        float startTime = Time.time;
        GameObject pieceToMove = GetPieceAtPos(oldSpace);
        GameObject takenPiece = GetPieceAtPos(newSpace);
        if (takenPiece != null)
        {
            DisablePiece(newSpace);
        }
        piecePositions.Remove(oldSpace);
        piecePositions[newSpace] = pieceToMove;
        if ((SpaceY(newSpace) == 0 || SpaceY(newSpace) == 7)) //pawn got maybe promoted
        {
            if (moveGenerator.board.Contains(newSpace, whitePiece | queen)) ChangePieceToQueen(newSpace, white);
            if (moveGenerator.board.Contains(newSpace, blackPiece | queen)) ChangePieceToQueen(newSpace, black);
        }
        while (Time.time - startTime <= animTime)
        {
            if (pieceToMove == null) break;
            pieceToMove.transform.position = Vector3.Lerp(spaceHandler.ChessSpaceToWorldSpace(oldSpace), spaceHandler.ChessSpaceToWorldSpace(newSpace), (Time.time - startTime) / animTime);
            yield return 0;
        }
    }

    public void MovePieceSpriteAnimated(int oldSpace, int newSpace, float animTime)
    {
        StartCoroutine(PieceAnimationCoroutine(oldSpace, newSpace, animTime));
    }

    public void DisablePiece(int position)
    {
        GetPieceAtPos(position).SetActive(false);
        piecePositions.Remove(position);
    }

    public void ChangePieceSprite(int position, Sprite newSprite)
    {
        GameObject piece = GetPieceAtPos(position);
        if (piece == null) return;
        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        renderer.sprite = newSprite;
    }

    public void ChangePieceToQueen(int position, int color)
    {//PLEASE FOR THE LOVE OF GOD CHANGE THAT AGAIN
        if (color == ChessBoard.black) ChangePieceSprite(position, pieceObjects[10].GetComponent<SpriteRenderer>().sprite);
        else ChangePieceSprite(position, pieceObjects[4].GetComponent<SpriteRenderer>().sprite);
    }

    public GameObject GetPieceAtPos(int position)
    {
        if (piecePositions.ContainsKey(position))
        {
            return piecePositions[position];
        }
        return null;
    }
}
