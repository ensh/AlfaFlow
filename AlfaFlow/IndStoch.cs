using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndStoch : ITemporalObserver, IParameterized, IDisposable
	{
		string _token;
		string[] _out_tokens;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndStoch(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_nd = _nk = _ns = 1 ;
			_out_tokens = Tokens();
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#STOCH#", String.Format("NK={0}&ND={1}&NS={1}", _nk, _nd, _ns)),
				String.Concat(_token, "#STOCHD#", String.Format("NK={0}&ND={1}&NS={1}", _nk, _nd, _ns)),
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
		}

		public void Apply(TemporalValue value)
		{
			LastTimeStamp = value.TimeStamp;
			_manager.AddTemporalValue(_out_tokens[0], value);
			_manager.AddTemporalValue(_out_tokens[1], value);
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
		int _nk, _nd, _ns;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "NK" && parameters[1].Name == "ND" && parameters[2].Name == "NS")
			{
				if (_nk != (int)parameters[0].Value || _nd != (int)parameters[1].Value || _ns != (int)parameters[2].Value)
				{
					Unsubscribe();
					_nk = (int)parameters[0].Value;
					_nd = (int)parameters[1].Value;
					_ns = (int)parameters[2].Value;
					_out_tokens = Tokens();
					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token })[0];
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("NK", _nk), new IndParameter("ND", _nd), new IndParameter("NS", _ns) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
