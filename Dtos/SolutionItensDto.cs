namespace BasePointGenerator.Dtos
{
    public record SolutionItensDto
    {
        public Solution Solution { get; set; }
        public SolutionItem CoreProject { get; set; }
        public SolutionItem CoreTestsProject { get; set; }
        public SolutionItem DapperProject { get; set; }
        public SolutionItem AspNetPresentationProject { get; set; }
    }
}