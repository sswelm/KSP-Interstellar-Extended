using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
	public static class Safe
	{
		public static decimal ToDecimal(this double input)
		{
			if (input < (double)decimal.MinValue)
				return decimal.MinValue;
			else if (input > (double)decimal.MaxValue)
				return decimal.MaxValue;
			else
				return (decimal)input;
		}
	}
}
