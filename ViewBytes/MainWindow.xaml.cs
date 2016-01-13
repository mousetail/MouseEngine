using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace ViewBytes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string filename= "\C:\Users\Maurits\IF\MouseEngine\output.ulx";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void fire(object sender, EventArgs e)
        {
            clear();
            restart();
            //Scroll1
        }

        void restart()
        {
            clear();
            FileStream f=File.Open(filename, FileMode.Open);
            BinaryReader r=new BinaryReader(f);
            byte[] f=r.re
            f.Close();

            Label l3;

            foreach (byte b in bytes)
            {
                l3 = new Label();
                l3.Content = b.ToString();
                //Scroll1.addChild(l3);
                Scroll1.Content.GetType();
            }
        }

        void clear()
        {
            while (this.VisualChildrenCount != 0)
            {
                this.RemoveVisualChild(this.GetVisualChild(0));
            }
        }


    }
}
