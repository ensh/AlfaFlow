using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class CandleAggregator : ITemporalObserver, IParameterized, IDisposable
	{
		TemporalContextManager m_manager;
		IDisposable m_unsubscriber;
		string m_token_in, m_token_out;
		long m_timeframe, m_nexttime;
		public CandleAggregator(TemporalContextManager manager, string token_in, string token_out, long timeframe)
		{
			m_manager = manager;
			m_token_in = token_in;
			m_token_out = token_out;
			m_timeframe = timeframe;
			m_nexttime = 0;
			m_values = new double[TemporalContextManager.CandleArraySize];
		}

		public string[] Tokens()
		{
			return new[]
			{
				m_token_out
			};
		}

		double[] m_values;

		#region ITemporalObserver
		public void Reset()
		{
			m_nexttime = 0;
			m_values = new double[TemporalContextManager.CandleArraySize];

			m_manager.ResetStream(m_token_out);
			m_manager.ClearStream(m_token_out);
		}

		public void Apply(TemporalValue value)
		{
			throw new NotImplementedException();
		}

		public void Apply(params TemporalValue[] values)
		{
			if (m_nexttime == 0)
			{
				for(int i = 0; i < values.Length; i++) m_values[i] = values[i];
				m_nexttime = values[0].TimeStamp + m_timeframe;
				return;
			}

			if (m_nexttime > values[0].TimeStamp)
			{
				m_values[TemporalContextManager.HighArrayIndex] = Math.Max(values[TemporalContextManager.HighArrayIndex].Value, 
					m_values[TemporalContextManager.HighArrayIndex]);
				m_values[TemporalContextManager.LowArrayIndex] = Math.Max(values[TemporalContextManager.LowArrayIndex].Value,
					m_values[TemporalContextManager.LowArrayIndex]);

				m_values[TemporalContextManager.CloseArrayIndex] = values[TemporalContextManager.CloseArrayIndex].Value;
				m_values[TemporalContextManager.OpenInterestArrayIndex] = values[TemporalContextManager.OpenInterestArrayIndex].Value;
				m_values[TemporalContextManager.VolumeArrayIndex] += values[TemporalContextManager.VolumeArrayIndex].Value;
				m_values[TemporalContextManager.VolumeAskArrayIndex] += values[TemporalContextManager.VolumeAskArrayIndex].Value;
				m_values[TemporalContextManager.VolumeBidArrayIndex] += values[TemporalContextManager.VolumeBidArrayIndex].Value;
				m_values[TemporalContextManager.TradesCountArrayIndex] += values[TemporalContextManager.TradesCountArrayIndex].Value;
			}
			else
			{
				TemporalValue[] result = m_values.Select(v => new TemporalValue(m_nexttime, v)).ToArray();
				m_manager.AddTemporalValues(m_token_out, result);

				for (int i = 0; i < values.Length; i++) m_values[i] = values[i];
				m_nexttime += m_timeframe;
			}
		}
		#endregion

		#region IParameterized
		public IndParameter[] GetParameters()
		{
			throw new NotImplementedException();
		}

		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Start" && parameters[1] == "KeepValues")
			{
				if (parameters.Length >= 3 && parameters[2] == "Priority")
					m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token_in }, parameters[1], parameters[2])[0];
				else
					m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token_in }, parameters[1])[0];
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
