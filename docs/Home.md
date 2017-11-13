**Project Description**
zlibnet - c# zlib wrapper library


Features:
-zip/unzip
-compression/decompression streams (DeflateStream, GZipStream, ZLibStream)
-fast, since using the native/unmanaged zlib library/dll
-UTF8 zip entry name/comment
-Unicode zip file name
-Zip64 support (auto detect)
-64bit support
-Embedded zlib dlls with unpack/load from temp folde

Not supported/zlib limitations:
-encryption/password protection
-multi volume archives

zlibnet is a souped up library based on 2 other libraries, mainly:
[http://www.organicbit.com/zip](http://www.organicbit.com/zip)
[http://zlibnetwrapper.sourceforge.net](http://zlibnetwrapper.sourceforge.net)

The organicbit part (zip/unzip) is left mainly intact, but some bugfixes.
The zlibnetwrapper (stream) was written in managed C++, so it was ported to C#, with some changes.

In addition, I added a wrappers (DynaZip inspired Zipper/UnZipper) around the organicbit part, to simplify zipping/unzipping.

The reason for creating this library was I wanted to migrate from DynaZip but:
-the managed zip implementations I found performed worse than I could live with (the best one I found was over twice as slow as the DynaZip library). This library matches (or outperforms) DynaZip in speed. 
-all the zlib wrappers I found was incomplete (had zip/unzip functionality or stream functionality, but not both), buggy or written in managed c++.

If speed is not important for you, you are probably better off using:
[https://icsharpcode.github.io/SharpZipLib](https://icsharpcode.github.io/SharpZipLib)
or
[http://zipstorer.codeplex.com](http://zipstorer.codeplex.com)

Examples:
{{
Zipper z = new Zipper();
z.ZipFile = @"c:\test\my.zip";
z.ItemList.Add(@"c:\stuff\**.**");
z.ItemList.Add(@"d:\other\stuff\**.**");
z.ExcludeFollowing.Add("*.jpg");
z.PathInZip = enPathInZip.Relative;
z.Recurse = true;
z.Zip();
}}

{{
UnZipper uz = new UnZipper();
uz.Destination = @"c:\test\out\";
uz.IfFileExist = enIfFileExist.Exception;
uz.ItemList.Add("**.**");
uz.Recurse = true;
uz.ZipFile = @"c:\test\my.zip";
uz.UnZip();
}}
