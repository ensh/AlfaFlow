using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndBollinger : ITemporalObserver, IParameterized, IDisposable
	{
		string _token;
		string[] _out_tokens;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndBollinger(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_n = 1;
			_k = 1.0;
			_out_tokens = Tokens();
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#BOLM#", String.Format("N={0}&K={1}", _n, _k)),
				String.Concat(_token, "#BOLL#", String.Format("N={0}&K={1}", _n, _k)),
				String.Concat(_token, "#BOLU#", String.Format("N={0}&K={1}", _n, _k)),
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
			_manager.ClearStream(_out_tokens[2]);
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
		int _n;
		double _k;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "N" && parameters[1].Name == "K")
			{
				if (_n != (int)parameters[0].Value || _k != (double)parameters[1].Value)
				{
					Unsubscribe();
					_n = (int)parameters[0].Value;
					_k = (double)parameters[1].Value;
					_out_tokens = Tokens();
					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token })[0];
				}
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
