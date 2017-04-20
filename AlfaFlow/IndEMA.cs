using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndEMA : ITemporalObserver<double>, ITemporalObserver<double[]>, IDisposable, IParameterized
	{
		int m_period, m_index;
		string m_token, m_restoken;
		TemporalContextManager m_manager;
		IDisposable m_unsubscriber;
		public IndEMA(TemporalContextManager manager, string token)
		{
			m_period = 1;
			m_token = token;
			m_manager = manager;
			m_KC = (double)2 / (m_period + 1);
			m_KE = 1 - m_KC;
			m_restoken = TemporalContextManager.GetTokenName();
			m_next = false;
		}

		#region ITemporalObserver
		public void Reset()
		{
			m_ema = 0;
			m_next = false;
			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		bool m_next;
		double m_ema;
		ICollection<TemporalValue<double>> queue;
		public void Apply(TemporalValue<double> value)
		{
			m_ema = (m_next) ? m_KE * m_ema + m_KC * value : value;
			//m_manager.AddTemporalValue(m_restoken, new TemporalValue<double>(value.TimeStamp, m_ema));

			if (queue == null)
			{
				queue = (ICollection<TemporalValue<double>>)m_manager[m_restoken];
				if (queue == null)
				{
					m_manager.AddTemporalValue(m_restoken, new TemporalValue<double>(value.TimeStamp, m_ema));
					return;
				}
			}
			queue.Add(new TemporalValue<double>(value.TimeStamp, m_ema));
			m_next = true;
        }
		public void Apply(TemporalValue<double[]> tv)
		{
			Apply(new TemporalValue<double>((long)tv, tv.Value[m_index]));
		}
		#endregion ITemporalObserver

		public string Token
		{
			get
			{
				return m_restoken;
			}
		}
		void Unsubscribe()
		{
			if (m_unsubscriber != null)
			{
				m_unsubscriber.Dispose();
				m_unsubscriber = null;
			}
		}

		#region IParametrized
		double m_KC, m_KE;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Period")
			{
				if (m_period != parameters[0])
				{
					Unsubscribe();

					m_ema = 0;
					m_period = parameters[0];
					m_KC = (double)2 / (m_period + 1);
					m_KE = 1 - m_KC;
				}
			}

			if (parameters[0] == "Start")
			{
				IndParameter indexParamater = parameters.FirstOrDefault(p => p == "Index");
				IndParameter priorityParamater = parameters.FirstOrDefault(p => p == "Priority");

				if (indexParamater.Value != null)
				{
					m_index = indexParamater;
					if (priorityParamater.Value != null)
						m_unsubscriber = m_manager.AddTemporalObserver<double[]>(this, m_token, priorityParamater);
					else
						m_unsubscriber = m_manager.AddTemporalObserver<double[]>(this, m_token);
				}
				else
				{
					if (priorityParamater.Value != null)
						m_unsubscriber = m_manager.AddTemporalObserver<double>(this, m_token, null, priorityParamater);
					else
						m_unsubscriber = m_manager.AddTemporalObserver<double>(this, m_token);
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("Period", m_period) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}

	}
}
