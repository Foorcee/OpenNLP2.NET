using NUnit.Framework;
using OpenNLP2.Build;
using OpenNLP2.NET.POSTag;
using OpenNLP2.NET.Tokenize;

namespace OpenNLP2.Tests;

public class Tests
{
    private const string POS_MODEL_URL = "https://dlcdn.apache.org/opennlp/models/ud-models-1.1/opennlp-en-ud-ewt-pos-1.1-2.4.0.bin";
    private const string TOKENIZER_MODEL_URL = "https://dlcdn.apache.org/opennlp/models/ud-models-1.1/opennlp-en-ud-ewt-tokens-1.1-2.4.0.bin";
    
    [Test]
    public void PartOfSpeechTagger()
    {
        var model = new POSModel(GetModel(POS_MODEL_URL, "en-pos.bin"));
        var tagger = new POSTagger(model);
        
        var sentence = new[]
        {
            "Most", "large", "cities", "in", "the", "US", "had",
            "morning", "and", "afternoon", "newspapers", "."
        };
        var tags = tagger.Tag(sentence);
        
        Console.WriteLine(string.Join(";", tags));
        Assert.That(tags.Length, Is.EqualTo(12));
        
        var probs = tagger.Probabilities();
        Console.WriteLine(string.Join(";", probs));
        Assert.That(probs.Length, Is.EqualTo(12));
    }
    
    [Test]
    public void Tokenization()
    {
        var model = new TokenizerModel(GetModel(TOKENIZER_MODEL_URL, "en-token.bin"));
        var tokenizer = new TokenizerME(model);

        var tokens = tokenizer.Tokenize("An input sample sentence.");
        Console.WriteLine(string.Join(";", tokens));

        Assert.That(tokens.Length, Is.EqualTo(5));
    }

    private string GetModel(string url, string fileName)
    {
        var asmFolder = Path.GetDirectoryName(GetType().Assembly.Location);
        var filePath = Path.GetFullPath(Path.Combine(asmFolder, fileName));
        if (!File.Exists(filePath))
        {
            FileDownloadHelper.DownloadFileAsync(url, filePath).GetAwaiter().GetResult();
        }
        return filePath;
    }
}