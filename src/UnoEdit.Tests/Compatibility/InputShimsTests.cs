using NUnit.Framework;
using System.Windows.Input;

namespace UnoEdit.Tests.Compatibility;

[TestFixture]
public class InputShimsTests
{
    [Test]
    public void RoutedCommand_Execute_InvokesRegisteredBinding()
    {
        var command = new RoutedCommand("Test", typeof(InputShimsTests));
        int callCount = 0;

        _ = new CommandBinding(command, (_, e) =>
        {
            callCount++;
            e.Handled = true;
        });

        command.Execute("payload");

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void RoutedCommand_CanExecute_UsesBindingHandler()
    {
        var command = new RoutedCommand("Test", typeof(InputShimsTests));

        _ = new CommandBinding(
            command,
            (_, e) => e.Handled = true,
            (_, e) =>
            {
                e.CanExecute = false;
                e.Handled = true;
            });

        Assert.That(command.CanExecute(null), Is.False);
    }

    [Test]
    public void TextCompositionEventArgs_ControlText_IsSetForControlCharacters()
    {
        var args = new TextCompositionEventArgs("\t");

        Assert.That(args.ControlText, Is.EqualTo("\t"));
        Assert.That(args.SystemText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TextCompositionEventArgs_SystemText_PreservesExplicitSystemText()
    {
        var args = new TextCompositionEventArgs(string.Empty, "x");

        Assert.That(args.SystemText, Is.EqualTo("x"));
        Assert.That(args.ControlText, Is.EqualTo(string.Empty));
    }
}
