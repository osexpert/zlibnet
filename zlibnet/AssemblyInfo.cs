
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("zlibnet")]
[assembly: AssemblyDescription("zlibnet")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://zlibnet.codeplex.com")]
[assembly: AssemblyCopyright("Copyright (C) Jean-loup Gailly, Mark Adler, Gerry Shaw, Dave F. Baskin, Gunnar Dalsnes")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("0.9.0.*")]


public static class ZLibDll
{
	/// <summary>
	/// 32bit zlib dll in same folder as application
	/// </summary>
	internal const string Name = "zlib32.dll";
	/// <summary>
	/// 64bit zlib dll in same folder as application
	/// </summary>
//	internal const string Name = "zlib64.dll";
	/// <summary>
	/// 32bit and 64bit zlib dll's renamed to same name zlib.dll and:
	/// -32bit version is placed in C:\WINDOWS\SysWOW64
	/// -64bit version is placed in C:\WINDOWS\system32
	/// The correct dll will be used automatically.
	/// </summary>
//	internal const string Name = "zlib.dll";
}

// This will not compile with Visual Studio.  If you want to build a signed
// executable use the NAnt build file.  To build under Visual Studio just
// exclude this file from the build.
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile(@"..\Zip.key")]
//[assembly: AssemblyKeyName("")]
