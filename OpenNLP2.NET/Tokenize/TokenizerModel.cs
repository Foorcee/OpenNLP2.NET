namespace OpenNLP2.NET.Tokenize;

public class TokenizerModel
{
    internal opennlp.tools.tokenize.TokenizerModel Model { get; }

    public TokenizerModel(string filePath)
    {
        var file = new java.io.File(filePath);
        if (!file.exists())
            throw new FileNotFoundException();

        Model = new opennlp.tools.tokenize.TokenizerModel(file);
    }
}