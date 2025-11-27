using System.Text;

public class YamlBuilder
{
    private readonly StringBuilder _builder = new();
    private const string Indent = "  ";
    private int _indentLevel = 0;

    // Optional: keep a raw line adder if you want (can remove if undesired)
    public YamlBuilder AddLine(string line)
    {
        _builder.AppendLine(line);
        return this;
    }

    /// <summary>
    /// Writes a parent line at the current level, then executes the provided action
    /// at one level deeper. When the action completes, the indent level is restored.
    /// </summary>
    public YamlBuilder AddParent(string line, Action<YamlBuilder> children)
    {
        AppendLineWithIndent(line);
        _indentLevel++;
        try
        {
            children?.Invoke(this);
        }
        finally
        {
            _indentLevel--;
        }
        return this;
    }

    /// <summary>
    /// Writes a child line at the current (possibly nested) level.
    /// Use inside an AddParent block to emit children and siblings.
    /// </summary>
    public YamlBuilder AddChild(string line)
    {
        AppendLineWithIndent(line);
        return this;
    }

    public string Build() => _builder.ToString();

    private void AppendLineWithIndent(string line)
    {
        var indentation = string.Concat(Enumerable.Repeat(Indent, _indentLevel));
        _builder.AppendLine($"{indentation}{line}");
    }
}
