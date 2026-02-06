using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Services;
using Xunit;

namespace ModernizationPlatform.Worker.UnitTests.Analysis;

public class AnalysisOutputParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsOutput()
    {
        var parser = new AnalysisOutputParser();
        var json = "{\"findings\":[{\"severity\":\"High\",\"category\":\"Security\",\"title\":\"Issue\",\"description\":\"Desc\",\"filePath\":\"src/app.cs\"}],\"metadata\":{\"analysisType\":\"Security\",\"totalFindings\":1,\"executionDurationMs\":100}}";

        var output = parser.Parse(json);

        Assert.Single(output.Findings);
        Assert.Equal("Security", output.Metadata.AnalysisType);
    }

    [Fact]
    public void Parse_CodeFenceJson_ReturnsOutput()
    {
        var parser = new AnalysisOutputParser();
        var content = "```json\n{\"findings\":[],\"metadata\":{\"analysisType\":\"Obsolescence\",\"totalFindings\":0,\"executionDurationMs\":50}}\n```";

        var output = parser.Parse(content);

        Assert.Empty(output.Findings);
        Assert.Equal("Obsolescence", output.Metadata.AnalysisType);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsParsingException()
    {
        var parser = new AnalysisOutputParser();
        var content = "not-json";

        Assert.Throws<AnalysisOutputParsingException>(() => parser.Parse(content));
    }
}
