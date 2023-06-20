using System.Text.RegularExpressions;

namespace PresetCLI.Synths;

public abstract class SynthService : ISynthService
{
    private static readonly Regex _fileNameInvalidCharsRegex = new("[^a-zA-Z0-9_]");

    public abstract string PresetPath(PresetSearchResult result);

    protected string NormalizeFileName(string name)
    {
        return _fileNameInvalidCharsRegex.Replace(name, "_");
    }
}