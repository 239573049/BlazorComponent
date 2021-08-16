﻿using BlazorComponent.Doc.CLI.Interfaces;
using BlazorComponent.Doc.CLI.Wrappers;
using BlazorComponent.Doc.Models;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorComponent.Doc.CLI.Commands
{
    public class GenerateDemoJsonCommand : IAppCommand
    {
        public string Name => "demo2json";

        public void Execute(CommandLineApplication command)
        {
            command.Description = "Generate json file of demos";
            command.HelpOption();

            CommandArgument directoryArgument = command.Argument(
                "source", "[Required] The directory of demo files.");

            CommandArgument outputArgument = command.Argument(
                "output", "[Required] The directory where the json file to output");

            command.OnExecute(() =>
            {
                string source = directoryArgument.Value;
                string output = outputArgument.Value;

                if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
                {
                    Console.WriteLine($"Invalid source: {source}");
                    return 1;
                }

                if (string.IsNullOrEmpty(output))
                {
                    output = "./";
                }

                string demoDirectory = Path.Combine(Directory.GetCurrentDirectory(), source);

                try
                {
                    GenerateFiles(demoDirectory, output);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    return -1;
                }

                return 0;
            });
        }

        private void GenerateFiles(string demoDirectory, string output)
        {
            DirectoryInfo demoDirectoryInfo = new DirectoryInfo(demoDirectory);
            if (!demoDirectoryInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                Console.WriteLine("{0} is not a directory", demoDirectory);
                return;
            }

            IList<Dictionary<string, DemoComponentModel>> componentList = null;
            IList<string> demoTypes = null;

            var directories = demoDirectoryInfo.GetFileSystemInfos()
                .SelectMany(x => (x as DirectoryInfo).GetFileSystemInfos()).OrderBy(r=>r.Name);

            foreach (FileSystemInfo component in directories)
            {
                if (!(component is DirectoryInfo componentDirectory)) continue;

                FileSystemInfo docDir = componentDirectory.GetFileSystemInfos("doc")?.FirstOrDefault();
                FileSystemInfo demoDir = componentDirectory.GetFileSystemInfos("demo")?.FirstOrDefault();

                Dictionary<string, DemoComponentModel> componentDic = new Dictionary<string, DemoComponentModel>();

                if (docDir != null && docDir.Exists)
                {
                    foreach (FileSystemInfo docItem in (docDir as DirectoryInfo).GetFileSystemInfos().OrderBy(r => r.Name))
                    {
                        string language = docItem.Name.Replace("index.", "").Replace(docItem.Extension, "");
                        string content = File.ReadAllText(docItem.FullName);
                        (Dictionary<string, string> Meta, string desc, string apiDoc) docData = DocWrapper.ParseDemoDoc(content);

                        componentDic.Add(language, new DemoComponentModel()
                        {
                            Title = docData.Meta["title"],
                            SubTitle = docData.Meta.TryGetValue("subtitle", out string subtitle) ? subtitle : null,
                            Type = docData.Meta["type"],
                            Desc = docData.desc,
                            ApiDoc = GetApiDoc(docData.apiDoc),
                            Cols = docData.Meta.TryGetValue("cols", out var cols) ? int.Parse(cols) : (int?)null,
                            Cover = docData.Meta.TryGetValue("cover", out var cover) ? cover : null,
                        });
                    }
                }

                if (demoDir != null && demoDir.Exists)
                {
                    foreach (IGrouping<string, FileSystemInfo> demo in (demoDir as DirectoryInfo).GetFileSystemInfos()
                            .OrderBy(r=>r.Name)
                            .GroupBy(x => x.Name
                            .Replace(x.Extension, "")
                            .Replace("-", "")
                            .Replace("_", "")
                            .Replace("Demo", "")
                            .ToLower()))
                    {
                        List<FileSystemInfo> showCaseFiles = demo.ToList();
                        FileSystemInfo razorFile = showCaseFiles.FirstOrDefault(x => x.Extension == ".razor");
                        FileSystemInfo descriptionFile = showCaseFiles.FirstOrDefault(x => x.Extension == ".md");
                        string code = razorFile != null ? File.ReadAllText(razorFile.FullName) : null;

                        string demoType = $"{demoDirectoryInfo.Name}{razorFile.FullName.Replace(demoDirectoryInfo.FullName, "").Replace("/", ".").Replace("\\", ".").Replace(razorFile.Extension, "")}";

                        (DescriptionYaml Meta, string Style, Dictionary<string, string> Descriptions) descriptionContent = descriptionFile != null ? DocWrapper.ParseDescription(File.ReadAllText(descriptionFile.FullName)) : default;

                        demoTypes ??= new List<string>();
                        demoTypes.Add(demoType);

                        foreach (KeyValuePair<string, string> title in descriptionContent.Meta.Title)
                        {
                            string language = title.Key;
                            List<DemoItemModel> list = componentDic[language].DemoList ??= new List<DemoItemModel>();

                            list.Add(new DemoItemModel()
                            {
                                Title = title.Value,
                                Order = descriptionContent.Meta.Order,
                                Iframe = descriptionContent.Meta.Iframe,
                                Code = code,
                                Description = descriptionContent.Descriptions[title.Key],
                                Name = descriptionFile?.Name.Replace(".md", ""),
                                Style = descriptionContent.Style,
                                Debug = descriptionContent.Meta.Debug,
                                Docs = descriptionContent.Meta.Docs,
                                Type = demoType
                            });
                        }
                    }
                }

                componentList ??= new List<Dictionary<string, DemoComponentModel>>();
                componentList.Add(componentDic);
            }

            if (componentList == null)
                return;

            string configFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), output);

            if (!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            var componentI18N = componentList
                .SelectMany(x => x).GroupBy(x => x.Key);

            foreach (IGrouping<string, KeyValuePair<string, DemoComponentModel>> componentDic in componentI18N)
            {
                IEnumerable<DemoComponentModel> components = componentDic.Select(x => x.Value);
                string componentJson = JsonSerializer.Serialize(components, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                string configFilePath = Path.Combine(configFileDirectory, $"components.{componentDic.Key}.json");

                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                }

                File.WriteAllText(configFilePath, componentJson);

                Console.WriteLine("Generate demo file to {0}", configFilePath);
            }

            var demoJson = JsonSerializer.Serialize(demoTypes, new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            string demoFilePath = Path.Combine(configFileDirectory, $"demoTypes.json");
            if (File.Exists(demoFilePath))
            {
                File.Delete(demoFilePath);
            }
            File.WriteAllText(demoFilePath, demoJson);
        }

        private string GetApiDoc(string apiDoc)
        {
            var h1Class = "\"m-heading text-h3 text-sm-h3 mb-2\""; ;
            var h2Class = "\"m-heading text-h4 text-sm-h4 mb-3\"";
            var aClass = "\"text-decoration-none text-right text-md-left\"";

            apiDoc = Regex.Replace(apiDoc, "<h(?<n>1|2)>(?<title>.*)<\\/h(1|2)>", m => m.Groups["n"].ToString() == "1" ? $@"
                <h1 class={h1Class}>
                    <a class={aClass}>#</a>
                    {m.Groups["title"]}
                </h1>" :
                $@"
                <h2 class={h2Class}>
                    <a class={aClass}>#</a>
                    {m.Groups["title"]}
                </h2>");

            apiDoc = "<section id=\"api\">" + apiDoc + "</section>";

            apiDoc = Regex.Replace(apiDoc, "<a href", "<a class=\"app-link text-decoration-none primary--text font-weight-medium d-inline-block\" href");

            return apiDoc;
        }
    }
}
