using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndOhlcAggregator : ITemporalObserver<double[]>, IParameterized, IDisposable
	{
		string m_token, m_restoken;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		public IndOhlcAggregator(TemporalContextManager manager, string token)
		{
			m_token = token;
			m_restoken = TemporalContextManager.GetTokenName();
			m_manager = manager;
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
			m_nexttimeframe = 0;
			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		TemporalValue<double[]> m_value;
		public void Apply(TemporalValue<double[]> newCandle)
		{
			if (m_nexttimeframe == 0)
			{
				m_nexttimeframe = newCandle.TimeStamp + m_timeframe;
				m_value = CandleFactory.GetOhlcCandle(m_nexttimeframe, newCandle.Value);
			}
			else
			{
				if (newCandle.TimeStamp < m_nexttimeframe)
				{
					m_value.SetClose(newCandle.Close());
					m_value.SetOpenInterest(newCandle.OpenInterest());

					m_value.SetHigh(Math.Max(newCandle.High(), m_value.High()));
					m_value.SetLow(Math.Min(newCandle.Low(), m_value.Low()));

					m_value.SetVolume(m_value.Volume() + newCandle.Volume());
					m_value.SetVolumeAsk(m_value.VolumeAsk() + newCandle.VolumeAsk());
				}
				else
				{
					m_manager.AddTemporalValue(m_restoken, m_value);
					m_nexttimeframe = newCandle.TimeStamp + m_timeframe;
					m_value = CandleFactory.GetOhlcCandle(m_nexttimeframe, newCandle.Value);
				}
			}
		}
		#endregion

		#region IParametrized

		long m_timeframe, m_nexttimeframe;
		public IndParameter[] GetParameters()
		{
			throw new NotImplementedException();
		}

		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Timeframe")
			{
				if (m_timeframe != parameters[0])
				{
					Unsubscribe();
					m_timeframe = parameters[0];
					m_nexttimeframe = 0;
				}
			}

			if (parameters[0] == "Start")
			{
				IndParameter priorityParamater = parameters.FirstOrDefault(p => p == "Priority");

				if (priorityParamater.Value != null)
					m_unsubscriber = m_manager.AddTemporalObserver(this, m_token, null, priorityParamater);
				else
					m_unsubscriber = m_manager.AddTemporalObserver(this, m_token);
			}
		}
		#endregion

		void Unsubscribe()
		{
			if (m_unsubscriber != null)
			{
				m_unsubscriber.Dispose();
				m_unsubscriber = null;
			}
		}
		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
