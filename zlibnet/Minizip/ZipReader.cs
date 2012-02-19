using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace ZLibNet
{
	/// </code>
	/// </example>
	public class ZipReader : IEnumerable<ZipEntry>, IDisposable
	{
		/// <summary>ZipFile handle to read data from.</summary>
		IntPtr _handle = IntPtr.Zero;

		/// <summary>Name of zip file.</summary>
		string _fileName = null;

		/// <summary>Contents of zip file directory.</summary>
		//        ZipEntryCollection _entries = null;

		/// <summary>Global zip file comment.</summary>
		string _comment = null;

		/// <summary>Current zip entry open for reading.</summary>
		ZipEntry _current = null;

		/// <summary>Initializes a instance of the <see cref="ZipReader"/> class for reading the zip file with the given name.</summary>
		/// <param name="fileName">The name of zip file that will be read.</param>
		public ZipReader(string fileName)
		{
			_fileName = fileName;
			_handle = Minizip.unzOpen(fileName);
			if (_handle == IntPtr.Zero)
			{
				string msg = String.Format("Could not open zip file '{0}'.", fileName);
				throw new ZipException(msg);
			}
		}

		/// <summary>Cleans up the resources used by this zip file.</summary>
		~ZipReader()
		{
			CloseFile();
		}

		/// <remarks>Dispose is synonym for Close.</remarks>
		void IDisposable.Dispose()
		{
			Close();
		}

		/// <summary>Closes the zip file and releases any resources.</summary>
		public void Close()
		{
			// Free unmanaged resources.
			CloseFile();

			// If base type implements IDisposable we would call it here.

			// Request the system not call the finalizer method for this object.
			GC.SuppressFinalize(this);
		}

		/// <summary>Gets the name of the zip file that was passed to the constructor.</summary>
		public string Name
		{
			get { return _fileName; }
		}

		/// <summary>Gets the global comment for the zip file.</summary>
		public string Comment
		{
			get
			{
				if (_comment == null)
				{
					ZipFileInfo info = new ZipFileInfo();
					int result = Minizip.unzGetGlobalInfo(_handle, out info);
					if (result < 0)
					{
						string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
						throw new ZipException(msg, result);
					}

					byte[] buffer = new byte[info.CommentLength];
					result = Minizip.unzGetGlobalComment(_handle, buffer, (uint)buffer.Length);
					if (result < 0)
					{
						string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
						throw new ZipException(msg, result);
					}

					//file comment is for some weird reason ANSI, while entry name + comment is OEM...
					_comment = Encoding.Default.GetString(buffer);

				}
				return _comment;
			}
		}

		/////// <summary>Gets a <see cref="ZipEntryCollection"/> object that contains all the entries in the zip file directory.</summary>
		//Commented. Can't risk someone accessing this while enumerating.
		//If we should have it, we'd have to fill it in the ctor.
		//public ZipEntryCollection Entries
		//{
		//    get
		//    {
		//        if (_entries == null)
		//        {
		//            _entries = new ZipEntryCollection();

		//            int result = ZipLib.unzGoToFirstFile(_handle);
		//            if (result == ZipReturnCode.EndOfListOfFile)
		//            {
		//                // last entry found - not an exceptional case
		//                return _entries;
		//            }
		//            else if (result < 0)
		//                throw new ZipException("unzGoToFirstFile failed.", result);

		//            while (true)
		//            {
		//                ZipEntry entry = new ZipEntry(_handle);
		//                _entries.Add(entry);
		//                result = ZipLib.unzGoToNextFile(_handle);
		//                if (result == ZipReturnCode.EndOfListOfFile)
		//                {
		//                    // last entry found - not an exceptional case
		//                    break;
		//                }
		//                else if (result < 0)
		//                    throw new ZipException("unzGoToNextFile failed.", result);
		//            }
		//        }
		//        return _entries;
		//    }
		//}

		ZipEntry Current
		{
			get
			{
				return _current;
			}
		}

		public IEnumerator<ZipEntry> GetEnumerator()
		{
			// Will protect agains most common case, but if someone gets two enumerators up front and uses them,
			// we wont catch it.
			if (_current != null)
				throw new InvalidOperationException("Entry already open/enumeration already in progress");
			return new ZipEntryEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <summary>Sets <see cref="Current"/> to the next zip entry.</summary>
		/// <returns><c>true</c> if the next entry is not <c>null</c>; otherwise <c>false</c>.</returns>
		bool MoveNext()
		{

			int result;
			if (_current == null)
			{
				result = Minizip.unzGoToFirstFile(_handle);
			}
			else
			{
				CloseCurrentEntry();
				result = Minizip.unzGoToNextFile(_handle);
			}

			if (result == ZipReturnCode.EndOfListOfFile)
			{
				// no more entries
				_current = null;
			}
			else if (result < 0)
			{
				throw new ZipException("MoveNext failed.", result);
			}
			else
			{
				// entry found
				OpenCurrentEntry();
			}

			return (_current != null);
		}

		/// <summary>Move to just before the first entry in the zip directory.</summary>
		void Reset()
		{
			CloseCurrentEntry();
		}

		private void CloseCurrentEntry()
		{
			if (_current != null)
			{
				int result = Minizip.unzCloseCurrentFile(_handle);
				if (result < 0)
				{
					throw new ZipException("Could not close zip entry.", result);
				}
				_current = null;
			}
		}

		private void OpenCurrentEntry()
		{
			_current = new ZipEntry(_handle);
			int result = Minizip.unzOpenCurrentFile(_handle);
			if (result < 0)
			{
				_current = null;
				throw new ZipException("Could not open entry for reading.", result);
			}
		}

		/// <summary>Uncompress a block of bytes from the current zip entry and writes the data in a given buffer.</summary>
		/// <param name="buffer">The array to write data into.</param>
		/// <param name="index">The byte offset in <paramref name="buffer"/> at which to begin writing.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		public int Read(byte[] buffer, int index, int count)
		{
			using (FixedArray fixedBuff = new FixedArray(buffer))
			{
				int bytesRead = Minizip.unzReadCurrentFile(_handle, fixedBuff[index], (uint)count);
				if (bytesRead < 0)
				{
					throw new ZipException("Error reading zip entry.", bytesRead);
				}
				return bytesRead;
			}
		}

		public void Read(Stream writer)
		{
			int i;
			byte[] buff = new byte[0x1000];
			while ((i = this.Read(buff, 0, buff.Length)) > 0)
				writer.Write(buff, 0, i);
		}

		private void CloseFile()
		{
			if (_handle != IntPtr.Zero)
			{
				try
				{
					CloseCurrentEntry();
				}
				finally
				{
					int result = Minizip.unzClose(_handle);
					if (result < 0)
					{
						throw new ZipException("Could not close zip file.", result);
					}
					_handle = IntPtr.Zero;
				}
			}
		}


		class ZipEntryEnumerator : IEnumerator<ZipEntry>
		{
			ZipReader pReader;
			public ZipEntryEnumerator(ZipReader zr)
			{
				pReader = zr;
			}
			public ZipEntry Current
			{
				get { return pReader._current; }
			}
			public void Dispose()
			{
				pReader.CloseCurrentEntry();
			}
			object IEnumerator.Current
			{
				get { return Current; }
			}
			public bool MoveNext()
			{
				return pReader.MoveNext();
			}
			public void Reset()
			{
				pReader.Reset();
			}
		}
	}

	//public class ZipEntryCollection : List<ZipEntry>
	//{
	//    public ZipEntryCollection()
	//    {
	//    }
	//    public ZipEntryCollection(IEnumerable<ZipEntry> entries)
	//        : base(entries)
	//    {
	//    }
	//}
}
