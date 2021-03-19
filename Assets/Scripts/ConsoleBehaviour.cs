using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChessConsole
{
    public readonly IEnumerable<IConsoleCommand> chessCommands;
    public ChessConsole(IEnumerable<IConsoleCommand> commands)
    {
        chessCommands = commands;
    }

    public void ParseCommandLine(string commandLine)
    {
        string[] inputList = commandLine.Split(' ');
        string commandToRun = inputList[0];
        string[] args = inputList.Skip(1).ToArray();
        ProcessCommand(commandToRun, args);
    }

    public void ProcessCommand(string calledCommand, string[] args) //actually runs commands
    {
        foreach (var command in chessCommands)
        {
            if(!command.CommandString.Equals(calledCommand, System.StringComparison.OrdinalIgnoreCase)) //not a command
            {
                continue;
            }
            if(command.Action(args))
            {
                return;
            }
        }
    }
}


public class ConsoleBehaviour : MonoBehaviour
{
    [SerializeField] private ConsoleCommand[] consoleCommands;

    [SerializeField] private InputField inputUI;
    [SerializeField] private Text outputUI;

    ChessConsole console;

    void Start()
    {
        inputUI.onEndEdit.AddListener(RunCommand);
        console = new ChessConsole(consoleCommands);
    }

    void RunCommand(string input)
    {
        if (input != "")
        {
            outputUI.text += "\n> " + input;
            console.ParseCommandLine(input);
            inputUI.text = string.Empty;
        }
        inputUI.ActivateInputField();
    }

    public void Print(string line)
    {
        outputUI.text += "\n" + line;
    }


}
