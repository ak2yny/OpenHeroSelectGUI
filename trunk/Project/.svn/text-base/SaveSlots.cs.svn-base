using System;
using System.Windows.Forms;
using System.Collections;
namespace WFA_MUA
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

            addSaveSlots(this.DropDownItems);
        }
        private void addSaveSlots(ToolStripItemCollection items)
        {
            //test(mnuSaveSlots.DropDownItems);
            ToolStripMenuItem mnuAll = new ToolStripMenuItem();
            // 
            // mnuAll
            // 
            mnuAll.Name = "mnuAll";
            mnuAll.Size = new System.Drawing.Size(152, 22);
            mnuAll.Text = "All";
            mnuAll.Click += new EventHandler(this.mnuAll_Click);
            items.Add(mnuAll);
            
            for (int i = 1; i <= 20; i++)
            {
                ToolStripMenuItem mnuSlot;
                mnuSlot = new ToolStripMenuItem();
                mnuSlot.Name = "mnuSlot" + i;
                mnuSlot.Size = new System.Drawing.Size(152, 22);
                mnuSlot.Text = "Slot " + i;
                mnuSlot.Click += new System.EventHandler(this.mnuSlot_Click);
                items.Add(mnuSlot);
            }
        }
        private void mnuSlot_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem slot = sender as ToolStripMenuItem;
            slot.Checked = !slot.Checked;
        }
        private void mnuAll_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem slot in this.DropDownItems) 
            {
                if (slot.Name.StartsWith("mnuSlot"))
                    slot.Checked = false;
            }
        }
        public void cleanAll()
        {
            mnuAll_Click(null, null);
        }
        public void setChecked(int i)
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
