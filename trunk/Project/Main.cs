using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
namespace WFA_MUA
{
    public partial class Main : Form
    {
        private string EXE_PATH;
        private IniFile iniCfg;
        private string tmpFile;
        private SaveSlots saveSlots;
        public Main()
        {
            InitializeComponent();
            saveSlots = new SaveSlots();
            mnuMenu.Items.Add(saveSlots);

            EXE_PATH = System.IO.Directory.GetCurrentDirectory();
            /// We will not need that, since OHS will handle the compilation.
            tmpFile = EXE_PATH + "/tmp/test.txt";
        }
        /// <summary>
        /// Load Ini File
        /// </summary>
        private void loadIni()
        {
            string path = EXE_PATH + "\\sys\\Config.ini";
            FileInfo file = new FileInfo(path);
            if (file.Exists)
            {
                iniCfg = new IniFile(path);
                log("Ini file loaded!");
            }
            else 
            {
                log("ERROR: Config.ini file not found!!");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loadIni();
            populateAvailable();
            loadDefaultChars(iniCfg);            
        }
        /// <summary>
        /// Load the initial chars from INI file
        /// </summary>
        private void loadDefaultChars(IniFile ini)
        {            
            string chars = ini.IniReadValue("mua", "chars");
            //LOAD chars
            for (int i = 1; i <= 27; i++)
            {
                if (i == 27) i = 96;

                string path = ini.IniReadValue("defaultchars", i + "");
                if (!path.Equals(""))
                {
                    if (!chars.Equals("")) path = chars + "//" + path;
                    if (!path.EndsWith(".txt")) path += ".txt";

                    FileInfo file = new FileInfo(path);
                    path = file.FullName;
                    if (file.Exists)
                    {
                        string name = file.Name.Remove(file.Name.Length - 4);
                        addSelectedChar(i, name, path);
                    }
                    else
                    {
                        log("ERROR: file not found: " + path);
                    }
                }
            }
            //load save slots
            saveSlots.cleanAll();
            for (int i = 1; i <= 20; i++) 
            {
                string slot = ini.IniReadValue("saves", "slot" + i);
                if (slot.ToUpper().Equals("TRUE")) {
                    saveSlots.setChecked(i);
                }
            }
        }
        /// <summary>
        /// Load chars from disc
        /// </summary>
        private void populateAvailable()
        {
            trvAvailableChars.Nodes.Clear();
            DirectoryInfo folder = new DirectoryInfo(iniCfg.IniReadValue("mua","chars"));
            if (folder.Exists)
            {
                populateAvailable(folder, trvAvailableChars.Nodes);
                trvAvailableChars.Sort();
            }
            else 
            {
                log("ERROR: folder not found: " + folder.FullName );
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="nodes"></param>
        /// <seealso cref="http://www.c-sharpcorner.com/UploadFile/scottlysle/TreeviewBasics04152007195731PM/TreeviewBasics.aspx"/>
        private void populateAvailable(DirectoryInfo folder, TreeNodeCollection nodes)
        {
            foreach(DirectoryInfo subfolder in folder.GetDirectories()){
                TreeNode node = new TreeNode();
                node.Text = subfolder.Name;
                nodes.Add(node);

                populateAvailable(subfolder, node.Nodes);
            }
            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                TreeNode node = new TreeNode();
                node.Text = file.Name.Remove(file.Name.Length - 4);
                node.Tag = file.FullName;
                nodes.Add(node);
            }
        }

        /// <summary>
        /// Mark a char as selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trvAvailableChars_DoubleClick(object sender, EventArgs e)
        {
            if ((lstSelected.Items.Count <= 26) || !txtPosition.Text.Equals(""))
            {
                if (trvAvailableChars.SelectedNode.Tag != null)
                {

                    string name = trvAvailableChars.SelectedNode.Text.ToString();
                    string path = trvAvailableChars.SelectedNode.Tag.ToString();
                    int pos = getFreePosition();

                    if (!txtPosition.Text.Equals(""))
                    {
                        pos = Int32.Parse(txtPosition.Text);
                        foreach (ListViewItem lvi in lstSelected.Items) 
                        {
                            if (Int32.Parse(lvi.Text) == pos){ //REPLACE POSITION
                                lstSelected.Items.Remove(lvi);
                            }else if(name.Equals(lvi.SubItems[1].Text)) { //PREVENT SAME CHAR
                                objMenu.setTextbox(Int32.Parse(lvi.Text), "");
                                lstSelected.Items.Remove(lvi);
                            }
                        }
                    }

                    addSelectedChar(pos,name, path);


                    txtPosition.Text = "";
                    pos = getFreePosition();
                    if (pos!=0)
                        txtPosition.Text = pos + "";
                }
            }
            else 
            {
                log("ERROR: there is no space available");
            }
        }
        /// <summary>
        /// Return the first free position
        /// </summary>
        /// <returns></returns>
        private int getFreePosition()
        {
            for (int i = 1; i <= 26; i++) {
                if (objMenu.getTextbox(i).CharName.Equals(""))
                    return i;
            }
            if (objMenu.getTextbox(27).CharName.Equals(""))
                return 96;
            return 0;
        }
        /// <summary>
        /// Add a char in lstSelected
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="name"></param>
        /// <param name="path"></param>
        private void addSelectedChar(int pos, string name, string path)
        {
            objMenu.setTextbox(pos, name);
            ListViewItem lvi = new ListViewItem(pos + "");
            if (pos < 10)
                lvi.Text = "0" + lvi.Text;
            lvi.SubItems.Add(name);
            lvi.SubItems.Add(path);
            lstSelected.Items.Add(lvi);

            lvwColumnSorter.Order = SortOrder.Descending;
            lstSelected_ColumnClick(this,new ColumnClickEventArgs(0));

            //log(path + " loaded");
        }

        private void lstSelected_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSelected.SelectedItems.Count>0)
                txtPosition.Text = lstSelected.SelectedItems[0].Text;
        }
        /// <summary>
        /// Remove a selected char from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemove_Click(object sender, EventArgs e)
        {

            if (lstSelected.Items.Count == 0) 
            {
                log("ERROR: There is no char to remove");
            }
            else if (lstSelected.SelectedItems.Count == 0)
            {
                log("ERROR: select char(s) to remove");
            }
            else
            {
                foreach (ListViewItem lvi in lstSelected.SelectedItems)
                {
                    objMenu.setTextbox(Int32.Parse(lvi.SubItems[0].Text), "");
                    lstSelected.Items.Remove(lvi);
                }
                txtPosition.Text = "";
            }
        }
        /// <summary>
        /// Replace the MUA config file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRun_Click(object sender, EventArgs e)
        {
            if (lstSelected.Items.Count > 0)
            {
                generateTextFile();

                string aux = "-s \"" + tmpFile + "\" \"" + EXE_PATH + "/tmp/herostat.engb\"";
                debug("Compiling: " + aux);
                //compile the herostat.engb
                Util.runDosCommnand(EXE_PATH + "/sys/xmlb-compile.exe", aux);
                if (new FileInfo(EXE_PATH + "/tmp/Herostat.engb").Exists)
                {
                    string mua = iniCfg.IniReadValue("mua", "path");
                    if (new DirectoryInfo(mua).Exists)
                    {
                        //Copy herostat to MUA dir
                        FileInfo file = new FileInfo(EXE_PATH + "/tmp/herostat.engb");
                        file.CopyTo(mua + "/data/herostat.engb", true);

                        //Check saves
                        if (saveSlots.SelectedItems.Count > 0)
                        {
                            setSaves(false);
                        }

                        //Run MUA
                        Util.runDosCommnand(mua + "/Game.exe", "");

                        if (saveSlots.SelectedItems.Count>0)
                        {
                            MessageBox.Show("Press ok to restore saves (after close Game.exe)","Waiting...");
                            setSaves(true);
                        }
                    }
                    else
                    {
                        log("ERROR: folder not found: " + mua);
                    }
                }
                else
                {
                    log("INTERNAL ERROR: tmp/herostat.engb not found.");
                }
            }
            else
            {
                log("ERROR: No character selected");
            }
        }
        /// <summary>
        /// Generate the temporary text file with all selected chars
        /// </summary>
        private void generateTextFile()
        {
            DirectoryInfo folder = new DirectoryInfo(EXE_PATH + "/tmp");
            if (!folder.Exists)
                folder.Create();

            StreamWriter writer = new StreamWriter(tmpFile, false);
            writer.WriteLine("XMLB characters {");
            foreach (ListViewItem lvi in lstSelected.Items)
            {
                string path = lvi.SubItems[2].Text;
                int pos = Int32.Parse(lvi.SubItems[0].Text);

                writer.WriteLine("");
                writer.Write(readFile(path, pos));
                writer.WriteLine("");
            }
            writer.Write(readFile(EXE_PATH + "/sys/endoffile.txt", 0));
            writer.WriteLine("}");
            writer.Close();
        }
        /// We need a function which writes all information to the OHS cfg files.
        /// Let OHS handle that part (needs to run with admin permission): OpenHeroSelect.exe -a
        /// Util.runElevated("OpenHeroSelect.exe", "-a");
        /// <summary>
        /// Return all file content and replace the <b>menulocation</b> information
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPos"></param>
        /// <returns></returns>
        private string readFile(string path, int newPos)
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
        private void log(string msg){
            txtDebug.Text= msg + "\r\n" + txtDebug.Text;
        }
        /// What is this duplicate?
        /// <summary>
        /// Same as log
        /// </summary>
        /// <param name="msg"></param>
        private void debug(string msg)
        {
            //txtDebug.Text = msg + "\r\n" + txtDebug.Text;
        }
        /// <summary>
        /// Observer. Update the position field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pos"></param>
        private void objMenu_OnDoubleClickChar(string name, int pos)
        {
            txtPosition.Text = pos + "";
        }
        /// <summary>
        /// Check the folders again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReload_Click(object sender, EventArgs e)
        {
            populateAvailable();
        }

