using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceHandler : MonoBehaviour
{
    public BoardCreation boardCreation;
    void Start()
    {
        //boardCreation = GetComponent<BoardCreation>();
    }

    public Vector3 ChessSpaceToWorldSpace(int x, int y)
    {
        return new Vector3(-3.5f + x + transform.position.x, -3.5f + y + transform.position.y, 0);
    }

    public Vector3 ChessSpaceToWorldSpace(int space)
    {
        int x = ChessBoard.SpaceX(space);
        int y = ChessBoard.SpaceY(space);
        return new Vector3(-3.5f + x + transform.position.x, -3.5f + y + transform.position.y, 0);
    }

    public int WorldSpaceToChessSpace(Vector2 koords)
    {
        int x = Mathf.RoundToInt(koords.x + 3.5f - transform.position.x);
        int y = Mathf.RoundToInt(koords.y + 3.5f - transform.position.y);
        return (0<=x && x<8 && 0<=y && y<8) ? x + 8 * y : -1;
    }

    GameObject GetSpaceObjectAtPosition(int pos)
    {
        return boardCreation.spaces[pos];
    }

    //Never ever use that again
    public void HighlightSpace(int x, int y, Color color, float lerpValue = 1)
    {
        GameObject spaceToHighlight = GetSpaceObjectAtPosition(x + 8 * y);
        spaceToHighlight.GetComponent<SpriteRenderer>().color = Color.Lerp(spaceToHighlight.GetComponent<SpriteRenderer>().color, color , lerpValue);
    }

    public void HighlightSpace(int space, Color color, float lerpValue = 1)
    {
        GameObject spaceToHighlight = GetSpaceObjectAtPosition(space);
        spaceToHighlight.GetComponent<SpriteRenderer>().color = Color.Lerp(spaceToHighlight.GetComponent<SpriteRenderer>().color, color, lerpValue); 
    }

    public void UnHighlightSpace(int space)
    {
        GameObject spaceToUnHighlight = GetSpaceObjectAtPosition(space);
        Color color;
        if (ChessBoard.SpaceColor(space) == 0) { color = boardCreation.blackColor; }
        else { color = boardCreation.whiteColor; }
        spaceToUnHighlight.GetComponent<SpriteRenderer>().color = color;
    }

    public void HighlightMoveList(List<int> moveList, Color color, float lerpValue = 1)
    {
        foreach (int move in moveList)
        {
            HighlightSpace(move, color, lerpValue);
        }
    }

    public Color GetSpaceColorRGB(int x, int y)
    {
        Color color;
        if (ChessBoard.SpaceColor(8 * y + x) == 0) { color = boardCreation.blackColor; }
        else { color = boardCreation.whiteColor; }
        return color;
    }

    public Color GetSpaceColorRGB(int space)
    {
        Color color;
        if (ChessBoard.SpaceColor(space) == 0) { color = boardCreation.blackColor; }
        else { color = boardCreation.whiteColor; }
        return color;
    }

    public void UnHighlightAll()
    {
        //Oh no dont loop through all of them
        for (int i = 0; i < 64; i++)
        {
            UnHighlightSpace(i);
        }
    }
}
