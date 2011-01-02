using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace ZLibNet
{
	internal class FixedArray : IDisposable
	{
		GCHandle pHandle;
		Array pArray;

		public FixedArray(Array array)
		{
			pArray = array;
			pHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
		}
		#region IDisposable Members

		public void Dispose()
		{
			pHandle.Free();
		}

		public IntPtr this[int idx]
		{
			get
			{
				return Marshal.UnsafeAddrOfPinnedArrayElement(pArray, idx);
			}
		}
		public static implicit operator IntPtr(FixedArray fixedArray)
		{
			return fixedArray[0];
		}
		#endregion
	}

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
