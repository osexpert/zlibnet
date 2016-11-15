
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("zlibnet")]
[assembly: AssemblyDescription("zlibnet")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://zlibnet.codeplex.com")]
[assembly: AssemblyCopyright("Copyright (C) Gerry Shaw, Dave F. Baskin, Gunnar Dalsnes")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.3.3")]


public static class ZLibDll
{
	internal const string Name32 = "zlib32.dll";
	internal const string Name64 = "zlib64.dll";

	internal static bool Is64 = (IntPtr.Size == 8);

	internal const string ZLibDllFileVersion = "1.2.8.1";

	internal static string GetDllName()
	{
		if (Is64)
			return Name64;
		else
			return Name32;
	}
	
}

