using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Minimal cross-platform weak event manager used by the portable UnoEdit core.
	/// </summary>
	public abstract class WeakEventManagerBase<TManager, TEventSource>
		where TManager : WeakEventManagerBase<TManager, TEventSource>, new()
		where TEventSource : class
	{
		readonly ConditionalWeakTable<TEventSource, ListenerTable> listenerTables = new ConditionalWeakTable<TEventSource, ListenerTable>();
		static readonly TManager currentManager = new TManager();

		protected WeakEventManagerBase()
		{
			Debug.Assert(GetType() == typeof(TManager));
		}

		public static void AddListener(TEventSource source, System.Windows.IWeakEventListener listener)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));

			ListenerTable table = currentManager.listenerTables.GetValue(source, _ => {
				currentManager.StartListening(source);
				return new ListenerTable();
			});
			table.Add(listener);
		}

		public static void RemoveListener(TEventSource source, System.Windows.IWeakEventListener listener)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));

			if (currentManager.listenerTables.TryGetValue(source, out ListenerTable table)) {
				table.Remove(listener);
				if (table.IsEmpty) {
					currentManager.StopListening(source);
					currentManager.listenerTables.Remove(source);
				}
			}
		}

		protected abstract void StartListening(TEventSource source);
		protected abstract void StopListening(TEventSource source);

		protected void DeliverEvent(object sender, EventArgs e)
		{
			if (sender is not TEventSource source)
				return;

			if (!listenerTables.TryGetValue(source, out ListenerTable table))
				return;

			bool hasLiveListeners = false;
			foreach (System.Windows.IWeakEventListener listener in table.GetLiveListeners()) {
				hasLiveListeners = true;
				listener.ReceiveWeakEvent(typeof(TManager), sender, e);
			}

			if (!hasLiveListeners) {
				StopListening(source);
				listenerTables.Remove(source);
			}
		}

		sealed class ListenerTable
		{
			readonly List<WeakReference<System.Windows.IWeakEventListener>> listeners = new List<WeakReference<System.Windows.IWeakEventListener>>();

			public bool IsEmpty => listeners.Count == 0;

			public void Add(System.Windows.IWeakEventListener listener)
			{
				Prune();
				listeners.Add(new WeakReference<System.Windows.IWeakEventListener>(listener));
			}

			public void Remove(System.Windows.IWeakEventListener listener)
			{
				for (int i = listeners.Count - 1; i >= 0; i--) {
					if (!listeners[i].TryGetTarget(out System.Windows.IWeakEventListener target) || ReferenceEquals(target, listener)) {
						listeners.RemoveAt(i);
					}
				}
			}

			public IEnumerable<System.Windows.IWeakEventListener> GetLiveListeners()
			{
				for (int i = listeners.Count - 1; i >= 0; i--) {
					if (listeners[i].TryGetTarget(out System.Windows.IWeakEventListener listener)) {
						yield return listener;
					} else {
						listeners.RemoveAt(i);
					}
				}
			}

			void Prune()
			{
				for (int i = listeners.Count - 1; i >= 0; i--) {
					if (!listeners[i].TryGetTarget(out _)) {
						listeners.RemoveAt(i);
					}
				}
			}
		}
	}
}
