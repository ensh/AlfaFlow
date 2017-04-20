using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndPSAR: ITemporalObserver, IParameterized, IDisposable
	{
		string _token;
		string _out_token;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndPSAR(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_step = _maxstep = 1;
			_out_token = Tokens()[0];
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#PSAR#", String.Format("S={0}&M={1}", _step, _maxstep)),
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
		int _step, _maxstep;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "S" && parameters[1].Name == "M")
			{
				if (_step != (int)parameters[0].Value || _maxstep != (int)parameters[1].Value)
				{
					Unsubscribe();
					_step = (int)parameters[0].Value;
					_maxstep = (int)parameters[1].Value;
					_out_token = Tokens()[0];

					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token })[0];
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("S", _step), new IndParameter("M", _maxstep) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
