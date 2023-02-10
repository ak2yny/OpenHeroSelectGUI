using System;
using System.Windows.Forms;
using System.Collections;
namespace OpenHeroSelectGUI
{
    public class SaveSlots : ToolStripMenuItem
    {
        public SaveSlots() 
        {
            // 
            // mnuSaveSlots
            // 
            this.Name = "mnuSaveSlots";
            this.Size = new System.Drawing.Size(69, 20);
            this.Text = "Save Slots";

            AddSaveSlots(this.DropDownItems);
        }
        private void AddSaveSlots(ToolStripItemCollection items)
        {
            //test(mnuSaveSlots.DropDownItems);
            ToolStripMenuItem mnuAll = new ToolStripMenuItem
            {
                BackColor = System.Drawing.SystemColors.Window,
                Name = "mnuAll",
                Size = new System.Drawing.Size(152, 22),
                Text = "All"
            };
            mnuAll.Click += new EventHandler(this.MnuAll_Click);
            items.Add(mnuAll);
            
            for (int i = 1; i <= 10; i++)
            {
                ToolStripMenuItem mnuSlot;
                mnuSlot = new ToolStripMenuItem
                {
                    BackColor = System.Drawing.SystemColors.Window,
                    Name = "mnuSlot" + i,
                    Size = new System.Drawing.Size(152, 22),
                    Text = "Slot " + i
                };
                mnuSlot.Click += new EventHandler(this.MnuSlot_Click);
                items.Add(mnuSlot);
            }
        }
        private void MnuSlot_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem slot = sender as ToolStripMenuItem;
            slot.Checked = !slot.Checked;
        }
        private void MnuAll_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem slot in this.DropDownItems) 
            {
                if (slot.Name.StartsWith("mnuSlot"))
                    slot.Checked = false;
            }
        }
        public void CleanAll()
        {
            MnuAll_Click(null, null);
        }
        public void SetChecked(int i)
        {
            foreach (ToolStripMenuItem slot in this.DropDownItems) 
            {
                if (slot.Name.Equals("mnuSlot" + i))
                    slot.Checked = true;
            }
        }
        public IList SelectedItems
        {
            get {
                IList list = new ArrayList();
                foreach (ToolStripMenuItem slot in this.DropDownItems) 
                {
                    if (slot.Name.StartsWith("mnuSlot") && slot.Checked) 
                    {
                        list.Add(slot);
                    }
                }
                return list;
            }
        }
    }
}
