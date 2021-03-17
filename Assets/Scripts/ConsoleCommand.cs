using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConsoleCommand
{
    string CommandString { get; }
    string HelpString { get; }
    bool Action(string[] args);
}


public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
{
    [SerializeField] string command = string.Empty;
    [SerializeField] string help = string.Empty;
    string IConsoleCommand.CommandString => command;
    string IConsoleCommand.HelpString => help;
    public abstract bool Action(string[] args);
}

