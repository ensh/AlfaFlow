using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaFlow
{
	public struct IndParameter
	{
		public readonly string Name;
		public readonly object Value;

		public IndParameter(string name, object value)
		{
			Name = name; Value = value;
		}

		public override bool Equals(object obj)
		{
			IndParameter key = (IndParameter)obj;
			return String.CompareOrdinal(key.Name, Name) == 0 && key.Value.Equals(Value);
		}

		public bool Equals(IndParameter key)
		{
			return String.CompareOrdinal(key.Name, Name) == 0 && key.Value.Equals(Value);
		}

		public override int GetHashCode()
		{
			return Name.ToCharArray().Sum(c => (int)c) ^ Value.GetHashCode();
		}

		public static implicit operator int(IndParameter p)
		{
			return (int)p.Value;
		}

		public static implicit operator long(IndParameter p)
		{
			return (long)p.Value;
		}

		public static implicit operator bool(IndParameter p)
		{
			return (bool)p.Value;
		}

		public static implicit operator double(IndParameter p)
		{
			return (double)p.Value;
		}

        public static implicit operator float(IndParameter p)
        {
            return (float)p.Value;
        }

		public static implicit operator string(IndParameter p)
		{
			return p.Name;
		}
	}

	public interface IParameterized
	{
		IndParameter[] GetParameters();
		void SetParameters(params IndParameter[] parameters);
	}
}
