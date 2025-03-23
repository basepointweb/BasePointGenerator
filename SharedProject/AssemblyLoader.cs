using System.Reflection;

namespace SharedProject
{
    public static class AssemblyLoader
    {
        public static Assembly LoadFrom(string assemblyPath)
        {
            return Assembly.LoadFrom(assemblyPath);
        }
    }
}
