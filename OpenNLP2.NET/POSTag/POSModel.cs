namespace OpenNLP2.NET.POSTag;

public class POSModel
{
    internal opennlp.tools.postag.POSModel Model { get; }

    public POSModel(string filePath)
    {
        var file = new java.io.File(filePath);
        if (!file.exists())
            throw new FileNotFoundException();

        Model = new opennlp.tools.postag.POSModel(file);
    }
}