        private void trvAvailableChars_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void btnRemoveAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstSelected.Items) 
            {
                lstSelected.Items.Remove(lvi);
            }
            for (int i = 1; i <= 27; i++)
                objMenu.setTextbox(i, "");
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            txtDebug.Text = "";
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuSave_Click(object sender, EventArgs e)
        {
            saveFileDialog.AddExtension = true;
            saveFileDialog.DefaultExt = "ini";
            saveFileDialog.Filter = "Ini File (*.ini)|*.ini";
            DialogResult dialogResult = saveFileDialog.ShowDialog(this);
            if (dialogResult.ToString().Equals("OK"))
            {
                FileInfo file = new FileInfo(saveFileDialog.FileName);
                if (file.Exists)
                    file.Delete();

                IniFile ini = new IniFile(saveFileDialog.FileName);
                ini.IniWriteValue("about", "site", "http://muaopenheroselect.googlecode.com");

                //SAVE CHARS
                foreach (ListViewItem lvi in lstSelected.Items)
                {
                    int pos = Int32.Parse(lvi.Text);
                    string path = lvi.SubItems[2].Text;
                    ini.IniWriteValue("defaultchars",pos + "",path);
                }
                //SAVE SLOTS
                foreach (ToolStripMenuItem item in saveSlots.SelectedItems) 
                {
                    int pos = Int32.Parse(item.Name.Substring(7));
                    ini.IniWriteValue("saves","slot" + pos,"true");
                }
            }
        }

