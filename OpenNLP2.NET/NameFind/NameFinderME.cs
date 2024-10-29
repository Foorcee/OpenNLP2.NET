using System.Text;

namespace OpenNLP2.NET.NameFind;

public class NameFinderME
{
    private readonly opennlp.tools.namefind.NameFinderME _nameFinder;

    public NameFinderME(TokenNameFinderModel model)
    {
        _nameFinder = new opennlp.tools.namefind.NameFinderME(model.Model);
    }

    public Entity[] Find(string[] sentence)
    {
        var spans = _nameFinder.find(sentence);
        return spans.Select(sp => new Entity(sp.getStart(), sp.getEnd(), sp.getProb(), sp.getType())).ToArray();
    }

    public double[] GetProbabilites()
    {
        return _nameFinder.probs();
    }

    public record Entity(int Start, int End, double Probability, string? Type)
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(15);
            stringBuilder.Append('[');
            stringBuilder.Append(Start);
            stringBuilder.Append("..");
            stringBuilder.Append(End);
            stringBuilder.Append(')');
            if (Type != null)
            {
                stringBuilder.Append(' ');
                stringBuilder.Append(Type);
            }

            return stringBuilder.ToString();
        }
    }
}