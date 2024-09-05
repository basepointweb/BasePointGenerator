namespace BasePointGenerator.Dtos.Postman
{
    public record PostmanCollectionItemRequestBody
    {
        public string Mode { get; set; }
        public string Raw { get; set; }
        public PostmanCollectionItemRequestBodyOptions Options { get; set; }
    }
}