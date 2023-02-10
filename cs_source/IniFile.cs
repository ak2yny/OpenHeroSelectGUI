using System.Runtime.InteropServices;
using System.Text;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    /// <seealso cref="http://www.codeproject.com/KB/cs/cs_ini.aspx"/>
    public class IniFile
    {
        private string path;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,  string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section">Section name</PARAM>
        /// <PARAM name="Key">Key Name</PARAM>
        /// <PARAM name="Value">Value Name</PARAM>
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }
        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,255, this.path);
            return temp.ToString();
        }
    }
}
