using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
    public class TemporalQueue<T> : ITemporalStream, ITemporalValueSource<T>, ICollection<TemporalValue<T>>, IEnumerable<TemporalValue<T>>
    {
        public TemporalQueue(string description = null)
        {
			m_values = new ConcurrentQueue<TemporalValue<T>>();
			m_observers = new ConcurrentDictionary<ITemporalObserver<T>, int>(1, 16);
			Description = description ?? "";
        }

        public IDisposable Subscribe(object obj)
        {
			ITemporalObserver<T> observer = (ITemporalObserver<T>)obj;
			m_observers.TryAdd(observer, 0);
			return new Unsubscriber<T>(observer, this);
        }

        public void Reset()
        {
            foreach (var observer in m_observers) observer.Key.Reset();
        }

        public long TimeFrame
        {
            get { return 1; }
        }
		public string Description { get; protected set; }
        public event Action NoObservers;
        internal void Unssubscribe(ITemporalObserver<T> observer)
        {
			int i;
			m_observers.TryRemove(observer, out i);
			if (m_observers.Count == 0 && NoObservers != null) NoObservers();
        }

        public void Clear()
        {
            ((ICollection<TemporalValue<T>>)this).Clear();
        }

		long m_lasttime;
		int m_refcount;
        public bool TryGetValue(long time, out TemporalValue<T> result)
        {
            if (m_values.Count > 0)
            {
				if (m_observers.Count == 1)
					m_values.TryDequeue(out result);
				else
				{
					if (m_lasttime == time)
					{
						if (m_refcount == 1) m_values.TryDequeue(out result);
						else
						{
							m_refcount--;
							m_values.TryPeek(out result);
						}
					}
					else
					{
						m_refcount = m_observers.Count - 1;
						m_lasttime = time;
						m_values.TryPeek(out result);
					}
				}
                return true;
            }
            result = new TemporalValue<T>(time, default(T));
            return false;
        }

        #region ICollection<TemporalValue<T>>
        void ICollection<TemporalValue<T>>.Add(TemporalValue<T> value)
        {
            m_values.Enqueue(value);
            foreach (var observer in m_observers) observer.Key.Apply(value);
        }
        void ICollection<TemporalValue<T>>.Clear()
        {
			m_values = new ConcurrentQueue<TemporalValue<T>>();
            foreach (var observer in m_observers) observer.Key.Reset();
        }

        bool ICollection<TemporalValue<T>>.Contains(TemporalValue<T> item)
        {
			return m_values.Contains(item);
        }

        void ICollection<TemporalValue<T>>.CopyTo(TemporalValue<T>[] array, int arrayIndex)
        {
            foreach (var v in m_values.OrderBy(v => v.TimeStamp))
                array[arrayIndex++] = v;
        }

        int ICollection<TemporalValue<T>>.Count
        {
            get { return m_values.Count; }
        }

        bool ICollection<TemporalValue<T>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<TemporalValue<T>>.Remove(TemporalValue<T> item)
        {
			TemporalValue<T> forDeleteItem;
			if (m_values.TryPeek(out forDeleteItem) && forDeleteItem.TimeStamp == item.TimeStamp)
			{
				m_values.TryDequeue(out forDeleteItem);
				return true;
			}
            return false;
        }

        IEnumerator<TemporalValue<T>> IEnumerable<TemporalValue<T>>.GetEnumerator()
        {
            var list = this.ToList();
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TemporalValue<T>>)this).GetEnumerator();
        }
        #endregion

        #region Unsubscriber
        internal class Unsubscriber<Td> : IDisposable
        {
            public readonly ITemporalObserver<Td> m_observer;
            public readonly TemporalQueue<Td> m_queue;
            public Unsubscriber(ITemporalObserver<Td> observer, TemporalQueue<Td> queue)
            {
                m_observer = observer;
                m_queue = queue;
            }
            public void Dispose()
            {
                m_queue.Unssubscribe(m_observer);
            }
        }
        #endregion

		ConcurrentQueue<TemporalValue<T>> m_values;
		ConcurrentDictionary<ITemporalObserver<T>, int> m_observers;
    }
}
