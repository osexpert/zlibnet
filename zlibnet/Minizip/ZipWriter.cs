using System;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ZLibNet
{


	public class ZipWriter : IDisposable
	{

		//public CompressionMethod Method = CompressionMethod.Deflated;
		//public CompressionLevel Compression = CompressionLevel.Average;

		//		CompressionMethod _method = CompressionMethod.Deflated;
		//		int _level = (int)CompressionLevel.Default;


		/// <summary>Name of the zip file.</summary>
		string _fileName;

		/// <summary>Zip file global comment.</summary>
		string _comment = "";

		/// <summary>Current zip entry open for write.</summary>
		ZipEntry _current = null;

		/// <summary>Zip file handle.</summary>
		IntPtr _handle = IntPtr.Zero;

		/// <summary>Initializes a new instance fo the <see cref="ZipWriter"/> class with a specified file name.  Any Existing file will be overwritten.</summary>
		/// <param name="fileName">The name of the zip file to create.</param>
		public ZipWriter(string fileName)
		{
			_fileName = fileName;

			_handle = Minizip.zipOpen(fileName, 0 /* 0 = create new, 1 = append */ );
			if (_handle == IntPtr.Zero)
			{
				string msg = String.Format("Could not open zip file '{0}' for writing.", fileName);
				throw new ZipException(msg);
			}
		}

		/// <summary>Cleans up the resources used by this zip file.</summary>
		~ZipWriter()
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

		/// <summary>Gets the name of the zip file.</summary>
		public string Name
		{
			get
			{
				return _fileName;
			}
		}

		/// <summary>Gets and sets the zip file comment.</summary>
		public string Comment
		{
			get { return _comment; }
			set { _comment = value; }
		}

		/// <summary>Creates a new zip entry in the directory and positions the stream to the start of the entry data.</summary>
		/// <param name="entry">The zip entry to be written.</param>
		/// <remarks>Closes the current entry if still active.</remarks>
		public void AddEntry(ZipEntry entry)
		{
			//Close previous entry (if any).
			//Will trigger write of central dir info for previous file and may throw.
			CloseCurrentEntry();

			ZipFileEntryInfo info = new ZipFileEntryInfo();
			info.ZipDateTime = entry.ModifiedTime;
			info.ExternalFileAttributes = (uint)entry.GetFileAttributesForZip();

			byte[] extra = null;
			uint extraLength = 0;
			if (entry.ExtraField != null)
			{
				extra = entry.ExtraField;
				extraLength = (uint)entry.ExtraField.Length;
			}

			string nameForZip = entry.GetNameForZip();

			uint flagBase = 0;
			if (entry.UTF8Encoding)
				flagBase |= ZipEntryFlag.UTF8;
			else
			{
				if (!nameForZip.IsAscii())
					throw new ArgumentException("Name can only contain Ascii 8 bit characters.");
				if (entry.Comment != null && !entry.Comment.IsAscii())
					throw new ArgumentException("Comment can only contain Ascii 8 bit characters.");
			}

			Encoding encoding = entry.UTF8Encoding ? Encoding.UTF8 : Minizip.OEMEncoding;
			byte[] name = encoding.GetBytes(nameForZip);
			byte[] comment = null;
			if (entry.Comment != null)
				comment = encoding.GetBytes(entry.Comment);

			int result = Minizip.zipOpenNewFileInZip4_64(
				_handle,
				name,
				ref info,
				extra,
				extraLength,
				null,
				0,
				comment, //null is ok here
				(int)entry.Method,
				entry.Level,
				flagBase,
				entry.Zip64);

			if (result < 0)
				throw new ZipException("AddEntry error.", result);

			_current = entry;
		}


		/// <summary>Gets and sets the default compresion level for zip file entries.  See <see cref="CompressionMethod"/> for a partial list of values.</summary>
		//public int Level
		//{
		//    get { return _level; }
		//    set
		//    {
		//        if (value < -1 || value > 9)
		//        {
		//            throw new ArgumentOutOfRangeException("Level", value, "Level value must be between -1 and 9.");
		//        }
		//        _level = value;
		//    }
		//}

		///// <summary>Gets and sets the default compresion method for zip file entries.  See <see cref="CompressionMethod"/> for a list of possible values.</summary>
		//public CompressionMethod Method
		//{
		//    get { return _method; }
		//    set { _method = value; }
		//}


		/// <summary>Compress a block of bytes from the given buffer and writes them into the current zip entry.</summary>
		/// <param name="buffer">The array to read data from.</param>
		/// <param name="index">The byte offset in <paramref name="buffer"/> at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to write.</param>
		public void Write(byte[] buffer, int index, int count)
		{
			using (FixedArray fixedBuffer = new FixedArray(buffer))
			{
				int result = Minizip.zipWriteInFileInZip(_handle, fixedBuffer[index], (uint)count);
				if (result < 0)
					throw new ZipException("Write error.", result);
			}
		}

		public void Write(Stream reader)
		{
			int i;
			byte[] buff = new byte[0x1000];
			while ((i = reader.Read(buff, 0, buff.Length)) > 0)
				Write(buff, 0, i);
		}

		private void CloseCurrentEntry()
		{
			if (_current != null)
			{
				int result = Minizip.zipCloseFileInZip(_handle);
				if (result < 0)
					throw new ZipException("Could not close entry.", result);
				_current = null;
			}
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
					//file comment is for some weird reason ANSI, while entry name + comment is OEM...
					int result = Minizip.zipClose(_handle, _comment);
					if (result < 0)
						throw new ZipException("Could not close zip file.", result);
					_handle = IntPtr.Zero;
				}
			}
		}
	}
}
