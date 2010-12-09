using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace ZLibNet
{
	public class ZList<X> : List<X>
	{
		public void Add(params X[] items)
		{
			foreach (X i in items)
				base.Add(i);
		}
		public void AddRange(IEnumerable items)
		{
			foreach (X i in items)
				base.Add(i);
		}
	}


	internal static class BitFlag
	{
		internal static bool IsSet(int bits, int flag)
		{
			return (bits & flag) == flag;
		}
		internal static bool IsSet(uint bits, uint flag)
		{
			return (bits & flag) == flag;
		}
		//internal static uint Set(uint bits, uint flag)
		//{
		//    return bits | flag;
		//}
		//internal static int Set(int bits, int flag)
		//{
		//    return bits | flag;
		//}
	}
}
