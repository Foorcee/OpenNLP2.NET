using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using DotLang.CodeAnalysis.Syntax;
using OpenNLP2.Build;

const int JAVA8_CLS_VER = 52;

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

//Run jdeps => dependency graph
string[] files = Directory.GetFiles(inputPath, "*.jar");
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


var pattern = new Regex("-(?<version>[0-9.]*).jar$", RegexOptions.Compiled);
string? GetVersion(string str)
{
    var match = pattern.Match(str);
    return match.Success ? match.Groups["version"].Value : null;
}

Dictionary<string, IKVMTask> cache = new Dictionary<string, IKVMTask>();
IKVMTask GetTask(string jarFile)
{
    if (cache.TryGetValue(jarFile, out IKVMTask? task))
        return task;
    
    var deps = depsLookup.TryGetValue(jarFile, out var dependencies) 
        ? dependencies.Select(GetTask).ToList() 
        : new List<IKVMTask>();
    
    var ver = GetVersion(jarFile) ?? "1.0.0"; //TODO: release.AssemblyVersion
    
    task = new IKVMTask(jarFile, ver,  deps);
    cache[jarFile] = task;
    return task;
} 

var libOutPath = Path.Combine("apache-opennlp", "libsJvm8");
if (Directory.Exists(libOutPath))
    Directory.Delete(libOutPath, true);

//Get the tasks
var tasks = jars.Select(GetTask).ToList();
foreach (var task in tasks)
{
    var sourceJar = Path.Combine(inputPath, task.JarFile);
    var targetJar = Path.Combine(libOutPath, task.JarFile);
    ushort? clsVersion = JarClassVersionReader.GetClassVersion(sourceJar);

    if (clsVersion is > JAVA8_CLS_VER) //Größer als Java 8
    {
        string cpArgs = string.Empty;
        if (task.Dependencies.Any())
        {
            string deps = string.Join(";", task.GetAllDependencies()
                .Where(t => t.JarFile != "jvmdowngrader-api.jar")
                .Select(t => Path.Combine(inputPath, t.JarFile)).Distinct());
            cpArgs = $"-cp {deps}";
        }
        
        task.Dependencies.Add(GetTask("jvmdowngrader-api.jar"));
        
        //Covert to Java 8
        ExecuteCommand($"java -jar jvmdowngrader.jar -c {JAVA8_CLS_VER} downgrade -t {sourceJar} {targetJar} {cpArgs}");
    }
    else
    {
        File.Copy(sourceJar, targetJar, true);
    }
}

var apiPath = Path.Combine(libOutPath, "jvmdowngrader-api.jar");
if (File.Exists(apiPath))
    File.Delete(apiPath);

//Extract the JVM Downgrader API
ExecuteCommand($"java -jar jvmdowngrader.jar -c 52 debug downgradeApi {apiPath}");

var libDir = "..\\apache-opennlp\\libs\\";

//Copy libs to Solution Folder 
string libDirSolution = Path.Combine("..\\..\\.." , libDir);
if (Directory.Exists(libDirSolution))
    Directory.Delete(libDirSolution, true);
Directory.CreateDirectory(libDirSolution);

foreach (var file in Directory.GetFiles(libOutPath))
{
    var dest = Path.Combine(libDirSolution, Path.GetFileName(file));
    File.Copy(file, dest, overwrite: true);

    //Ensure that all class version is 52 (Java 8)
    ushort? clsVersion = JarClassVersionReader.GetClassVersion(dest);
    Console.WriteLine(Path.GetFileName(dest) + " -> Class Version: " + clsVersion);
    
    
    //Workaround => Delete all module-info.class entries
    using ZipArchive archive = ZipFile.Open(dest, ZipArchiveMode.Update);
    var entries = archive.Entries.Where(e => e.Name.Equals("module-info.class")).ToArray();
    foreach (var zipArchiveEntry in entries)
    {
        zipArchiveEntry.Delete();
    }
}

//Output the targets
foreach (var ikvmTask in cache.Values)
{
    Console.WriteLine(ikvmTask.GetTarget(libDir));
}

int ExecuteCommand(string command)
{
    command = command.Trim();
    var space = command.IndexOf(' ');
    var process = Process.Start(command[..space], command[(space + 1)..]);
    process.WaitForExit();
    return process.ExitCode;
}