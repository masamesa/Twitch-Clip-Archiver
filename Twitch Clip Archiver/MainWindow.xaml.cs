using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace Twitch_Clip_Archiver
{
    using Microsoft.Win32;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using Twitch_Clip_Archiver.Extensions;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "Open JSON";
            fd.Filter = "JSON File (*.json)|*.JSON";
            if (fd.ShowDialog() == false)
                return;


            SaveFileDialog fe = new SaveFileDialog();
            fe.ValidateNames = false;
            fe.CheckFileExists = false;
            fe.CheckPathExists = false;
            fe.FileName = "Save Here";
            fe.Title = "Save video";

            if (fe.ShowDialog() == false)
                return;


            FetchClips fc = new FetchClips();
            AllocConsole();
            fc.Fetch(TextClientID.Password, TextTwitchName.Text, Path.GetDirectoryName(fe.FileName), fd.FileName);
        }

        private void Fetch_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fe = new SaveFileDialog();
            fe.ValidateNames = false;
            fe.CheckFileExists = false;
            fe.CheckPathExists = false;
            fe.FileName = "Save Here";
            fe.Title = "Save video";

            if (fe.ShowDialog() == false)
                return;

            FetchClips fc = new FetchClips();
            AllocConsole();
            fc.Fetch(TextClientID.Password, TextTwitchName.Text, Path.GetDirectoryName(fe.FileName));

        }
    }
}
