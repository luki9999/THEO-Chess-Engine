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

    public UIEnginePlayerButton blackButton;
    public UIEnginePlayerButton whiteButton;

    [SerializeField] GameMngr manager;

    Color prevColor;
    
    public void ReloadButtonColors()
    {
        whiteButton.IsActivated = (manager.engineState == EngineState.White || manager.engineState == EngineState.Both);
        blackButton.IsActivated = (manager.engineState == EngineState.Black || manager.engineState == EngineState.Both);
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
        bool playPressed = playButton.activeInHierarchy;
        playButton.SetActive(!playPressed);
        stopButton.SetActive(playPressed);
    }
}
