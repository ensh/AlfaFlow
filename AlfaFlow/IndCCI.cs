using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndCCI: ITemporalObserver, IParameterized, IDisposable
	{
		string _token;
		string _out_token;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndCCI(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_period = 1;
			_out_token = Tokens()[0];
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#CCI#", String.Format("Period={0}", _period)),
			};
		}

		#region ITemporalObserver
		public void Reset()
		{
			LastTimeStamp = 0;

			_manager.ResetStream(_out_token);
			_manager.ClearStream(_out_token);
		}

		public void Apply(TemporalValue value)
		{
			LastTimeStamp = value.TimeStamp;
			_manager.AddTemporalValue(_out_token, value);
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
					_out_token = Tokens()[0];
					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token })[0];
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
