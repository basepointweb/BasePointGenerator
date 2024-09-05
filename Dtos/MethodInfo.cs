namespace BasePointGenerator.Dtos
{
    public class MethodInfo
    {
        public string Type { get; }
        public string Name { get; }

        public MethodInfo(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}