using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AlfaFlow
{

	public enum OhlcTemporalValue
	{ 
		Open = 0, 
		High = 1, 
		Low = 2, 
		Close = 3, 
		Volume = 4, 
		OpenInterest = 5, 
		VolumeAsk = 6,
		ArraySizeValue = 7
	}

	public static class CandleFactory
	{
		public static TemporalValue<double[]> GetOhlcCandle(long timeStamp)
		{
			return new TemporalValue<double[]>(timeStamp, 
				new double[(int)OhlcTemporalValue.ArraySizeValue]);
		}
		public static TemporalValue<double[]> GetOhlcCandle(long timeStamp, double[] candleValues)
		{
			var newCandleValues = new double[(int)OhlcTemporalValue.ArraySizeValue];
			candleValues.CopyTo(newCandleValues, 0);
			return new TemporalValue<double[]>(timeStamp, newCandleValues); 
		}
		public static bool IsOhlcTemporalValue(this TemporalValue<double[]> tv)
		{
			return tv.Value.Length == (int)OhlcTemporalValue.ArraySizeValue;
		}
		public static double Open(this TemporalValue<double[]> tv)
		{
			return tv.Value[(int)OhlcTemporalValue.Open];
		}
		public static double High(this TemporalValue<double[]> tv)
		{
			return tv.Value[(int)OhlcTemporalValue.High];
		}
		public static double Low(this TemporalValue<double[]> tv)
		{
			return tv.Value[(int)OhlcTemporalValue.Low];
		}
		public static double Close(this TemporalValue<double[]> tv)
		{
			return tv.Value[(int)OhlcTemporalValue.Close];
		}
		public static long Volume(this TemporalValue<double[]> tv)
		{

			return new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.Volume];
		}
		public static long OpenInterest(this TemporalValue<double[]> tv)
		{
			return new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.OpenInterest];
		}
		public static long VolumeAsk(this TemporalValue<double[]> tv)
		{
			return new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.VolumeAsk];
		}

		public static void SetOpen(this TemporalValue<double[]> tv, double value)
		{
			tv.Value[(int)OhlcTemporalValue.Open] = value;
		}
		public static void SetClose(this TemporalValue<double[]> tv, double value)
		{
			tv.Value[(int)OhlcTemporalValue.Close] = value;
		}
		public static void SetHigh(this TemporalValue<double[]> tv, double value)
		{
			tv.Value[(int)OhlcTemporalValue.High] = value;
		}
		public static void SetLow(this TemporalValue<double[]> tv, double value)
		{
			tv.Value[(int)OhlcTemporalValue.Low] = value;
		}
		public static void SetVolume(this TemporalValue<double[]> tv, long value)
		{
			new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.Volume] = value;
		}
		public static void SetOpenInterest(this TemporalValue<double[]> tv, long value)
		{
			new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.OpenInterest] = value;
		}
		public static void SetVolumeAsk(this TemporalValue<double[]> tv, long value)
		{
			new Double2LongConverter(tv.Value).longArray[(int)OhlcTemporalValue.VolumeAsk] = value;
		}

		[StructLayout(LayoutKind.Explicit)]
		internal struct Double2LongConverter
		{
			[FieldOffset(0)]
			public double[] doubleArray;
			[FieldOffset(0)]
			public long[] longArray;
			public Double2LongConverter(double[] array)
			{
				longArray = null;
				doubleArray = array;
			}
			public Double2LongConverter(long[] array)
			{
				doubleArray = null;
				longArray = array;
			}
		}
	}
}
