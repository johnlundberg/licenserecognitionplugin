using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace lpr_plugin
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!TryParseArgs(args, workingDirectory, out Arguments arguments)) { return; }

            using (Process process = new Process())
            {
                //Set up start info
                process.StartInfo.FileName = arguments.ExecutablePath;
                process.StartInfo.Arguments = $"-j \"{arguments.FilePath}\"";
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
                    }
                }, TaskCreationOptions.LongRunning);

                //Start the process
                process.Start();

                //Start task to read stdOut.
                outputReaderTask.Start();
                process.WaitForExit();
                outputReaderTask.Wait();

                var output = JsonConvert.DeserializeObject<AlprOutput>(stringOutput);
                var pluginResult = new PluginResult();
                if (output?.Results != null)
                {
                    foreach (var result in output.Results)
                    {
                        if (result.Confidence >= arguments.ConfidenceThreshold)
                        {
                            pluginResult.Bookmarks.Add(new Bookmark()
                            {
                                BookmarkPath = "ALPR/" + result.Plate,
                                Comment = $"Confidence: {result.Confidence:00.0}"
                            });
                        }
                    }
                }
                var pluginResultString = JsonConvert.SerializeObject(pluginResult);
                Console.WriteLine(pluginResultString);
            }
        }

        private static bool TryParseArgs(string[] args, string workingDirectory, out Arguments arguments)
        {
            arguments = new Arguments();
            if (args.Length != 3)
            {
                Console.Error.WriteLine($"Invalid arguments: " + string.Join(" ", args));
                Console.Error.WriteLine($"Expected arguments: <path to alpr> <path to image> <confidence threshold>");
                return false;
            }

            arguments.ExecutablePath = Path.Combine(workingDirectory, args[0]);
            if (!File.Exists(arguments.ExecutablePath))
            {
                Console.Error.WriteLine($"Could not find executable: " + arguments.ExecutablePath);
                return false;
            }

            arguments.FilePath = args[1];
            if (!File.Exists(arguments.FilePath))
            {
                Console.Error.WriteLine($"Could not find image: " + arguments.FilePath);
                return false;
            }

            if (!double.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double threshold))
            {
                Console.Error.WriteLine($"Could not parse confidence threshold, should be on form 00.0 but was " + args[2]);
                return false;
            }
            arguments.ConfidenceThreshold = threshold;
            return true;
        }
    }
}