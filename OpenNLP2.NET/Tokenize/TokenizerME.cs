namespace OpenNLP2.NET.Tokenize;

public class TokenizerME
{
    private readonly opennlp.tools.tokenize.TokenizerME _tokenizer;
    
    public TokenizerME(TokenizerModel model)
    {
        _tokenizer = new opennlp.tools.tokenize.TokenizerME(model.Model);
    }

    public string[] Tokenize(string text)
    {
        return _tokenizer.tokenize(text);
    }

    public double[] Probabilities()
    {
        return _tokenizer.getTokenProbabilities();
    }
}