using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ZLibNet
{

	/// <summary>Represents a entry in a zip file.</summary>
	public class ZipEntry
	{

		string _name = String.Empty;
		uint _crc = 0;
		long _compressedLength;
		long _uncompressedLength;
		byte[] _extraField = null;
		string _comment = String.Empty;
		DateTime _modifiedTime = DateTime.Now;
		FileAttributes _fileAttributes;
		CompressionMethod _method = CompressionMethod.Deflated;
		int _level = (int)CompressionLevel.Default;
		bool _isDirectory;
		/// <summary>
		/// Utf8 filename/comment.
		/// Not supported by Windows Compressed Folders
		/// </summary>
		bool _UTF8Encoding;
		bool _zip64;

		/// <summary>Initializes a instance of the <see cref="ZipEntry"/> class with the given name.</summary>
		/// <param name="name">The name of entry that will be stored in the directory of the zip file.</param>
		public ZipEntry(string name, bool isDirectory)
		{
			Name = name;
			_isDirectory = isDirectory;
		}

		/// <summary>Initializes a instance of the <see cref="ZipEntry"/> class with the given name.</summary>
		/// <param name="name">The name of entry that will be stored in the directory of the zip file.</param>
		public ZipEntry(string name)
			: this(name, false)
		{
		}


		/// <summary>Creates a new Zip file entry reading values from a zip file.</summary>
		internal ZipEntry(IntPtr handle)
		{
			ZipEntryInfo64 entryInfo = new ZipEntryInfo64();
			int result = Minizip.unzGetCurrentFileInfo64(handle, out entryInfo, null, 0, null, 0, null, 0);
			if (result != 0)
			{
				throw new ZipException("Could not read entry from zip file " + Name, result);
			}

			_extraField = new byte[entryInfo.ExtraFieldLength];
			byte[] entryNameBuffer = new byte[entryInfo.FileNameLength];
			byte[] commentBuffer = new byte[entryInfo.CommentLength];

			result = Minizip.unzGetCurrentFileInfo64(handle, out entryInfo,
				entryNameBuffer, (uint)entryNameBuffer.Length,
				_extraField, (uint)_extraField.Length,
				commentBuffer, (uint)commentBuffer.Length);

			if (result != 0)
			{
				throw new ZipException("Could not read entry from zip file " + Name, result);
			}

			this._UTF8Encoding = BitFlag.IsSet(entryInfo.Flag, ZipEntryFlag.UTF8);
			Encoding encoding = this._UTF8Encoding ? Encoding.UTF8 : Minizip.OEMEncoding;

			_name = encoding.GetString(entryNameBuffer);
			//null or empty string if empty buffer?
			_comment = encoding.GetString(commentBuffer);
			_crc = entryInfo.Crc;
			_compressedLength = (long)entryInfo.CompressedSize;
			_uncompressedLength = (long)entryInfo.UncompressedSize;
			_method = (CompressionMethod)entryInfo.CompressionMethod;
			_modifiedTime = entryInfo.ZipDateTime;
			_fileAttributes = (FileAttributes)entryInfo.ExternalFileAttributes;
			_isDirectory = InterpretIsDirectory();
		}

		//private ExtraField[] GetExtraFields(byte[] _extraFields)
		//{
		//    throw new NotImplementedException();
		//}

		//struct ExtraField
		//{
		//    public Int16 Tag;
		//    public byte[] Data;
		//}

		//private byte[] GetUTF8Name(byte[] ExtraField)
		//{
		//    byte[] data = GetTag(ExtraField, 0x7075);
		//    if (data == null)
		//        return null;
		//}

		//private byte[] GetUTF8Comment(byte[] ExtraField)
		//{
		//    byte[] data = GetTag(ExtraField, 0x6375);
		//    return null;
		//    BitConverter.ToInt16(
		//}

		//private byte[] GetTag(byte[] ExtraField, int p)
		//{
		//    throw new NotImplementedException();
		//}

		private bool InterpretIsDirectory()
		{
			bool winDir = ((_fileAttributes & FileAttributes.Directory) != 0); //windows
			bool otherDir = _name.EndsWithDirSep(); // other os'
			bool isDir = winDir || otherDir;
			if (isDir)
			{
				Debug.Assert(Name.Length > 0);
				Debug.Assert(Method == CompressionMethod.Stored);
				Debug.Assert(CompressedLength == 0);
				Debug.Assert(Length == 0);
			}

			return isDir;
		}


		/// <summary>Gets and sets the local file comment for the entry.</summary>
		/// <remarks>
		///   <para>Currently only Ascii 8 bit characters are supported in comments.</para>
		///   <para>A comment cannot exceed 65535 bytes.</para>
		/// </remarks>
		public string Comment
		{
			get { return _comment; }
			set
			{
				// null comments are valid
				if (value != null)
				{
					if (value.Length > 0xffff)
					{
						throw new ArgumentOutOfRangeException("Comment cannot not exceed 65535 characters.");
					}
				}
				_comment = value;
			}
		}

		/// <summary>Gets the compressed size of the entry data in bytes, or -1 if not known.</summary>
		public long CompressedLength
		{
			get { return _compressedLength; }
		}

		/// <summary>Gets the CRC-32 checksum of the uncompressed entry data.</summary>
		public uint Crc
		{
			get { return _crc; }
		}

		// true = Use UTF8 for name and comment
		public bool UTF8Encoding
		{
			get
			{
				return this._UTF8Encoding;
			}
			set
			{
				this._UTF8Encoding = value;
			}
		}

		/// <summary>Gets and sets the optional extra field data for the entry.</summary>
		/// <remarks>ExtraField data cannot exceed 65535 bytes.</remarks>
		public byte[] ExtraField
		{
			get
			{
				return _extraField;
			}
			set
			{
				if (value.Length > 0xffff)
				{
					throw new ArgumentOutOfRangeException("ExtraField cannot not exceed 65535 bytes.");
				}
				_extraField = value;
			}
		}

		///// <summary>Gets and sets the default compresion method for zip file entries.  See <see cref="CompressionMethod"/> for a list of possible values.</summary>
		public CompressionMethod Method
		{
			get { return _method; }
			set { _method = value; }
		}

		///// <summary>Gets and sets the default compresion level for zip file entries.  See <see cref="CompressionMethod"/> for a partial list of values.</summary>
		public int Level
		{
			get { return _level; }
			set
			{
				if (value < -1 || value > 9)
				{
					throw new ArgumentOutOfRangeException("Level", value, "Level value must be between -1 and 9.");
				}
				_level = value;
			}
		}

		public bool Zip64
		{
			get
			{
				return this._zip64;
			}
			set
			{
				this._zip64 = value;
			}
		}

		/// <summary>Gets the size of the uncompressed entry data in in bytes.</summary>
		public long Length
		{
			get { return _uncompressedLength; }
		}

		/// <summary>Gets and sets the modification time of the entry.</summary>
		public DateTime ModifiedTime
		{
			get { return _modifiedTime; }
			set { _modifiedTime = value; }
		}

		/// <summary>Gets and sets the name of the entry.</summary>
		/// <remarks>
		///   <para>Currently only Ascii 8 bit characters are supported in comments.</para>
		///   <para>A comment cannot exceed 65535 bytes.</para>
		/// </remarks>
		public string Name
		{
			get { return _name; }
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("Name cannot be null.");
				}
				if (value.Length > 0xffff)
				{
					throw new ArgumentOutOfRangeException("Name cannot not exceed 65535 characters.");
				}
				_name = value;
			}
		}

		internal string GetNameForZip()
		{
			string nameForZip = _name;

			if (_isDirectory)
				nameForZip = nameForZip.SetEndDirSep();

			// name in zip should always be stored with slashes (for compat)
			return nameForZip.Replace('\\', '/');
		}

		/// <summary>Flag that indicates if this entry is a directory or a file.</summary>
		public bool IsDirectory
		{
			get
			{
				return _isDirectory;
			}
		}

		/// <summary>Gets the compression ratio as a percentage.</summary>
		/// <remarks>Returns -1.0 if unknown.</remarks>
		public float Ratio
		{
			get
			{
				float ratio = -1.0f;
				if (Length > 0)
				{
					ratio = Convert.ToSingle(Length - CompressedLength) / Length;
				}
				return ratio;
			}
		}

		internal FileAttributes GetFileAttributesForZip()
		{
			FileAttributes att = this._fileAttributes;
			if (this._isDirectory)
				att |= FileAttributes.Directory;
			return att;
		}

		public FileAttributes FileAttributes
		{
			get
			{
				return _fileAttributes;
			}
			set
			{
				_fileAttributes = value;
			}
		}

		/// <summary>Returns a string representation of the Zip entry.</summary>
		public override string ToString()
		{
			return String.Format("{0} {1}", Name, base.ToString());
		}
	}
}
