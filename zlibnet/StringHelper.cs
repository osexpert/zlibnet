using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ZLibNet
{
	internal static class StringHelper
	{
		public static string SetEndDirSep(this string s)
		{
			if (s.EndsWithDirSep())
				return s;
			else
				return s + Path.DirectorySeparatorChar;
		}

		public static string TrimStartDirSep(this string s)
		{
			return s.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		public static string TrimEndDirSep(this string s)
		{
			return s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		public static bool EndsWithDirSep(this string str)
		{
			if (str.Length == 0)
				return false;
			char lastChar = str[str.Length - 1];
			return lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar;
		}
		public static bool StartsWithDirSep(this string str)
		{
			if (str.Length == 0)
				return false;
			char firstChar = str[0];
			return firstChar == Path.DirectorySeparatorChar || firstChar == Path.AltDirectorySeparatorChar;
		}
		public static bool WildcardMatch(this string str, string wildcompare, bool ignoreCase)
		{
			if (ignoreCase)
				return str.ToLower().WildcardMatch(wildcompare.ToLower());
			else
				return str.WildcardMatch(wildcompare);
		}

		/// <summary>Check if <paramref name="str"/> only contains Ascii 8 bit characters.</summary>
		public static bool IsAscii(this string str)
		{
			foreach (char ch in str)
			{
				if (ch > 0xff)
				{
					return false;
				}
			}
			return true;
		}

		public static bool WildcardMatch(this string str, string wildcompare)
		{
			if (string.IsNullOrEmpty(wildcompare))
				return str.Length == 0;

			// workaround: *.* should get all
			wildcompare = wildcompare.Replace("*.*", "*");

			int pS = 0;
			int pW = 0;
			int lS = str.Length;
			int lW = wildcompare.Length;

			while (pS < lS && pW < lW && wildcompare[pW] != '*')
			{
				char wild = wildcompare[pW];
				if (wild != '?' && wild != str[pS])
					return false;
				pW++;
				pS++;
			}

			int pSm = 0;
			int pWm = 0;
			while (pS < lS && pW < lW)
			{
				char wild = wildcompare[pW];
				if (wild == '*')
				{
					pW++;
					if (pW == lW)
						return true;
					pWm = pW;
					pSm = pS + 1;
				}
				else if (wild == '?' || wild == str[pS])
				{
					pW++;
					pS++;
				}
				else
				{
					pW = pWm;
					pS = pSm;
					pSm++;
				}
			}
			while (pW < lW && wildcompare[pW] == '*')
				pW++;
			return pW == lW && pS == lS;
		}
	}
}
