namespace SharedProject
{
    public class BasePointMethod
    {
        public BasePointMethod(string name, bool isAcessor, BasePointType returnType)
        {
            this.Name = name;
            this.IsAcessor = isAcessor;
            this.ReturnType = returnType;
        }

        public string Name { get; set; }
        public bool IsAcessor { get; set; }
        public BasePointType ReturnType { get; set; }
    }
}