using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenHeroSelectGUI
{
    /// <summary>
    /// Menulocations. Should be adjusted to accept location setup from ini files.
    /// </summary>
    /// <seealso cref="http://marvelmods.com/forum/index.php?topic=732.196 "/>
    public partial class Menu : UserControl
    {
        public delegate void delegateDoubleClickChar (string name, int pos);
        public event delegateDoubleClickChar? OnDoubleClickChar;

        private readonly MenulocationBoxes[] all;
        public Menu()
        {
            InitializeComponent();
            
            /// This needs to be reworked, the list should support infinite (or at least 50) characters.
            all = new MenulocationBoxes[] { txtC01,
                txtC01, txtC02, txtC03, txtC04, txtC05, txtC06, txtC07, txtC08, txtC09, txtC10,
                txtC11, txtC12, txtC13, txtC14, txtC15, txtC16, txtC17, txtC18, txtC19, txtC20,
                txtC21, txtC22, txtC23, txtC24, txtC25, txtC26,
                txtC96
            };

            /// OHS uses XML and JSON file extension. The txt variable should probably be changed.
            foreach (MenulocationBoxes loc in all)
            {
                if (loc != null)
                {
                    loc.Click += new EventHandler(Box_Click);
                    loc.DoubleClick += new EventHandler(Box_Click);
                    loc.MouseMove += new MouseEventHandler(Box_MouseHover);
                    loc.MouseHover += new EventHandler(Box_MouseHover);
                    loc.MouseLeave += new EventHandler(Box_MouseLeave);
                }
            }
        }

        void Box_MouseLeave(object sender, EventArgs e)
        {
            txtCurrent.Text = "";
        }
        private void Box_MouseHover(object sender, EventArgs e)
        {
            MenulocationBoxes txt = (MenulocationBoxes)sender;
            txtCurrent.Text = txt.CharName;
        }
        private void Box_Click(object sender, EventArgs e)
        {
            MenulocationBoxes txt = (MenulocationBoxes)sender;
            string name = txt.CharName;
            int pos = Int32.Parse(txt.Text);

            OnDoubleClickChar?.Invoke(name, pos);

        }
        private void Menu_Load(object sender, EventArgs e)
        {
            /// Nothing declared. I had to fix an error message, no idea what this does, yet.
        }

        public MenulocationBoxes GetMenulocationBox(int i)
        {
            if (i == 96) return all[27];
            return all[i];
        }
        public void SetMenulocationBox(int i, string text)
        {
            if (i == 96 || (i > 0 && i < 27))
            {
                MenulocationBoxes txt = GetMenulocationBox(i);
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
    public class MenulocationBoxes : TextBox
    {
        private string? charName;
        public string CharName
        {
            get
            {
                return charName is null ? "" : charName;
            }
            set
            {
                charName = value;
            }
        }
    }
}