        private void mnuLoad_Click(object sender, EventArgs e)
        {
            openFileDialog.AddExtension = true;
            openFileDialog.DefaultExt = "ini";
            openFileDialog.Filter = "Ini File (*.ini)|*.ini";
            DialogResult dialogResult = openFileDialog.ShowDialog(this);
            if (dialogResult.ToString().Equals("OK"))
            {
                btnRemoveAll_Click(sender, e);
                IniFile ini = new IniFile(openFileDialog.FileName);
                loadDefaultChars(ini);
                log("INI loaded: " + openFileDialog.FileName);
            }
        }
        private void setSaves(bool restore)
        {
            string SAVES_PATH = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Activision/Marvel Ultimate Alliance/Save";
            DirectoryInfo folder = new DirectoryInfo(SAVES_PATH);

            if (!restore)
            {
                foreach (FileInfo file in folder.GetFiles("*.save"))
                    file.MoveTo(file.FullName + ".bak");

                foreach (ToolStripMenuItem item in saveSlots.SelectedItems)
                {
                    int pos = Int32.Parse(item.Name.Substring(7)) - 1;
                    FileInfo file = new FileInfo(SAVES_PATH + "//saveslot" + pos + ".save.bak");
                    if (file.Exists)
                    {
                        string newName = file.FullName.Substring(0, file.FullName.Length - 4);
                        debug(newName);
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
        private void menuDefaultChars_Click(object sender, EventArgs e)
        {
            btnRemoveAll_Click(sender, e);
            loadDefaultChars(iniCfg);
        }
    }
}
