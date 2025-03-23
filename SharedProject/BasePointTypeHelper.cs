using Newtonsoft.Json;
using System.Diagnostics;

namespace SharedProject
{
    public static class BasePointTypeHelper
    {
        public static BasePointType GetBasePointTypeFromAssembly(string assemblyPath, string typeFullQualifiedName)
        {
            var json = CallBasePointGeneratorAssemblyAnalizer(assemblyPath, typeFullQualifiedName);

            return JsonConvert.DeserializeObject<BasePointType>(json);
        }

        private static string CallBasePointGeneratorAssemblyAnalizer(string assemblyPath, string typeFullQualifiedName)
        {
            string userAppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string typeAnalizerToolPath = System.IO.Path.Combine(userAppDataLocal, "BasePointGenerator", "BasePointGeneratorAssemblyAnalizer.exe");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = typeAnalizerToolPath,
                Arguments = $"{assemblyPath} {typeFullQualifiedName}",
                RedirectStandardOutput = true, // Capturar saída
                UseShellExecute = false,
                CreateNoWindow = true // Rodar sem abrir janela
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
        }
    }
}
