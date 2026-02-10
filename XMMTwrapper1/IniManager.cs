using IniParser;
using IniParser.Model;
using System.Text;

public class IniManager
{
    private readonly FileIniDataParser _parser;
    private IniData _data;
    private readonly string _filePath;

    public IniManager(string fileName)
    {
        // Use UTF8 to ensure special characters in macros are preserved
        _parser = new FileIniDataParser();

        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        _filePath = Path.Combine(appPath, fileName);

        Reload();
    }

    public void Reload()
    {
        if (File.Exists(_filePath))
        {
            _data = _parser.ReadFile(_filePath);
        }
        else
        {
            // Create fresh data if file is missing
            _data = new IniData();
            Save();
        }
    }

    public string GetValue(string section, string key, string defaultValue = "")
    {
        // 1. Check if section exists, then if key exists
        if (_data.Sections.ContainsSection(section) && _data[section].ContainsKey(key))
        {
            string val = defaultValue;
            try
            {
                val = _data[section][key];
            } catch {
                // In case of any error, fall through to set default
            }
            if (!string.IsNullOrEmpty(val)) return val;
        }

        // 2. If we got here, it's missing. Set the default and save it.
        SetValue(section, key, defaultValue);
        return defaultValue;
    }

    public void SetValue(string section, string key, string value)
    {
        // Ensure the section exists before writing to it
        if (!_data.Sections.ContainsSection(section))
        {
            _data.Sections.AddSection(section);
        }

        _data[section][key] = value;
        Save();
    }

    public void Save()
    {
        _parser.WriteFile(_filePath, _data, Encoding.UTF8);
    }
}



