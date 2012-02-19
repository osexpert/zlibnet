using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ZLibNet
{
	internal class FileSpecMatcher
	{
		class FileSpec
		{
			public string FileName;
			public string DirName;

			public FileSpec(string fileSpec, bool recurseFiles)
			{
				DirName = Path.GetDirectoryName(fileSpec);
				if (DirName == null)
					throw new ArgumentException("invalid fileSpec");

				FileName = Path.GetFileName(fileSpec);
				if (FileName == null)
					throw new ArgumentException("invalid fileSpec");

				DirName = DirName.SetEndDirSep().TrimStartDirSep();

				if (recurseFiles && FileName.Length > 0)
				{
					DirName += "*";
				}
			}
		}

		List<FileSpec> pFileSpecs = new List<FileSpec>();

		/// <summary>
		/// if true, the fileName in spec matches any file in any subdirs of the matched dir.
		/// if false, spec matches one exact dir + file
		/// </summary>
		public FileSpecMatcher(List<string> specs, bool recurseFiles)
		{
			foreach (string spec in specs)
				pFileSpecs.Add(new FileSpec(spec, recurseFiles));
		}

		public bool MatchSpecs(string entryName, bool entryIsDir)
		{
			if (entryIsDir)
				entryName = entryName.SetEndDirSep();

			string entryDirName = Path.GetDirectoryName(entryName); //will create a backslashed name
			string entryFileName = Path.GetFileName(entryName);

			//trimStart: dont want to make an empty string \
			//also, if someone sends paths that start with \, well get rid of them leading \
			//also, we never want an entry to start with \ (scary!)
			entryDirName = entryDirName.SetEndDirSep().TrimStartDirSep();

			foreach (FileSpec fileSpec in pFileSpecs)
			{
				bool fileMatch = entryFileName.WildcardMatch(fileSpec.FileName, true);
				bool dirMath = entryDirName.WildcardMatch(fileSpec.DirName, true);

				if (dirMath && fileMatch)
					return true;
			}

			return false;
		}
	}
}
