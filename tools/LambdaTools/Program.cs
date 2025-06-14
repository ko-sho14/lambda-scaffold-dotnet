using System.CommandLine;
using System.Diagnostics;

var nameOption = new Option<string>(
    aliases: new[] { "--name", "-n" },
    description: "The name of the new Lambda function (e.g., MyNewBatch).")
{ IsRequired = true };

var typeOption = new Option<string>(
    aliases: new[] { "--type", "-t" },
    description: "The type of project template to generate ('simple' or 'ddd').",
    getDefaultValue: () => "simple");

typeOption.AddValidator(result =>
{
    var type = result.GetValueForOption(typeOption);
    if (type != "simple" && type != "ddd")
    {
        result.ErrorMessage = "Type must be either 'simple' or 'ddd'.";
    }
});

var rootCommand = new RootCommand("A tool to create and configure a new Lambda project.");
rootCommand.AddOption(nameOption);
rootCommand.AddOption(typeOption);

rootCommand.SetHandler((name, type) =>
{
    Console.WriteLine($"🚀 Starting creation of '{name}' with '{type}' template...");
    try
    {
        var generator = new ProjectGenerator(name);
        if (type == "simple")
        {
            generator.CreateSimple();
        }
        else if (type == "ddd")
        {
            generator.CreateDdd();
        }
        Console.WriteLine($"✅ Successfully created Lambda project '{name}'.");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ An error occurred: {ex.Message}");
        Console.ResetColor();
    }
}, nameOption, typeOption);

return await rootCommand.InvokeAsync(args);


/// <summary>
/// プロジェクト生成のロジックをまとめたクラス
/// </summary>
public class ProjectGenerator
{
    private readonly string _name;
    private readonly string _repoRoot;
    private readonly string _slnPath;
    private readonly string _functionRoot;

    public ProjectGenerator(string name)
    {
        _name = name;
        _repoRoot = GetRepositoryRoot();

        // --- ▼ ソリューションファイルを自動検出するロジック ▼ ---
        var slnFiles = Directory.GetFiles(_repoRoot, "*.sln");
        if (slnFiles.Length == 0)
        {
            throw new FileNotFoundException("No .sln file found in the repository root. Please create a solution file first.");
        }
        if (slnFiles.Length > 1)
        {
            throw new InvalidOperationException("Multiple .sln files found in the repository root. The tool cannot determine which one to use.");
        }
        _slnPath = slnFiles[0]; // 見つかった唯一の .sln ファイルを使用
        Console.WriteLine($"-> Targeting solution file: {Path.GetFileName(_slnPath)}");
        // --- ▲ ここまでが修正箇所 ▲ ---

        _functionRoot = Path.Combine(_repoRoot, "functions", _name);
    }

    // ... (CreateSimple, CreateDdd, RunProcess, GetRepositoryRoot, MoveAndFlattenDirectory の各メソッドは変更なし) ...
    public void CreateSimple()
    {
        var projectName = $"{_name}.Lambda";
        var testProjectName = $"{projectName}.Tests";

        Console.WriteLine($"-> Generating template for '{projectName}'...");
        RunProcess("dotnet", $"new lambda.EmptyFunction -n {projectName} -o {_functionRoot}");

        Console.WriteLine("-> Flattening directory structure...");

        var nestedSrcDir = Path.Combine(_functionRoot, "src", projectName);
        var finalSrcDir = Path.Combine(_functionRoot, "src");
        MoveAndFlattenDirectory(nestedSrcDir, finalSrcDir);

        var nestedTestDir = Path.Combine(_functionRoot, "test", testProjectName);
        var finalTestDir = Path.Combine(_functionRoot, "test");
        MoveAndFlattenDirectory(nestedTestDir, finalTestDir);

        Console.WriteLine("-> Adding projects to solution...");
        var finalSrcProj = Path.Combine(finalSrcDir, $"{projectName}.csproj");
        var finalTestProj = Path.Combine(finalTestDir, $"{testProjectName}.csproj");
        RunProcess("dotnet", $"sln \"{_slnPath}\" add \"{finalSrcProj}\"");
        RunProcess("dotnet", $"sln \"{_slnPath}\" add \"{finalTestProj}\"");
    }

    public void CreateDdd()
    {
        var appPath = Path.Combine(_functionRoot, "src", $"{_name}.Application");
        var domainPath = Path.Combine(_functionRoot, "src", $"{_name}.Domain");
        var infraPath = Path.Combine(_functionRoot, "src", $"{_name}.Infrastructure");
        var appTestPath = Path.Combine(_functionRoot, "test", $"{_name}.Application.Tests");
        var domainTestPath = Path.Combine(_functionRoot, "test", $"{_name}.Domain.Tests");

        var appProj = Path.Combine(appPath, $"{_name}.Application.csproj");
        var domainProj = Path.Combine(domainPath, $"{_name}.Domain.csproj");
        var infraProj = Path.Combine(infraPath, $"{_name}.Infrastructure.csproj");
        var appTestProj = Path.Combine(appTestPath, $"{_name}.Application.Tests.csproj");
        var domainTestProj = Path.Combine(domainTestPath, $"{_name}.Domain.Tests.csproj");

        Console.WriteLine("-> Generating DDD project structure...");
        RunProcess("dotnet", $"new classlib -n {_name}.Domain -o {domainPath}");
        RunProcess("dotnet", $"new classlib -n {_name}.Infrastructure -o {infraPath}");
        string appTempDir = Path.Combine(Path.GetTempPath(), $"lambda-gen-{Guid.NewGuid()}");
        RunProcess("dotnet", $"new lambda.EmptyFunction -n {_name}.Application -o {appTempDir}");
        Directory.Move(Path.Combine(appTempDir, "src", $"{_name}.Application"), appPath);
        Directory.Delete(appTempDir, true);

        RunProcess("dotnet", $"new xunit -n {_name}.Domain.Tests -o {domainTestPath}");
        RunProcess("dotnet", $"new xunit -n {_name}.Application.Tests -o {appTestPath}");

        Console.WriteLine("-> Setting up project references...");
        RunProcess("dotnet", $"add \"{appProj}\" reference \"{domainProj}\"");
        RunProcess("dotnet", $"add \"{appProj}\" reference \"{infraProj}\"");
        RunProcess("dotnet", $"add \"{infraProj}\" reference \"{domainProj}\"");
        RunProcess("dotnet", $"add \"{domainTestProj}\" reference \"{domainProj}\"");
        RunProcess("dotnet", $"add \"{appTestProj}\" reference \"{appProj}\"");

        Console.WriteLine("-> Adding all projects to solution...");
        var allProjects = new[] { appProj, domainProj, infraProj, appTestProj, domainTestProj };
        foreach (var proj in allProjects)
        {
            RunProcess("dotnet", $"sln \"{_slnPath}\" add \"{proj}\"");
        }
    }

    private void MoveAndFlattenDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Move(file, Path.Combine(destinationDir, Path.GetFileName(file)));
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            Directory.Move(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
        }
        Directory.Delete(sourceDir, true);
    }

    private void RunProcess(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _repoRoot
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command `{command} {args}` failed with exit code {process.ExitCode}.\nOutput: {output}\nError: {error}");
        }
        Console.Write(output);
    }

    private string GetRepositoryRoot()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
        {
            currentDir = currentDir.Parent;
        }
        return currentDir?.FullName ?? throw new Exception("Could not find the repository root. Make sure you are running this within a git repository.");
    }
}
