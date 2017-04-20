using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
    public class TemporalObserverStub<T> : ITemporalObserver<T>
    {
        public void Reset() { }
        public void Apply(TemporalValue<T> value) { }
    }

	public class TemporalContextManager
	{
		public const int UserPriority = 10000;
		public const int IndicatorPriority = 20000;
		public const int InternalIndicatorPriority = 30000;
		public const int NewTokenPriority = 40000;

		public TemporalContextManager()
		{
			m_token_names = new ConcurrentDictionary<string, int>(1, 1024);
			m_reg_contexts = new SortedSet<ITemporalContext>( new PriorityComparer() );
			m_streams = new ConcurrentDictionary<string, ITemporalStream>(1, 1024);
			m_lock = new object();
		}

		ConcurrentDictionary<string, int> m_token_names;
		SortedSet<ITemporalContext> m_reg_contexts;
		ConcurrentDictionary<string, ITemporalStream> m_streams;

		public ITemporalStream this[string token]
		{
			get
			{
				ITemporalStream result = null;
				m_streams.TryGetValue(token, out result);
				return result;
			}
		}
		public ITemporalStream AddTemporalStream<T>(string token, string description = null)
		{
			ITemporalStream stream = m_streams.GetOrAdd(token,
				_token =>
				{
					stream = new TemporalStream<T>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

			return stream;
		}
		public ITemporalStream AddTemporalQueue<T>(string token, string description = null)
		{
			ITemporalStream stream = m_streams.GetOrAdd(token,
				_token =>
				{
					stream = new TemporalQueue<T>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

			return stream;
		}
		public void AddTemporalValue<T>(string token, TemporalValue<T> value)
		{
			ITemporalStream stream = AddTemporalStream<T>(token);
			((ICollection<TemporalValue<T>>)stream).Add(value);
		}
		public void SendTemporalValue<T>(string token, TemporalValue<T> value)
		{
			ITemporalStream queue = AddTemporalQueue<T>(token);
			((ICollection<TemporalValue<T>>)queue).Add(value);
		}
		public IDisposable AddTemporalObserver<T>(ITemporalObserver<T> observer, string token, string description = null, 
			int priority = TemporalContextManager.NewTokenPriority)
		{		
			ITemporalStream stream = m_streams.GetOrAdd(token,
				_token =>
				{
					stream = new TemporalStream<T>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});
			TemporalContext<T> ctx = new TemporalContext<T>((ITemporalValueSource<T>)stream, observer, priority);
			return stream.Subscribe(new TemporalContextHelper<T>(ctx, this));
		}
		public IDisposable AddTemporalQueueObserver<T>(ITemporalObserver<T> observer, string token, string description = null, 
			int priority = TemporalContextManager.NewTokenPriority)
		{
			ITemporalStream stream = m_streams.GetOrAdd(token,
				_token =>
				{
					stream = new TemporalQueue<T>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

			TemporalContext<T> ctx = new TemporalContext<T>((ITemporalValueSource<T>)stream, observer, priority);
			return stream.Subscribe(new TemporalContextHelper<T>(ctx, this));
		}
		public IDisposable[] AddTemporalObservers<T1, T2>(ITemporalObserver<T1> observer1, ITemporalObserver<T2> observer2, string[] tokens,
			string description = null, int priority = TemporalContextManager.NewTokenPriority)
        {
            int idx = 0;
			ITemporalStream stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T1>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link1 = TemporalLinker.Create((TemporalStream<T1>)stream, observer1);

            idx++;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T1>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link2 = TemporalLinker.Create((TemporalStream<T2>)stream, observer2);

            TemporalLinkedContext ctx = new TemporalLinkedContext(new[] { link1, link2 }, priority);

            return new IDisposable[]{
                ((ITemporalStream)link1.m_stream).Subscribe(new TemporalLinkedContextHelper<T1>(ctx, this, 0)),
                ((ITemporalStream)link2.m_stream).Subscribe(new TemporalLinkedContextHelper<T2>(ctx, this, 1))
            };
        }

		public IDisposable[] AddTemporalObservers<T1, T2, T3>(ITemporalObserver<T1> observer1, ITemporalObserver<T2> observer2, ITemporalObserver<T3> observer3, string[] tokens, string description = null, 
			int priority = TemporalContextManager.NewTokenPriority)
        {
            int idx = 0;
            ITemporalStream stream;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T1>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

			TemporalLinker link1 = TemporalLinker.Create((TemporalStream<T1>)stream, observer1);

            idx++;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T2>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

			TemporalLinker link2 = TemporalLinker.Create((TemporalStream<T2>)stream, observer2);

            idx++;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T3>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link3 = TemporalLinker.Create((TemporalStream<T3>)stream, observer3);

            TemporalLinkedContext ctx = new TemporalLinkedContext(new[] { link1, link2, link3 }, priority);

            return new IDisposable[]{
                ((ITemporalStream)link1.m_stream).Subscribe(new TemporalLinkedContextHelper<T1>(ctx, this, 0)),
                ((ITemporalStream)link2.m_stream).Subscribe(new TemporalLinkedContextHelper<T2>(ctx, this, 1)),
                ((ITemporalStream)link3.m_stream).Subscribe(new TemporalLinkedContextHelper<T3>(ctx, this, 2))
            };
        }

		public IDisposable[] AddTemporalObservers<T1, T2, T3, T4>(ITemporalObserver<T1> observer1, ITemporalObserver<T2> observer2, 
			ITemporalObserver<T3> observer3, ITemporalObserver<T4> observer4, string[] tokens, string description = null, 
			int priority = TemporalContextManager.NewTokenPriority)
        {
            int idx = 0;
            ITemporalStream stream;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T1>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link1 = TemporalLinker.Create((TemporalStream<T1>)stream, observer1);

            idx++;
            if (!m_streams.TryGetValue(tokens[idx], out stream))
            {
				stream = new TemporalStream<T2>(description);
                m_streams.TryAdd(tokens[idx], stream);
				stream.NoObservers += () =>
				{
					ITemporalStream forNull;
					m_streams.TryRemove(tokens[idx], out forNull);
				};

            }
            TemporalLinker link2 = TemporalLinker.Create((TemporalStream<T2>)stream, observer2);

            idx++;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T3>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link3 = TemporalLinker.Create((TemporalStream<T3>)stream, observer3);

            idx++;
			stream = m_streams.GetOrAdd(tokens[idx],
				token =>
				{
					stream = new TemporalStream<T4>(description);
					stream.NoObservers += () =>
					{
						ITemporalStream forNull;
						m_streams.TryRemove(token, out forNull);
					};
					return stream;
				});

            TemporalLinker link4 = TemporalLinker.Create((TemporalStream<T4>)stream, observer4);

            TemporalLinkedContext ctx = new TemporalLinkedContext(new[] { link1, link2, link3, link4 }, priority);

            return new IDisposable[]{
                ((ITemporalStream)link1.m_stream).Subscribe(new TemporalLinkedContextHelper<T1>(ctx, this, 0)),
                ((ITemporalStream)link2.m_stream).Subscribe(new TemporalLinkedContextHelper<T2>(ctx, this, 1)),
                ((ITemporalStream)link3.m_stream).Subscribe(new TemporalLinkedContextHelper<T3>(ctx, this, 2)),
                ((ITemporalStream)link4.m_stream).Subscribe(new TemporalLinkedContextHelper<T4>(ctx, this, 3)),
            };
        }

		public void ResetStream(string token)
		{
			ITemporalStream stream;
			if (m_streams.TryGetValue(token, out stream))
				stream.Reset();
		}

		public void ClearStream(string token)
		{
			ITemporalStream stream;
			if (m_streams.TryGetValue(token, out stream))
				stream.Clear();
		}

		object m_lock;
		public void Run()
		{
			HashSet<ITemporalContext> wait_context = new HashSet<ITemporalContext>();
			lock (m_lock)
			{
				do
				{
					while (m_reg_contexts.Count > 0)
					{
						var ctx = m_reg_contexts.Min;

						if (ctx.Activate())
							wait_context.Add(ctx);
						m_reg_contexts.Remove(ctx);
					}
					m_reg_contexts = new SortedSet<ITemporalContext>(wait_context, new PriorityComparer());
					wait_context.Clear();
				} while (m_reg_contexts.Count > 0);
			}
		}

        public static string GetTokenName()
        {
            return Guid.NewGuid().ToString("N");
        }
		internal class PriorityComparer : IComparer<ITemporalContext>
		{
			public int Compare(ITemporalContext x, ITemporalContext y)
			{
				//if (x.Priority == y.Priority)
				//	return x.GetHashCode() - y.GetHashCode();
				return x.Priority - y.Priority;
			}
		}

		internal class TemporalContextHelper<T> : ITemporalObserver<T>
		{
			TemporalContext<T> m_context;
			TemporalContextManager m_manager;
			public TemporalContextHelper(TemporalContext<T> context, TemporalContextManager manager)
			{
				m_context = context; 
				m_manager = manager;
				m_manager.AddTemporalValue("Contexts", new TemporalValue<object>(DateTime.Now.Ticks, m_context));
			}
			public void Reset() 
			{ 
				m_context.Reset();
				m_manager.m_reg_contexts.Add(m_context);
			}
			public void Apply(TemporalValue<T> value)
			{
				m_context.RegToken(value.TimeStamp);
				m_manager.m_reg_contexts.Add(m_context);
			}
		}

        internal class TemporalLinkedContextHelper<T> : ITemporalObserver<T>
        {
            int m_index;
            TemporalLinkedContext m_context;
            TemporalContextManager m_manager;
            public TemporalLinkedContextHelper(TemporalLinkedContext context, TemporalContextManager manager, int index)
            {
                m_index = index; m_context = context; m_manager = manager;
				m_manager.AddTemporalValue("Contexts", new TemporalValue<object>(DateTime.Now.Ticks, m_context));
            }
            public void Reset()
            {
                m_context.Reset();
                m_manager.m_reg_contexts.Add(m_context);
            }
            public void Apply(TemporalValue<T> value)
            {
                m_context.RegToken(m_index, value.TimeStamp);
                m_manager.m_reg_contexts.Add(m_context);
            }
        }
	}
}
