using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

public class CompositeCommand : ICommand
{
    private readonly IEnumerable<ICommand> _commands;

    public CompositeCommand(params ICommand[] commands) => _commands = commands.AsEnumerable();

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => _commands.All(command => command.CanExecute(parameter));

    public void Execute(object parameter) => _commands.ToList().ForEach(command => command.Execute(parameter));
}