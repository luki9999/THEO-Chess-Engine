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
    string lastInput = "";

    void Start()
    {
        inputUI.onEndEdit.AddListener(RunCommand);
        console = new ChessConsole(consoleCommands);
        foreach (ConsoleCommand command in consoleCommands)
        {
            command.Init();
        }
    }

    void RunCommand(string input)
    {
        if (input == "#") //character to pull down last line
        {
            inputUI.text = lastInput;
        } else if (input != "")
        {
            outputUI.text += "\n> " + input;
            console.ParseCommandLine(input);
            lastInput = input;
            inputUI.text = string.Empty;
        } 
        inputUI.ActivateInputField();
    }

    public void Print(string line)
    {
        outputUI.text += "\n" + line;
    }

    public void Clear()
    {
        outputUI.text = "";
    }

    public void Help()
    {
        foreach(ConsoleCommand command in consoleCommands)
        {
            if (command is HelpCommand) continue; //excludes the help command
            Print(command.commandString);
            Print(command.helpString);
            Print("");
        }
    }
}
