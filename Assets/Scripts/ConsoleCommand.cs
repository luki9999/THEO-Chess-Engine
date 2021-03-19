using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConsoleCommand
{
    string CommandString { get; }
    string HelpString { get; }
    bool Action(string[] args);
    void Init();
}


public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
{
    [SerializeField] public string commandString = string.Empty;
    [TextArea(5,10)][SerializeField] public string helpString = string.Empty;
    string IConsoleCommand.CommandString => commandString;
    string IConsoleCommand.HelpString => helpString;
    public abstract bool Action(string[] args);
    public abstract void Init();
}

