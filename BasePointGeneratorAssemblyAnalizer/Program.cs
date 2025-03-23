using Newtonsoft.Json;
using SharedProject;

var assemblyPath = args[0];
var typeToAnalize = args[1];

var assembly = AssemblyLoader.LoadFrom(assemblyPath);

var type = assembly.GetType(typeToAnalize);

var typeJson = JsonConvert.SerializeObject(
    type.ToBasePointType(ignoredProperties: ["PersistedValues", "State", "Observers"]),
     new JsonSerializerSettings
     {
         ReferenceLoopHandling = ReferenceLoopHandling.Ignore
     }
    );

Console.WriteLine(typeJson);