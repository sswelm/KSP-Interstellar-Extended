using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FNPlugin.Extensions
{
	public static class Approximate
	{
		public static float FourthRoot(this float z)
		{
			return Sqrt(Sqrt(z));
		}

		public static float Sqrt(this int z)
		{
			return ((float)z).Sqrt();
		}

		public static float Sqrt(this double z)
		{
			return ((float)z).Sqrt();
		}

		public static float Sqrt(this float z)
		{
			if (z == 0) return 0;
			FloatIntUnion u;
			u.tmp = 0;
			u.f = z;
			u.tmp -= 1 << 23; /* Subtract 2^m. */
			u.tmp >>= 1; /* Divide by 2. */
			u.tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
			return u.f;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatIntUnion
		{
			[FieldOffset(0)]
			public float f;

			[FieldOffset(0)]
			public int tmp;
		}
	}
}
