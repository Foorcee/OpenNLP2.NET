namespace OpenNLP2.NET.NameFind;

public class TokenNameFinderModel
{
    internal opennlp.tools.namefind.TokenNameFinderModel Model { get; }

    public TokenNameFinderModel(string filePath)
    {
        var file = new java.io.File(filePath);
        if (!file.exists())
            throw new FileNotFoundException();

        Model = new opennlp.tools.namefind.TokenNameFinderModel(file);
    }
}