using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndSMA : ITemporalObserver<double>, ITemporalObserver<double[]>, IParameterized, IDisposable
	{
		string m_token, m_restoken;
		int m_period, m_index;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		SortedSet<TemporalValue<double>> m_window;
		public IndSMA(TemporalContextManager manager, string token)
		{
			m_period = 1;
			m_token = token;
			m_manager = manager;
			m_window = new SortedSet<TemporalValue<double>>(new TemporalValueComparer<double>());
			m_restoken = TemporalContextManager.GetTokenName();
		}

		#region ITemporalObserver
		public void Reset()
		{
			m_sum = 0;
			m_window = new SortedSet<TemporalValue<double>>(new TemporalValueComparer<double>());
			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		double m_sum;
		ICollection<TemporalValue<double>> queue;
		public void Apply(TemporalValue<double> value)
		{
			if (m_period == m_window.Count)
			{
				var last = m_window.Min;

				//m_manager.AddTemporalValue(m_restoken, new TemporalValue<double>(m_window.Max.TimeStamp, m_sum / m_period));

				if (queue == null)
				{
					queue = (ICollection<TemporalValue<double>>)m_manager[m_restoken];
					if (queue == null)
					{
						m_manager.AddTemporalValue(m_restoken, new TemporalValue<double>(m_window.Max.TimeStamp, m_sum / m_period));
						goto icontinue;
					}
				}
				queue.Add(new TemporalValue<double>(m_window.Max.TimeStamp, m_sum / m_period));
				icontinue:
				m_sum -= last;
				m_window.Remove(last);
			}

			m_sum += value; 
			m_window.Add(value);			
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
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Period")
			{
				if (m_period != parameters[0])
				{
					Unsubscribe();
					m_sum = 0;
					m_period = parameters[0];
					m_window = new SortedSet<TemporalValue<double>>(new TemporalValueComparer<double>());
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
