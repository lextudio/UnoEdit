namespace System.Windows
{
	public interface IWeakEventListener
	{
		bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e);
	}
}

namespace System.Windows.Documents
{
	public enum LogicalDirection
	{
		Backward = 0,
		Forward = 1
	}
}
