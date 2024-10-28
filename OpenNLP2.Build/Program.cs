// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using DotLang.CodeAnalysis.Syntax;
using OpenNLP2.Build;

Console.WriteLine("Download JvmDowngrader...");
await FileDownloadHelper.DownloadFileAsync(
    "https://github.com/unimined/JvmDowngrader/releases/download/1.2.0/jvmdowngrader-1.2.0-all.jar",
    "jvmdowngrader.jar");
Console.WriteLine("Downloaded JvmDowngrader successfully.");

Console.WriteLine("Download Apache OpenNLP...");
await FileDownloadHelper.DownloadFileAsync(
    "https://dlcdn.apache.org/opennlp/opennlp-2.4.0/apache-opennlp-2.4.0-bin.zip",
    "apache-opennlp.zip");

Console.WriteLine("Downloaded Apache OpenNLP successfully.");

var inputPath = Path.Combine("apache-opennlp", "libs");
if (!Directory.Exists("apache-opennlp"))
{
    Directory.CreateDirectory(inputPath);
    using ZipArchive zip = ZipFile.OpenRead("apache-opennlp.zip");
    var libs = zip.Entries.Where(entry => entry.Name.EndsWith(".jar") && entry.FullName.Contains("lib")).ToList();
    foreach (var lib in libs)
    {
        lib.ExtractToFile(Path.Combine(inputPath, lib.Name), true);
    }
}

var libOutPath = Path.Combine("apache-opennlp", "libsJvm8");
if (Directory.Exists(libOutPath))
    Directory.Delete(libOutPath, true);

//Covert to Java 8
ExecuteCommand($"java -jar jvmdowngrader.jar -c 52 downgrade -t {inputPath} {libOutPath}");

var apiPath = Path.Combine(libOutPath, "jvmdowngrader-api.jar");
if (File.Exists(apiPath))
    File.Delete(apiPath);

//Extract the JVM Downgrader API
ExecuteCommand($"java -jar jvmdowngrader.jar -c 52 debug downgradeApi {apiPath}");

//Run jdeps => dependency graph
string[] files = Directory.GetFiles(libOutPath, "*.jar");
var cmd = $"jdeps --multi-release 9 -filter:package -dotoutput dot {string.Join(" ", files)}";
ExecuteCommand(cmd);

//Parse the dependency graph
var depsGrapthStr = File.ReadAllText("./dot/summary.dot");
var parser = new Parser(depsGrapthStr);

var jars = files.Select(Path.GetFileName).ToHashSet();

var depsLookup = parser.Parse().Graphs[0].Statements
    .OfType<EdgeStatementSyntax>()
    .Select(x => (
        ((NodeStatementSyntax)x.Left).Identifier.IdentifierToken.StringValue, 
        ((NodeStatementSyntax)x.Right).Identifier.IdentifierToken.StringValue))
    .Where(pair => jars.Contains(pair.Item1) && jars.Contains(pair.Item2))
    .GroupBy(pair => pair.Item1)
    .ToDictionary(
        group => group.Key, 
        group => group.Select(pair => pair.Item2).ToList()
    );

Dictionary<string, IKVMTask> cache = new Dictionary<string, IKVMTask>();

var pattern = new Regex("-(?<version>[0-9.]*).jar$", RegexOptions.Compiled);
string? GetVersion(string str)
{
    var match = pattern.Match(str);
    return match.Success ? match.Groups["version"].Value : null;
}

IKVMTask GetTask(string jarFile)
{
    if (cache.TryGetValue(jarFile, out IKVMTask task))
        return task;
    
    var deps = depsLookup.TryGetValue(jarFile, out var dependencies) 
        ? dependencies.Select(GetTask).ToList() 
        : new List<IKVMTask>();
    
    if (!jarFile.Contains("jvmdowngrader-api"))
        deps.Add(GetTask("jvmdowngrader-api.jar"));
    
    var ver = GetVersion(jarFile) ?? "1.0.0"; //TODO: release.AssemblyVersion
    
    task = new IKVMTask(jarFile, ver, deps);
    cache[jarFile] = task;
    return task;
} 

//Get the tasks
var tasks = jars.Select(GetTask).ToList();

var libDir = "..\\apache-opennlp\\libs\\";

//Copy libs to Solution Folder 
string libDirSolution = Path.Combine("..\\..\\.." , libDir);
if (!Directory.Exists(libDirSolution))
    Directory.CreateDirectory(libDirSolution);

foreach (var file in Directory.GetFiles(libOutPath))
{
    File.Copy(file, Path.Combine(libDirSolution, Path.GetFileName(file)));
}


//Output the targets
foreach (var ikvmTask in tasks)
{
    Console.WriteLine(ikvmTask.GetTarget(libDir));
    //Compile(ikvmTask);
}

void Compile(IKVMTask task)
{
    string cmd = $"./ikvm/ikvmc {GetIKVMCommandLineArgs(task, "apache-opennlp\\libs\\")}";
    Console.WriteLine(cmd);
    var code = ExecuteCommand(cmd);
    Console.WriteLine($"Done {task.DllFile} ({code})");
}

string GetIKVMCommandLineArgs(IKVMTask task, string jarLibDir)
{
    var dependencies = task.Dependencies
        .SelectMany(x => 
        {
            Compile(x);
            return x.DllReferences();
        })
        .Distinct();
    
    
    var sb = new StringBuilder();
    sb.AppendFormat(" -out:{0}", task.DllFile);

    if (!string.IsNullOrEmpty(task.Version))
    {
        sb.AppendFormat(" -version:{0}", task.Version);
    }

    sb.Append(" -r:IKVM.Runtime.dll");

    if (dependencies.Any())
    {
        sb.Append(" "+ string.Join(" ", dependencies.Select(dep => $"-r:\"{dep}\"")));
    }
    
    sb.Append(" -nostdlib");
    
    sb.Append(" " + Path.Combine(jarLibDir, task.JarFile));
    
    return sb.ToString();
}

int ExecuteCommand(string command)
{
    var space = command.IndexOf(' ');
    var process = Process.Start(command[..space], command[(space + 1)..]);
    process.WaitForExit();
    return process.ExitCode;
}

record IKVMTask(string JarFile, string Version, List<IKVMTask> Dependencies)
{
    public string DllFile => Path.ChangeExtension(Path.GetFileName(JarFile), ".dll");
    
    public IEnumerable<string> DllReferences() => Dependencies.Select(dep => dep.DllFile);

    public string GetTarget(string directory)
    {
        var depsJars = Dependencies
            .Select(dep => Path.Combine(directory, dep.JarFile)).ToArray();
        var refs = string.Join(";", depsJars);
        var refTarget = depsJars.Any() ? $"\n     <References>{refs}</References>" : string.Empty;
        
        return $"""
               <IkvmReference Include="{Path.Combine(directory, JarFile)}">
                    <Aliases>{Path.GetFileNameWithoutExtension(JarFile)}</Aliases>
                    <AssemblyVersion>{Version}</AssemblyVersion>
                    <AssemblyFileVersion>{Version}</AssemblyFileVersion>{refTarget}
               </IkvmReference>
               """;
    }
}