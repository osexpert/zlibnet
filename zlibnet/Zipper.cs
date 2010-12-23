﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ZLibNet
{

	public class Zipper
	{
		//        public enum ZipMethod
		//        {
		//            Create,
		////			Add
		//        }
		public bool Recurse; //def true??
		public string ZipFile;
		/// <summary>
		/// More than 64k count zip entries in zip
		/// More than 4GB data per zip entriy (does not work, but in minizip)
		/// Zip's larger than 4GB is supporten in any case thou.
		/// </summary>
		public bool Zip64;
		/// <summary>
		/// Use UTF8 for zip entry name/comment
		/// </summary>
		public bool UTF8Encoding;
		/// <summary>
		/// List of files, dirs etc FULL PATH. With wildcards.
		/// 
		//                TRUE – only the beginning of the path specification of the item must match the path
		//specification of the filespec for the item to be selected. This allows items within the
		//Filespec path and in any of its subdirectories to be selected.
		//FALSE – the path specification of the item must match that of the filespec exactly
		//for the item to be selected. Items in any subdirectories of the filespec path are not
		//selected.
		//                For example, assume that the filespec is ABC\*.C and the ZIP file contains two
		//items, ABC\TEXT.C and ABC\DEF\TEXT.C. If recurseFlag is FALSE, only
		//ABC\TEXT.C is selected. If recurseFlag is TRUE, both files are selected
		//
		//
		// PS: c:\some\dir or c:\some\dir\ will not include any files in dir (or if recursive, files in subdirs).
		// It will include subdirs thou (if recursive), but this is mostly useless.
		// Conclusion: it is meaningless to add dirs without file/mask to ItemList.
		//
		// The most logical would be to add *.* automatically if no file/mask specified,
		// but keep DZ compat for now. Maybe change later.
		/// </summary>
		public ZList<string> ItemList = new ZList<string>();
		/// <summary>
		/// Files to store
		/// </summary>
		public ZList<string> StoreSuffixes = new ZList<string>();
		//This functionality is more confusing than usefull -> made private
		private bool NoDirectoryEntries = false;
		public ZList<string> ExcludeFollowing = new ZList<string>();
		public ZList<string> IncludeOnlyFollowing = new ZList<string>();
		//		public bool DontCheckNames;
		public bool UseTempFile = true; //bad def?
		public enPathInZip PathInZip = enPathInZip.Relative; //good def? yes
		public string Comment;

		// buffer to hold temp bytes
		byte[] pBuffer;

		public void Zip()
		{
			if (ZipFile == null)
				throw new ArgumentException("ZipFile is null");
			if (ItemList.Count == 0)
				throw new ArgumentException("ItemList is empty");

			if (Path.GetExtension(ZipFile).Length == 0)
				ZipFile = Path.ChangeExtension(ZipFile, "zip");

			string realZipFile = null;
			if (UseTempFile)
			{
				realZipFile = ZipFile;
				ZipFile = GetTempFileName(ZipFile);
			}

			FileSpecMatcher excludes = null;
			if (ExcludeFollowing.Count > 0)
				excludes = new FileSpecMatcher(ExcludeFollowing, true);
			FileSpecMatcher includes = null;
			if (IncludeOnlyFollowing.Count > 0)
				includes = new FileSpecMatcher(IncludeOnlyFollowing, true);

			pBuffer = new byte[4096];


			/*
			1) collect files. if we find a file several times its ok, as long as the zipped name is the same, else exception! (typically when 2 items are same dir, but different level and we store relative path)
			 * Same with zipped name: if two different files map to same zipped name -> exception (typically when no path is stored + recursive)
			 * 
			 * 
			*/

			List<FileSystemEntry> fsEntries = CollectFileSystemEntries();

			try
			{
				bool addedSomeEntry = false;

				//hmmm...denne vil adde hvis fila eksisterer? Nei...vi bruker append = 0
				using (ZipWriter writer = new ZipWriter(ZipFile))
				{
					writer.Comment = this.Comment;

					foreach (FileSystemEntry fsEntry in fsEntries)
					{
						if (IsIncludeFile(fsEntry.ZippedName, fsEntry.IsDirectory, includes, excludes))
						{
							if (fsEntry.IsDirectory)
							{
								if (!AddDirEntries)
									throw new Exception("!AddDirEntries but still got dir");

								DirectoryInfo di = (DirectoryInfo)fsEntry.FileSystemInfo;
								ZipEntry entry = new ZipEntry(fsEntry.ZippedName, true);
								entry.ModifiedTime = GetLastWriteTimeFixed(di);
								entry.FileAttributes = di.Attributes;
								entry.UTF8Encoding = this.UTF8Encoding;
								entry.Zip64 = this.Zip64;
								entry.Method = CompressionMethod.Stored; //DIR
								writer.AddEntry(entry);
							}
							else
							{
								FileInfo fi = (FileInfo)fsEntry.FileSystemInfo;
								if (fi.Length > UInt32.MaxValue)
									throw new NotSupportedException("Files above 4GB not supported (not even with Zip64: bug in zlib/minizip, will create corrupt zip)");
								ZipEntry entry = new ZipEntry(fsEntry.ZippedName);
								entry.ModifiedTime = GetLastWriteTimeFixed(fi);
								entry.FileAttributes = fi.Attributes;
								entry.UTF8Encoding = this.UTF8Encoding;
								entry.Zip64 = this.Zip64;
								if (fi.Length == 0 || IsStoreFile(fsEntry.ZippedName))
									entry.Method = CompressionMethod.Stored;
								writer.AddEntry(entry);

								using (FileStream reader = fi.OpenRead())
								{
									int byteCount;
									while ((byteCount = reader.Read(pBuffer, 0, pBuffer.Length)) > 0)
										writer.Write(pBuffer, 0, byteCount);
								}
							}

							addedSomeEntry = true;
						}
					}
				}

				if (!addedSomeEntry)
					throw new ZipException("Nothing to add");

				if (UseTempFile)
				{
					File.Delete(realZipFile); //overwrite
					File.Move(ZipFile, realZipFile);
					ZipFile = realZipFile;
				}
			}
			catch
			{
				File.Delete(ZipFile);
				throw;
			}
			//finally
			//{
			//    File.Delete(tempZip);
			//}
		}

		private bool IsStoreFile(string fileName)
		{
			if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				return true;

			foreach (string suffix in StoreSuffixes)
				if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		DateTime GetLastWriteTimeFixed(FileSystemInfo fsi)
		{
			//http://www.codeproject.com/KB/files/csharpfiledate.aspx?msg=2885854#xx2885854xx
			TimeSpan localOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
			return fsi.LastWriteTimeUtc + localOffset;
		}

		private List<FileSystemEntry> CollectFileSystemEntries()
		{
			Dictionary<string, FileSystemEntry> htEntries = new Dictionary<string, FileSystemEntry>(StringComparer.OrdinalIgnoreCase);

			foreach (string item in ItemList)
			{
				string itemDirName = Path.GetDirectoryName(item);
				DirectoryInfo baseDi = new DirectoryInfo(itemDirName);

				Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
				dirs.Push(baseDi);

				string itemFileName = Path.GetFileName(item);

				while (dirs.Count != 0)
				{
					DirectoryInfo di = dirs.Pop();

					ProcessDir(htEntries, baseDi, di, itemFileName);

					if (Recurse)
						foreach (DirectoryInfo subDi in di.GetDirectories())
							dirs.Push(subDi);
				}
			}

			List<FileSystemEntry> result = new List<FileSystemEntry>(htEntries.Values);
			result.Sort();
			return result;
		}

		private bool AddDirEntries
		{
			get
			{
				return !(PathInZip == enPathInZip.None || NoDirectoryEntries);
			}
		}

		private void ProcessDir(Dictionary<string, FileSystemEntry> htEntries,
			DirectoryInfo baseDi, DirectoryInfo di, string itemFileName)
		{
			if (AddDirEntries && di != baseDi)
			{
				AddFsEntry(htEntries, baseDi, di);
			}

			// di.GetFiles("") does work (always returns 0 files), but this is more readable/logical:
			if (itemFileName.Length > 0)
			{
				foreach (FileInfo fi in di.GetFiles(itemFileName))
				{
					AddFsEntry(htEntries, baseDi, fi);
				}
			}
		}

		private void AddFsEntry(Dictionary<string, FileSystemEntry> htEntries, DirectoryInfo baseDi, FileSystemInfo fsi)
		{
			string zippedName = GetPathInZip(baseDi, fsi);
			FileSystemEntry fsEntry = new FileSystemEntry(zippedName, fsi);
			// Remove trailing dir sep from key since we want to catch file and dirs with same name clash
			string key = zippedName.TrimEndDirSep();
			FileSystemEntry existingEntry = null;
			if (htEntries.TryGetValue(key, out existingEntry))
			{
				if (fsEntry.IsDirectory && existingEntry.IsDirectory)
				{
					//don't care about two different dirs added with same name in zip. DZ does the same.
				}
				else if (fsEntry.IsFile && existingEntry.IsFile &&
						existingEntry.FullName.Equals(fsEntry.FullName, StringComparison.OrdinalIgnoreCase))
				{
					//ok. same file added (several times) with same name in zip -> just add it once
				}
				else
				{
					//not ok. different files/file+dir added with same name in zip
					throw new ArgumentException(string.Format("Both {0} {1} and {2} {3} maps to {4} in zip",
						existingEntry.IsDirectory ? "dir" : "file",
						existingEntry.FileSystemInfo.FullName,
						fsEntry.IsDirectory ? "dir" : "file",
						fsEntry.FileSystemInfo.FullName,
						fsEntry.ZippedName));
				}
			}
			else
			{
				htEntries.Add(key, fsEntry);
			}

		}

		private bool IsIncludeFile(string zippedName, bool isDir, FileSpecMatcher includes, FileSpecMatcher excludes)
		{
			if (includes == null || includes.MatchSpecs(zippedName, isDir))
				if (excludes == null || !excludes.MatchSpecs(zippedName, isDir))
					return true;

			return false;
		}

		private string GetTempFileName(string zipFile)
		{
			int i = 0;
			while (true)
			{
				string tempFile = zipFile + ".";
				if (i > 0)
					tempFile += i;
				tempFile += "tmp";

				if (!File.Exists(tempFile))
					return tempFile;
				i++;
			}
		}

		private string GetPathInZip(DirectoryInfo baseDi, FileSystemInfo fsi)
		{
			string name = null;

			if (NoDirectoryEntries && fsi is DirectoryInfo)
				throw new Exception("NoDirectoryEntries but trying to create name for DirectoryInfo");

			switch (PathInZip)
			{
				//Absolute = relative from root dir
				case enPathInZip.Absolute:
					name = GetRelativeName(fsi.FullName, baseDi.Root.FullName);
					break;
				// AbsoluteRoot is not supported by Windows Compressed folders! (Will show zip as empty)
				case enPathInZip.AbsoluteRoot:
					name = fsi.FullName;
					break;
				case enPathInZip.Relative:
					name = GetRelativeName(fsi.FullName, baseDi.FullName);
					break;
				case enPathInZip.None:
					name = fsi.Name;
					break;
				default:
					throw new NotImplementedException("enPathInZip " + PathInZip);

			}

			if (name.Length == 0)
				throw new Exception(string.Format("Zipped name for {0} {1} is empty",
					fsi is DirectoryInfo ? "dir" : "file",
					fsi.FullName));

			//Not really needed, but will give same sorting as DZ, making comparison with DZ easier/possible.
			if (fsi is DirectoryInfo)
				name = name.SetEndDirSep();

			return name;
		}

		private string GetRelativeName(string full, string baseDir)
		{
			return full.Substring(baseDir.Length).TrimStartDirSep();
		}

		/// <summary>
		/// 
		/// </summary>
		class FileSystemEntry : IComparable<FileSystemEntry>
		{
			public string ZippedName;
			public FileSystemInfo FileSystemInfo;

			public FileSystemEntry(string zippedName, FileSystemInfo fsi)
			{
				this.ZippedName = zippedName;
				this.FileSystemInfo = fsi;
			}

			public bool IsFile
			{
				get { return !IsDirectory; }
			}
			public bool IsDirectory
			{
				get { return FileSystemInfo is DirectoryInfo; }
			}
			public string FullName
			{
				get { return FileSystemInfo.FullName; }
			}

			public int CompareTo(FileSystemEntry that)
			{
				return this.ZippedName.CompareTo(that.ZippedName);
			}
		}

	}

	public enum enPathInZip
	{
		/// <summary>
		/// relative to item in ItemList
		/// </summary>
		Relative,
		/// <summary>
		/// Absolute from first directory on drive (test\a.c)
		/// </summary>
		Absolute,
		/// <summary>
		/// Absolute from root (C:\test\a.c))
		/// </summary>
		AbsoluteRoot,
		/// <summary>
		/// No path stored, all files on root
		/// </summary>
		None,
	}

}
