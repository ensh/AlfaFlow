using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndWMA : ITemporalObserver<double>, ITemporalObserver<double[]>, IParameterized, IDisposable
	{
		int m_period, m_index;
		string m_token, m_restoken;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		SortedSet<TemporalValue<double>> m_window;
		public IndWMA(TemporalContextManager manager, string token)
		{
			m_wma = 0;			
			m_period = 1;
			m_token = token;
			m_manager = manager;
			m_sumWeight = 0; for (int i = 1; i <= m_period; i++) m_sumWeight += i;
			m_window = new SortedSet<TemporalValue<double>>(new TemporalValueComparer<double>());
			m_restoken = TemporalContextManager.GetTokenName();
		}

		#region ITemporalObserver
		public void Reset()
		{
			m_wma = 0;
			m_window = new SortedSet<TemporalValue<double>>(new TemporalValueComparer<double>());
			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		double m_wma;
		public void Apply(TemporalValue<double> value)
		{
			if (m_period+1 == m_window.Count)
			{
                m_window.Remove(m_window.Min);
                m_wma = 0; double k = 1; 
                foreach (var tv in m_window) 
                { 
                    m_wma += tv * k; k++; 
                }

				m_manager.AddTemporalValue<double>(m_restoken, new TemporalValue<double>(m_window.Max.TimeStamp, m_wma / m_sumWeight));
			}

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
		double m_sumWeight = 0;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Period")
			{
				if (m_period != parameters[0])
				{
					Unsubscribe();
					m_wma = 0;
					m_period = parameters[0];
					m_sumWeight = 0; for (int i = 1; i <= m_period; i++) m_sumWeight += i;
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
