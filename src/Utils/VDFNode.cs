using Microsoft.Toolkit.HighPerformance;

namespace TEKLauncher.Utils;

/// <summary>A recursive node for reading and writing VDF (Valve Data Format).</summary>
class VDFNode
{
    /// <summary>Initializes a new instance of <see cref="VDFNode"/> with specified key.</summary>
    /// <param name="key">Key of the entry.</param>
    public VDFNode(string key) => Key = key;
    /// <summary>Initializes a new instance of <see cref="VDFNode"/> and fills it with data read by <paramref name="reader"/>.</summary>
    /// <param name="reader">Reader of the stream that contains the text to parse.</param>
    public VDFNode(StreamReader reader)
    {
        Key = reader.ReadLine()![1..^1]; //Omit quotation marks
        Parse(reader);
    }
    /// <summary>Key of the entry, or name of the object.</summary>
    public string Key { get; set; }
    /// <summary>Value of the entry, <see langword="null"/> if the entry is an object.</summary>
    public string? Value { get; set; }
    /// <summary>Child nodes if <see langword="this"/> is an object.</summary>
    public List<VDFNode>? Children { get; private set; }
    /// <summary>Retrieves object's child with specified <paramref name="key"/>.</summary>
    /// <param name="key">Key of the entry to retieve.</param>
    public VDFNode? this[string key] => Children?.Find(s => s.Key == key);
    /// <summary>Parses inner text of the object into its children.</summary>
    /// <param name="reader">Reader of the stream that contains the text to parse.</param>
    void Parse(StreamReader reader)
    {
        Children = new();
        string? line;
        while ((line = reader.ReadLine()) is not null && !line.EndsWith('}'))
        {
            line = line.Replace(@"\""", "\0");
            int qmc = 0;
            for (int i = 0; i < line.Length; i++)
                if (line[i] == '"')
                    qmc++;
            if (qmc == 0) //Most likely empty line => nothing to read
                continue;
            string[] substrings = line.Split('"');
            if (qmc == 2) //1 string => object name
            {
                var child = new VDFNode(substrings[1]);
                child.Parse(reader);
                Children.Add(child);
            }
            else if (qmc == 4) //2 strings => key-value pair
                Children.Add(new(substrings[1]) { Value = substrings[3].Replace("\0", @"\""") });
        }
    }
    /// <summary>Writes the contents of the node to a stream.</summary>
    /// <param name="writer">Writer of the stream that the node will be written to.</param>
    /// <param name="identLevel">Level of identation, used for child nodes.</param>
    public void Write(StreamWriter writer, int identLevel = 0)
    {
        Span<char> tabs = stackalloc char[identLevel];
        for (int i = 0; i < identLevel; i++)
            tabs[i] = '\t';
        writer.Write(tabs);
        writer.Write($"\"{Key}\"");
        if (Children is null)
            writer.WriteLine($"\t\t\"{Value}\"");
        else
        {
            writer.Write('\n');
            writer.Write(tabs);
            writer.Write("{\n");
            foreach (var child in Children)
                child.Write(writer, identLevel + 1);
            writer.Write(tabs);
            writer.Write("}\n");
        }
    }
}