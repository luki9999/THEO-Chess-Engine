using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoardCreation : MonoBehaviour
{
    //linking
    public GameMngr manager;
    public SpaceHandler spaceHandler;
    [HideInInspector] public UnityEvent creationFinished = new UnityEvent();

    //inputs
    public Color whiteColor;
    public Color blackColor;
    public GameObject space;
    
    //core
    public List<GameObject> spaces;

    void Start()
    {
        GenerateBoard();
        creationFinished.Invoke();
    }

    public void GenerateBoard()
    {
        RemoveBoard();
        GameObject newSpace;
        SpriteRenderer newRenderer;
        int currentSpace;
        for (int i = 0; i<64; i++) {
            currentSpace = (manager.boardFlipped) ? SpaceHandler.FlipIndex(i) : i;
            newSpace = Instantiate(space, spaceHandler.ChessSpaceToWorldSpace(currentSpace), Quaternion.identity, gameObject.transform);
            newRenderer = newSpace.GetComponent<SpriteRenderer>();
            if (ChessBoard.SpaceColor(currentSpace) == 0) { newRenderer.color = blackColor; }
            else { newRenderer.color = whiteColor; }
            spaces.Add(newSpace);
        }
    }

    public void RemoveBoard()
    {
        int children = gameObject.transform.childCount;
        for (int i = children-1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        spaces = new List<GameObject>();
    }
}
