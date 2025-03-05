using BasePointGenerator.Dtos;
using BasePointGenerator.Dtos.Postman;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BasePointGenerator
{
    public static class PostmanCollectionFactory
    {
        public static string Create(
            string fileContent,
            string filePath,
            IList<PropertyInfo> classProperties,
            IList<MethodInfo> methods,
            FileContentGenerationOptions options)
        {
            if (!classProperties.Any())
                throw new ValidationException("It wasn't identified public properties to generate builder class");

            var originalClassName = GetOriginalClassName(fileContent);

            return CreatePostmanCollection(fileContent, originalClassName, classProperties, methods, filePath, options);
        }

        public static string GetCollectionFileName()
        {
            var collectionName = GetNameRootProjectName() + ".postman_collection.json";

            return collectionName;
        }

        private static string CreatePostmanCollection(string fileContent, string originalClassName, IList<PropertyInfo> properties, IList<MethodInfo> methods, string filePath, FileContentGenerationOptions options)
        {
            var collectionName = GetCollectionFileName();

            Directory.CreateDirectory(filePath);

            var collectionFile = Path.Combine(filePath, string.Concat(collectionName));

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var postmanCollection = new PostmanCollection()
            {
                Item = new List<PostmanCollectionItem>(),
                Info = new PostmanCollectionInfo()
                {
                    _postman_id = Guid.NewGuid(),
                    Name = GetNameRootProjectName(),
                    Schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
                    _exporter_id = "11707669"
                }
            };

            if (File.Exists(collectionFile))
            {
                var collectionFileContent = File.ReadAllText(collectionFile);

                postmanCollection = JsonConvert.DeserializeObject<PostmanCollection>(collectionFileContent);
            }
            else
            {
                var postmanCollectionJsonEmpty = JsonConvert.SerializeObject(postmanCollection, serializerSettings);

                File.WriteAllText(collectionFile, postmanCollectionJsonEmpty);
            }

            PostmanCollectionItem collectionItem =
                postmanCollection.Item.FirstOrDefault(p => p.Name == originalClassName.ToPlural());

            if (collectionItem is null)
            {
                collectionItem = new PostmanCollectionItem()
                {
                    Name = originalClassName.ToPlural(),
                    Item = new List<PostmanCollectionItemItem>()
                };

                postmanCollection.Item.Add(collectionItem);
            }

            if (options.GenerateCreateUseCase)
            {
                PostmanCollectionItemItem item =
                    collectionItem.Item.FirstOrDefault(p => p.Name == $"Create {originalClassName}");

                if (item is null)
                {
                    collectionItem.Item.Add(
                        new PostmanCollectionItemItem()
                        {
                            Name = $"Create {originalClassName}",
                            Event = new List<PostmanCollectionItemEvent>()
                            {
                            new PostmanCollectionItemEvent()
                            {
                                Listen = "test",
                                Script = new PostmanCollectionItemEventScript()
                                {
                                    Exec  = new List<string>()
                                    {
                                        "if (pm.response.to.have.status(201)){\r",
                                        "    var jsonData = JSON.parse(responseBody);\r",
                                        $"    pm.environment.set(\"{originalClassName.GetWordWithFirstLetterDown()}Id\", jsonData.id);   \r",
                                        "}"
                                    },
                                    Type = "text/javascript",
                                    Packages = new PostmanCollectionItemEventScriptPackage()
                                }
                            }
                            },
                            Request = new PostmanCollectionItemRequest()
                            {
                                Method = "POST",
                                Header = new List<string>(),
                                Body = new PostmanCollectionItemRequestBody()
                                {
                                    Mode = "raw",
                                    Raw = GetPropertiesJson(properties),
                                    Options = new PostmanCollectionItemRequestBodyOptions()
                                    {
                                        Raw = new PostmanCollectionItemRequestBodyOptionsRaw()
                                        {
                                            Language = "json"
                                        }
                                    }
                                },
                                Url = new PostmanCollectionItemRequestUrl()
                                {
                                    Raw = $"{{apiUrlPrefix}}/api/{originalClassName.GetWordWithFirstLetterDown()}",
                                    Host = new List<string>() { "{{apiUrlPrefix}}" },
                                    Path = new List<string>() { "api", $"{originalClassName.ToPlural().GetWordWithFirstLetterDown()}" }
                                }
                            },
                            Response = new List<PostmanCollectionItemResponse>()
                        });
                }
                else
                {
                    item.Request.Body.Raw = GetPropertiesJson(properties);
                }
            };

            if (options.GenerateUpdateUseCase)
            {
                PostmanCollectionItemItem item =
                    collectionItem.Item.FirstOrDefault(p => p.Name == $"Update {originalClassName}");

                if (item is null)
                {
                    collectionItem.Item.Add(new PostmanCollectionItemItem()
                    {
                        Name = $"Update {originalClassName}",
                        Event = new List<PostmanCollectionItemEvent>()
                            {
                                new PostmanCollectionItemEvent()
                                {
                                    Listen = "test",
                                    Script = new PostmanCollectionItemEventScript()
                                    {
                                        Exec  = new List<string>()
                                        {
                                           "if (pm.response.to.have.status(201)){\r",
                                            "    var jsonData = JSON.parse(responseBody);\r",
                                            $"    pm.environment.set(\"{originalClassName.GetWordWithFirstLetterDown()}Id\", jsonData.id);   \r",
                                            "}"
                                        },
                                        Type = "text/javascript",
                                        Packages = new PostmanCollectionItemEventScriptPackage()
                                    }
                                }
                            },
                        Request = new PostmanCollectionItemRequest()
                        {
                            Method = "PUT",
                            Header = new List<string>(),
                            Body = new PostmanCollectionItemRequestBody()
                            {
                                Mode = "raw",
                                Raw = GetPropertiesJson(properties),
                                Options = new PostmanCollectionItemRequestBodyOptions()
                                {
                                    Raw = new PostmanCollectionItemRequestBodyOptionsRaw()
                                    {
                                        Language = "json"
                                    }
                                }
                            },
                            Url = new PostmanCollectionItemRequestUrl()
                            {
                                Raw = $"{{apiUrlPrefix}}/api/{originalClassName.ToPlural().GetWordWithFirstLetterDown()}/{{{originalClassName.GetWordWithFirstLetterDown()}Id}}",
                                Host = new List<string>() { "{{apiUrlPrefix}}" },
                                Path = new List<string>() { "api", $"{originalClassName.ToPlural().GetWordWithFirstLetterDown()}", $"{{{originalClassName.GetWordWithFirstLetterDown()}Id}}" }
                            }
                        },
                        Response = new List<PostmanCollectionItemResponse>()
                    });
                }
                else
                {
                    item.Request.Body.Raw = GetPropertiesJson(properties);
                }
            };

            if (options.GenerateDeleteUseCase)
            {
                PostmanCollectionItemItem item =
                    collectionItem.Item.FirstOrDefault(p => p.Name == $"Delete {originalClassName}");

                if (item is null)
                {
                    collectionItem.Item.Add(new PostmanCollectionItemItem()
                    {
                        Name = $"Delete {originalClassName}",
                        Request = new PostmanCollectionItemRequest()
                        {
                            Method = "DELETE",
                            Header = new List<string>(),
                            Url = new PostmanCollectionItemRequestUrl()
                            {
                                Raw = $"{{apiUrlPrefix}}/api/{originalClassName.ToPlural().GetWordWithFirstLetterDown()}/{{{originalClassName.GetWordWithFirstLetterDown()}Id}}",
                                Host = new List<string>() { "{{apiUrlPrefix}}" },
                                Path = new List<string>() { "api", $"{originalClassName.ToPlural().GetWordWithFirstLetterDown()}", $"{{{originalClassName.GetWordWithFirstLetterDown()}Id}}" }
                            }
                        },
                        Response = new List<PostmanCollectionItemResponse>()
                    });
                }
            };

            if (options.GenerateGetUseCase)
            {
                PostmanCollectionItemItem item =
                    collectionItem.Item.FirstOrDefault(p => p.Name == $"Get {originalClassName}ById");

                if (item is null)
                {
                    collectionItem.Item.Add(new PostmanCollectionItemItem()
                    {
                        Name = $"Get {originalClassName}ById",
                        Request = new PostmanCollectionItemRequest()
                        {
                            Method = "GET",
                            Header = new List<string>(),
                            Url = new PostmanCollectionItemRequestUrl()
                            {
                                Raw = $"{{apiUrlPrefix}}/api/{originalClassName.ToPlural()}/{{{originalClassName.GetWordWithFirstLetterDown()}Id}}",
                                Host = new List<string>() { "{{apiUrlPrefix}}" },
                                Path = new List<string>() { "api", $"{originalClassName.ToPlural().GetWordWithFirstLetterDown()}", $"{{{originalClassName.GetWordWithFirstLetterDown()}Id}}" }
                            }
                        },
                        Response = new List<PostmanCollectionItemResponse>()
                    });
                }

                PostmanCollectionItemItem getItems =
                    collectionItem.Item.FirstOrDefault(p => p.Name == $"Get {originalClassName}s");

                if (getItems is null)
                {
                    collectionItem.Item.Add(new PostmanCollectionItemItem()
                    {
                        Name = $"Get {originalClassName.ToPlural()}",
                        Request = new PostmanCollectionItemRequest()
                        {
                            Method = "POST",
                            Header = new List<string>(),
                            Url = new PostmanCollectionItemRequestUrl()
                            {
                                Raw = $"{{apiUrlPrefix}}/api/{originalClassName.ToPlural()}/get-by-filter?itemsPerPage=10&pageNumber=1&filterValue=Name",
                                Host = new List<string>() { "{{apiUrlPrefix}}" },
                                Path = new List<string>() { "api", $"{originalClassName.GetWordWithFirstLetterDown()}s", $"get-by-filter" },
                                Query = new List<PostmanCollectionItemRequestUrlQuery>()
                                    {
                                        new PostmanCollectionItemRequestUrlQuery()
                                        {
                                            Key = "itemsPerPage",
                                            Value = "10"
                                        },
                                        new PostmanCollectionItemRequestUrlQuery()
                                        {
                                            Key = "pageNumber",
                                            Value = "1"
                                        },
                                        new PostmanCollectionItemRequestUrlQuery()
                                        {
                                            Key = "filterValue",
                                            Value = "Name"
                                        }
                                    }
                            }
                        },
                        Response = new List<PostmanCollectionItemResponse>()
                    });
                }
            };

            var postmanCollectionJson = JsonConvert.SerializeObject(postmanCollection, serializerSettings);

            return postmanCollectionJson;
        }

        private static string GetPropertiesJson(IList<PropertyInfo> properties)
        {
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializerSettings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(properties.ToDictionary(x => x.Name, x => ""), serializerSettings);
        }

        private static string GetNameRootProjectName()
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            return solution.Name.Replace(".sln", "");
        }

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}