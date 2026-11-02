using System.Reflection;
var pig = Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages", "pdfpig"), "UglyToad.PdfPig.dll", SearchOption.AllDirectories)
    .First(f => f.Contains("net8") || f.Contains("net6") || f.Contains("netstandard2"));
var asm = Assembly.LoadFrom(pig);
var tl = asm.GetTypes().First(t => t.Name == "TextLine");
foreach (var p in tl.GetProperties()) Console.WriteLine($"prop {p.PropertyType.Name} {p.Name}");
foreach (var m in tl.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.Name.StartsWith("get_")))
    Console.WriteLine($"m {m.ReturnType.Name} {m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})");
