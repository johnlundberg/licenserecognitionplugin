using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace lpr_plugin
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!TryParseArgs(args, out Arguments arguments)) { return; }

            using (Process process = new Process())
            {
                //Set up start info
                process.StartInfo.FileName = args[0];
                process.StartInfo.Arguments = $"-j {args[1]}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                //Create tasks to write stdIn and read stdOut and stdErr.
                process.StartInfo.RedirectStandardOutput = true;

                string stringOutput = null;
                var outputReaderTask = new Task(() =>
                {
                    using (var r = new StreamReader(process.StandardOutput.BaseStream))
                    {
                        stringOutput = r.ReadToEnd();
                        Console.WriteLine(stringOutput);
                    }
                }, TaskCreationOptions.LongRunning);

                //Start the process
                process.Start();
                //Start task to read stdOut.
                outputReaderTask.Start();
                process.WaitForExit();
                outputReaderTask.Wait();

                var output = JsonConvert.DeserializeObject<AlprOutput>(stringOutput);
                List<Bookmark> bookmarkResults = new List<Bookmark>();
                foreach (var result in output.Results)
                {
                    bookmarkResults.Add(new Bookmark()
                    {
                        BookmarkPath = "ALPR/" + result.Plate,
                        Comment = $"Confidence: {result.Confidence:00.0}"
                    });
                }

#if DEBUG
                Console.ReadLine();
#endif
            }
        }

        private static bool TryParseArgs(string[] args, out Arguments arguments)
        {
            arguments = null;
            if (args.Length != 3)
            {
                Console.Error.WriteLine($"Invalid arguments: " + string.Join(" ", args));
                Console.Error.WriteLine($"Expected arguments: <path to alpr> <path to image> <confidence threshold>");
                return false;
            }
            return true;
        }
    }
}