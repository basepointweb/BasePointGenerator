namespace SharedProject
{
    public class BasePointProperty
    {
        public string Name { get; protected set; }
        public BasePointType Type { get; protected set; }

        public BasePointProperty(string name, BasePointType type)
        {
            Name = name;
            Type = type;
        }
    }
}