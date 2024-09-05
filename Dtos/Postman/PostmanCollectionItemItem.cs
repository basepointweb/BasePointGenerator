using System.Collections.Generic;

namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItemItem
    {
        public string Name { get; set; }
        public IList<PostmanCollectionItemEvent> Event { get; set; }
        public PostmanCollectionItemRequest Request { get; set; }
        public IList<PostmanCollectionItemResponse> Response { get; set; }
    }
}