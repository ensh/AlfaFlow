using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
    public class IndTemporalAggregator<T> : ITemporalObserver<T>, IParameterized, IDisposable
    {
		string m_token, m_restoken;
        int m_period;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		SortedSet<TemporalValue<double>> m_window;
        public IndTemporalAggregator(TemporalContextManager manager, string token)
		{
			m_period = 1;
			m_token = token;
            m_start = m_end = 0;
			m_manager = manager;
			m_restoken = TemporalContextManager.GetTokenName();
		}

		#region ITemporalObserver
		public void Reset()
		{
            m_start = m_end = 0;
			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

        long m_start, m_end;
		public void Apply(TemporalValue<T> value)
		{
            if (m_end >= value.TimeStamp - m_period)
            {
                m_end = value.TimeStamp;
                return;
            } else
                if (m_start == 0 && m_end == 0)
                {
                    m_start = m_end = value.TimeStamp;
                    return;
                }

            m_manager.AddTemporalValue(m_restoken, new TemporalValue<long>(m_start, m_end));
            m_start = m_end = value.TimeStamp;
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

		public TemporalValue<long> Current
		{
			get { return new TemporalValue<long>(m_start, m_end); }
		}

		public void EmitLastValue()
		{
			m_manager.AddTemporalValue(m_restoken, new TemporalValue<long>(m_start, m_end));
		}

		#region IParametrized
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Period")
			{
				if (m_period != parameters[0])
				{
					Unsubscribe();
					m_period = parameters[0];
                }
			}

			if (parameters[0] == "Start")
			{
				IndParameter priorityParamater = parameters.FirstOrDefault(p => p == "Priority");
								
				if (priorityParamater.Value != null)
					m_unsubscriber = m_manager.AddTemporalObserver<T>(this, m_token, null, priorityParamater);				
				else
					m_unsubscriber = m_manager.AddTemporalObserver<T>(this, m_token);					
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
