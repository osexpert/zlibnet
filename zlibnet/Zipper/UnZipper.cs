using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ZLibNet
{
	public class UnZipper
	{
		public string ZipFile;
		/// <summary>
		/// Tipper disse er fra rota??
		/// ABC\JOG.* ABC\RUN.PCX *.HLP "SPORTS FANS\BASKETBALL.TIF"
		/// PS: stien kan ikke ha wildcards
		/// </summary>
		public ZList<string> ItemList = new ZList<string>();
		public bool Recurse; //how does it work? //good def?
		/// <summary>
		/// ONly valid for files. Dirs are always created and last writeTime is updated
		/// </summary>
		public enIfFileExist IfFileExist = enIfFileExist.Exception; //good def?
		public bool NoDirectoryNames;
		public string Destination = null;
		byte[] buffer;

		public void UnZip()
		{
			if (Destination == null)
				throw new ArgumentException("Destination is null");
			if (ItemList.Count == 0)
				throw new ArgumentException("ItemList is empty");
			//if (Filespecs.Count == 0)
			//    Filespecs.Add("*");

			FileSpecMatcher fileSpecs = new FileSpecMatcher(ItemList, Recurse);

			bool unzippedSomeEntry = false;

			using (ZipReader reader = new ZipReader(ZipFile))
			{
				// buffer to hold temp bytes
				buffer = new byte[4096];

				foreach (ZipEntry entry in reader)
				{
					if (fileSpecs.MatchSpecs(entry.Name, entry.IsDirectory))
					{
						if (entry.IsDirectory)
						{
							//FIXME: bør kanskje ha sjekk på om flere filer med samme navn havner på rota og overskriver hverandre?
							if (!NoDirectoryNames)
							{
								string dirName = CreateUnzippedName(entry);
								DirectoryInfo di = new DirectoryInfo(dirName);
								if (!di.Exists)
									di.Create();
								SetLastWriteTimeFixed(di, entry.ModifiedTime);
							}
						}
						else
						{
							string fileName = CreateUnzippedName(entry);
							FileInfo fi = new FileInfo(fileName);
							if (!fi.Directory.Exists)
								fi.Directory.Create();

							if (fi.Exists)
							{
								switch (IfFileExist)
								{
									case enIfFileExist.Exception:
										throw new ZipException("File already exists: " + fileName);
									case enIfFileExist.Skip:
										continue;
									case enIfFileExist.Overwrite:
										break; //fall thru
									default:
										throw new NotImplementedException("enIfFileExist " + IfFileExist);
								}
							}

							using (FileStream writer = fi.Create())
							{
								int byteCount;
								while ((byteCount = reader.Read(buffer, 0, buffer.Length)) > 0)
									writer.Write(buffer, 0, byteCount);
							}

							SetLastWriteTimeFixed(fi, entry.ModifiedTime);
						}

						unzippedSomeEntry = true;
					}
				}
			}

			if (!unzippedSomeEntry)
				throw new ZipException("No files to unzip");
		}

		void SetLastWriteTimeFixed(FileSystemInfo fsi, DateTime dt)
		{
			//http://www.codeproject.com/KB/files/csharpfiledate.aspx?msg=2885854#xx2885854xx
			TimeSpan localOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
			fsi.LastWriteTimeUtc = dt - localOffset;
		}

		private string CreateUnzippedName(ZipEntry entry)
		{
			string name = null;
			if (NoDirectoryNames)
			{
				if (entry.IsDirectory)
					throw new Exception("NoDirectoryNames but got dir");
				name = Path.GetFileName(entry.Name);
			}
			else
				name = entry.Name.TrimStartDirSep(); //trim not requred since we dont use Path.Combine, but do it anyways

			//PS: don't use Path.Combine here! if name is absolute, it will override destination!
			//use Path.GetFullPath to normalize path. also it will give error if invalid chars in path
			//FIXME: figure out if other ziputils allow storing relative path's in zip (\..\..\test) and how they handle extraction
			//of such items.
			string unzippedName = Path.GetFullPath(Destination + Path.DirectorySeparatorChar + name);
			CreateUnzippedNameEventArgs ea = new CreateUnzippedNameEventArgs(unzippedName, entry.IsDirectory);
			OnCreateUnzippedName(ea);
			return ea.UnzippedName;
		}

		private void OnCreateUnzippedName(CreateUnzippedNameEventArgs ea)
		{
			if (CreateUnzippedNameEvent != null)
				CreateUnzippedNameEvent(this, ea);
		}

		public event CreateUnzippedNameEventHandler CreateUnzippedNameEvent;

	}

	public delegate void CreateUnzippedNameEventHandler(object sender, CreateUnzippedNameEventArgs ea);
	public class CreateUnzippedNameEventArgs : EventArgs
	{
		public string UnzippedName { get; set; }
		public bool IsDirectory { get; private set; }
		public CreateUnzippedNameEventArgs(string unzippedName, bool isDirectory)
		{
			this.UnzippedName = unzippedName;
			this.IsDirectory = isDirectory;
		}
	}

	public enum enIfFileExist
	{
		Overwrite,
		Skip,
		Exception,
	}
}
