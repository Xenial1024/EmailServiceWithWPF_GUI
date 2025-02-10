using System.Threading.Tasks;
using System.Windows.Input;
using System;

public class AsyncCompositeCommand : ICommand
{
    private readonly Func<Task> _execute;

    public AsyncCompositeCommand(params Func<Task>[] commands)
    {
        _execute = async () =>
        {
            foreach (var command in commands)
                await command();
        };
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => true;

    public async void Execute(object parameter) => await _execute();
}
