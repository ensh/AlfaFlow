using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public class IndMFI: ITemporalObserver, IParameterized, IDisposable
	{
		string[] _tokens;
		string _out_token;
		TemporalContextManager _manager;


		IDisposable _unsubscriber;
		//TemporalSync _sync_values;
		public IndMFI(TemporalContextManager manager, string[] tokens)
		{
			_manager = manager;
			_tokens = tokens;
			_tp = _psum = _nsum = 0;
			_pos_flow_window = new double[1];
			_neg_flow_window = new double[1];
			_out_token = Tokens()[0];
		}
		public string[] Tokens()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var t in _tokens)
				sb.Append(t + "#");
			sb.AppendFormat("MFI#Period={0}", _pos_flow_window.Length);

			return new[] { sb.ToString() };
		}

		#region ITemporalObserver
		public void Reset()
		{
			LastTimeStamp = 0;
			_tp = _psum = _nsum = 0;
			_pos_flow_window = new double[_pos_flow_window.Length];
			_neg_flow_window = new double[_neg_flow_window.Length];
			_window_idx = 0;
			_manager.ResetStream(_out_token);
			_manager.ClearStream(_out_token);
		}

		public void Apply(TemporalValue value)
		{
			LastTimeStamp = value.TimeStamp;
			_manager.AddTemporalValue(_out_token, value);
		}

		double _tp, _psum, _nsum;
		//MAX#MIN#CLOSE#VOL
		public void Apply(params TemporalValue[] values)
		{
			LastTimeStamp = values[0].TimeStamp;
			double TP = (values[0].Value + values[1].Value + values[2].Value) / 3;

			if (_tp == 0)
			{
				_tp = TP;
				return;
			}

			double posFlow = 0, negFlow = 0;

			if (_tp != TP)
			{
				if (_tp > TP)
				{
					negFlow = TP * values[3].Value;
				}
				else
				{
					posFlow = TP * values[3].Value;
				}
			}

			int i = _window_idx % _pos_flow_window.Length;

			_psum += posFlow - _pos_flow_window[i]; 
			_nsum += negFlow - _neg_flow_window[i];

			_pos_flow_window[i] = posFlow;
			_neg_flow_window[i] = negFlow;
			_window_idx++;

			if (_window_idx >= _neg_flow_window.Length && _nsum != 0)
				_manager.AddTemporalValue(_out_token, new TemporalValue(values[0].TimeStamp, 100 * (1 - 1/(1 + _psum / _nsum))));
		}
		public long LastTimeStamp { get; set; }
		#endregion ITemporalObserver

		void Unsubscribe()
		{
			if (_unsubscriber != null)
			{
				_unsubscriber.Dispose();
				_unsubscriber = null;
			}

		}

		#region IParametrized
		public void SetParameters(params IndParameter[] parameters)
		{
			if (parameters[0].Name == "Period")
			{
				if (_pos_flow_window.Length != (int)parameters[0].Value)
				{
					Unsubscribe();
					_window_idx = 0;
					_pos_flow_window = new double[(int)parameters[0].Value];
					_neg_flow_window = new double[(int)parameters[0].Value];
					_out_token = Tokens()[0];

                    //_sync_values = new TemporalSync(_manager, _tokens);
                    //_sync_values.SetParameters(new IndParameter("Delta", (long)1));
                    //_unsubscriber = _manager.AddTemporalObserver(this, new[] { _sync_values.Tokens()[0] }, false)[0];
				}
			}
		}

		public IndParameter[] GetParameters()
		{
			return new[] { new IndParameter("Period", _pos_flow_window.Length) };
		}
		#endregion IParameterized
		int _window_idx;
		double[] _pos_flow_window, _neg_flow_window;
		public void Dispose()
		{
			Unsubscribe();
		}
	}
}
