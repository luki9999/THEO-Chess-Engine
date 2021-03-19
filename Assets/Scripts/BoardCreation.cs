using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoardCreation : MonoBehaviour
{
    public Color whiteColor;
    public Color blackColor;
    public GameObject space;
    public GameMngr manager;

    public List<GameObject> spaces;

    public UnityEvent creationFinished = new UnityEvent();

    public SpaceHandler spaceHandler;
    // Start is called before the first frame update
    void Start()
    {
        GenerateBoard();
        creationFinished.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
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
