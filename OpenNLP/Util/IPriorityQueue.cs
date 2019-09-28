using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNLP.Util
{
    public interface IPriorityQueue<E> /*:ISet<E>*/
    {

        /// <summary>
        /// Finds the object with the highest priority, removes it, and returns it.
        /// </summary>
        /// <returns>the object with highest priority</returns>
        E RemoveFirst();

        /// <summary>
        /// Finds the object with the highest priority and returns it, without modifying the queue.
        /// </summary>
        /// <returns>the object with minimum key</returns>
        E GetFirst();

        /// <summary>
        /// Gets the priority of the highest-priority element of the queue (without modifying the queue).
        /// </summary>
        /// <returns>The priority of the highest-priority element of the queue.</returns>
        double GetPriority();

        /// <summary>
        /// Get the priority of a key.
        /// </summary>
        /// <param name="key">The object to assess</param>
        /// <returns>A key's priority. If the key is not in the queue, Double.NEGATIVE_INFINITY is returned.</returns>
        double GetPriority(E key);

        /// <summary>
        /// Convenience method for if you want to pretend relaxPriority doesn't exist,
        /// or if you really want to use the return conditions of add().
        /// </summary>
        /// <returns> <tt>true</tt> if this set did not already contain the specified element.</returns>
        bool Add(E key, double priority);

        /// <summary>
        /// Changes a priority, either up or down, adding the key it if it wasn't there already.
        /// </summary>
        /// <returns>whether the priority actually changed.</returns>
        bool ChangePriority(E key, double priority);

        /// <summary>
        /// Increases the priority of the E key to the new priority if the old priority
        ///  was lower than the new priority. Otherwise, does nothing.
        /// </summary>
        bool RelaxPriority(E key, double priority);

        List<E> ToSortedList();

        /// <summary>
        /// Returns a representation of the queue in decreasing priority order,
        /// displaying at most maxKeysToPrint elements.
        /// </summary>
        /// <param name="maxKeysToPrint">The maximum number of keys to print. 
        /// Less are printed if there are less than this number of items in the PriorityQueue. 
        /// If this number is non-positive, then all elements in the PriorityQueue are printed.</param>
        /// <returns>A string representation of the high priority items in the queue.</returns>
        string ToString(int maxKeysToPrint);
    }
}