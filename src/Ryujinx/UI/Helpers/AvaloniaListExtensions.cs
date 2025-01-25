using Avalonia.Collections;
using System.Collections.Generic;

namespace Ryujinx.Ava.UI.Helpers
{
    public static class AvaloniaListExtensions
    {
        /// <summary>
        /// Adds or Replaces an item in an AvaloniaList irrespective of whether the item already exists
        /// </summary>
        /// <typeparam name="T">The type of the element in the AvaoloniaList</typeparam>
        /// <param name="list">The list containing the item to replace</param>
        /// <param name="item">The item to replace</param>
        /// <param name="addIfNotFound">True to add the item if its not found</param>
        /// <returns>True if the item was found and replaced, false if it was addded</returns>
        /// <remarks>
        /// The indexes on the AvaloniaList will only replace if the item does not match, 
        /// this causes the items to not be replaced if the Equality is customised on the 
        /// items. This method will instead find, remove and add the item to ensure it is
        /// replaced correctly.
        /// </remarks>
        public static bool ReplaceWith<T>(this AvaloniaList<T> list, T item, bool addIfNotFound = true)
        {
            int index = list.IndexOf(item);

            if (index != -1)
            {
                list.RemoveAt(index);
                list.Insert(index, item);
                return true;
            }
            else
            {
                list.Add(item);
                return false;
            }
        }

        /// <summary>
        /// Adds or Replaces items in an AvaloniaList from another list irrespective of whether the item already exists
        /// </summary>
        /// <typeparam name="T">The type of the element in the AvaoloniaList</typeparam>
        /// <param name="list">The list containing the item to replace</param>
        /// <param name="sourceList">The list of items to be actually added to `list`</param>
        /// <param name="matchingList">The items to use as matching records to search for in the `sourceList', if not found this item will be added instead</params>
        public static void AddOrReplaceMatching<T>(this AvaloniaList<T> list, IList<T> sourceList, IList<T> matchingList)
        {
            foreach (T match in matchingList)
            {
                int index = sourceList.IndexOf(match);
                if (index != -1)
                {
                    list.ReplaceWith(sourceList[index]);
                }
                else
                {
                    list.ReplaceWith(match);
                }
            }
        }
    }
}