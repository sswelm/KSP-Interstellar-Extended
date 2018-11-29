using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FNPlugin.Extensions
{
	public static class Decimal
	{
		public static decimal ToDecimal(this float input)
		{
			if (input < (double)decimal.MinValue)
				return decimal.MinValue;
			else if (input > (double)decimal.MaxValue)
				return decimal.MaxValue;
			else
				return (decimal)input;
		}

		public static decimal ToDecimal(this double input)
		{
			if (input < (double)decimal.MinValue)
				return decimal.MinValue;
			else if (input > (double)decimal.MaxValue)
				return decimal.MaxValue;
			else
				return (decimal)input;
		}

		// x - a number, from which we need to calculate the square root
		// epsilon - an accuracy of calculation of the root from our number.
		// The result of the calculations will differ from an actual value
		// of the root on less than epslion.
		public static decimal Sqrt(this decimal x, decimal epsilon = 0.0M)
		{
			if (x < 0)
				return 0; 	//throw new OverflowException("Cannot calculate square root from a negative number");

			decimal current = (decimal)Math.Sqrt((double)x), previous;
			do
			{
				previous = current;
				if (previous == 0.0M) return 0;
				current = (previous + x / previous) / 2;
			}
			while (Math.Abs(previous - current) > epsilon);
			return current;
		}



		// Adjust this to modify the precision
		public const int ITERATIONS = 27;

		public static int Factorial(int n)
		{
			int j = 1;
			for (int i = 1; i <= n; i++) { j = j * i; }
			return j;
		}

		// power series
		public static decimal DecimalExp(decimal power)
		{
			int iteration = ITERATIONS;
			decimal result = 1;
			while (iteration > 0)
			{
				decimal fatorial = Factorial(iteration);
				result += Pow(power, iteration) / fatorial;
				iteration--;
			}
			return result;
		}

		// natural logarithm series
		public static decimal LogN(decimal number)
		{
			decimal aux = (number - 1);
			decimal result = 0;
			uint iteration = ITERATIONS;
			while (iteration > 0)
			{
				result += Pow(aux, iteration) / iteration;
				iteration--;
			}
			return result;
		}

		// From http://www.daimi.au.dk/~ivan/FastExpproject.pdf
		// Left to Right Binary Exponentiation
		public static decimal Pow(this decimal x, uint y)
		{
			decimal A = 1m;
			BitArray e = new BitArray(BitConverter.GetBytes(y));
			int t = e.Count;

			for (int i = t - 1; i >= 0; --i)
			{
				A *= A;
				if (e[i] == true)
				{
					A *= x;
				}
			}
			return A;
		}

		// from https://pastebin.com/ZXpn4cvh
		public static decimal Pow(decimal baseValue, decimal expValue)
		{
			return DecimalExp(expValue * LogN(baseValue));
		}
	}
}
