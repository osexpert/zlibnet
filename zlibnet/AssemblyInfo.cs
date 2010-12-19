
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("zlibnet")]
[assembly: AssemblyDescription("zlibnet")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://zlibnet.codeplex.com")]
[assembly: AssemblyCopyright("Copyright (C) Jean-loup Gailly, Mark Adler, Gerry Shaw, Dave F. Baskin, Gunnar Dalsnes")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.0")]


public static class ZLibDll
{
	internal const string Name32 = "zlib32.dll";
	internal const string Name64 = "zlib64.dll";
}

// This will not compile with Visual Studio.  If you want to build a signed
// executable use the NAnt build file.  To build under Visual Studio just
// exclude this file from the build.
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile(@"..\Zip.key")]
//[assembly: AssemblyKeyName("")]
