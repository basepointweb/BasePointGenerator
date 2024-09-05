namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItemEvent
    {
        public string Listen { get; set; }
        public PostmanCollectionItemEventScript Script { get; set; }
    }
}
