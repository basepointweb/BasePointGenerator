using System.Collections.Generic;

namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItemRequestUrl
    {
        public string Raw { get; set; }
        public IList<string> Host { get; set; }
        public IList<string> Path { get; set; }
        public IList<PostmanCollectionItemRequestUrlQuery> Query { get; set; }
    }
}