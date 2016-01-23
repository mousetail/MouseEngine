using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpcodeEdit
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ListBox lis = new ListBox();
            lis.Items.Add("1");
            lis.Parent = this;
            lis.Location = new Point(5, 5);
            lis.Size = new Size(100, 300);
            GroupBox gro = new OpcodeGroup();
            gro.Parent = this;
            gro.Location = new Point(110, 5);
            gro.Text = "First op code";
        }
    }

    class OpcodeGroup: GroupBox
    {
        List<TextBox> boxes=new List<TextBox>();
        FlowLayoutPanel panel;
        Button delButton;
        Label llabel;
        Random random=new Random();

        public OpcodeGroup ()
        {
            FlowLayoutPanel mflow = new FlowLayoutPanel();
            mflow.AutoSize = true;
            mflow.FlowDirection = FlowDirection.TopDown;
            mflow.Parent = this;
            FlowLayoutPanel f = new FlowLayoutPanel();
            this.AutoSize = true;
            f.AutoSize = true;
            f.Size = new Size(500, 300);
            f.Parent = mflow;
            mflow.Location = new Point(5, 15);
            Button but1 = new Button();
            but1.Text = "Add";
            but1.Parent = f;
            but1.Click += addBox;
            Button but2 = new Button();
            but2.Text = "Del";
            but2.Parent = f;
            but2.Click += RemoveBox;
            but2.Enabled = false;
            delButton = but2;
            panel = f;

            llabel = new Label();
            llabel.AutoSize = true;
          ;
            llabel.Parent = mflow;

            UpdateLabel();

        }

        private void UpdateLabel()
        {
            if (boxes.Count == 0)
            {
                llabel.Text = "Example: ";
                return;
            }
            string s = "";
            foreach (TextBox t in boxes)
            {
                s += t.Text;
                s += random.Next() % 10;
            }
            llabel.Text = "Example: \"" + s.Substring(0, s.Length - 1)+"\"";
        }

        public void OnEdit(object sender, EventArgs arg)
        {
            UpdateLabel();
        }

        public void addBox(object sender, EventArgs args)
        {
            TextBox box = new TextBox();
            box.Parent = panel;
            box.TextChanged += OnEdit;
            boxes.Add(box);
            delButton.Enabled = true;
            delButton.Parent = null;
            delButton.Parent = panel;
            UpdateLabel();
            
            
        }

        public void RemoveBox(object sender, EventArgs arg)
        {
            if (boxes.Count > 0)
            {
                TextBox b = boxes[boxes.Count - 1];
                boxes.RemoveAt(boxes.Count - 1);
                b.Parent = null;
            }
            if (boxes.Count == 0)
            {
                delButton.Enabled = false;
            }

            UpdateLabel();
            
        }
    }
}
