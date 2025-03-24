namespace BasePointGenerator.Dtos
{
    public class MethodInfo
    {
        public string Type { get; }
        public string Name { get; }

        public bool DeclaredInBaseEntity { get; set; }

        public MethodInfo(string type, string name, bool declaredInBaseEntity)
        {
            Type = type;
            Name = name;
            DeclaredInBaseEntity = declaredInBaseEntity;
        }
    }
}