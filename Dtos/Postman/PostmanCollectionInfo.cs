namespace BasePointGenerator.Dtos
{
    public record PostmanCollectionInfo
    {
        public Guid _postman_id { get; set; }
        public string Name { get; set; }
        public string Schema { get; set; }
        public string _exporter_id { get; set; }
    }
}