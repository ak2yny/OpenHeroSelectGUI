using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace WFA_MUA
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="http://marvelmods.com/forum/index.php?topic=732.196 "/>
    public partial class Menu : UserControl
    {
        public delegate void delegateDoubleClickChar (string name, int pos);
        public event delegateDoubleClickChar OnDoubleClickChar;

        private TextboxChar[] all;
        public Menu()
        {
            InitializeComponent();
            
            all = new TextboxChar[] { null, 
                txtC01, txtC02, txtC03, txtC04, txtC05, txtC06, txtC07, txtC08, txtC09, txtC10, 
                txtC11, txtC12, txtC13, txtC14, txtC15, txtC16, txtC17, txtC18, txtC19, txtC20, 
                txtC21, txtC22, txtC23, txtC24, txtC25, txtC26, 
                txtC96 
            };

            foreach (TextboxChar txt in all)
            {
                if (txt != null)
                {
                    txt.Click += new EventHandler(txt_Click);
                    txt.DoubleClick += new EventHandler(txt_Click);
                    txt.MouseMove += new MouseEventHandler(txt_MouseHover);
                    txt.MouseHover += new EventHandler(txt_MouseHover);
                    txt.MouseLeave += new EventHandler(txt_MouseLeave);
                }
            }
        }

        void txt_MouseLeave(object sender, EventArgs e)
        {
            txtCurrent.Text = "(put the mouse over some block)";
        }
        private void txt_MouseHover(object sender, EventArgs e)
        {
            TextboxChar txt = (TextboxChar)sender;
            txtCurrent.Text = txt.CharName;
        }
        private void txt_Click(object sender, EventArgs e)
        {
            TextboxChar txt = (TextboxChar)sender;
            string name = txt.CharName;
            int pos = Int32.Parse(txt.Text);
            
            if (OnDoubleClickChar!=null)
                OnDoubleClickChar(name, pos);

        }
        private void Menu_Load(object sender, EventArgs e)
        {

        }
        public TextboxChar getTextbox(int i)
        {
            if (i >= 1 && i <= 27)
            {
                return all[i];
            }
            else if (i == 96)
            {
                return all[27];
            }
            return null;
        }
        public void setTextbox(int i, string text)
        {
            TextboxChar txt = getTextbox(i);
            if (txt != null)
            {
                txt.CharName = text;
                if (text != "")
                {
                    txt.ForeColor = Color.White;
                    txt.BackColor = Color.Black;
                }
                else
                {
                    txt.ForeColor = Color.Black;
                    txt.BackColor = Color.White;
                }
            }
        }

    }
    public class TextboxChar : System.Windows.Forms.TextBox
    {
        private string charName;
        public string CharName
        {
            get
            {
                return charName;
            }
            set
            {
                charName = value;
            }
        }
    }
}
