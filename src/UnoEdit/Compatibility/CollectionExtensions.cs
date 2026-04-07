using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Utils
{
	public static class CollectionExtensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
		{
			foreach (T element in elements) {
				collection.Add(element);
			}
		}
	}
}
