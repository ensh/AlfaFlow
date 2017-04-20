using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndEnvelop : ITemporalObserver<double>, IParameterized, IDisposable
	{
		string m_token, m_restoken;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		IndSMA m_indSMA;
		public IndEnvelop(TemporalContextManager manager, string token)
		{
			_n = 30;
			_k = 1.5;
			m_token = token;
			m_manager = manager;
			m_restoken = TemporalContextManager.GetTokenName();
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
			m_indSMA.Reset();

			m_manager.ResetStream(m_restoken);
			m_manager.ClearStream(m_restoken);
		}

		ICollection<TemporalValue<double[]>> queue;
		public void Apply(TemporalValue<double> value)
		{
			if (queue == null)
			{
				queue = (ICollection<TemporalValue<double[]>>)m_manager[m_restoken];
				if (queue == null)
				{
					m_manager.AddTemporalValue(m_restoken,
						new TemporalValue<double[]>(value.TimeStamp, new[] { (1 - _k) * value.Value, (1 + _k) * value.Value }));
					return;
				}
			}

			queue.Add(new TemporalValue<double[]>(value.TimeStamp, new []{(1 - _k) * value.Value, (1 + _k) * value.Value}));
		}

		#endregion ITemporalObserver

		void Unsubscribe()
		{
			if (m_unsubscriber != null)
			{
				m_unsubscriber.Dispose();
				m_unsubscriber = null;
			}

			if (m_indSMA != null)
			{
				m_indSMA.Dispose();
				m_indSMA = null;
			}
		}

		#region IParametrized

		int _n;
		double _k;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "N" && parameters[1] == "K")
			{
				if (_n != parameters[0] || _k != parameters[1])
				{
					Unsubscribe();
					_n = parameters[0];
					_k = parameters[1];

					m_indSMA = new IndSMA(m_manager, m_token);
					m_indSMA.SetParameters(new IndParameter("Period", _n));
				}
			}

			if (parameters[0] == "Start")
			{
				IndParameter indexParamater = parameters.FirstOrDefault(p => p == "Index");
				IndParameter priorityParamater = parameters.FirstOrDefault(p => p == "Priority");

				if (indexParamater.Value != null)
					m_indSMA.SetParameters(
						new IndParameter("Start", true), 
						new IndParameter("Index", indexParamater),
						new IndParameter("Priority", TemporalContextManager.InternalIndicatorPriority)
						);
				else
					m_indSMA.SetParameters(new IndParameter("Start", true),
						new IndParameter("Priority", TemporalContextManager.InternalIndicatorPriority));

				if (priorityParamater.Value != null)
					m_unsubscriber = m_manager.AddTemporalObserver<double>(this, m_indSMA.Token, null, priorityParamater);
				else
					m_unsubscriber = m_manager.AddTemporalObserver<double>(this, m_indSMA.Token);
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("N", _n), new IndParameter("K", _k) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
