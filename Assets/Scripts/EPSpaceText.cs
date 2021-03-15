using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EPSpaceText : MonoBehaviour
{
    public GameMngr mngr;
    Text txt;
    // Start is called before the first frame update
    void Start()
    {
        txt = GetComponent<Text>();
        mngr.moveMade.AddListener(OnMove);
    }

    // Update is called once per frame
    void OnMove()
    {
        txt.text = ChessBoard.SpaceName(mngr.moveGenerator.gameData.epSpace);
    }
}
