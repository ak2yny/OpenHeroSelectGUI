using System;
using System.Text;
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
        private IniFile iniGUI;
        private readonly SaveSlots saveSlots;
        private readonly string cdPath;
        private readonly string game;
        private readonly string Heroes;
        private string iniOHS;
        private string? jsonOHS;
        private string? GIP;
        // Will have to be a function
        private readonly int[] MenulocationSet = Enumerable.Range(1, 26).Concat(new[] { 96 }).ToArray();
        public class OHSsettings
        {
            public string? menulocationsValue { get; set; }
            public bool rosterHack { get; set; }
            public string? rosterValue { get; set; }
            public string? gameInstallPath { get; set; }
            public string? exeName { get; set; }
            public string? herostatName { get; set; }
            public bool unlocker { get; set; }
            public bool launchGame { get; set; }
            public bool saveTempFiles { get; set; }
            public bool showProgress { get; set; }
            public bool debugMode { get; set; }
        }
        public OHSsettings OHScfg = new OHSsettings
        {
            menulocationsValue = "temp.OHSGUI",
            rosterHack = true,
            rosterValue = "temp.OHSGUI",
            gameInstallPath = "test",
            exeName = "Game.exe",
            herostatName = "herostat.engb",
            unlocker = false,
            launchGame = false,
            saveTempFiles = false,
            showProgress = false,
            debugMode = false
        };
        public Main()
        {
            InitializeComponent();
            saveSlots = new SaveSlots();
            mnuMenu.Items.Add(saveSlots);

            cdPath = Directory.GetCurrentDirectory();
            // REM OHS Paths. Should support XML2 as well!
            game = cdPath + "\\mua\\";
            // We should change that to txt again, but people are just too used to xml?
            // Should also be Heroes folder with xml, json, and txt properties eventually
            Heroes = game + "xml\\";
            iniGUI = new IniFile(game + "config_GUI.ini");
            iniOHS = game + "config.ini";
        }
        /// <summary>
        /// Installation Path. Should be the call of a path field (or text) with a browse function. Example, other controls like this need to be added.
        /// </summary>
        private void UpdateInstallationPath(string path)
        {
            DirectoryInfo IP = new DirectoryInfo(path + "\\data");
            if (IP.Exists)
            {
                GIP = path;
            }
            else
            {
                Log("ERROR: Installation path not valid!\r\n" + path);
            }
        }
        /// <summary>
        /// Load OHS JSON File Config.ini and GUI ini. Incomplete, as we want more settings and controls (such as unlocker).
        /// </summary>
        private void LoadSettings(string path)
        {
            string Gpath = path.Remove(path.LastIndexOf(".")) + "_GUI.ini";
            iniGUI = new IniFile(Gpath);
            if (File.Exists(path))
            {
                jsonOHS = File.ReadAllText(path);
                OHScfg = JsonConvert.DeserializeObject<OHSsettings>(jsonOHS);
                UpdateInstallationPath(OHScfg.gameInstallPath);
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(OHScfg))
                {
                    string setting = property.Name;
                    object value = property.GetValue(OHScfg);
                    Log(setting + ": " + value);
                }
                Log("Settings loaded:");
            }
            else
            {
                jsonOHS = "";
                Log("ERROR: Configuration not found: " + path);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings(iniOHS);
            PopulateAvailable();
            LoadDefaultChars();
        }
        /// <summary>
        /// Load the character and save slot information.
        /// </summary>
        private void LoadDefaultChars()
        {
            string mlCFG = cdPath + "\\mua\\menulocations\\" + OHScfg.menulocationsValue + ".cfg";
            string rrCFG = game + "rosters\\" + OHScfg.rosterValue + ".cfg";
            if (File.Exists(mlCFG) && File.Exists(rrCFG))
            {
                string[] OHSrr = File.ReadAllLines(rrCFG);
                string[] OHSml = File.ReadAllLines(mlCFG);
                int[] l = Array.ConvertAll(OHSml, s => int.Parse(s));
                var mlload = l.Intersect(MenulocationSet);
                //LOAD chars
                foreach (int i in mlload)
                {
                    int index = Array.IndexOf(l, i);
                    string path = OHSrr[index];
                    if (!path.Equals(""))
                    {
                        string dir = Heroes + path.Remove(path.LastIndexOf("\\")+1);
                        string name = path.Substring(path.LastIndexOf("\\")+1);
                        string[] hs = Directory.GetFiles(dir, name + ".*");
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

        /// We need Drag & Drop support! But that's in the Designer.
        /// <summary>
        /// Mark a char as selected
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
                                objMenu.SetMenulocationBox(Int32.Parse(lvi.Text), "");
                                lstSelected.Items.Remove(lvi);
                            }
                        }
                    }
                    AddSelectedChar(pos,name, path);

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
        /// Return the first free position. Needs to be reworked.
        /// </summary>
        /// <returns></returns>
        private int GetFreePosition()
        {
            foreach (int i in MenulocationSet)
            {
                if (objMenu.GetMenulocationBox(i).CharName.Equals(""))
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
            objMenu.SetMenulocationBox(pos, name);
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
                    objMenu.SetMenulocationBox(Int32.Parse(lvi.SubItems[0].Text), "");
                    lstSelected.Items.Remove(lvi);
                }
                txtPosition.Text = "";
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
                if (GIP != null)
                {
                    GenerateCfgFiles("temp.OHSGUI", iniOHS);
                    // For XML2, add argument -x > "-a -x"
                    Util.RunElevated("OpenHeroSelect.exe", "-a");
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
                    Log("ERROR: Game installation path not defined");
                }
            }
            else
            {
                Log("ERROR: OpenHeroSelect.exe not found!");
            }
        }
        /// <summary>
        /// Generate the OHS CFG + INI files. Needs to be updated for XML2.
        /// </summary>
        private void GenerateCfgFiles(string name, string Oini)
        {
            if (lstSelected.Items.Count > 0)
            {
                string rrCFG = game + "rosters\\" + name + ".cfg";
                WriteCfg(rrCFG, 2);
                // XML2 does not need the menulocations file. Need to add that.
                string? mlname = (name == OHScfg.rosterValue) ?
                    OHScfg.menulocationsValue :
                    name;
                string mlCFG = cdPath + "\\mua\\menulocations\\" + mlname + ".cfg";
                WriteCfg(mlCFG, 0);
                OHSsettings newCfg = new OHSsettings
                {
                    // Need to add options for herostat.engb, Game.exe, and many more
                    menulocationsValue = mlname,
                    rosterHack = OHScfg.rosterHack,
                    rosterValue = name,
                    gameInstallPath = GIP,
                    exeName = OHScfg.exeName,
                    herostatName = OHScfg.herostatName,
                    unlocker = OHScfg.unlocker,
                    launchGame = OHScfg.launchGame,
                    saveTempFiles = OHScfg.saveTempFiles,
                    showProgress = OHScfg.showProgress,
                    debugMode = OHScfg.debugMode
                };
                File.WriteAllText(Oini, JsonConvert.SerializeObject(newCfg, Formatting.Indented));
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

        private void BtnRemoveAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstSelected.Items) 
            {
                lstSelected.Items.Remove(lvi);
            }
            foreach (int i in MenulocationSet)
                objMenu.SetMenulocationBox(i, "");
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
                /// I have to try if that works.
                iniOHS = openFileDialog.FileName;
                LoadSettings(iniOHS);
                BtnRemoveAll_Click(sender, e);
                LoadDefaultChars();
            }
        }
        private void SetSaves(bool restore)
        {
            string SAVES_PATH = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Activision/Marvel Ultimate Alliance/Save";
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
            BtnRemoveAll_Click(sender, e);
            LoadDefaultChars();
        }

        private void OpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
