namespace BasePointGenerator.Dtos
{
    public record FileContentGenerationOptions
    {
        public bool OnlyProcessFilePaths { get; set; }
        public bool GenerateCreateUseCase { get; set; }
        public bool GenerateUpdateUseCase { get; set; }
        public bool GenerateDeleteUseCase { get; set; }
        public bool GenerateGetUseCase { get; set; }
    }
}