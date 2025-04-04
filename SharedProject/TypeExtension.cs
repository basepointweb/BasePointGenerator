using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedProject
{
    public static class TypeExtension
    {
        private static readonly HashSet<Type> _primitiveTypes = new()
        {
            typeof(string), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid),
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(void)
        };

        public static bool IsPrimitive(this Type type)
        {
            return _primitiveTypes.Contains(type) || type.IsEnum;
        }

        public static List<Type> GetAncestors(this Type type)
        {
            var ancestors = new List<Type>();

            while (type.BaseType != null)
            {
                ancestors.Add(type.BaseType);
                type = type.BaseType;
            }

            return ancestors;
        }

        public static bool IsSubClassOfBaseEntity(this Type type)
        {
            var ancestors = type.GetAncestors();

            return ancestors.Any(t => t.Name == "BaseEntity");
        }


        public static bool IsListProperty(this Type type)
        {
            return type.Name.Contains("EntityList") || type.Name.Contains("List") || type.Name.Contains("Enumerable") || type.Name.Contains("Collection");
        }

        public static Type GetUnderlyingType(this Type type)
        {
            var resultType = type;

            if (type.IsListProperty())
            {
                resultType = type.GenericTypeArguments[0];

                var underlyingType = Nullable.GetUnderlyingType(resultType);

                resultType = underlyingType ?? resultType;
            }
            else
            {
                var underlyingType = Nullable.GetUnderlyingType(type);

                resultType = underlyingType ?? type;
            }

            return resultType;
        }

        public static string GetFriendlyTypeName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            var typeName = type.Name;
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
                typeName = typeName.Remove(backtickIndex);

            var genericArgs = type.GetGenericArguments()
                                  .Select(t => GetFriendlyTypeName(t))
                                  .ToArray();

            return $"{typeName}<{string.Join(", ", genericArgs)}>";
        }

        public static string GetFormattedType(this Type type)
        {
            var typeStr = GetFormattedBasicType(type.Name);

            if (type.Name.Contains("EntityList") || type.Name.Contains("List") || type.Name.Contains("Enumerable") || type.Name.Contains("Collection"))
            {
                typeStr = type.GetFriendlyTypeName();
            }
            else
            {
                var underlyingType = Nullable.GetUnderlyingType(type);

                if (underlyingType is not null)
                    typeStr = $"Nullable<{GetFormattedBasicType(underlyingType.Name)}>";
            }

            return typeStr;
        }

        public static string GetFormattedBasicType(string typeName)
        {
            var basicTypeName = typeName;

            switch (typeName)
            {
                case "String":
                    basicTypeName = "string";
                    break;
                case "Decimal":
                    basicTypeName = "decimal";
                    break;
                case "Int32":
                    basicTypeName = "int";
                    break;
                default:
                    break;
            }

            return basicTypeName;
        }

        public static BasePointType ToBasePointType(
            this Type type,
            Dictionary<string, List<BasePointType>>? basePointTypeInstances = null,
            HashSet<string>? ignoredProperties = null)
        {
            ignoredProperties ??= new HashSet<string>();

            basePointTypeInstances ??= new Dictionary<string, List<BasePointType>>();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !ignoredProperties.Contains(p.Name));

            var ancestors = type.GetAncestors();

            var basePointType = new BasePointType { Name = type.GetFormattedType(), UnderlyingType = type.GetUnderlyingType().Name, UnderlyingTypeIsSubClassOfBaseEntity = type.GetUnderlyingType().IsSubClassOfBaseEntity(), Namespace = type.Namespace, IsPrimitive = type.IsPrimitive(), IsSubClassOfBaseEntity = type.IsSubClassOfBaseEntity() };

            foreach (var property in properties)
            {
                var propType = property.PropertyType;

                var propertytypeAncestors = propType.GetAncestors();

                var propertyType = new BasePointType { Name = propType.GetFormattedType(), UnderlyingType = propType.GetUnderlyingType().Name, UnderlyingTypeIsSubClassOfBaseEntity = propType.GetUnderlyingType().IsSubClassOfBaseEntity(), Namespace = propType.Namespace, IsPrimitive = propType.IsPrimitive(), IsSubClassOfBaseEntity = propType.IsSubClassOfBaseEntity() };

                basePointTypeInstances.TryGetValue(type.GetUnderlyingType().Name, out var instances);

                if (!propType.IsPrimitive())
                {
                    if (instances is null)
                    {
                        instances = [propertyType];

                        basePointTypeInstances.Add(type.GetUnderlyingType().Name, instances);

                        propertyType = propType.ToBasePointType(basePointTypeInstances, ignoredProperties);
                    }
                }

                basePointType.AddPropertyIfNotExists(new BasePointProperty(property.Name, propertyType, property.DeclaringType.GetUnderlyingType().Name == "BaseEntity"));

                if (instances is not null)
                {
                    SincronizeAllInstances(basePointType, instances);
                }
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
               .Where(m => !ignoredProperties.Contains(m.Name.Substring(4)));

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;

                var methodTypeAncestors = returnType.GetAncestors();

                var methodReturnType = new BasePointType { Name = returnType.GetUnderlyingType().Name, UnderlyingType = returnType.GetUnderlyingType().Name, UnderlyingTypeIsSubClassOfBaseEntity = returnType.GetUnderlyingType().IsSubClassOfBaseEntity(), Namespace = returnType.Namespace, IsPrimitive = returnType.IsPrimitive(), IsSubClassOfBaseEntity = returnType.IsSubClassOfBaseEntity() };

                basePointTypeInstances.TryGetValue(type.GetUnderlyingType().Name, out var instances);

                if (!returnType.IsPrimitive())
                {
                    if (instances is null)
                    {
                        instances = [methodReturnType];

                        basePointTypeInstances.Add(type.GetUnderlyingType().Name, instances);

                        methodReturnType = returnType.ToBasePointType(basePointTypeInstances, ignoredProperties);
                    }
                }

                basePointType.AddMethodIfNotExists(new BasePointMethod(method.Name, method.IsSpecialName, method.DeclaringType.GetUnderlyingType().Name == "BaseEntity", methodReturnType));

                if (instances is not null)
                {
                    SincronizeAllInstances(basePointType, instances);
                }
            }

            SincronizePropertiyTypesWithMethodTypes(basePointType);

            return basePointType;
        }

        private static void SincronizePropertiyTypesWithMethodTypes(BasePointType basePointType)
        {
            var nonPrimitiveProperties = basePointType.Properties.Where(p => !p.Type.IsPrimitive);

            foreach (var property in nonPrimitiveProperties)
            {
                var methods = basePointType.Methods.Where(m => m.ReturnType.Name == property.Type.Name);

                foreach (var method in methods)
                {
                    method.ReturnType = property.Type;
                }
            }
        }

        private static void SincronizeAllInstances(BasePointType currentType, List<BasePointType> instances)
        {
            var typeName = currentType.Name;

            var instancesWithSameType = instances.Where(t => t.Name == typeName);

            foreach (var instance in instancesWithSameType)
            {
                instance.AddMissingProperties(currentType);

                instance.AddMissingMethods(currentType);

                currentType.AddMissingProperties(instance);

                currentType.AddMissingMethods(instance);
            }
        }
    }
}
