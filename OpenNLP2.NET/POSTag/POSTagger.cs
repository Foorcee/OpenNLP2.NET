namespace OpenNLP2.NET.POSTag;

public class POSTagger
{
    private readonly opennlp.tools.postag.POSTaggerME _tagger;
    
    public POSTagger(POSModel model)
    {
        _tagger = new opennlp.tools.postag.POSTaggerME(model.Model);
    }

    public string[] Tag(string[] sentence)
    {
        return _tagger.tag(sentence);
    }
    
    public double[] Probabilities()
    {
        return _tagger.probs();
    }
}