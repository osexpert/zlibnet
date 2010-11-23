
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("zlibnet")]
[assembly: AssemblyDescription("zlibnet")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://zlibnet.codeplex.com")]
[assembly: AssemblyCopyright("Copyright (C) Gerry Shaw, Dave F. Baskin, Jean-loup Gailly, Mark Adler")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.0")]


public static class ZLibDll
{
	internal const string ZLibVersion = "1.2.5";

#if ANYCPU
	internal const string Name = "zlib.dll";
#elif X86
	internal const string Name = "zlib32.dll";
#elif X64
	internal const string Name = "zlib64.dll";
#endif
}

// This will not compile with Visual Studio.  If you want to build a signed
// executable use the NAnt build file.  To build under Visual Studio just
// exclude this file from the build.
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile(@"..\Zip.key")]
//[assembly: AssemblyKeyName("")]
