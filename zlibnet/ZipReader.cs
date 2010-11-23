using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace ZLibNet
{

    /// </code>
    /// </example>
    public class ZipReader : IEnumerator<ZipEntry>, IDisposable {

        /// <summary>ZipFile handle to read data from.</summary>
        IntPtr _handle = IntPtr.Zero;

        /// <summary>Name of zip file.</summary>
        string _fileName = null;

        /// <summary>Contents of zip file directory.</summary>
        ZipEntryCollection _entries = null;

        /// <summary>Global zip file comment.</summary>
        string _comment = null;

        /// <summary>True if an entry is open for reading.</summary>
        bool _entryOpen = false;

        /// <summary>Current zip entry open for reading.</summary>
        ZipEntry _current = null;

        /// <summary>Initializes a instance of the <see cref="ZipReader"/> class for reading the zip file with the given name.</summary>
        /// <param name="fileName">The name of zip file that will be read.</param>
        public ZipReader(string fileName) {
            _fileName = fileName;
            _handle = ZipLib.unzOpen(fileName);
            if (_handle == IntPtr.Zero) {
                string msg = String.Format("Could not open zip file '{0}'.", fileName);
                throw new ZipException(msg);
            }
        }

        /// <summary>Cleans up the resources used by this zip file.</summary>
        ~ZipReader() {
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

        /// <summary>Gets the name of the zip file that was passed to the constructor.</summary>
        public string Name {
            get { return _fileName; }
        }

        /// <summary>Gets the global comment for the zip file.</summary>
        public string Comment {
            get {
                if (_comment == null) {
                    ZipFileInfo info;
                    int result = 0;
                    unsafe {
                       result = ZipLib.unzGetGlobalInfo(_handle, &info);
                    }
                    if (result < 0) {
                        string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
						throw new ZipException(msg, result);
                    }

                    byte[] buffer = new byte[info.CommentLength];
                    result = ZipLib.unzGetGlobalComment(_handle, buffer, (uint) buffer.Length);
                    if (result < 0) {
                        string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
                        throw new ZipException(msg, result);
                    }

					//file comment is for some weird reason ANSI, while entry name + comment is OEM...
					_comment = Encoding.Default.GetString(buffer);

                }
                return _comment;
            }
        }

        /// <summary>Gets a <see cref="ZipEntryCollection"/> object that contains all the entries in the zip file directory.</summary>
        public ZipEntryCollection Entries {
            get {
                if (_entries == null) {
                    _entries = new ZipEntryCollection();

                    int result = ZipLib.unzGoToFirstFile(_handle);
					if (result == (int)ErrorCode.EndOfListOfFile)
					{
						// last entry found - not an exceptional case
						return _entries;
					}
					else if (result < 0)
						throw new ZipException("unzGoToFirstFile failed.", result);

                    while (true)
					{
                        ZipEntry entry = new ZipEntry(_handle);
                        _entries.Add(entry);
                        result = ZipLib.unzGoToNextFile(_handle);
						if (result == (int)ErrorCode.EndOfListOfFile)
						{
							// last entry found - not an exceptional case
							break;
						}
						else if (result < 0)
							throw new ZipException("unzGoToNextFile failed.", result);
                    }
                }
                return _entries;
            }
        }

		object IEnumerator.Current
		{
			get
			{
				return _current;
			}
		}

        /// <summary>Gets the current entry in the zip file..</summary>
        public ZipEntry Current {
            get {
                return _current;
            }
        }

		public IEnumerator<ZipEntry> GetEnumerator()
		{
			return this;
		}

        /// <summary>Advances the enumerator to the next element of the collection.</summary>
        /// <summary>Sets <see cref="Current"/> to the next zip entry.</summary>
        /// <returns><c>true</c> if the next entry is not <c>null</c>; otherwise <c>false</c>.</returns>
        public bool MoveNext() {
            // close any open entry
            CloseEntry();

            int result;
            if (_current == null) {
                result = ZipLib.unzGoToFirstFile(_handle);
            } else {
                result = ZipLib.unzGoToNextFile(_handle);
            }

			if (result == (int)ErrorCode.EndOfListOfFile)
			{
				// last entry found - not an exceptional case
				_current = null;
			}
			else if (result < 0)
			{
				throw new ZipException("MoveNext failed.", result);
			}
			else
			{
				// entry found
				OpenEntry();
			}

            return (_current != null);
        }

        /// <summary>Move to just before the first entry in the zip directory.</summary>
        public void Reset() {
            CloseEntry();
            _current = null;
        }

        /// <summary>Seek to the specified entry.</summary>
        /// <param name="entryName">The name of the entry to seek to.</param>
        public void Seek(string entryName) {

            CloseEntry();
            int result = ZipLib.unzLocateFile(_handle, entryName, 0);
            if (result < 0) {
                string msg = String.Format("Could not locate entry named '{0}'.", entryName);
                throw new ZipException(msg, result);
            }
            OpenEntry();
        }

        private void OpenEntry() {
            _current = new ZipEntry(_handle);
            int result = ZipLib.unzOpenCurrentFile(_handle);
            if (result < 0) {
                _current = null;
				throw new ZipException("Could not open entry for reading.", result);
            }
            _entryOpen = true;
        }

        /// <summary>Uncompress a block of bytes from the current zip entry and writes the data in a given buffer.</summary>
        /// <param name="buffer">The array to write data into.</param>
        /// <param name="index">The byte offset in <paramref name="buffer"/> at which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        public int Read(byte[] buffer, int index, int count) {
            if (index != 0) {
                throw new ArgumentException("index", "Only index values of zero currently supported.");
            }
            int bytesRead = ZipLib.unzReadCurrentFile(_handle, buffer, (uint) count);
            if (bytesRead < 0) {
				throw new ZipException("Error reading zip entry.", bytesRead);
            }
            return bytesRead;
        }

        private void CloseEntry() {
            if (_entryOpen) {
                int result = ZipLib.unzCloseCurrentFile(_handle);
                if (result < 0) {
	                 throw new ZipException("Could not close zip entry.", result);
                }
                _entryOpen = false;
            }
        }

        private void CloseFile() {
            if (_handle != IntPtr.Zero) {
                CloseEntry();
                int result = ZipLib.unzClose(_handle);
                if (result < 0) {
                    throw new ZipException("Could not close zip file.", result);
                }
                _handle = IntPtr.Zero;
            }
        }
    }
}
