//
// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;

namespace Amazon.Kinesis.ClientLibrary.Bootstrap
{
    /// <summary>
    /// Represents a Maven java package. We need to download a bunch of these in order
    /// to use the java KCL.
    /// </summary>
    internal class MavenPackage
    {
        public readonly String GroupId;
        public readonly String ArtifactId;
        public readonly String Version;

        /// <summary>
        /// Gets the name of the jar file of this Maven package.
        /// </summary>
        /// <value>The name of the jar file.</value>
        public String FileName
        {
            get { return String.Format("{0}-{1}.jar", ArtifactId, Version); }
        }

        public MavenPackage(String groupId, String artifactId, String version)
        {
            GroupId = groupId;
            ArtifactId = artifactId;
            Version = version;
        }

        /// <summary>
        /// Check if the jar file for this Maven package already exists on disk.
        /// </summary>
        /// <param name="folder">Folder to look in.</param>
        public bool Exists(String folder)
        {
            return File.Exists(Path.Combine(folder, FileName));
        }

        /// <summary>
        /// Download the jar file for this Maven package.
        /// </summary>
        /// <param name="folder">Folder to download the file into.</param>
        public void Fetch(String folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            String destination = Path.Combine(folder, FileName);
            if (!File.Exists(destination)) {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                    try {
                        Console.Error.WriteLine(Url + " --> " + destination);
                        using var response = client.GetAsync(new Uri(Url)).GetAwaiter().GetResult();
                        response.EnsureSuccessStatusCode();

                        using var fs = new FileStream(destination, FileMode.Create);
                        response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new Exception($"Failed to download {FileName}: {ex.Message}", ex);
                    }
            }
        }

        /// <summary>
        /// Gets the URL to the jar file for this Maven package.
        /// </summary>
        /// <value>The URL.</value>
        private String Url
        {
            get
            {
                List<String> urlParts = new List<String>();
                urlParts.AddRange(GroupId.Split('.'));
                urlParts.Add(ArtifactId);
                urlParts.Add(Version);
                urlParts.Add(FileName);
                return "https://repo1.maven.org/maven2/" + String.Join("/", urlParts);
            }
        }
    }

    /// <summary>
    /// Command line options.
    /// </summary>
    class Options
    {
        [Option('j', "java", Required = false,
            HelpText =
                "Path to java, used to start the KCL multi-lang daemon. Attempts to auto-detect if not specified.")]
        public string JavaLocation { get; set; }

        [Option('p', "properties", Required = true, HelpText = "Path to properties file used to configure the KCL.")]
        public string PropertiesFile { get; set; }

        [Option("jar-folder", Required = false, HelpText = "Folder to place required jars in. Defaults to ./jars")]
        public string JarFolder { get; set; }

        [Option('e', "execute", HelpText =
            "Actually launch the KCL. If not specified, prints the command used to launch the KCL.")]
        public bool ShouldExecute { get; set; }

        [Option('l', "log-configuration", Required = false, HelpText = "A Logback XML configuration file")]
        public string LogbackConfiguration { get; set; }
    }

    internal enum OperatingSystemCategory
    {
        UNIX,
        WINDOWS
    }

    /// <summary>
    /// The Bootstrap program helps the user download and launch the KCL multi-lang daemon (which is in java).
    /// </summary>
    class MainClass
    {
        private static readonly OperatingSystemCategory CURRENT_OS = Environment.OSVersion.ToString().Contains("Unix")
            ? OperatingSystemCategory.UNIX
            : OperatingSystemCategory.WINDOWS;

        private static readonly List<MavenPackage> MAVEN_PACKAGES = ParseMavenPackages();

