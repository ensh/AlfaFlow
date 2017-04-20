using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public struct TemporalValue<T> : IEquatable<TemporalValue<T>>
	{
		public readonly long TimeStamp;
		public readonly T Value;
		public TemporalValue(long time, T value)
		{
			TimeStamp = time;
			Value = value;
		}

		public override bool Equals(object obj)
		{
			TemporalValue<T> key = (TemporalValue<T>)obj;
			return key.TimeStamp == TimeStamp;
		}

		public bool Equals(TemporalValue<T> key)
		{
			return key.TimeStamp == TimeStamp;
		}

		public override int GetHashCode()
		{
			return (int)(TimeStamp - s_timestampBase);
		}

		public static implicit operator T(TemporalValue<T> v)
		{
			return v.Value;
		}

		public static explicit operator long(TemporalValue<T> v)
		{
			return v.TimeStamp;
		}

        public static explicit operator DateTime(TemporalValue<T> v)
        {
			return new DateTime(v.TimeStamp);
        }

		static TemporalValue()
		{
			s_timestampBase = new DateTime(1990, 1, 1).Ticks;
		}
		static long s_timestampBase;
    }

	public class TemporalValueComparer<T> : IComparer<TemporalValue<T>>
	{
		public int Compare(TemporalValue<T> x, TemporalValue<T> y)
		{
			if (x.TimeStamp > y.TimeStamp) return 1;
			if (x.TimeStamp < y.TimeStamp) return -1;
			return 0;

			//long xy = x.TimeStamp - y.TimeStamp;
			//long yx = y.TimeStamp - x.TimeStamp;
			//return (int)(xy >> 63) | (int)((ulong)yx >> 63);
		}
	}

	public interface ITemporalObserver<T>
	{
		void Reset();
		void Apply(TemporalValue<T> value);
	}

	public interface ITemporalStream
	{
		void Reset();
		void Clear();
		long TimeFrame {get;}
		string Description { get; }
		event Action NoObservers;
		IDisposable Subscribe(object observer);
	}

	public class TemporalStream<T> : ITemporalStream, ITemporalValueSource<T>,  ICollection<TemporalValue<T>>, IEnumerable<TemporalValue<T>>
	{
		public TemporalStream(long timeFrame, string description = null)
			: this(description)
		{
			m_timeFrame = timeFrame;
		}	
		public TemporalStream(string description = null)
		{
			m_values = new ConcurrentDictionary<long, T>(1, 1024);
			m_observers = new ConcurrentDictionary<ITemporalObserver<T>, int>(1, 16);
			m_timeFrame = 1;
			Description = description ?? "";
		}

		public IDisposable Subscribe(object obj)
		{
			ITemporalObserver<T> observer = (ITemporalObserver<T>)obj;
			m_observers.GetOrAdd(observer,
				_ =>
				{
					foreach (var v in m_values)
						observer.Apply(new TemporalValue<T>(v.Key, v.Value));
					return 0;
				});

			return new Unsubscriber<T>(observer, this);
		}

		public void Reset()
		{
			foreach (var observer in m_observers) observer.Key.Reset();
		}

		long m_timeFrame;
		public long TimeFrame
		{
			get { return m_timeFrame; }
			set
			{
				if (m_timeFrame < value) m_timeFrame = value;
			}
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

		public bool TryGetValue(long time, out TemporalValue<T> result)
		{
			T res;
			if (m_values.TryGetValue(time, out res))
			{
				result = new TemporalValue<T>(time, res);
				return true;
			}
            result = new TemporalValue<T>(time, default(T));
			return false;
		}

		#region ICollection<TemporalValue<T>>
		void ICollection<TemporalValue<T>>.Add(TemporalValue<T> value)
		{
			//if (m_values.ContainsKey(value.TimeStamp)) return;
			//m_values.Add(value.TimeStamp, value.Value);
			m_values[value.TimeStamp] = value.Value;
			foreach (var observer in m_observers) observer.Key.Apply(value);
		}
		void ICollection<TemporalValue<T>>.Clear()
		{
			m_values = new ConcurrentDictionary<long, T>();
			foreach (var observer in m_observers) observer.Key.Reset();
		}

		bool ICollection<TemporalValue<T>>.Contains(TemporalValue<T> item)
		{
			return m_values.ContainsKey(item.TimeStamp);
		}

		void ICollection<TemporalValue<T>>.CopyTo(TemporalValue<T>[] array, int arrayIndex)
		{
			foreach (var v in m_values.OrderBy(v => v.Key))	
				array[arrayIndex++] = new TemporalValue<T>(v.Key, v.Value);
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
			T i;
			return m_values.TryRemove(item.TimeStamp, out i);
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
			public readonly TemporalStream<Td> m_stream;
			public Unsubscriber(ITemporalObserver<Td> observer, TemporalStream<Td> stream)
			{
				m_observer = observer;
				m_stream = stream;
			}
			public void Dispose()
			{
				m_stream.Unssubscribe(m_observer);
			}
		}
		#endregion

		ConcurrentDictionary<long, T> m_values;
		ConcurrentDictionary<ITemporalObserver<T>, int> m_observers;
	}
}
