using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndMACD : ITemporalObserver<double[]>, IParameterized, IDisposable
	{
		int m_index;
		string m_token, m_restoken;
		TemporalContextManager m_manager;
		IDisposable m_unsubscriber;
		IndLine m_line;
		IndSignal m_signal;
		public IndMACD(TemporalContextManager manager, string token, int index = 0)
		{
			m_token = token;
			m_manager = manager;
			m_ns = m_nf = m_nsg = 1 ;
			m_restoken = TemporalContextManager.GetTokenName();
			m_index = index;
		}
		public string Token
		{
			get
			{
				return m_restoken;
			}
		}

		#region ITemporalObserver
		public void Reset()
		{
			m_line.Reset();

			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		ICollection<TemporalValue<double[]>> queue;
		public void Apply(TemporalValue<double[]> tv)
		{
			// line, signal, hist
			if (queue == null)
			{
				queue = (ICollection<TemporalValue<double[]>>)m_manager[m_restoken];
				if (queue == null)
				{
					m_manager.AddTemporalValue(m_restoken, 
						new TemporalValue<double[]>(tv.TimeStamp,
							new[] { tv.Value[0], tv.Value[1], tv.Value[0] - tv.Value[1] }));
					return;
				}
			}

			queue.Add(new TemporalValue<double[]>(tv.TimeStamp,
				new[] { tv.Value[0], tv.Value[1], tv.Value[0] - tv.Value[1] }));
		}
		#endregion ITemporalObserver

		void Unsubscribe()
		{
			if (m_unsubscriber != null)
			{
				m_unsubscriber.Dispose();
				m_unsubscriber = null;
			}

			if (m_signal != null)
			{
				m_signal.Dispose();
				m_signal = null;
			}

			if (m_line != null)
			{
				m_line.Dispose();
				m_line = null;
			}
		}

		#region IParametrized
		int m_nf, m_ns, m_nsg;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Fast" && parameters[1] == "Slow" && parameters[2] == "Signal")
			{
				if (m_nf != parameters[0] || m_ns != parameters[1] || m_nsg != parameters[2])
				{
					Unsubscribe();

					m_nf = parameters[0];
					m_ns = parameters[1];
					m_nsg = parameters[2];
				}
			}

			if (parameters[0] == "Start")
			{
				IndParameter indexParamater = parameters.FirstOrDefault(p => p == "Index");
				IndParameter priorityParamater = parameters.FirstOrDefault(p => p == "Priority");

				if (indexParamater.Value != null)
				{
					m_line = new IndLine(m_manager, m_token, m_nf, m_ns, indexParamater);
				}
				else
					m_line = new IndLine(m_manager, m_token, m_nf, m_ns, -1);

				m_signal = new IndSignal(m_manager, m_line.Token, m_nsg);

				if (priorityParamater.Value != null)
					m_unsubscriber = m_manager.AddTemporalObserver(this, m_signal.Token, null, priorityParamater);
				else
					m_unsubscriber = m_manager.AddTemporalObserver(this, m_signal.Token);
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("Fast", m_nf), new IndParameter("Slow", m_ns), new IndParameter("Signal", m_nsg) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}

		internal class IndLine : ITemporalObserver<double>, IDisposable
		{		
			string m_token;
			IDisposable[] m_unsubscribers;
			IndEMA m_indSlowEMA, m_indFastEMA;
			TemporalContextManager m_manager;
			public IndLine(TemporalContextManager manager, string token, int fastPeriod, int slowPeriod, int index)
			{
				m_manager = manager;
				m_unsubscribers = new IDisposable[2];

				m_indFastEMA = new IndEMA(m_manager, token);
				m_indFastEMA.SetParameters(new IndParameter("Period", fastPeriod));

				m_indSlowEMA = new IndEMA(m_manager, token);
				m_indSlowEMA.SetParameters(new IndParameter("Period", slowPeriod));

                m_token = TemporalContextManager.GetTokenName();

				m_indFastEMA.SetParameters(
					new IndParameter("Start", true),
					new IndParameter("Priority", TemporalContextManager.InternalIndicatorPriority + 6),
					new IndParameter((index == -1) ? "nIndex" : "Index", index));
				m_indSlowEMA.SetParameters(
					new IndParameter("Start", true),
					new IndParameter("Priority", TemporalContextManager.InternalIndicatorPriority + 7),
					new IndParameter((index == -1) ? "nIndex" : "Index", index));

				m_unsubscribers[0] = m_manager.AddTemporalQueueObserver<double>(this, m_indFastEMA.Token, "FastEMA", TemporalContextManager.InternalIndicatorPriority + 4);
				m_unsubscribers[1] = m_manager.AddTemporalQueueObserver<double>(this, m_indSlowEMA.Token, "SlowEMA", TemporalContextManager.InternalIndicatorPriority + 5);
			}

			public string Token
			{
				get
				{
					return m_token;
				}
			}

			#region ITemporalObserver
			public void Reset()
			{
				m_indFastEMA.Reset();
				m_indSlowEMA.Reset();

				m_manager.ResetStream(m_token);
				m_manager.ClearStream(m_token);
			}

			int m_calls;
			TemporalValue<double> m_fast;
			ICollection<TemporalValue<double>> queue;
			public void Apply(TemporalValue<double> value)
			{
				if (m_calls % 2 == 0) m_fast = value;
				else
				{
					if (queue == null) queue = (ICollection<TemporalValue<double>>)m_manager[m_token];
					//m_manager.SendTemporalValue<double>(m_token,
					//	new TemporalValue<double>((long)m_fast, (double)m_fast - (double)value));
					queue.Add(new TemporalValue<double>((long)m_fast, (double)m_fast - (double)value));
				}
				m_calls++;
			}
			#endregion

			void Unsubscribe()
			{
				if (m_unsubscribers != null)
				{
					foreach(var uns in m_unsubscribers) uns.Dispose();
					m_unsubscribers = null;
				}

				if (m_indFastEMA != null)
				{
					m_indFastEMA.Dispose();
					m_indFastEMA = null;
				}

				if (m_indSlowEMA != null)
				{
					m_indSlowEMA.Dispose();
					m_indSlowEMA = null;
				}
			}

			public void Dispose()
			{
				Unsubscribe();
			}
		}

		internal class IndSignal : ITemporalObserver<double>, IDisposable
		{
			string m_token;
			IDisposable[] m_unsubscribers;
			TemporalContextManager m_manager;
			IndEMA m_indSignalEMA;
			public IndSignal(TemporalContextManager manager, string token, int signalPeriod)
			{
				m_manager = manager;
				m_unsubscribers = new IDisposable[2];

				m_token = TemporalContextManager.GetTokenName();

				m_indSignalEMA = new IndEMA(m_manager, token);
				m_indSignalEMA.SetParameters(new IndParameter("Period", signalPeriod));

				m_unsubscribers[0] = m_manager.AddTemporalQueueObserver<double>(this, m_indSignalEMA.Token, "SignalEMA",
					TemporalContextManager.InternalIndicatorPriority + 3);
				m_unsubscribers[1] = m_manager.AddTemporalQueueObserver<double>(this, token, "Line", 
					TemporalContextManager.InternalIndicatorPriority + 2);

				m_manager.AddTemporalQueue<double[]>(m_token, "Line&Signal");

				m_indSignalEMA.SetParameters(
					new IndParameter("Start", true), 
					new IndParameter("Priority", TemporalContextManager.InternalIndicatorPriority + 1));

			}

			public string Token
			{
				get
				{
					return m_token;
				}
			}

			#region ITemporalObserver
			public void Reset()
			{
				m_indSignalEMA.Reset();

				m_manager.ResetStream(m_token);
				m_manager.ClearStream(m_token);
			}

			int m_calls;
			double m_line;
			ICollection<TemporalValue<double[]>> queue;
			public void Apply(TemporalValue<double> tv)
			{
				if (m_calls %2 == 0) m_line = tv.Value;
				else
				{
					if (queue == null) queue = (ICollection<TemporalValue<double[]>>)m_manager[m_token];
					//m_manager.SendTemporalValue<double[]>(m_token, 
					//	new TemporalValue<double[]>((long)tv, new []{ m_line, tv.Value}));
					queue.Add(new TemporalValue<double[]>((long)tv, new[] { m_line, tv.Value }));
				}
				m_calls++;
			}
			#endregion

			void Unsubscribe()
			{
				if (m_unsubscribers != null)
				{					
					foreach(var uns in m_unsubscribers) uns.Dispose();
					m_unsubscribers = null;
				}

				if (m_indSignalEMA != null)
				{
					m_indSignalEMA.Dispose();
					m_indSignalEMA = null;
				}
			}

			public void Dispose()
			{
				Unsubscribe();
			}
		}
	}

}
