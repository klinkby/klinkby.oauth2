using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else

[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("Mads Klinkby")]
[assembly: AssemblyProduct("Klinkby")]
[assembly: AssemblyCopyright("Copyright © Mads Breusch Klinkby 2011-2014")]
[assembly: AssemblyCulture("en-US")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]