using System;
using System.Windows.Controls;
using Microsoft.Phone.Controls.Primitives;
using System.Collections.Generic;

namespace Heatpump_Control
{
    public class ListLoopingDataSource<T> : LoopingDataSourceBase
    {
        private LinkedList<T> linkedList;
        private List<LinkedListNode<T>> sortedList;
        private IComparer<T> comparer;
        private NodeComparer nodeComparer;

        public ListLoopingDataSource()
        {
        }

        public IEnumerable<T> Items
        {
            get
            {
                return this.linkedList;
            }
            set
            {
                this.SetItemCollection(value);
            }
        }

        private void SetItemCollection(IEnumerable<T> collection)
        {
            this.linkedList = new LinkedList<T>(collection);

            this.sortedList = new List<LinkedListNode<T>>(this.linkedList.Count);
            // initialize the linked list with items from the collections
            LinkedListNode<T> currentNode = this.linkedList.First;
            while (currentNode != null)
            {
                this.sortedList.Add(currentNode);
                currentNode = currentNode.Next;
            }

            IComparer<T> comparer = this.comparer;
            if (comparer == null)
            {
                // if no comparer is set use the default one if available
                if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
                {
                    comparer = Comparer<T>.Default;
                }
                else
                {
                    throw new InvalidOperationException("There is no default comparer for this type of item. You must set one.");
                }
            }

            this.nodeComparer = new NodeComparer(comparer);
            this.sortedList.Sort(this.nodeComparer);
        }

        public IComparer<T> Comparer
        {
            get
            {
                return this.comparer;
            }
            set
            {
                this.comparer = value;
            }
        }

        public override object GetNext(object relativeTo)
        {
            // find the index of the node using binary search in the sorted list
            int index = this.sortedList.BinarySearch(new LinkedListNode<T>((T)relativeTo), this.nodeComparer);
            if (index < 0)
            {
                return default(T);
            }

            // get the actual node from the linked list using the index
            LinkedListNode<T> node = this.sortedList[index].Next;
            if (node == null)
            {
                // if there is no next node, return null (does not loop over)
                return null;
            }
            return node.Value;
        }

        public override object GetPrevious(object relativeTo)
        {
            int index = this.sortedList.BinarySearch(new LinkedListNode<T>((T)relativeTo), this.nodeComparer);
            if (index < 0)
            {
                return default(T);
            }
            LinkedListNode<T> node = this.sortedList[index].Previous;
            if (node == null)
            {
                // if there is no previous node, return null (does not loop over)
                return null;
            }
            return node.Value;
        }

        private class NodeComparer : IComparer<LinkedListNode<T>>
        {
            private IComparer<T> comparer;

            public NodeComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            #region IComparer<LinkedListNode<T>> Members

            public int Compare(LinkedListNode<T> x, LinkedListNode<T> y)
            {
                return this.comparer.Compare(x.Value, y.Value);
            }

            #endregion
        }
    }
}