        private static List<MavenPackage> ParseMavenPackages()
        {
            string xmlns = "{http://maven.apache.org/POM/4.0.0}";
            XElement mavenRoot = XElement.Load("../pom.xml");

            Dictionary<string, string> commonVersions = new Dictionary<string, string>();
            foreach (XElement el in mavenRoot.Descendants(xmlns + "properties").Elements())
            {
                commonVersions.Add("${" + el.Name.ToString().Replace(xmlns, "") + "}", (string)el);
            }

            List<MavenPackage> packages = new List<MavenPackage>();
            foreach (XElement el in mavenRoot.Descendants(xmlns + "dependency"))
            {
                string version = (string)el.Element(xmlns + "version");
                if (commonVersions.ContainsKey(version))
                {
                    packages.Add(new MavenPackage(
                        (string)el.Element(xmlns + "groupId"),
                        (string)el.Element(xmlns + "artifactId"),
                        commonVersions[version]));
                }
                else
                {
                    packages.Add(new MavenPackage(
                        (string)el.Element(xmlns + "groupId"),
                        (string)el.Element(xmlns + "artifactId"),
                        version));
                }
            }

            return packages;
        }

        /// <summary>
        /// Downloads all the required jars from Maven and returns a classpath string that includes all those jars.
        /// </summary>
        /// <returns>Classpath string that includes all the jars downloaded.</returns>
        /// <param name="jarFolder">Folder into which to save the jars.</param>
        private static string FetchJars(string jarFolder)
        {
            if (jarFolder == null)
            {
                jarFolder = "jars";
            }

            if (!Path.IsPathRooted(jarFolder))
            {
                jarFolder = Path.Combine(Directory.GetCurrentDirectory(), jarFolder);
            }

            Console.Error.WriteLine("Fetching required jars...");

            foreach (MavenPackage mp in MAVEN_PACKAGES)
            {
                mp.Fetch(jarFolder);
            }

            Console.Error.WriteLine("Done.");

            List<string> files = Directory.GetFiles(jarFolder).Where(f => f.EndsWith(".jar")).ToList();
            files.Add(Directory.GetCurrentDirectory());
            return string.Join(Path.PathSeparator.ToString(), files);
        }

        private static string FindJava(string java)
        {
            // See if "java" is already in path and working.
            if (java == null)
            {
                java = "java";
            }

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = java,
                    Arguments = "-version",
                    UseShellExecute = false
                }
            };
            try
            {
                proc.Start();
                proc.WaitForExit();
                return java;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding Java: {ex.Message}");
                return null;
            }
            //TODO find away to read from registery on different OSs
            // Failing that, look in the registry.
            //bool hasRegistry = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            //foreach (var view in new [] { RegistryView.Registry64, RegistryView.Registry32 })
            //{
            //    var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            //    var javaRootKey = localKey.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment");
            //    foreach (var jreKeyName in javaRootKey.GetSubKeyNames())
            //    {
            //        var jreKey = javaRootKey.OpenSubKey(jreKeyName);
            //        var javaHome = jreKey.GetValue("JavaHome") as string;
            //        var javaExe = Path.Combine(javaHome, "bin", "java.exe");
            //        if (File.Exists(javaExe))
            //        {
            //            return javaExe;
            //        }
            //    }
            //}
            //return null;
        }

        public static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);

            parserResult.WithParsed(options =>
            {
                string javaClassPath = FetchJars(options.JarFolder);

                string java = FindJava(options.JavaLocation);

                if (java == null)
                {
                    Console.Error.WriteLine(
                        "java could not be found. You may need to install it, or manually specify the path to it.");

                    Environment.Exit(2);
                }

                List<string> cmd = new List<string>()
                {
                    java,
                    "-cp",
                    javaClassPath,
                    "software.amazon.kinesis.multilang.MultiLangDaemon",
                    "-p",
                    options.PropertiesFile
                };
                if (!string.IsNullOrEmpty(options.LogbackConfiguration))
                {
                    cmd.Add("-l");
                    cmd.Add(options.LogbackConfiguration);
                }
                if (options.ShouldExecute)
                {
                    // Start the KCL.
                    Process proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = cmd[0],
                            Arguments = string.Join(" ", cmd.Skip(1)),
                            UseShellExecute = false
                        }
                    };
                    proc.Start();
                    proc.WaitForExit();
                }
                else
                {
                    // Print out a command that can be used to start the KCL.
                    string c = string.Join(" ", cmd.Select(f => "\"" + f + "\""));
                    if (CURRENT_OS == OperatingSystemCategory.WINDOWS)
                    {
                        c = "& " + c;
                    }

                    Console.WriteLine(c);
                }
            });
        }
    }
}