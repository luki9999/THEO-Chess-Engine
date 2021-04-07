using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHelper : MonoBehaviour
{
    public GameObject xLetters;
    public GameObject xLettersFlipped;

    public GameObject yNumbers;
    public GameObject yNumbersFlipped;

    public GameObject playButton;
    public GameObject stopButton;

    public Image blackButton;
    public Image whiteButton;

    [SerializeField] GameMngr manager;

    Color prevColor;
    private void Start()
    {
        prevColor = blackButton.color;
    }
    public void ReloadButtonColors()
    {
        if (manager.engineState == EngineState.White)
        {
            whiteButton.color = manager.boardCreation.whiteColor;
            blackButton.color = prevColor;
        }
        else if (manager.engineState == EngineState.Black)
        {
            blackButton.color = manager.boardCreation.blackColor;
            whiteButton.color = prevColor;
        }
        else if (manager.engineState == EngineState.Both)
        {
            whiteButton.color = manager.boardCreation.whiteColor;
            blackButton.color = manager.boardCreation.blackColor;
        }
        else
        {
            whiteButton.color = prevColor;
            blackButton.color = prevColor;
        }
    }

    public void FlipBoardNumbering()
    {
        bool alreadyFlipped = xLettersFlipped.activeInHierarchy;
        xLettersFlipped.SetActive(!alreadyFlipped);
        yNumbersFlipped.SetActive(!alreadyFlipped);
        xLetters.SetActive(alreadyFlipped);
        yNumbers.SetActive(alreadyFlipped);
    }

    public void FlipPlayButton()
    {
        bool playPressed = playButton.activeSelf;
        playButton.SetActive(!playPressed);
        stopButton.SetActive(playPressed);
    }
}
