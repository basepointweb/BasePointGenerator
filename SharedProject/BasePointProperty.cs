namespace SharedProject
{
    public class BasePointProperty
    {
        public string Name { get; protected set; }
        public BasePointType Type { get; protected set; }
        public bool DeclaredInBaseEntity { get; set; }

        public BasePointProperty(string name, BasePointType type, bool declaredInBaseEntity)
        {
            Name = name;
            Type = type;
            DeclaredInBaseEntity = declaredInBaseEntity;
        }
    }
}