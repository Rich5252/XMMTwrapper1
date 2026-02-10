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




/*
using IniParser;
using IniParser.Model;
using System.IO;

public class IniManager
{
    private readonly FileIniDataParser _parser;
    private IniData _data;
    private readonly string _filePath;

    // The constructor now handles the path logic automatically
    public IniManager(string fileName)
    {
        _parser = new FileIniDataParser();

        // 1. Get the folder where the .exe is located
        string appPath = AppDomain.CurrentDomain.BaseDirectory;

        // 2. Combine with your filename
        _filePath = Path.Combine(appPath, fileName);

        if (!File.Exists(_filePath))
        {
            _data = new IniData();
            Save();
        }
        else
        {
            _data = _parser.ReadFile(_filePath);
        }
    }

    public string GetValue(string section, string key, string defaultValue = "")
    {
        if (_data[section][key] == "") SetValue(section, key, defaultValue);
        return _data[section][key] ?? defaultValue;
    }

    public void SetValue(string section, string key, string value)
    {
        _data[section][key] = value;
        Save();
    }

    public void Save()
    {
        _parser.WriteFile(_filePath, _data);
    }

    // Optional: Exposed property if you need to see where the file is stored
    public string FullPath => _filePath;
}
*/