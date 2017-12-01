
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace PSArcTool
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    class Program
    {
        static void Main(string[] args)
        {
            var waitForEnter = false;

            try
            {

                Console.WriteLine("PSArcTool");
                Console.WriteLine("Periander 2016-08-17");
                Console.WriteLine("I was mucking around with No Man's Sky modding and wanted to make extracting/creating PAK files easier.");
                Console.WriteLine("Modified by monkeyman192 to allow for mod conflict checking (use the arg -list for a simple list, and -listall for a detailed breakdown of clashes)");

                // let's go through the arguments and see if we have any kwargs
                // possibilities:
                // out=fname         - this is the name of the output pak file
                // -list             - this will return a list of all files in the pak file
                string fname = "";
                bool returnlist = false;
                string returnlistmode = "concise";
                foreach (string arg in args)
                {
                    if (arg.Contains("out="))
                    {
                        fname = String.Format("{0}.pak", arg.Substring(4));
                        args = args.Where(val => val != arg).ToArray();
                    }
                    else
                        fname = "psarc.pak";
                    if (arg == "-list" || arg == "-listall")
                    {
                        returnlist = true;
                        if (arg == "-list")
                            returnlistmode = "concise";
                        if (arg == "-listall")
                            returnlistmode = "verbose";
                        args = args.Where(val => val != arg).ToArray();
                    }
                }
                //Console.WriteLine(String.Format("output name: {0}", fname));
                /*foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }*/


                //// TESTING 
                //args = new[]
                //{
                //    @"C:\Program Files (x86)\Steam\steamapps\common\No Man's Sky\GAMEDATA\PCBANKS\PSARC.PAK"
                //    //@"C:\Program Files (x86)\Steam\steamapps\common\No Man's Sky\GAMEDATA\PCBANKS\A.txt",
                //    //@"C:\Program Files (x86)\Steam\steamapps\common\No Man's Sky\GAMEDATA\PCBANKS\B.txt"

                //    //@"C:\Users\peria\Desktop\New folder\SF\psarc.pak"
                //    //@"C:\Users\peria\Desktop\New folder\SF\A.txt",
                //    //@"C:\Users\peria\Desktop\New folder\SF\B.txt"

                //};

                if (returnlist)
                {
                    // check to see if all the arguments are files ending with .pak, or the arguments specified are folders
                    if (args.Any() && 
                        (args.All(arg => File.Exists(arg) && (arg.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) || arg.EndsWith(".psarc", StringComparison.OrdinalIgnoreCase))) ||
                        args.All(arg => Directory.Exists(arg))))
                    {
                        // let's get all the sub-directory paks:
                        List<string> extraMods = new List<string>();
                        foreach (string dir in args.Where(dir => Directory.Exists(dir)))
                        {
                            IEnumerable<string> files = Directory.EnumerateFiles(dir, "*.pak", SearchOption.AllDirectories);
                            extraMods.AddRange(files);
                        }
                        // and then add them to the args list
                        extraMods.AddRange(args);
                        args = extraMods.ToArray<string>();

                        // List of kvp's, with the key being the name of the pak, and the value being a list of all contained files
                        Dictionary<string, List<string>> containedFiles = new Dictionary<string, List<string>>();
                        // Dictionary with the key being the name of the 
                        Dictionary<string, List<string>> sharedFiles = new Dictionary<string, List<string>>();
                        foreach (var pakFilePath in args)
                        {
                            // get the contents of the pak
                            List<string> pakContents = Functions.ListContents(pakFilePath);
                            // next, iterate over the current contents of the contained files dict
                            foreach (KeyValuePair<string, List<string>> kvp in containedFiles)
                            {
                                // for each pak file, create an enumerable with the list of shared files
                                IEnumerable<string> intersection = kvp.Value.Intersect(pakContents);
                                // and for each file that is shared, add the information to the sharedFiles dictionary
                                foreach (string file in intersection)
                                {
                                    // if the file that is shared is already a key in the shared file dictionary, then simply add the new name to the list for the key
                                    if (sharedFiles.ContainsKey(file))
                                        sharedFiles[file].Add(pakFilePath);
                                    // otherwise add a new list with the names of both pak's
                                    else
                                        sharedFiles.Add(file, new List<string> { kvp.Key, pakFilePath });
                                }
                            }
                            containedFiles.Add(pakFilePath, Functions.ListContents(pakFilePath));
                        }

                        if (sharedFiles.Count() != 0)
                        {
                            Console.WriteLine("\nThe following mods clash:");
                            if (returnlistmode == "concise")
                            {
                                IEnumerable<string> clashingMods = Enumerable.Empty<string>();
                                foreach (KeyValuePair<string, List<string>> kvp in sharedFiles)
                                    clashingMods = clashingMods.Union(kvp.Value);
                                foreach (string mod in clashingMods)
                                    Console.WriteLine(mod);
                            }
                            // in this case, write all the files that clash in each mod (they asked for it!!)
                            else if (returnlistmode == "verbose")
                            {
                                foreach (KeyValuePair<string, List<string>> kvp in sharedFiles)
                                {
                                    foreach (string mod in kvp.Value)
                                    {
                                        Console.WriteLine("{0} is in the file {1}", kvp.Key, mod);
                                    }
                                }
                            }
                        }
                        else
                            Console.WriteLine("\nNo mods clash!");

                        Console.WriteLine("\nPress ENTER to quit.");
                        Console.ReadLine();
                    }
                }
                else
                {
                    if (args.Any() &&
                        args.All(arg => File.Exists(arg) && (arg.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) || arg.EndsWith(".psarc", StringComparison.OrdinalIgnoreCase))))
                    {
                        foreach (var pakFilePath in args)
                            Functions.Extract(pakFilePath);

                    }
                    else if (args.Any() && args.All(arg => File.Exists(arg) || Directory.Exists(arg)))
                    // A collection of files or folders
                    {
                        Functions.Create(args, string.Empty, fname);
                    }
                    else
                    {
                        waitForEnter = true;
                        Console.WriteLine("Usage: Pass PAK/PSARC files as arguments to extract (click and drag on to PSArcTool). Pass anything else to create a PAK file.");
                    }
                }

            }
            catch (Exception exc)
            {
                waitForEnter = true;
                Console.WriteLine(exc);
            }

            if (waitForEnter)
            {
                Console.WriteLine("Press ENTER to quit.");
                Console.ReadLine();
            }
        }
    }
}
