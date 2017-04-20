using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace AlfaFlow
{
	public interface ITemporalContext
	{
        void RegToken(long timeStamp);
        void RegToken(int token_id, long timeStamp);
        bool Activate();
        int Priority { get; }
	}

	public interface ITemporalValueSource<T>
	{
		bool TryGetValue(long time, out TemporalValue<T> result);
	}

	public struct TemporalContextToken
	{
		public readonly int Context;
		public readonly long TimeStamp;
		public TemporalContextToken(int context, long timeStamp)
		{
			Context = context; TimeStamp = timeStamp;
		}
	}

	public class TemporalContextTokenComparer : IComparer<TemporalContextToken>
	{
		public int Compare(TemporalContextToken x, TemporalContextToken y)
		{
			//if (x.Context > y.Context) return 1;
			//if (x.Context < y.Context) return -1;

			//if (x.TimeStamp > y.TimeStamp) return 1;
			//if (x.TimeStamp < y.TimeStamp) return -1;
			//return 0;

			if (x.Context == y.Context)
			{
				long xy = x.TimeStamp - y.TimeStamp;
				long yx = y.TimeStamp - x.TimeStamp;
				return (int)(xy >> 63) | (int)((ulong)yx >> 63);
			}
			return x.Context - y.Context;
		}
	}

	public class TemporalContext<T> : ITemporalContext
	{
		public TemporalContext(ITemporalValueSource<T> values, ITemporalObserver<T> observer, 
			int priority = TemporalContextManager.IndicatorPriority)
		{
			m_values = values;
			m_observer = observer;
			m_tokens = new SortedSet<TemporalContextToken>(new TemporalContextTokenComparer());
			m_state = 0;
			m_number = 0;

			Priority = priority;
		}

		int m_number, m_state;

        public int Priority { get; protected set; }
        public void RegToken(int i, long time)
        { 
        }
		public void RegToken(long time)
		{
			lock (m_tokens)
			{
				if (!m_tokens.Add(new TemporalContextToken(0, time))) return;
				switch (m_state)
				{
					// контекст будет сброшен
					case 1:
					// активное состояние контекста
					case 2:
						if (m_tokens.Min.TimeStamp > time) // куда-то в середину, тоже пересчитать все...
						{
							m_state = 1;
							return;
						}

						break;
					default:
						break;
				}
			}
		}

		public void Reset()
		{
			m_observer.Reset();
			m_state = 1;
		}

        TemporalValue<T> m_value;
		public bool Activate()
		{
			lock (m_tokens)
			{
				TemporalContextToken t;
				switch (m_state)
				{
					// ожидаем готовности первого набора токенов
					case 0:					
						if (m_tokens.Count > 0)
						{
							m_state = 2;
							m_number++;
							goto do_next;
						}
						break;
					// пришел новый ранний токен - сбрасываем вычисленное состояние и начинаем сначала
					case 1:
						m_observer.Reset();
						m_state = 0;

						// переписываем у всех необработанных токенов контекст
						while ((t = m_tokens.Min).Context < m_number)
						{
							m_tokens.Add(new TemporalContextToken(m_number, t.TimeStamp));
							m_tokens.Remove(t);
						}
						m_number++;
						break;
					// ищем новый доступный токен и вызываем обработку
					case 2:
					do_next:
						t = m_tokens.Min;
						if (t.Context >= m_number) return false;
						if (!m_values.TryGetValue(t.TimeStamp, out m_value)) return false;

						m_tokens.Add(new TemporalContextToken(m_number, t.TimeStamp));
						m_tokens.Remove(t);

						m_observer.Apply(m_value);
						break;
					default:
						return false;
				}
				return true;
			}
		}

		SortedSet<TemporalContextToken> m_tokens;
		ITemporalValueSource<T> m_values;
		ITemporalObserver<T> m_observer;
    }

    public struct TemporalLinker
    {
        public readonly object m_stream, m_observer;
        public readonly long TimeFrame;
        TemporalLinker(object stream, object observer, long timeframe, Action reset, Func<long, bool> apply)
        {
            m_reset = reset;
            m_apply = apply;
            m_stream = stream;
            m_observer = observer;
            TimeFrame = timeframe;
        }
        Func<long, bool> m_apply;
        Action m_reset;
        public static TemporalLinker Create<T>(TemporalStream<T> stream, ITemporalObserver<T> observer)
        {
            return new TemporalLinker(stream, observer,
                stream.TimeFrame,
                () => observer.Reset(),
                (time) =>
                {
                    TemporalValue<T> value;
                    if (stream.TryGetValue(time, out value))
                    {
                        observer.Apply(value);
                        return true;
                    }
                    return false;
                });
        }

        public static implicit operator Func<long, bool>(TemporalLinker l)
        {
            return l.m_apply;
        }
        public static implicit operator Action(TemporalLinker l)
        {
            return l.m_reset;
        }
    }


	public struct TemporalLinkedContextToken
	{
		public readonly int Linker;
		public readonly int Context;
		public readonly long TimeStamp;
		public TemporalLinkedContextToken(int linker, int context, long timeStamp)
		{
			Linker = linker;  Context = context; TimeStamp = timeStamp;
		}
	}

	public class TemporalLinkedContextTokenComparer : IComparer<TemporalLinkedContextToken>
	{
		public int Compare(TemporalLinkedContextToken x, TemporalLinkedContextToken y)
		{
			if (x.Context == y.Context)
			{
				long xy = x.TimeStamp - y.TimeStamp;
				if (xy == 0) return x.Linker - y.Linker;

				long yx = y.TimeStamp - x.TimeStamp;
				return (int)(xy >> 63) | (int)((ulong)yx >> 63);
			}
			return x.Context - y.Context;
		}
	}
	public class TemporalLinkedContext : ITemporalContext
	{
		public TemporalLinkedContext(TemporalLinker[] linkers, 
			int priority = TemporalContextManager.IndicatorPriority)
		{
			m_linkers = linkers;
			m_tokens = new SortedSet<TemporalLinkedContextToken>(new TemporalLinkedContextTokenComparer());
			m_lstart = new long[m_linkers.Length];
			m_lnext = new long[m_linkers.Length];
			m_state = 0;
			m_number = 0;

			Priority = priority;
            TimeFrame = m_linkers.Min(l => l.TimeFrame);
		}

		int m_number, m_state;
        public int Priority { get; protected set; }
        public long TimeFrame { get; protected set; }
        public void RegToken(long time)
        { 
        }
		public void RegToken(int i, long time)
		{
			lock (m_tokens)
			{
				if (!m_tokens.Add(new TemporalLinkedContextToken(i, 0, time))) return;
				if (m_lstart[i] > time || m_lstart[i] == 0) m_lstart[i] = time;

				switch (m_state)
				{
					// контекст будет сброшен
					case 1:
					// активное состояние контекста
					case 2:
						if (m_tokens.Min.TimeStamp < time) // куда-то в середину, тоже пересчитать все...
						{
							m_state = 1;
							return;
						}

						break;
					default:
						break;
				}
			}
		}

		public void Reset()
		{
			((Action)m_linkers[m_linkers.Length - 1])();
			m_state = 1;
		}

		public TemporalLinkedContextToken[] FindTimeSlice()
		{
			TemporalLinkedContextToken t;
			TemporalLinkedContextToken[] ts = new TemporalLinkedContextToken[m_linkers.Length];
			while ((t = m_tokens.Min).Context < m_number)
			{
				var range = m_tokens.GetViewBetween(t, new TemporalLinkedContextToken(0, m_number -1, t.TimeStamp + TimeFrame));

				if (range.Count < m_linkers.Length) 
				{
					m_tokens.Remove(t);
					m_tokens.Add(new TemporalLinkedContextToken(t.Linker, m_number, t.TimeStamp));
				}

				int count = 0;
				foreach(var rt in range)
				{
					if (ts[rt.Linker].TimeStamp != 0)
					{
						for (int i = 0; i < ts.Length; i++)
						{
							if (ts[i].TimeStamp != 0)
							{
								m_tokens.Remove(ts[i]);
								m_tokens.Add(new TemporalLinkedContextToken(i, m_number, ts[i].TimeStamp));
							}
						}
						break;
					}

					ts[rt.Linker] = rt;
					if ((count++) == m_linkers.Length) return ts;
				}
			}

			return null;
		}

		public bool Activate()
		{
			lock (m_tokens)
			{
				TemporalLinkedContextToken t;
				switch (m_state)
				{
					// ожидаем готовности первого набора токенов
					case 0:
						long max_start = long.MinValue;
						for (int i = 0; i < m_lstart.Length; i++)
						{
							if (m_lstart[i] <= 0) return false;
							if (m_lstart[i] > max_start) max_start = m_lstart[i];
						}

						while ((t = m_tokens.Min).TimeStamp <= max_start - TimeFrame)
						{
							m_tokens.Add(new TemporalLinkedContextToken(t.Linker, m_number, t.TimeStamp));
							m_tokens.Remove(t);
						}

						m_state = 2;
						m_number++;
						goto do_step;
					// пришел новый ранний токен - сбрасываем вычисленное состояние и начинаем сначала
					case 1:
						((Action)m_linkers[m_linkers.Length - 1])();
						m_state = 0;

						// переписываем у всех необработанных токенов контекст
						while ((t = m_tokens.Min).Context < m_number)
						{
							m_tokens.Add(new TemporalLinkedContextToken(t.Linker, m_number, t.TimeStamp));
							m_tokens.Remove(t);
						}

						m_number++;
						break;
					// ищем новый доступный токен и вызываем обработку
					case 2:
					do_step:
						{
							TemporalLinkedContextToken[] tokens;
							if ((tokens = FindTimeSlice()) == null) return false;
							for (int i = 0; i < tokens.Length; i++)
							{
								if (!((Func<long, bool>)m_linkers[i])(tokens[i].TimeStamp))
								{
									// недостаточно токенов для вызова, подождем...
									return false;
								}
								m_tokens.Add(new TemporalLinkedContextToken(i, m_number, tokens[i].TimeStamp));
								m_tokens.Remove(tokens[i]);
							}
						}
						break;
					default:
						return false;
				}
				return true;
			}
		}

		SortedSet<TemporalLinkedContextToken> m_tokens;
		TemporalLinker[] m_linkers;
		long[] m_lstart, m_lnext;
	}
}
