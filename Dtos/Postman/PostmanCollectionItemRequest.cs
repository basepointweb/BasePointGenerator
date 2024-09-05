using System.Collections.Generic;

namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItemRequest
    {
        public string Method { get; set; }
        public IList<string> Header { get; set; }
        public PostmanCollectionItemRequestBody Body { get; set; }
        public PostmanCollectionItemRequestUrl Url { get; set; }
    }
}