using IniParser;
using IniParser.Model;

namespace ValeoItacCheck
{
    public class IniReader
    {
        private readonly FileIniDataParser _parser;
        private IniData _data;
        private string _filePath;

        public IniReader(string filePath = "AppSetting.ini")
        {
            _parser = new FileIniDataParser();
            _filePath = filePath;
        }

        public string GetSetting(string section, string key)
        {
            _data = _parser.ReadFile(_filePath);
            return _data[section][key];
        }
    }
}
