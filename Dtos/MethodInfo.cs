namespace BasePointGenerator.Dtos
{
    public class MethodInfo
    {
        public string Type { get; }
        public string UnderlyingType { get; }
        public string Name { get; }

        public bool DeclaredInBaseEntity { get; set; }

        public MethodInfo(string type, string underlyingType, string name, bool declaredInBaseEntity)
        {
            Type = type;
            UnderlyingType = underlyingType;
            Name = name;
            DeclaredInBaseEntity = declaredInBaseEntity;
        }
    }
}