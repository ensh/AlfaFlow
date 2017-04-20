using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndMomentum: ITemporalObserver, IParameterized, IDisposable
	{
		string m_token, m_tokens;
        int m_period, m_index;
        TemporalContextManager m_manager;
		IDisposable m_unsubscriber;
        public IndMomentum(TemporalContextManager manager, string token, int index = 0)
		{
			m_manager = manager;
			m_token = token;
			m_window = new SortedSet<TemporalValue>(new TemporalValueComparer());
            m_period = 1;
            m_tokens = TemporalContextManager.GetTokenName();
			m_index = index;
		}
		public string[] Tokens()
		{
			return new[] 
			{
                m_tokens
				//String.Concat(m_token, "#MOM#", String.Format("Period={0}", m_period)),
			};
		}

		#region ITemporalObserver
		public void Reset()
		{
            m_window = new SortedSet<TemporalValue>(new TemporalValueComparer());
			m_manager.ResetStream(m_tokens);
			m_manager.ClearStream(m_tokens);
		}

		public void Apply(TemporalValue value)
		{
            if (m_period == m_window.Count)
            {
                m_manager.AddTemporalValue(m_tokens, 
                    new TemporalValue(m_window.Max.TimeStamp, m_window.Max.Value / m_window.Min.Value));
                m_window.Remove(m_window.Min);
            }

            m_window.Add(value);	
		}
		public void Apply(params TemporalValue[] values)
		{
            Apply(values[m_index]);
		}
		#endregion ITemporalObserver

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
                    m_window = new SortedSet<TemporalValue>(new TemporalValueComparer());
					m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token })[0];
				}
			}

            if (parameters[0] == "Start" && parameters[1] == "KeepValues")
            {
                if (parameters.Length >= 3 && parameters[2] == "Priority")
                    m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token }, parameters[1], parameters[2])[0];
                else
                    m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token }, parameters[1])[0];
            }
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("Period", m_period) };
		}
		#endregion IParameterized

		SortedSet<TemporalValue> m_window;
		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
