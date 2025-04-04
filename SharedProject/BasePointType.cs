using System.Collections.Generic;
using System.Linq;

namespace SharedProject
{
    public class BasePointType
    {
        public string Name { get; set; }
        public string UnderlyingType { get; set; }
        public bool UnderlyingTypeIsSubClassOfBaseEntity { get; set; }
        public string Namespace { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsSubClassOfBaseEntity { get; set; }

        public List<BasePointProperty> Properties { get; set; }
        public List<BasePointMethod> Methods { get; set; }

        public BasePointType()
        {
            Name = string.Empty;
            UnderlyingType = string.Empty;
            UnderlyingTypeIsSubClassOfBaseEntity = false;
            Namespace = string.Empty;
            Properties = new List<BasePointProperty>();
            Methods = new List<BasePointMethod>();
        }

        public BasePointType(BasePointType other)
        {
            Name = other.Name;
            UnderlyingType = other.Name;
            UnderlyingTypeIsSubClassOfBaseEntity = other.UnderlyingTypeIsSubClassOfBaseEntity;
            Namespace = other.Namespace;
            IsPrimitive = other.IsPrimitive;
            IsSubClassOfBaseEntity = other.IsSubClassOfBaseEntity;
            Properties = new List<BasePointProperty>(other.Properties);
            Methods = new List<BasePointMethod>(other.Methods);
        }

        public void AddMissingProperties(BasePointType another)
        {
            foreach (var property in another.Properties)
            {
                AddPropertyIfNotExists(property);
            }
        }

        public void AddPropertyIfNotExists(BasePointProperty property)
        {
            if (!Properties.Any(p => p.Name == property.Name))
            {
                Properties.Add(property);
            }
        }

        public void AddMissingMethods(BasePointType another)
        {
            foreach (var method in another.Methods)
            {
                AddMethodIfNotExists(method);
            }
        }

        public void AddMethodIfNotExists(BasePointMethod property)
        {
            if (!Methods.Any(p => p.Name == property.Name))
            {
                Methods.Add(property);
            }
        }
    }
}