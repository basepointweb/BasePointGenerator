namespace SharedProject
{
    public class BasePointMethod
    {
        public BasePointMethod(string name, bool isAcessor, bool declaredInBaseEntity, BasePointType returnType)
        {
            this.Name = name;
            this.IsAcessor = isAcessor;
            this.DeclaredInBaseEntity = DeclaredInBaseEntity;
            this.ReturnType = returnType;
        }

        public string Name { get; set; }
        public bool IsAcessor { get; set; }
        public bool DeclaredInBaseEntity { get; set; }
        public BasePointType ReturnType { get; set; }
    }
}