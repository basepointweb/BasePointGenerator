using System.Collections.Generic;

namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItem
    {
        public string Name { get; set; }
        public IList<PostmanCollectionItemItem> Item { get; set; }
    }
}
