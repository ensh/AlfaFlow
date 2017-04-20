using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndADX : ITemporalObserver, IParameterized, IDisposable
	{
		string _token;
		string[] _out_tokens;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndADX(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_period = 1;
			_out_tokens = Tokens();
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#ADX#", String.Format("Period={0}", _period)),
				String.Concat(_token, "#ADXPDI#", String.Format("Period={0}", _period)),
				String.Concat(_token, "#ADXNDI#", String.Format("Period={0}", _period)),
			};
		}

		#region ITemporalObserver
		public void Reset()
		{
			LastTimeStamp = 0;

			_manager.ResetStream(_out_tokens[0]);
			_manager.ClearStream(_out_tokens[0]);
			_manager.ResetStream(_out_tokens[1]);
			_manager.ClearStream(_out_tokens[1]);
			_manager.ResetStream(_out_tokens[2]);
			_manager.ClearStream(_out_tokens[3]);
		}

		public void Apply(TemporalValue value)
		{
			LastTimeStamp = value.TimeStamp;
			_manager.AddTemporalValue(_out_tokens[0], value);
			_manager.AddTemporalValue(_out_tokens[1], value);
			_manager.AddTemporalValue(_out_tokens[2], value);
		}
		public void Apply(params TemporalValue[] value)
		{
			throw new NotImplementedException();
		}
		public long LastTimeStamp { get; set; }
		#endregion ITemporalObserver

		void Unsubscribe()
		{
			if (_unsubscribe != null)
			{
				_unsubscribe.Dispose();
				_unsubscribe = null;
			}
		}

		#region IParametrized
		int _period;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "Period")
			{
				if (_period != (int)parameters[0].Value)
				{
					Unsubscribe();
					_period = (int)parameters[0].Value;
					_out_tokens = Tokens();
					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token }, (bool)parameters[0].Value)[0];
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("Period", _period) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
