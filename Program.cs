﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEpubToText
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args is null || args.Count() < 2)
                {
                    Console.WriteLine("Syntax: <from_path> <to_path>");
                    return 1;
                }
                Console.WriteLine("SimpleEpubToText: {0} {1}", args[0], args[1]);
                ConvertAllEpub(args[0], args[1]);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 2;
            }
            finally
            {
#if DEBUG
                Console.ReadLine();
#endif
            }
        }

        private static void ConvertAllEpub(string fromFolder, string toFolder)
        {
            if (!Directory.Exists(toFolder))
            {
                Directory.CreateDirectory(toFolder);
            }
            foreach (string filepath in Directory.EnumerateFiles(fromFolder, "*.epub", SearchOption.TopDirectoryOnly))
            {
                DoConversion(fromFolder, toFolder, Path.GetFileName(filepath));
            }
            foreach (string ds in Directory.EnumerateDirectories(fromFolder))
            {
                string subdir = Path.GetFileName(ds);
                ConvertAllEpub(Path.Combine(fromFolder, subdir), Path.Combine(toFolder, subdir));
            }
        }

        private static void DoConversion(string fromFolder, string toFolder, string filename)
        {
            Console.WriteLine(filename);
        }
    }
}
