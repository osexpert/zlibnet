using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

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

		~FixedArray()
		{
			pHandle.Free();
		}

		#region IDisposable Members

		public void Dispose()
		{
			pHandle.Free();
			GC.SuppressFinalize(this);
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


	internal static class StreamHelper
	{
		public static void CopyTo(this Stream stream, Stream destination)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (!stream.CanRead && !stream.CanWrite)
			{
				throw new ObjectDisposedException("StreamClosed");
			}
			if (!destination.CanRead && !destination.CanWrite)
			{
				throw new ObjectDisposedException("destination", "StreamClosed");
			}
			if (!stream.CanRead)
			{
				throw new NotSupportedException("UnreadableStream");
			}
			if (!destination.CanWrite)
			{
				throw new NotSupportedException("UnwritableStream");
			}
			InternalCopyTo(stream, destination, 0x1000);
		}

		public static void CopyTo(this Stream stream, Stream destination, int bufferSize)
		{
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize", "NeedPosNum");
			}
			if (!stream.CanRead && !stream.CanWrite)
			{
				throw new ObjectDisposedException("StreamClosed");
			}
			if (!destination.CanRead && !destination.CanWrite)
			{
				throw new ObjectDisposedException("destination", "StreamClosed");
			}
			if (!stream.CanRead)
			{
				throw new NotSupportedException("UnreadableStream");
			}
			if (!destination.CanWrite)
			{
				throw new NotSupportedException("UnwritableStream");
			}
			InternalCopyTo(stream, destination, bufferSize);
		}

		private static void InternalCopyTo(Stream src, Stream destination, int bufferSize)
		{
			int num;
			byte[] buffer = new byte[bufferSize];
			while ((num = src.Read(buffer, 0, buffer.Length)) != 0)
			{
				destination.Write(buffer, 0, num);
			}
		}
	}
}
