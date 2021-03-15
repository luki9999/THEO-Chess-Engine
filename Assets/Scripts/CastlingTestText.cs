using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CastlingTestText : MonoBehaviour
{
    public GameMngr gameMngr;
    public int index;

    MoveGenerator board;
    Text txt;
    // Start is called before the first frame update
    void Start()
    {
        board = gameMngr.moveGenerator;
        gameMngr.moveMade.AddListener(OnMove);
        txt = GetComponent<Text>();
    }

    // OnMove is called afer each comitted move
    void OnMove()
    {
        txt.text = board.castling[index].ToString();
    }
}
