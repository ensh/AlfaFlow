using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndAO: ITemporalObserver<double>, IParameterized, IDisposable
	{
		string _token;
		string _out_token;
		TemporalContextManager _manager;
		IDisposable _unsubscribe;
		public IndAO(TemporalContextManager manager, string token)
		{
			_manager = manager;
			_token = token;
			_ns = _nf = 1;
			_out_token = Tokens()[0];
		}
		public string[] Tokens()
		{
			return new[] 
			{
				String.Concat(_token, "#AO#", String.Format("NS={0}&NF={1}", _ns, _nf)),
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
		int _ns, _nf;
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "NS" && parameters[1].Name == "NF")
			{
				if (_ns != (int)parameters[0].Value || _nf != (int)parameters[1].Value)
				{
					Unsubscribe();
					_ns = (int)parameters[0].Value;
					_nf = (int)parameters[1].Value;
					_out_token = Tokens()[0];
					_unsubscribe = _manager.AddTemporalObserver(this, new[] { _token }, (bool)parameters[0].Value)[0];
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("NS", _ns), new IndParameter("NF", _nf) };
		}
		#endregion IParameterized

		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
