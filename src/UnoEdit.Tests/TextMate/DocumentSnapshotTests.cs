using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.TextMate;
using NUnit.Framework;

namespace UnoEdit.Tests.TextMate
{
    [TestFixture]
    public class DocumentSnapshotTests
    {
        [Test]
        public void Snapshot_TracksLineTextAndTerminators()
        {
            var document = new TextDocument("alpha\r\nbeta\r\ngamma");
            var snapshot = new DocumentSnapshot(document);

            Assert.That(snapshot.LineCount, Is.EqualTo(3));
            Assert.That(snapshot.GetLineText(0), Is.EqualTo("alpha"));
            Assert.That(snapshot.GetLineTextIncludingTerminator(0), Is.EqualTo("alpha\r\n"));
            Assert.That(snapshot.GetLineTerminator(1), Is.EqualTo("\r\n"));
            Assert.That(snapshot.GetLineLength(2), Is.EqualTo(5));
            Assert.That(snapshot.GetTotalLineLength(2), Is.EqualTo(5));
        }

        [Test]
        public void Snapshot_Update_ReflectsDocumentMutation()
        {
            var document = new TextDocument("alpha\nbeta");
            var snapshot = new DocumentSnapshot(document);

            document.Insert(0, "zero\n");
            snapshot.Update(null);

            Assert.That(snapshot.LineCount, Is.EqualTo(3));
            Assert.That(snapshot.GetLineTextIncludingTerminator(0), Is.EqualTo("zero\n"));
            Assert.That(snapshot.GetLineText(1), Is.EqualTo("alpha"));
        }
    }
}
