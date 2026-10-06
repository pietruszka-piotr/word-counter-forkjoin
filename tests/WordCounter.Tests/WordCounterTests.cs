using wspolbiezne_3;
using Xunit;

namespace WordCounting.Tests;

public sealed class WordCounterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "word-counter-tests", Guid.NewGuid().ToString("N"));

    public WordCounterTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void AWordSpanningTheReadBufferIsCountedOnce()
    {
        string path = WriteFile("long.txt", new string('a', 1024 * 1024 + 1));
        Assert.Equal(1, WordCounter.CountWordsStream(path));
    }

    [Theory]
    [InlineData(1024 * 1024 - 1)]
    [InlineData(1024 * 1024)]
    public void ASeparatorAtEitherSideOfTheBufferBoundaryStartsANewWord(int firstWordLength)
    {
        string path = WriteFile("boundary.txt", new string('a', firstWordLength) + "\tsecond");
        Assert.Equal(2, WordCounter.CountWordsStream(path));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData(" \r\n\t", 0)]
    [InlineData("hello,world", 1)]
    [InlineData("one\u00a0two\r\nthree", 3)]
    public void CountsWhitespaceSeparatedWords(string content, long expected)
    {
        Assert.Equal(expected, WordCounter.CountWordsStream(WriteFile("input.txt", content)));
    }

    [Fact]
    public void OutputFilesAreExcludedWithoutExcludingASimilarlyNamedInputDirectory()
    {
        string source = WriteFile("source.txt", "input");
        string output = Path.Combine(_directory, "results");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "log.txt"), "previous generated report");
        string otherInput = WriteFile("results-archive/input.txt", "keep this file");

        string[] files = WordCounter.EnumerateInputFiles(_directory, output).ToArray();

        Assert.Equal(2, files.Length);
        Assert.Contains(source, files);
        Assert.Contains(otherInput, files);
    }

    [Fact]
    public void OutputOutsideInputDoesNotExcludeInputFiles()
    {
        string nestedInput = Path.Combine(_directory, "input");
        Directory.CreateDirectory(nestedInput);
        File.WriteAllText(Path.Combine(nestedInput, "source.txt"), "input");
        Assert.Single(WordCounter.EnumerateInputFiles(nestedInput, _directory));
    }

    [Fact]
    public void InputAndOutputCannotBeTheSameDirectory()
    {
        Assert.Throws<ArgumentException>(() => WordCounter.EnumerateInputFiles(_directory, _directory));
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.GetFullPath(Path.Combine(_directory, name));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
