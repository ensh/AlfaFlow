using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace AlfaFlow
{
	public class IndSlice : ITemporalObserver, IParameterized, IDisposable		
	{
		string m_token;
		string[] m_outTokens;
		IDisposable m_unsubscriber;
		TemporalContextManager m_manager;
		public IndSlice(TemporalContextManager manager, string token)
		{
			m_manager = manager;
			m_token = token;
			m_values = new TemporalValue[0];
			m_columns = new IndParameter[0];
			m_outTokens = new string[0];
		}

		#region ITemporalObserver
		public void Reset()
		{
			m_values = new TemporalValue[m_values.Length];
			for (int i = 0; i < m_outTokens.Length; i++)
			{
				m_manager.ResetStream(m_outTokens[i]);
				m_manager.ClearStream(m_outTokens[i]);
			}
		}

		Action<TemporalValue[], TemporalValue[]> apply;

		TemporalValue[] m_values;
		public void Apply(TemporalValue value)
		{
			//if (m_period == m_window.Count)
			//{
			//	var last = m_window.Min;
			//	m_manager.AddTemporalValue(m_outToken, new TemporalValue(m_window.Max.TimeStamp, m_sum / m_period));

			//	m_sum -= last;
			//	m_window.Remove(last);
			//}

			//m_sum += value; 
			//m_window.Add(value);			
		}
		public void Apply(params TemporalValue[] values)
		{
			apply(values, m_values);
			if (m_outTokens.Length == 1)
				m_manager.AddTemporalValues(m_outTokens[0], m_values);
			else
			{
				for (int i = 0; i < m_outTokens.Length; i++)
				{
					m_manager.AddTemporalValue(m_outTokens[i], m_values[i]);
				}
			}			
		}
		#endregion ITemporalObserver

		public string[] Tokens()
		{
			return m_outTokens;
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

		IndParameter[] m_columns;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0] == "Tokens" && parameters.Length > 1)
			{
				Unsubscribe();
				m_outTokens = new string[0];
				m_values = new TemporalValue[0];
				m_columns = parameters;
				ApplyColumnParameters(parameters[0]);
			}

			if (parameters[0] == "Start")
			{
				if (apply == null || m_columns.Length < 2)
					throw new NullReferenceException();

				if (parameters.Length >= 2 && parameters[1] == "Priority")
					m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token }, true, parameters[1])[0];				
				else
					m_unsubscriber = m_manager.AddTemporalObserver(this, new[] { m_token })[0];					
			}
		}

		void ApplyColumnParameters(int count)
		{
			if (count == 1)
			{
				m_values = new TemporalValue[m_columns.Skip(1).Count(c => (bool)c)];
				m_outTokens = new string[1];

				StringBuilder sb = new StringBuilder(m_token);
				for (int i = 1; i < m_columns.Length; i++)
					if (m_columns[i])
						sb.Append(String.Concat("#", m_columns[i]));
				m_outTokens[0] = sb.ToString();
			}
			else
			{
				if (count != m_columns.Skip(1).Count(c => (bool)c))
					throw new ArgumentOutOfRangeException();

				m_values = new TemporalValue[count];
				m_outTokens = new string[count];

				for (int i = 1, j = 0; i < m_columns.Length; i++)
					if (m_columns[i])
						m_outTokens[j++] = String.Concat(m_token, "#", m_columns[i]);
			}

			ParameterExpression vout = Expression.Parameter(typeof(TemporalValue[]), "vout");
			ParameterExpression vin = Expression.Parameter(typeof(TemporalValue[]), "vin");

			Func<ParameterExpression, ParameterExpression, int, int, Expression> CopyValueCreator =
				(ParameterExpression _vin, ParameterExpression _vout, int from, int to) =>
					Expression.Assign
						(
							Expression.ArrayAccess(_vout, Expression.Constant(to)),
							Expression.ArrayAccess(_vin, Expression.Constant(from))
						);

			List<Expression> exprs = new List<Expression>();

			for (int i = 1, j = 0; i < m_columns.Length; i++)
			{
				if (m_columns[i])
					exprs.Add(CopyValueCreator(vin, vout, i - 1, j++));
			}

			Expression exprResult = Expression.Block(exprs); 

			var funcResult = Expression.Lambda<Action<TemporalValue[], TemporalValue[]>>(exprResult, new[] { vin, vout });

			apply = funcResult.Compile();
		}

		public IndParameter[] GetParameters()
		{
			return m_columns;
		}
		#endregion IParameterized
		public void Dispose()
		{
			Unsubscribe();
		}

	}

}
