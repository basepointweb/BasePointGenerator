namespace BasePointGenerator.Dtos
{
    public class MethodInfo
    {
        public string Type { get; }
        public string UnderlyingType { get; }
        public bool UnderlyingTypeIsSubClassOfBaseEntity { get; }
        public string Name { get; }

        public bool DeclaredInBaseEntity { get; set; }

        public MethodInfo(string type, string underlyingType, bool underlyingTypeIsSubClassOfBaseEntity, string name, bool declaredInBaseEntity)
        {
            Type = type;
            UnderlyingType = underlyingType;
            UnderlyingTypeIsSubClassOfBaseEntity = underlyingTypeIsSubClassOfBaseEntity;
            Name = name;
            DeclaredInBaseEntity = declaredInBaseEntity;
        }
    }
}