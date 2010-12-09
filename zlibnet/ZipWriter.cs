using System;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics;

namespace ZLibNet {

   
    public class ZipWriter : IDisposable {

		//public CompressionMethod Method = CompressionMethod.Deflated;
		//public CompressionLevel Compression = CompressionLevel.Average;
		
//		CompressionMethod _method = CompressionMethod.Deflated;
//		int _level = (int)CompressionLevel.Default;


        /// <summary>Name of the zip file.</summary>
        string _fileName;

        /// <summary>Zip file global comment.</summary>
        string _comment = "";

        /// <summary>True if currently writing a new zip file entry.</summary>
        bool _entryOpen = false;

        /// <summary>Zip file handle.</summary>
        IntPtr _handle = IntPtr.Zero;

        /// <summary>Initializes a new instance fo the <see cref="ZipWriter"/> class with a specified file name.  Any Existing file will be overwritten.</summary>
        /// <param name="fileName">The name of the zip file to create.</param>
        public ZipWriter(string fileName) {
            _fileName = fileName;

            _handle = ZipLib.zipOpen(fileName, 0 /* 0 = create new, 1 = append */ );
            if (_handle == IntPtr.Zero) {
                string msg = String.Format("Could not open zip file '{0}' for writing.", fileName);
                throw new ZipException(msg);
            }
        }

        /// <summary>Cleans up the resources used by this zip file.</summary>
        ~ZipWriter() {
            CloseFile(); 
        }

        /// <remarks>Dispose is synonym for Close.</remarks>
        void IDisposable.Dispose() {
            Close();
        }

        /// <summary>Closes the zip file and releases any resources.</summary>
        public void Close() {
            // Free unmanaged resources.
            CloseFile();

            // If base type implements IDisposable we would call it here.

            // Request the system not call the finalizer method for this object.
            GC.SuppressFinalize(this);
        }

        /// <summary>Gets the name of the zip file.</summary>
        public string Name {
            get {
                return _fileName;
            }
        }

        /// <summary>Gets and sets the zip file comment.</summary>
        public string Comment {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>Creates a new zip entry in the directory and positions the stream to the start of the entry data.</summary>
        /// <param name="entry">The zip entry to be written.</param>
        /// <remarks>Closes the current entry if still active.</remarks>
        public void AddEntry(ZipEntry entry) {
            ZipFileEntryInfo info;
            info.ZipDateTime = entry.ModifiedTime;
			info.ExternalFileAttributes = (uint)entry.GetFileAttributesForZip();

            int result;
            unsafe {
                byte[] extra = null;
                uint extraLength = 0;
                if (entry.ExtraField != null) {
                    extra = entry.ExtraField;
                    extraLength = (uint) entry.ExtraField.Length;
                }

				uint flagBase = 0;
				if (entry.UTF8Encoding)
					flagBase |= (flagBase & ZipEntryFlag.UTF8);

				Encoding encoding = entry.UTF8Encoding ? Encoding.UTF8 : ZipLib.OEMEncoding;
				byte[] name = encoding.GetBytes(entry.GetNameForZip());
				byte[] comment = null;
				if (entry.Comment != null)
					comment = encoding.GetBytes(entry.Comment);

                result = ZipLib.zipOpenNewFileInZip4_64(
                    _handle,
					name,
                    &info,
                    extra, 
                    extraLength,
                    null, 
					0,
					comment, //null is ok here
                    (int) entry.Method,
                    entry.Level,
					flagBase,
					entry.Zip64);
            }

			if (result < 0)
				throw new ZipException("AddEntry error.", result);

			//TODO: set the ZipEntry ref instead? Easier debug etc.
            _entryOpen = true;
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
        public void Write(byte[] buffer, int index, int count) {
            int result = ZipLib.zipWriteInFileInZip(_handle, buffer, (uint) count);
			if (result < 0)
				throw new ZipException("Write error.", result);
        }

        private void CloseEntry() {
            if (_entryOpen) {
                int result = ZipLib.zipCloseFileInZip(_handle);
				if (result < 0)
					throw new ZipException("Could not close entry.", result);
                _entryOpen = false;
            }
        }

        void CloseFile() {
            if (_handle != IntPtr.Zero) {
                CloseEntry();
				//file comment is for some weird reason ANSI, while entry name + comment is OEM...
				int result = ZipLib.zipClose(_handle, _comment);
				if (result < 0)
					throw new ZipException("Could not close zip file.", result);
               
                _handle = IntPtr.Zero;
            }
        }
    }
}
