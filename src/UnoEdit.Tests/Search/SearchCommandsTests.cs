using System.Linq;
using System.Windows.Input;

using ICSharpCode.AvalonEdit.Search;

using NUnit.Framework;

namespace UnoEdit.Tests.Search
{
	[TestFixture]
	public class SearchCommandsTests
	{
		[Test]
		public void FindNext_UsesF3Gesture()
		{
			Assert.That(SearchCommands.FindNext.Name, Is.EqualTo("FindNext"));
			Assert.That(SearchCommands.FindNext.InputGestures.OfType<KeyGesture>().Single().Key, Is.EqualTo(Key.F3));
			Assert.That(SearchCommands.FindNext.InputGestures.OfType<KeyGesture>().Single().Modifiers, Is.EqualTo(ModifierKeys.None));
		}

		[Test]
		public void FindPrevious_UsesShiftF3Gesture()
		{
			Assert.That(SearchCommands.FindPrevious.Name, Is.EqualTo("FindPrevious"));
			Assert.That(SearchCommands.FindPrevious.InputGestures.OfType<KeyGesture>().Single().Key, Is.EqualTo(Key.F3));
			Assert.That(SearchCommands.FindPrevious.InputGestures.OfType<KeyGesture>().Single().Modifiers, Is.EqualTo(ModifierKeys.Shift));
		}

		[Test]
		public void CloseSearchPanel_UsesEscapeGesture()
		{
			Assert.That(SearchCommands.CloseSearchPanel.Name, Is.EqualTo("CloseSearchPanel"));
			Assert.That(SearchCommands.CloseSearchPanel.InputGestures.OfType<KeyGesture>().Single().Key, Is.EqualTo(Key.Escape));
		}
	}
}
