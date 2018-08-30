using System.Reflection;
using System.Runtime.CompilerServices;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.

[assembly: AssemblyTitle("Amazon Kinesis Client Library .NET")]
[assembly: AssemblyDescription("Amazon Kinesis Client Library for .NET")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Amazon.com, Inc")]
[assembly: AssemblyProduct("Amazon Kinesis Client Library .NET")]
[assembly: AssemblyCopyright("Copyright 2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

[assembly: AssemblyVersion("1.0")]
[assembly: AssemblyFileVersion("1.0.0")]

// The following attributes are used to specify the signing key for the assembly,
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

// Required for Substitutes in tests
[assembly:InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly:InternalsVisibleTo("ClientLibrary.Test")]