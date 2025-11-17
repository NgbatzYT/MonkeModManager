using MonkeModManager.Internals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonkeModManager
{
    public partial class BackupManager : Form
    {
        public BackupManager()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e) => Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups"));


        private void button3_Click(object sender, EventArgs e) //delete backup
        {
            if (listView1.CheckedItems == null) { return; }

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups");

            var a = MessageBox.Show($"Once a backup is deleted it's gone forever you can't recover it! This is a permanent action. Make sure this is the correct backup before deleting. Backup Name: {listView1.CheckedItems[0].Text}", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (a == DialogResult.Yes) {
                File.Delete(Path.Combine(dir, listView1.CheckedItems[0].Text));
                Init();
            }        
        }

        private void button2_Click(object sender, EventArgs e) //restroe backup
        {
            if (listView1.CheckedItems == null) { return; }

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups");
            var a = MessageBox.Show("Restoring a backup can break some of your currently installed mods so we recommend making a backup before restoring.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (a == DialogResult.Yes) {
                UnzipFile(File.ReadAllBytes(Path.Combine(dir, listView1.CheckedItems[0].Text)), Path.Combine(Form1.InstallDirectory, "BepInEx"));
                Form1.instance.GetInstalledMods();
                Init();
            }
        }

        private void UnzipFile(byte[] data, string directory)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (var unzip = new Unzip(ms))
                {
                    unzip.ExtractToDirectory(directory);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) // create backup
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups");
            ZipFile.CreateFromDirectory(Path.Combine(Form1.InstallDirectory, "BepInEx"), Path.Combine(dir, $"Backup-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.zip"));
            Init();
        }

        private void BackupManager_Load(object sender, EventArgs e) => Init();

        private void listView1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Index != e.Index)
                        item.Checked = false;
                }
            }
        }


        private void Init()
        {
            listView1.Items.Clear();

            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups"));

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonkeModManager", "Backups");

            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (Path.GetExtension(file).ToLower() == ".zip")
                    {
                        var name = Path.GetFileName(file);

                        if (name != null)
                        {
                            ListViewItem l = new(name);

                            l.SubItems.Add(File.GetCreationTime(file).ToString());
                            l.SubItems.Add(GetFileSize(file));

                            listView1.Items.Add(l);
                        }
                    }
                }
            }
        }

        public string GetFileSize(string filePath)
        {
            double s = new FileInfo(filePath).Length;
            string[] ss = ["B", "KB", "MB", "GB", "BRUH"];

            var thingy = 0;

            while (s >= 1024 && thingy < ss.Length) { 
                thingy++;
                s /= 1024;
            }

            return $"{s:F2} {ss[thingy]}";
        }
    }
}
