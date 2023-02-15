using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenHeroSelectGUI
{
    public partial class Main : Form
    {
        private IniFile? iniGUI;
        private readonly SaveSlots saveSlots;
        private readonly string cdPath;
        private string SwitchTo;
        public string game;
        private string Heroes;
        private string iniOHS;
        private bool SettingsLoaded = false;
        private string jsonOHS = "";
        private string GIP;
        // Will have to be a function
        private readonly int[] RangeXML2 = Enumerable.Range(1, 21).ToArray();
        private readonly int[] RangeMUA = Enumerable.Range(1, 26).Concat(new[] { 96 }).ToArray();
        private int[] MenulocationSet = Enumerable.Range(1, 26).Concat(new[] { 96 }).ToArray();
        public class OHSsettings
        {
            [DefaultValue(21)]
            public int? rosterSize { get; set; }
            [DefaultValue("temp.OHSGUI")]
            public string? menulocationsValue { get; set; }
            public bool? rosterHack { get; set; }
            [DefaultValue("temp.OHSGUI")]
            public string? rosterValue { get; set; }
            public string? gameInstallPath { get; set; }
            [DefaultValue("Game.exe")]
            public string? exeName { get; set; }
            [DefaultValue("herostat.engb")]
            public string? herostatName { get; set; }
            public bool unlocker { get; set; }
            public bool launchGame { get; set; }
            public bool saveTempFiles { get; set; }
            public bool showProgress { get; set; }
            public bool debugMode { get; set; }
            [DefaultValue("xml")]
            public string? herostatFolder { get; set; }
        }
        public OHSsettings OHScfg = new OHSsettings { };
        public Main()
        {
            InitializeComponent();
            saveSlots = new SaveSlots();
            mnuMenu.Items.Add(saveSlots);

            cdPath = Directory.GetCurrentDirectory();
            SwitchTo = "XML2";
            game = cdPath + "\\mua\\";
            Heroes = game + "xml\\";
            iniOHS = game + "config.ini";
            GIP = "C:\\Program Files (x86)\\Activision\\Marvel - Ultimate Alliance";
        }
        /// <summary>
        /// Load default settings of the defined game and update all characters and locations according to it.
        /// </summary>
        private void UpdateFromGameInfo(string g)
        {
            LoadSettings(cdPath + "\\" + g + "\\config.ini");
            if (SettingsLoaded) UpdateGameInfo(g);

        }
        /// <summary>
        /// Load settings with config file path.
        /// </summary>
        private void UpdateFromConfig(string path)
        {
            LoadSettings(path);
            if (SettingsLoaded)
            {
                string g = (jsonOHS.Contains("\"rosterSize\":")) ?
                    "xml2" :
                    "mua";
                UpdateGameInfo(g);
            }
        }
        /// <summary>
        /// Update the variables, window elements, and reload the data according to the game. Cheap tab-switch.
        /// </summary>
        private void UpdateGameInfo(string g)
        {
            game = cdPath + "\\" + g + "\\";
            Heroes = (System.IO.Path.IsPathRooted(OHScfg.herostatFolder)) ?
                OHScfg.herostatFolder + "\\" :
                game + OHScfg.herostatFolder + "\\";
            if (g == "mua")
            {
                SwitchTo = "XML2";
                objXML2.Visible = false;
                objMenu.Visible = true;
                btnGame.Text = "Switch to XML2";
                MenulocationSet = RangeMUA;
            }
            else
            {
                SwitchTo = "MUA";
                objXML2.Visible = true;
                objMenu.Visible = false;
                btnGame.Text = "Switch to MUA";
                MenulocationSet = RangeXML2;
            }
            PopulateAvailable();
            RemoveAll();
            LoadDefaultChars();
        }
        /// <summary>
        /// Load OHS JSON File Config.ini and GUI ini. Incomplete, as we want more settings and controls (such as unlocker).
        /// </summary>
        private void LoadSettings(string path)
        {
            SettingsLoaded = false;
            string Gpath = path.Remove(path.LastIndexOf(".")) + "_GUI.ini";
            iniGUI = new IniFile(Gpath);
            if (File.Exists(path))
            {
                try
                {
                    jsonOHS = File.ReadAllText(path);
                    OHScfg = JsonConvert.DeserializeObject<OHSsettings>(jsonOHS, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Populate });
                    foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(OHScfg))
                    {
                        string setting = property.Name;
                        object value = property.GetValue(OHScfg);
                        Log(setting + ": " + value);
                    }
                    Log("Settings loaded:");
                    // Installation Path. Needs to be updated with a brows function. Other settings as well.
                    GIP = OHScfg.gameInstallPath;
                    iniOHS = path;
                    SettingsLoaded = true;
                }
                catch(JsonReaderException je)
                {
                    Log(je.ToString());
                }
                catch(Exception e)
                {
                    Log(e.ToString());
                }
            }
            else
            {
                Log("ERROR: Configuration not found: " + path);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateFromGameInfo("mua");
        }
        /// <summary>
        /// Load the character and save slot information.
        /// </summary>
        private void LoadDefaultChars()
        {
            string rrCFG = game + "rosters\\" + OHScfg.rosterValue + ".cfg";
            string mlCFG = (SwitchTo == "XML2") ?
                game + "menulocations\\" + OHScfg.menulocationsValue + ".cfg" :
                rrCFG;
            if (File.Exists(mlCFG) && File.Exists(rrCFG))
            {
                string[] OHSrr = File.ReadAllLines(rrCFG);
                string[] OHSml = File.ReadAllLines(mlCFG);
                int[] l = (SwitchTo == "XML2") ?
                    Array.ConvertAll(OHSml, s => int.Parse(s)) :
                    Enumerable.Range(1, OHSrr.Length).ToArray();
                var mlload =  l.Intersect(MenulocationSet);
                // LOAD chars
                foreach (int i in mlload)
                {
                    int index = Array.IndexOf(l, i);
                    string path = OHSrr[index].Replace("\\", "/");
                    if (!path.Equals(""))
                    {
                        string dir = Heroes + Regex.Match(path, @".*\/").Value;
                        string name = Regex.Match(path, @"(?:[^/](?!\/))+$").Value;
                        string[] hs = Directory.GetFiles(dir, name + ".??????????");
                        if (hs.Length != 0)
                        {
                            AddSelectedChar(i, name, path);
                        }
                        else
                        {
                            Log("ERROR: Herostat not found: " + path);
                        }
                    }
                }
            }
            else
            {
                Log(mlCFG);
                Log(rrCFG);
                Log("ERROR: files not found:");
            }
            //load save slots
            saveSlots.CleanAll();
            for (int i = 1; i <= 10; i++) 
            {
                string slot = iniGUI.IniReadValue("saves", "slot" + i);
                if (slot.ToUpper().Equals("TRUE")) {
                    saveSlots.SetChecked(i);
                }
            }
        }
        /// <summary>
        /// Load characters from disk
        /// </summary>
        private void PopulateAvailable()
        {
            trvAvailableChars.Nodes.Clear();
            DirectoryInfo folder = new DirectoryInfo(Heroes);
            if (folder.Exists)
            {
                PopulateAvailable(folder, trvAvailableChars.Nodes);
                trvAvailableChars.Sort();
            }
            else 
            {
                Log("ERROR: folder not found: " + Heroes);
            }
        }
        /// <summary>
        /// Tree View
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="nodes"></param>
        /// <seealso cref="http://www.c-sharpcorner.com/UploadFile/scottlysle/TreeviewBasics04152007195731PM/TreeviewBasics.aspx"/>
        private void PopulateAvailable(DirectoryInfo folder, TreeNodeCollection nodes)
        {
            foreach(DirectoryInfo subfolder in folder.GetDirectories()){
                TreeNode node = new TreeNode
                {
                    Text = subfolder.Name
                };
                nodes.Add(node);

                PopulateAvailable(subfolder, node.Nodes);
            }
            var found = new List<string>();
            found.AddRange(folder.GetFiles("*.*")
                                 .Select(f => f.FullName.Remove(f.FullName.LastIndexOf(".")))
                                 .Distinct());
            foreach (string file in found)
            {
                TreeNode node = new TreeNode
                {
                    Text = file.Substring(file.LastIndexOf("\\")+1),
                    Tag = file
                };
                nodes.Add(node);
            }
        }

        /// <summary>
        /// Mark a char as selected. We need Drag & Drop support! But that's in the Designer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrvAvailableChars_DoubleClick(object sender, EventArgs e)
        {
            if ((lstSelected.Items.Count <= 49) || !txtPosition.Text.Equals(""))
            {
                if (trvAvailableChars.SelectedNode.Tag is object)
                {
                    string name = trvAvailableChars.SelectedNode.Text.ToString();
                    string path = trvAvailableChars.SelectedNode.Tag.ToString().Substring(Heroes.Length);
                    int pos = GetFreePosition();

                    if (!txtPosition.Text.Equals(""))
                    {
                        pos = Int32.Parse(txtPosition.Text);
                        foreach (ListViewItem lvi in lstSelected.Items)
                        {
                            if (Int32.Parse(lvi.Text) == pos)
                            { //REPLACE POSITION
                                lstSelected.Items.Remove(lvi);
                            }
                            else if (name.Equals(lvi.SubItems[1].Text))
                            { //PREVENT SAME CHAR
                                if (SwitchTo == "XML2") objMenu.SetMenulocationBox(Int32.Parse(lvi.Text), "");
                                lstSelected.Items.Remove(lvi);
                            }
                        }
                    }
                    if (pos != 0) AddSelectedChar(pos, name, path);

                    txtPosition.Text = "";
                    pos = GetFreePosition();
                    if (pos!=0)
                        txtPosition.Text = pos + "";
                }
            }
            else 
            {
                Log("ERROR: No space available!");
            }
        }
        /// <summary>
        /// Same action as ObjMenu_OnDoubleClickChar, with single click. Currently does nothing.
        /// </summary>
        private void TrvAvailableChars_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }
        /// <summary>
        /// Observer. Update the position field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pos"></param>
        private void ObjMenu_OnDoubleClickChar(string name, int pos)
        {
            txtPosition.Text = pos + "";
        }
        /// <summary>
        /// Return the first free position.
        /// </summary>
        /// <returns></returns>
        private int GetFreePosition()
        {
            List<int> pos = new List<int>();
            foreach (ListViewItem lvi in lstSelected.Items) pos.Add(Int32.Parse(lvi.Text));
            foreach (int i in MenulocationSet)
            {
                if (!pos.Contains(i))
                {
                    return i;
                }
            }
            return 0;
        }
        /// <summary>
        /// Add a char in lstSelected
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="name"></param>
        /// <param name="path"></param>
        private void AddSelectedChar(int pos, string name, string path)
        {
            if (SwitchTo == "XML2") objMenu.SetMenulocationBox(pos, name);
            ListViewItem lvi = new ListViewItem(pos.ToString().PadLeft(2, '0'));
            lvi.SubItems.Add(name);
            lvi.SubItems.Add(path);
            lstSelected.Items.Add(lvi);

            lvwColumnSorter.Order = SortOrder.Descending;
            lstSelected_ColumnClick(this,new ColumnClickEventArgs(0));

            // log(path + " loaded");
        }

        private void LstSelected_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSelected.SelectedItems.Count>0)
                txtPosition.Text = lstSelected.SelectedItems[0].Text;
        }
        /// <summary>
        /// Remove a selected char from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRemove_Click(object sender, EventArgs e)
        {

            if (lstSelected.Items.Count == 0) 
            {
                Log("ERROR: There is no character to remove.");
            }
            else if (lstSelected.SelectedItems.Count == 0)
            {
                Log("ERROR: Select character(s) to remove.");
            }
            else
            {
                foreach (ListViewItem lvi in lstSelected.SelectedItems)
                {
                    if (SwitchTo == "XML2")
                        objMenu.SetMenulocationBox(Int32.Parse(lvi.SubItems[0].Text), "");
                    lstSelected.Items.Remove(lvi);
                }
                txtPosition.Text = "";
            }
        }
        /// <summary>
        /// Switch between MUA and XML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnGame_Click(object sender, EventArgs e)
        {
            if (SwitchTo == "MUA")
            {
                UpdateFromGameInfo("mua");
            }
            else
            {
                UpdateFromGameInfo("xml2");
            }
        }
        /// <summary>
        /// Replace the MUA config files. Need to test!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRun_Click(object sender, EventArgs e)
        {
            if (File.Exists(cdPath + "\\OpenHeroSelect.exe"))
            {
                DirectoryInfo IP = new DirectoryInfo(GIP + "\\data");
                if (IP.Exists)
                {
                    GenerateCfgFiles("temp.OHSGUI", iniOHS);
                    string arg = (SwitchTo == "XML2") ?
                        "-a" :
                        "-a -x";
                    Util.RunElevated("OpenHeroSelect.exe", arg);
                    string elog = cdPath + "\\error.log";
                    if (!File.Exists(elog))
                    {
                        //Check saves
                        if (OHScfg.launchGame && saveSlots.SelectedItems.Count > 0)
                        {
                            SetSaves(false);
                            MessageBox.Show("Press OK to restore saves (after closing the game).", "Waiting...");
                            SetSaves(true);
                        }
                    }
                    else
                    {
                        Log(File.ReadAllText(elog));
                        Process.Start("explorer.exe", "/select, \"" + elog + "\"");
                    }
                }
                else
                {
                    Log("ERROR: Installation path not valid!\r\n'" + IP + "' not found.");
                }
            }
            else
            {
                Log("ERROR: OpenHeroSelect.exe not found!");
            }
        }
        /// <summary>
        /// Generate the OHS CFG + INI files.
        /// </summary>
        private void GenerateCfgFiles(string name, string Oini)
        {
            if (lstSelected.Items.Count > 0)
            {
                string rrCFG = game + "rosters\\" + name + ".cfg";
                WriteCfg(rrCFG, 2);
                if (SwitchTo == "XML2")
                {
                    string? mlname = (name == OHScfg.rosterValue) ?
                        OHScfg.menulocationsValue :
                        name;
                    string mlCFG = cdPath + "\\mua\\menulocations\\" + mlname + ".cfg";
                    WriteCfg(mlCFG, 0);
                    OHScfg.menulocationsValue = mlname;
                    // OHScfg.rosterHack;
                    OHScfg.rosterSize = null;
                }
                else
                {
                    OHScfg.menulocationsValue = null;
                    OHScfg.rosterHack = null;
                    // OHScfg.rosterSize = 21;
                }
                // Need to add options for herostat.engb, Game.exe, and many more
                OHScfg.rosterValue = name;
                OHScfg.gameInstallPath = GIP;
                // OHScfg.exeName;
                // OHScfg.herostatName;
                // OHScfg.unlocker;
                // OHScfg.launchGame;
                // OHScfg.saveTempFiles;
                // OHScfg.showProgress;
                // OHScfg.debugMode;
                if (OHScfg.herostatFolder == "xml") OHScfg.herostatFolder = null;
                File.WriteAllText(Oini, JsonConvert.SerializeObject(OHScfg, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                string Gini = Oini.Remove(Oini.LastIndexOf(".")) + "_GUI.ini";
                File.Delete(Gini);
                IniFile iniGUI = new IniFile(Gini);
                foreach (ToolStripMenuItem item in saveSlots.SelectedItems) 
                {
                    int pos = Int32.Parse(item.Name.Substring(7));
                    iniGUI.IniWriteValue("saves","slot" + pos,"true");
                }
            }
            else
            {
                Log("ERROR: No characters selected. Information not saved.");
            }
        }
        /// <summary>
        /// Write the OHS CFG files from the list view information.
        /// </summary>
        private void WriteCfg(string path, int p)
        {
            File.Create(path).Dispose();
            using StreamWriter sw = File.AppendText(path);
            foreach (ListViewItem lvi in lstSelected.Items)
            {
                string line = lvi.SubItems[p].Text;
                sw.WriteLine(line);
            }
        }
        /// <summary>
        /// Old command. We may need part of it to write team_bonus and items in upcoming versions, so it's still here.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPos"></param>
        /// <returns></returns>
        private string ReadFile(string path, int newPos)
        {
            StringBuilder sb = new StringBuilder();
            StreamReader sr = new StreamReader(path);
            string line = sr.ReadLine();
            while (line != null)
            {
                if (line.Trim().StartsWith("menulocation"))
                {
                    sb.AppendLine("   menulocation = " + newPos + " ;");
                }
                else
                {
                    sb.AppendLine(line);
                }
                line = sr.ReadLine();
            }
            sr.Close();
            return sb.ToString();
        }
        /// <summary>
        /// Put a message in the log console
        /// </summary>
        /// <param name="msg"></param>
        private void Log(string msg){
            txtDebug.Text = msg + "\r\n" + txtDebug.Text;
        }
        /// <summary>
        /// Check the folders again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnReload_Click(object sender, EventArgs e)
        {
            PopulateAvailable();
        }
        /// <summary>
        /// Remove all heroes from the list view, and clear the menulocations for MUA.
        /// </summary>
        private void RemoveAll()
        {
            foreach (ListViewItem lvi in lstSelected.Items) 
            {
                lstSelected.Items.Remove(lvi);
            }
            if (SwitchTo == "XML2") foreach (int i in MenulocationSet)
                objMenu.SetMenulocationBox(i, "");
        }

        private void BtnRemoveAll_Click(object sender, EventArgs e)
        {
            RemoveAll();
        }

        private void BtnClean_Click(object sender, EventArgs e)
        {
            txtDebug.Text = "";
        }

        private void MnuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// Save dialogue.
        /// </summary>
        private void MnuSaveAs_Click(object sender, EventArgs e)
        {
            saveFileDialog.AddExtension = true;
            saveFileDialog.DefaultExt = "ini";
            saveFileDialog.Filter = "Ini File (*.ini)|*.ini";
            DialogResult dialogResult = saveFileDialog.ShowDialog(this);
            if (dialogResult.ToString().Equals("OK"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                GenerateCfgFiles(name, saveFileDialog.FileName);
            }
        }
        /// <summary>
        /// Save to opened CFG.
        /// </summary>
        private void MnuSave_Click(object sender, EventArgs e)
        {
            GenerateCfgFiles(OHScfg.rosterValue, iniOHS);
        }
        /// <summary>
        /// Load dialogue. Needs shortcut Ctrl+O.
        /// </summary>
        private void MnuLoad_Click(object sender, EventArgs e)
        {
            openFileDialog.AddExtension = true;
            openFileDialog.DefaultExt = "ini";
            openFileDialog.Filter = "Ini File (*.ini)|*.ini";
            DialogResult dialogResult = openFileDialog.ShowDialog(this);
            if (dialogResult.ToString().Equals("OK"))
            {
                UpdateFromConfig(openFileDialog.FileName);
            }
        }
        private void SetSaves(bool restore)
        {
            string GameFolder = (SwitchTo == "XML2") ?
                "Marvel Ultimate Alliance" :
                "X-Men Legends 2";
            string SAVES_PATH = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Activision/" + GameFolder + "/Save";
            DirectoryInfo folder = new DirectoryInfo(SAVES_PATH);

            if (!restore)
            {
                foreach (FileInfo file in folder.GetFiles("*.save"))
                    file.MoveTo(file.FullName + ".bak");

                foreach (ToolStripMenuItem item in saveSlots.SelectedItems)
                {
                    int pos = Int32.Parse(item.Name.Substring(7)) - 1;
                    FileInfo file = new FileInfo(SAVES_PATH + "/saveslot" + pos + ".save.bak");
                    if (file.Exists)
                    {
                        string newName = file.FullName.Substring(0, file.FullName.Length - 4);
                        // Should not need this. Otherwise get the code from the backup.
                        // debug(newName);
                        file.MoveTo(newName);
                    }
                }
            }
            else
            {
                foreach (FileInfo file in folder.GetFiles("*.save.bak"))
                {
                    string newName = file.FullName.Substring(0, file.FullName.Length - 4);
                    file.MoveTo(newName);
                }

            }

        }
        private void MenuDefaultChars_Click(object sender, EventArgs e)
        {
            RemoveAll();
            LoadDefaultChars();
        }

        private void OpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
