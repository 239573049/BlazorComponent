﻿using BlazorComponent.Doc.CLI.Interfaces;
using BlazorComponent.Doc.CLI.Models;
using BlazorComponent.Doc.CLI.Wrappers;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlazorComponent.Doc.CLI.Commands
{
    public class GenerateDocsToHtmlCommand : IAppCommand
    {
        public string Name => "docs2html";

        public void Execute(CommandLineApplication command)
        {
            command.Description = "Generate html files of docs";
            command.HelpOption();

            CommandArgument directoryArgument = command.Argument(
                "source", "[Required] The directory of docs files.");

            CommandArgument outputArgument = command.Argument(
                "output", "[Required] The directory where the json file to output");

            command.OnExecute(() =>
            {
                string source = directoryArgument.Value;
                string output = outputArgument.Value;

                if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
                {
                    Console.WriteLine("Invalid source.");
                    return 1;
                }

                if (string.IsNullOrEmpty(output))
                {
                    output = "./";
                }

                string demoDirectory = Path.Combine(Directory.GetCurrentDirectory(), source);

                GenerateFiles(demoDirectory, output);

                return 0;
            });
        }

        private void GenerateFiles(string docsDirectory, string output)
        {
            DirectoryInfo docsDirectoryInfo = new DirectoryInfo(docsDirectory);
            if (!docsDirectoryInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                Console.WriteLine("{0} is not a directory", docsDirectory);
                return;
            }

            foreach (FileSystemInfo docs in docsDirectoryInfo.GetFileSystemInfos())
            {
                if (docs.Extension != ".md")
                    continue;

                var fileName = docs.Name.Replace(docs.Extension, "");
                var content = File.ReadAllText(docs.FullName);
                var docInfo = DocWrapper.ParseDocs(content);
                var json = JsonSerializer.Serialize(new DocFileModel
                {
                    Order = docInfo.order,
                    Title = docInfo.title,
                    Html = docInfo.html
                }, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var configFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), output);
                if (!Directory.Exists(configFileDirectory))
                {
                    Directory.CreateDirectory(configFileDirectory);
                }

                var configFilePath = Path.Combine(configFileDirectory, $"{fileName}.json");

                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                }

                File.WriteAllText(configFilePath, json);
                Console.WriteLine("Generate demo file to {0}", configFilePath);
            }
        }
    }
}
