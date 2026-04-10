using System.Linq;
using System.Windows.Input;

using NUnit.Framework;

namespace ICSharpCode.AvalonEdit
{
	[TestFixture]
	public class AvalonEditCommandsTests
	{
		[Test]
		public void ToggleOverstrike_UsesInsertGesture()
		{
			Assert.That(AvalonEditCommands.ToggleOverstrike.Name, Is.EqualTo("ToggleOverstrike"));
			var gesture = AvalonEditCommands.ToggleOverstrike.InputGestures.OfType<KeyGesture>().Single();
			Assert.That(gesture.Key, Is.EqualTo(Key.Insert));
			Assert.That(gesture.Modifiers, Is.EqualTo(ModifierKeys.None));
		}

		[Test]
		public void DeleteLine_UsesCtrlDGesture()
		{
			Assert.That(AvalonEditCommands.DeleteLine.Name, Is.EqualTo("DeleteLine"));
			var gesture = AvalonEditCommands.DeleteLine.InputGestures.OfType<KeyGesture>().Single();
			Assert.That(gesture.Key, Is.EqualTo(Key.D));
			Assert.That(gesture.Modifiers, Is.EqualTo(ModifierKeys.Control));
		}

		[Test]
		public void IndentSelection_UsesCtrlIGesture()
		{
			Assert.That(AvalonEditCommands.IndentSelection.Name, Is.EqualTo("IndentSelection"));
			var gesture = AvalonEditCommands.IndentSelection.InputGestures.OfType<KeyGesture>().Single();
			Assert.That(gesture.Key, Is.EqualTo(Key.I));
			Assert.That(gesture.Modifiers, Is.EqualTo(ModifierKeys.Control));
		}

		[Test]
		public void TextTransformCommands_HaveStableNames()
		{
			Assert.That(AvalonEditCommands.ConvertToUppercase.Name, Is.EqualTo("ConvertToUppercase"));
			Assert.That(AvalonEditCommands.ConvertToLowercase.Name, Is.EqualTo("ConvertToLowercase"));
			Assert.That(AvalonEditCommands.ConvertToTitleCase.Name, Is.EqualTo("ConvertToTitleCase"));
			Assert.That(AvalonEditCommands.InvertCase.Name, Is.EqualTo("InvertCase"));
		}
	}
}
