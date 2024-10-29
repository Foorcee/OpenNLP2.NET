record IKVMTask(string JarFile, string Version, List<IKVMTask> Dependencies)
{
    public string DllFile => Path.ChangeExtension(Path.GetFileName(JarFile), ".dll");
    
    public IEnumerable<string> DllReferences() => Dependencies.Select(dep => dep.DllFile);
    
    public IEnumerable<IKVMTask> GetAllDependencies() {
        if (!Dependencies.Any())
        {
            return Array.Empty<IKVMTask>();
        }

        return Dependencies.SelectMany(task => new[] { task }.Concat(task.GetAllDependencies()));
    }

    public string GetTarget(string directory)
    {
        var depsJars = Dependencies
            .Select(dep => Path.Combine(directory, dep.JarFile)).ToArray();
        var refs = string.Join(";", depsJars);
        var refTarget = depsJars.Any() ? $"\n     <References>{refs}</References>" : string.Empty;
        
        return $"""
                <IkvmReference Include="{Path.Combine(directory, JarFile)}">
                     <AssemblyName>{Path.GetFileNameWithoutExtension(JarFile)}</AssemblyName>
                     <AssemblyVersion>{Version}</AssemblyVersion>
                     <AssemblyFileVersion>{Version}</AssemblyFileVersion>{refTarget}
                </IkvmReference>
                """;
    }
}