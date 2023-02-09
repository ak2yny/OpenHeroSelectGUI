using System.Windows.Forms;
namespace OpenHeroSelectGUI
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.trvAvailableChars = new System.Windows.Forms.TreeView();
            this.txtPosition = new System.Windows.Forms.TextBox();
            this.lstSelected = new System.Windows.Forms.ListView();
            this.Position = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Char = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Path = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.txtDebug = new System.Windows.Forms.TextBox();
            this.btnReload = new System.Windows.Forms.Button();
            this.lblAvailableChars = new System.Windows.Forms.Label();
            this.lblCurrentPos = new System.Windows.Forms.Label();
            this.lblSelectedChar = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnRemoveAll = new System.Windows.Forms.Button();
            this.btnClean = new System.Windows.Forms.Button();
            this.mnuMenu = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDefaultChars = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLoad = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.objMenu = new OpenHeroSelectGUI.Menu();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.mnuMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // trvAvailableChars
            // 
            this.trvAvailableChars.Location = new System.Drawing.Point(17, 80);
            this.trvAvailableChars.Margin = new System.Windows.Forms.Padding(4);
            this.trvAvailableChars.Name = "trvAvailableChars";
            this.trvAvailableChars.Size = new System.Drawing.Size(347, 527);
            this.trvAvailableChars.TabIndex = 1;
            this.trvAvailableChars.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TrvAvailableChars_AfterSelect);
            this.trvAvailableChars.DoubleClick += new System.EventHandler(this.TrvAvailableChars_DoubleClick);
            this.trvAvailableChars.KeyDown += new System.Windows.Forms.KeyEventHandler(this.trvAvailableChars_KeyDown);
            // 
            // txtPosition
            // 
            this.txtPosition.Location = new System.Drawing.Point(373, 29);
            this.txtPosition.Margin = new System.Windows.Forms.Padding(4);
            this.txtPosition.Name = "txtPosition";
            this.txtPosition.Size = new System.Drawing.Size(44, 22);
            this.txtPosition.TabIndex = 3;
            // 
            // lstSelected
            // 
            this.lstSelected.AllowColumnReorder = true;
            this.lstSelected.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Position,
            this.Char,
            this.Path});
            this.lstSelected.GridLines = true;
            this.lstSelected.HideSelection = false;
            this.lstSelected.HoverSelection = true;
            this.lstSelected.Location = new System.Drawing.Point(373, 80);
            this.lstSelected.Margin = new System.Windows.Forms.Padding(4);
            this.lstSelected.Name = "lstSelected";
            this.lstSelected.Size = new System.Drawing.Size(296, 527);
            this.lstSelected.TabIndex = 4;
            this.lstSelected.UseCompatibleStateImageBehavior = false;
            this.lstSelected.View = System.Windows.Forms.View.Details;
            this.lstSelected.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lstSelected_ColumnClick);
            this.lstSelected.SelectedIndexChanged += new System.EventHandler(this.LstSelected_SelectedIndexChanged);
            this.lstSelected.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lstSelected_KeyDown);
            // 
            // Position
            // 
            this.Position.Text = "Pos";
            this.Position.Width = 40;
            // 
            // Char
            // 
            this.Char.Text = "Character";
            this.Char.Width = 120;
            // 
            // Path
            // 
            this.Path.Text = "Path";
            this.Path.Width = 140;
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(532, 616);
            this.btnRemove.Margin = new System.Windows.Forms.Padding(4);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(117, 31);
            this.btnRemove.TabIndex = 5;
            this.btnRemove.Text = "Remove (Del)";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.BtnRemove_Click);
            // 
            // btnRun
            // 
            this.btnRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRun.Location = new System.Drawing.Point(680, 230);
            this.btnRun.Margin = new System.Windows.Forms.Padding(4);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(535, 58);
            this.btnRun.TabIndex = 6;
            this.btnRun.Text = "Run Marvel Ultimate Alliance";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.BtnRun_Click);
            // 
            // txtDebug
            // 
            this.txtDebug.BackColor = System.Drawing.SystemColors.WindowText;
            this.txtDebug.ForeColor = System.Drawing.SystemColors.Window;
            this.txtDebug.Location = new System.Drawing.Point(17, 654);
            this.txtDebug.Margin = new System.Windows.Forms.Padding(4);
            this.txtDebug.Multiline = true;
            this.txtDebug.Name = "txtDebug";
            this.txtDebug.ReadOnly = true;
            this.txtDebug.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDebug.Size = new System.Drawing.Size(1201, 69);
            this.txtDebug.TabIndex = 7;
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(128, 616);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(117, 31);
            this.btnReload.TabIndex = 8;
            this.btnReload.Text = "Reload (F5)";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            // 
            // lblAvailableChars
            // 
            this.lblAvailableChars.AutoSize = true;
            this.lblAvailableChars.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAvailableChars.Location = new System.Drawing.Point(105, 57);
            this.lblAvailableChars.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAvailableChars.Name = "lblAvailableChars";
            this.lblAvailableChars.Size = new System.Drawing.Size(158, 17);
            this.lblAvailableChars.TabIndex = 9;
            this.lblAvailableChars.Text = "Available Characters";
            // 
            // lblCurrentPos
            // 
            this.lblCurrentPos.AutoSize = true;
            this.lblCurrentPos.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentPos.Location = new System.Drawing.Point(225, 32);
            this.lblCurrentPos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCurrentPos.Name = "lblCurrentPos";
            this.lblCurrentPos.Size = new System.Drawing.Size(135, 17);
            this.lblCurrentPos.TabIndex = 10;
            this.lblCurrentPos.Text = "Current Position: ";
            // 
            // lblSelectedChar
            // 
            this.lblSelectedChar.AutoSize = true;
            this.lblSelectedChar.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectedChar.Location = new System.Drawing.Point(440, 57);
            this.lblSelectedChar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSelectedChar.Name = "lblSelectedChar";
            this.lblSelectedChar.Size = new System.Drawing.Size(155, 17);
            this.lblSelectedChar.TabIndex = 11;
            this.lblSelectedChar.Text = "Selected Characters";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::OpenHeroSelectGUI.Properties.Resources.Tux;
            this.pictureBox1.Location = new System.Drawing.Point(1050, 31);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(165, 169);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 12;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(677, 57);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(345, 85);
            this.label1.TabIndex = 13;
            this.label1.Text = "Marvel Ultimate Aliance \r\nOpen Hero Select GUI\r\n\r\nMore Information in:\r\nhttps://g" +
    "ithub.com/ak2yny/OpenHeroSelectGUI\r\n";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(677, 160);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 17);
            this.label2.TabIndex = 14;
            this.label2.Text = "by adamatti";
            // 
            // btnRemoveAll
            // 
            this.btnRemoveAll.Location = new System.Drawing.Point(389, 616);
            this.btnRemoveAll.Margin = new System.Windows.Forms.Padding(4);
            this.btnRemoveAll.Name = "btnRemoveAll";
            this.btnRemoveAll.Size = new System.Drawing.Size(117, 31);
            this.btnRemoveAll.TabIndex = 15;
            this.btnRemoveAll.Text = "Remove All";
            this.btnRemoveAll.UseVisualStyleBackColor = true;
            this.btnRemoveAll.Click += new System.EventHandler(this.BtnRemoveAll_Click);
            // 
            // btnClean
            // 
            this.btnClean.Location = new System.Drawing.Point(982, 616);
            this.btnClean.Margin = new System.Windows.Forms.Padding(4);
            this.btnClean.Name = "btnClean";
            this.btnClean.Size = new System.Drawing.Size(211, 31);
            this.btnClean.TabIndex = 16;
            this.btnClean.Text = "Clean Debug Window";
            this.btnClean.UseVisualStyleBackColor = true;
            this.btnClean.Click += new System.EventHandler(this.BtnClean_Click);
            // 
            // mnuMenu
            // 
            this.mnuMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mnuMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile});
            this.mnuMenu.Location = new System.Drawing.Point(0, 0);
            this.mnuMenu.Name = "mnuMenu";
            this.mnuMenu.Size = new System.Drawing.Size(1233, 28);
            this.mnuMenu.TabIndex = 17;
            this.mnuMenu.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuDefaultChars,
            this.mnuLoad,
            this.mnuSaveAs,
            this.mnuSave,
            this.mnuExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(46, 24);
            this.mnuFile.Text = "File";
            // 
            // menuDefaultChars
            // 
            this.menuDefaultChars.BackColor = System.Drawing.SystemColors.Window;
            this.menuDefaultChars.Name = "menuDefaultChars";
            this.menuDefaultChars.ShortcutKeyDisplayString = "L";
            this.menuDefaultChars.Size = new System.Drawing.Size(267, 26);
            this.menuDefaultChars.Text = "Load Default Characters";
            this.menuDefaultChars.Click += new System.EventHandler(this.MenuDefaultChars_Click);
            // 
            // mnuLoad
            // 
            this.mnuLoad.BackColor = System.Drawing.SystemColors.Window;
            this.mnuLoad.Name = "mnuLoad";
            this.mnuLoad.ShortcutKeyDisplayString = "Ctrl + O";
            this.mnuLoad.Size = new System.Drawing.Size(267, 26);
            this.mnuLoad.Text = "Load...";
            this.mnuLoad.Click += new System.EventHandler(this.MnuLoad_Click);
            // 
            // mnuSaveAs
            // 
            this.mnuSaveAs.BackColor = System.Drawing.SystemColors.Window;
            this.mnuSaveAs.Name = "mnuSaveAs";
            this.mnuSaveAs.ShortcutKeyDisplayString = "Ctrl + S";
            this.mnuSaveAs.Size = new System.Drawing.Size(267, 26);
            this.mnuSaveAs.Text = "Save As...";
            this.mnuSaveAs.Click += new System.EventHandler(this.MnuSaveAs_Click);
            // 
            // mnuSave
            // 
            this.mnuSave.BackColor = System.Drawing.SystemColors.Window;
            this.mnuSave.Name = "mnuSave";
            this.mnuSave.ShortcutKeyDisplayString = "S";
            this.mnuSave.Size = new System.Drawing.Size(267, 26);
            this.mnuSave.Text = "Save";
            this.mnuSave.Click += new System.EventHandler(this.MnuSave_Click);
            // 
            // mnuExit
            // 
            this.mnuExit.BackColor = System.Drawing.SystemColors.Window;
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.ShortcutKeyDisplayString = "Alt + F4";
            this.mnuExit.Size = new System.Drawing.Size(267, 26);
            this.mnuExit.Text = "Exit";
            this.mnuExit.Click += new System.EventHandler(this.MnuExit_Click);
            // 
            // objMenu
            // 
            this.objMenu.Location = new System.Drawing.Point(711, 297);
            this.objMenu.Margin = new System.Windows.Forms.Padding(5);
            this.objMenu.Name = "objMenu";
            this.objMenu.Size = new System.Drawing.Size(471, 265);
            this.objMenu.TabIndex = 2;
            this.objMenu.OnDoubleClickChar += new OpenHeroSelectGUI.Menu.delegateDoubleClickChar(this.ObjMenu_OnDoubleClickChar);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            this.openFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.OpenFileDialog_FileOk);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1233, 742);
            this.Controls.Add(this.btnClean);
            this.Controls.Add(this.btnRemoveAll);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblSelectedChar);
            this.Controls.Add(this.lblCurrentPos);
            this.Controls.Add(this.lblAvailableChars);
            this.Controls.Add(this.btnReload);
            this.Controls.Add(this.txtDebug);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lstSelected);
            this.Controls.Add(this.txtPosition);
            this.Controls.Add(this.objMenu);
            this.Controls.Add(this.trvAvailableChars);
            this.Controls.Add(this.mnuMenu);
            this.Icon = global::OpenHeroSelectGUI.Properties.Resources.SHIELD_Logo_GUI;
            this.KeyPreview = true;
            this.MainMenuStrip = this.mnuMenu;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Main";
            this.Text = "Open Hero Select GUI";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.shortcuts_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.mnuMenu.ResumeLayout(false);
            this.mnuMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void trvAvailableChars_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                TrvAvailableChars_DoubleClick(this, e);
        }

        private void lstSelected_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Delete))
            {
                BtnRemove_Click(this, e);
            }
        }
        private void shortcuts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode.Equals(Keys.O))
            {
                MnuLoad_Click(this, e);
            }
            else if (e.Control && e.KeyCode.Equals(Keys.S))
            {
                MnuSaveAs_Click(this, e);
            }
            else if (e.KeyCode.Equals(Keys.F5))
            {
                BtnReload_Click(this, e);
            }
            else if (e.KeyCode.Equals(Keys.L))
            {
                MenuDefaultChars_Click(this, e);
            }
            else if (e.KeyCode.Equals(Keys.S))
            {
                MnuSave_Click(this, e);
            }
            else
            {
                e.Handled = false;
            }
        }


        private void lstSelected_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
        {
            if(lstSelected.ListViewItemSorter==null)
                lstSelected.ListViewItemSorter = lvwColumnSorter;

            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.lstSelected.Sort();

        }

        #endregion

        private System.Windows.Forms.TreeView trvAvailableChars;
        private Menu objMenu;
        private System.Windows.Forms.TextBox txtPosition;
        private System.Windows.Forms.ListView lstSelected;
        private System.Windows.Forms.ColumnHeader Position;
        private System.Windows.Forms.ColumnHeader Char;
        private System.Windows.Forms.ColumnHeader Path;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.TextBox txtDebug;
        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();
        private Button btnReload;
        private Label lblAvailableChars;
        private Label lblCurrentPos;
        private Label lblSelectedChar;
        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
        private Button btnRemoveAll;
        private Button btnClean;
        private MenuStrip mnuMenu;
        private ToolStripMenuItem mnuFile;
        private ToolStripMenuItem mnuLoad;
        private ToolStripMenuItem mnuSave;
        private ToolStripMenuItem mnuSaveAs;
        private ToolStripMenuItem mnuExit;
        private SaveFileDialog saveFileDialog;
        private OpenFileDialog openFileDialog;
        private ToolStripMenuItem menuDefaultChars;
    }
}

