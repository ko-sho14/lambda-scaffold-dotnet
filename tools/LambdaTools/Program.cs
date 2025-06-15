using System.CommandLine;
using System.Diagnostics;

var nameOption = new Option<string>(
    aliases: new[] { "--name", "-n" },
    description: "The name of the new Lambda function (e.g., MyNewBatch).")
{ IsRequired = true };

// 'function' サブコマンドの定義
var functionCommand = new Command("function", "Create a new Lambda function project.");
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
functionCommand.AddOption(nameOption);
functionCommand.AddOption(typeOption);

// ルートコマンドの定義とサブコマンドの登録
var rootCommand = new RootCommand("A scaffolding tool for this repository.");
rootCommand.Name = "forge";
rootCommand.AddCommand(functionCommand);

functionCommand.SetHandler((name, type) =>
{
    Console.WriteLine($"🚀 Starting to forge '{name}' with '{type}' function template...");
    var generator = new ProjectGenerator(name);
    try
    {
        if (type == "simple") { generator.CreateSimpleFunction(); }
        else if (type == "ddd") { generator.CreateDddFunction(); }
        Console.WriteLine($"✅ Successfully forged function '{name}'.");
    }
    catch (Exception ex) { HandleError(ex); }
}, nameOption, typeOption);


// エラーハンドリング用の共通メソッド
void HandleError(Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ An error occurred: {ex.Message}");
    Console.ResetColor();
}

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
        var slnFiles = Directory.GetFiles(_repoRoot, "*.sln");
        if (slnFiles.Length == 0) throw new FileNotFoundException("No .sln file found in the repository root.");
        if (slnFiles.Length > 1) throw new InvalidOperationException("Multiple .sln files found in the repository root.");
        _slnPath = slnFiles[0];
        Console.WriteLine($"-> Targeting solution file: {Path.GetFileName(_slnPath)}");
        _functionRoot = Path.Combine(_repoRoot, "functions", name);
    }

    public void CreateSimpleFunction()
    {
        var projectName = $"{_name}.Lambda";
        var testProjectName = $"{projectName}.Tests";

        Console.WriteLine($"-> Generating base structure for '{projectName}'...");
        RunProcess("dotnet", $"new lambda.EmptyFunction -n {projectName} -o {_functionRoot}");

        Console.WriteLine("-> Flattening directory structure...");
        var nestedSrcDir = Path.Combine(_functionRoot, "src", projectName);
        var finalSrcDir = Path.Combine(_functionRoot, "src");
        CopyAndFlattenDirectory(nestedSrcDir, finalSrcDir);

        var nestedTestDir = Path.Combine(_functionRoot, "test", testProjectName);
        var finalTestDir = Path.Combine(_functionRoot, "test");
        CopyAndFlattenDirectory(nestedTestDir, finalTestDir);

        Console.WriteLine("-> Adding projects to solution...");
        var finalSrcProj = Path.Combine(finalSrcDir, $"{projectName}.csproj");
        var finalTestProj = Path.Combine(finalTestDir, $"{testProjectName}.csproj");
        RunProcess("dotnet", $"sln \"{_slnPath}\" add \"{finalSrcProj}\" \"{finalTestProj}\"");
    }

    public void CreateDddFunction()
    {
        var appName = $"{_name}.Application";
        var domainName = $"{_name}.Domain";
        var infraName = $"{_name}.Infrastructure";
        var appTestName = $"{appName}.Tests";
        var domainTestName = $"{domainName}.Tests";

        // 1. まず、Application層を含む基本骨格を `lambda.EmptyFunction` で最終的な場所に直接生成する
        Console.WriteLine($"-> Generating base structure with '{appName}'...");
        RunProcess("dotnet", $"new lambda.EmptyFunction -n {appName} -o {_functionRoot}");
        // この時点で以下のディレクトリが作成される:
        // functions/{_name}/src/{_name}.Application/
        // functions/{_name}/test/{_name}.Application.Tests/

        // 2. Domain層とInfrastructure層を、既存のsrcディレクトリ内に追加で生成する
        Console.WriteLine("-> Generating Domain and Infrastructure layers...");
        var domainPath = Path.Combine(_functionRoot, "src", domainName);
        var infraPath = Path.Combine(_functionRoot, "src", infraName);
        RunProcess("dotnet", $"new classlib -n {domainName} -o {domainPath}");
        RunProcess("dotnet", $"new classlib -n {infraName} -o {infraPath}");

        // 3. Domain層のテストプロジェクトを追加で生成する
        Console.WriteLine("-> Generating Domain tests...");
        var domainTestPath = Path.Combine(_functionRoot, "test", domainTestName);
        RunProcess("dotnet", $"new xunit -n {domainTestName} -o {domainTestPath}");

        // 4. パスを定義し、参照設定とソリューション追加を行う
        var appPath = Path.Combine(_functionRoot, "src", appName);
        var appProj = Path.Combine(appPath, $"{appName}.csproj");
        var domainProj = Path.Combine(domainPath, $"{domainName}.csproj");
        var infraProj = Path.Combine(infraPath, $"{infraName}.csproj");
        var appTestProj = Path.Combine(_functionRoot, "test", appTestName, $"{appTestName}.csproj");
        var domainTestProj = Path.Combine(domainTestPath, $"{domainTestName}.csproj");

        Console.WriteLine("-> Setting up project references...");
        RunProcess("dotnet", $"add \"{appProj}\" reference \"{domainProj}\" \"{infraProj}\"");
        RunProcess("dotnet", $"add \"{infraProj}\" reference \"{domainProj}\"");
        RunProcess("dotnet", $"add \"{domainTestProj}\" reference \"{domainProj}\"");
        RunProcess("dotnet", $"add \"{appTestProj}\" reference \"{appProj}\"");

        Console.WriteLine("-> Adding all projects to solution...");
        var allProjects = new[] { appProj, domainProj, infraProj, appTestProj, domainTestProj };
        RunProcess("dotnet", $"sln \"{_slnPath}\" add {string.Join(" ", allProjects.Select(p => $"\"{p}\""))}");
    }

    private void CopyAndFlattenDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        Directory.Delete(sourceDir, true);
    }

    private void RunProcess(string command, string args)
    {
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = "dotnet",
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
