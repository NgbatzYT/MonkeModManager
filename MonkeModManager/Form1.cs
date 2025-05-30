﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using MonkeModManager.Internals;
using MonkeModManager.Internals.SimpleJSON;

namespace MonkeModManager
{
    public partial class Form1 : Form
    {
        
        private string DefaultOculusInstallDirectory = @"C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag";
        private string DefaultSteamInstallDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag";
        public static string InstallDirectory = @"";
        Dictionary<string, int> groups = new Dictionary<string, int>();
        Dictionary<string, int> groupss = new Dictionary<string, int>();
        private List<ReleaseInfo> releases;
        private List<ReleaseInfo> releasesA;
        private bool modsDisabled;
        private string DefaultDoorstopPath = @"target_assembly=BepInEx\core\BepInEx.Preloader.dll";
        private int CurrentVersion = 8; // actual version is 2.4.0.0 // (big changes update).(Feature update).(minor update).(hotfix)
        public bool InstanceEnabled;
        public bool AutoUpdateEnabled;
        public float ClickerMoney = 0f;
        public float ClickPower = 1f;
        public readonly string VersionNumber = "2.4.0.0";
        private int monkeAmount = 5;
        
        public Form1() => InitializeComponent();
        
        private void buttonFolderBrowser_Click(object sender, EventArgs e)
        {
            using (var fileDialog = new OpenFileDialog())
            {
                fileDialog.FileName = "Gorilla Tag.exe";
                fileDialog.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*";
                fileDialog.FilterIndex = 1;
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = fileDialog.FileName;
                    if (Path.GetFileName(path).Equals("Gorilla Tag.exe"))
                    {
                        InstallDirectory = Path.GetDirectoryName(path);
                        textBoxDirectory.Text = InstallDirectory;
                        EditmmmConfig(InstallDirectory);
                    }
                    else
                        MessageBox.Show("That's not Gorilla Tag.exe! please try again!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonInstall_Click(object sender, EventArgs e) => new Thread(Install).Start();

        private void Install()
        {
            try
            {
                if (InstanceEnabled)
                {
                    UpdateStatus("Changing Unity Doorstop config");
                    DoorstopInstanceChange(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{checkedListBox1.CheckedItems[0]}\BepInEx\core\BepInEx.Preloader.dll"));
                    ChangeInstallButtonState(false);
                    UpdateStatus("Starting install sequence...");
                    foreach (ReleaseInfo release in releases)
                    {
                        if (release.Install)
                        {
                            UpdateStatus(string.Format("Downloading...{0}", release.Name));
                            byte[] file = DownloadFile(release.Link);
                            UpdateStatus(string.Format("Installing...{0}", release.Name));
                            string fileName = Path.GetFileName(release.Link);
                            if (Path.GetExtension(fileName).Equals(".dll"))
                            {
                                string dir;
                                if (release.InstallLocation == null)
                                {
                                    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{checkedListBox1.CheckedItems[0]}\BepInEx\plugins",
                                        Regex.Replace(release.Name, @"\s+", string.Empty));
                                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                }
                                else
                                {
                                    dir = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mmmInstances\{checkedListBox1.CheckedItems[0]}\", release.InstallLocation);
                                }
                                File.WriteAllBytes(Path.Combine(dir, fileName), file);

                                var dllFile = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mmmInstances\{checkedListBox1.CheckedItems[0]}\", @"BepInEx\plugins", fileName);
                                if (File.Exists(dllFile))
                                {
                                    File.Delete(dllFile);
                                }
                            }
                            else
                            {
                                UnzipFile(file,
                                    (release.InstallLocation != null)
                                        ? Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mmmInstances\{checkedListBox1.CheckedItems[0]}\", release.InstallLocation)
                                        : $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\mmmInstances\{checkedListBox1.CheckedItems[0]}\");
                            }
                            UpdateStatus(string.Format("Installed {0}!", release.Name));
                        }
                    }
                }
                else
                {
                    DoorstopInstanceChange(DefaultDoorstopPath);
                    ChangeInstallButtonState(false);
                    UpdateStatus("Starting install sequence...");
                    foreach (ReleaseInfo release in releases)
                    {
                        if (release.Install)
                        {
                            UpdateStatus(string.Format("Downloading...{0}", release.Name));
                            byte[] file = DownloadFile(release.Link);
                            UpdateStatus(string.Format("Installing...{0}", release.Name));
                            string fileName = Path.GetFileName(release.Link);
                            if (Path.GetExtension(fileName).Equals(".dll"))
                            {
                                string dir;
                                if (release.InstallLocation == null)
                                {
                                    dir = Path.Combine(InstallDirectory, @"BepInEx\plugins", Regex.Replace(release.Name, @"\s+", string.Empty));
                                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                }
                                else
                                {
                                    dir = Path.Combine(InstallDirectory, release.InstallLocation);
                                }
                                File.WriteAllBytes(Path.Combine(dir, fileName), file);

                                var dllFile = Path.Combine(InstallDirectory, @"BepInEx\plugins", fileName);
                                if (File.Exists(dllFile))
                                {
                                    File.Delete(dllFile);
                                }
                            }
                            else
                            {
                                UnzipFile(file, (release.InstallLocation != null) ? Path.Combine(InstallDirectory, release.InstallLocation) : InstallDirectory);
                            }
                            UpdateStatus(string.Format("Installed {0}!", release.Name));
                        }
                    }
                }
                UpdateStatus("Install complete!");
                ChangeInstallButtonState(true);
                this.Invoke((MethodInvoker)(() =>
                {
                    //Invoke so we can call from any thread
                    buttonToggleMods.Enabled = true;
                }));
                AddonInstall();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddonInstall()
        {
            try
            {
                UpdateStatus("Installing Addons...");
                ChangeInstallButtonState(false);
                foreach (ReleaseInfo release in releasesA)
                {
                    if (release.Install)
                    {
                        string dir;
                        UpdateStatus(string.Format("Downloading...{0}", release.Name));
                        byte[] file = DownloadFile(release.Link);
                        UpdateStatus(string.Format("Installing...{0}", release.Name));
                        string fileName = Path.GetFileName(release.Link);
                        dir = release.InstallLocation == null ? Path.Combine(InstallDirectory, @"BepInEx\plugins", fileName) : Path.Combine(InstallDirectory, release.InstallLocation);
                        if(!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        File.WriteAllBytes(Path.Combine(dir, fileName), file);
                    }
                }
                UpdateStatus("Install complete!");
                ChangeInstallButtonState(true);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        private void ChangeInstallButtonState(bool enabled)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                buttonInstall.Enabled = enabled;
            }));
        }
        
        private byte[] DownloadFile(string url)
        {
            WebClient client = new WebClient();
            client.Proxy = null;
            return client.DownloadData(url);
        }
        
        private void listViewMods_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ReleaseInfo release = (ReleaseInfo)e.Item.Tag;

            if (release.Dependencies.Count > 0)
            {
                foreach (ListViewItem item in listViewMods.Items)
                {
                    ReleaseInfo plugin = (ReleaseInfo)item.Tag;

                    if (plugin.Name == release.Name) continue;

                    // if this depends on plugin
                    if (release.Dependencies.Contains(plugin.Name))
                    {
                        if (e.Item.Checked)
                        {
                            item.Checked = true;
                            item.ForeColor = System.Drawing.Color.DimGray;
                        }
                        else
                        {
                            release.Install = false;
                            if (releases.Count(x => plugin.Dependents.Contains(x.Name) && x.Install) <= 1)
                            {
                                item.Checked = false;
                                item.ForeColor = System.Drawing.Color.Black;
                            }
                        }
                    }
                }
            }

            // don't allow user to uncheck if a dependent is checked
            if (release.Dependents.Count > 0)
            {
                if (releases.Count(x => release.Dependents.Contains(x.Name) && x.Install) > 0)
                {
                    e.Item.Checked = true;
                }
            }

            if (release.Name.Contains("BepInEx")) { e.Item.Checked = true; };
            release.Install = e.Item.Checked;
        }        
        private void listViewed_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ReleaseInfo release = (ReleaseInfo)e.Item.Tag;

            if (release.Dependencies.Count > 0)
            {
                foreach (ListViewItem item in listViewed.Items)
                {
                    ReleaseInfo plugin = (ReleaseInfo)item.Tag;

                    if (plugin.Name == release.Name) continue;
                    
                    if (release.Dependencies.Contains(plugin.Name))
                    {
                        if (e.Item.Checked)
                        {
                            item.Checked = true;
                            item.ForeColor = Color.DimGray;
                        }
                        else
                        {
                            release.Install = false;
                            if (releases.Count(x => plugin.Dependents.Contains(x.Name) && x.Install) <= 1)
                            {
                                item.Checked = false;
                                item.ForeColor = Color.Black;
                            }
                        }
                    }
                }
            }

            // don't allow user to uncheck if a dependent is checked
            if (release.Dependents.Count > 0)
            {
                if (releases.Count(x => release.Dependents.Contains(x.Name) && x.Install) > 0)
                {
                    e.Item.Checked = true;
                }
            }

            if (release.Name.Contains("BepInEx")) { e.Item.Checked = true; };
            release.Install = e.Item.Checked;
        }
        private void listViewMods_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            buttonModInfo.Enabled = listViewMods.SelectedItems.Count > 0;
        }
        void listViewMods_DoubleClick(object sender, EventArgs e) => OpenLinkFromRelease();
        void viewInfoToolStripMenuItem_Click(object sender, EventArgs e) => OpenLinkFromRelease();
        void buttonDiscordLink_Click(object sender, EventArgs e) => Process.Start("https://discord.gg/monkemod");
        
        private void buttonOpenGameFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(InstallDirectory))
                Process.Start(InstallDirectory);
        }

        private void buttonOpenConfigFolder_Click(object sender, EventArgs e)
        {
            var configDirectory = Path.Combine(InstallDirectory, @"BepInEx\config");
            if (Directory.Exists(configDirectory))
                Process.Start(configDirectory);
        }

        private void buttonOpenBepInExFolder_Click(object sender, EventArgs e)
        {
            var pluginsDirectory = Path.Combine(InstallDirectory, "BepInEx/plugins");
            if (Directory.Exists(pluginsDirectory))
                Process.Start(pluginsDirectory);
        }
        void buttonUninstallAll_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show(
                "You are about to delete all your mods. This cannot be undone!\n\nAre you sure you wish to continue?",
                "Confirm Delete",
                MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                UpdateStatus("Uninstalling all mods");
                
                var pluginsPath = Path.Combine(InstallDirectory, @"BepInEx\plugins");
                
                try
                {
                    foreach (var d in Directory.GetDirectories(pluginsPath))
                    {
                        Directory.Delete(d, true);
                    }

                    foreach (var f in Directory.GetFiles(pluginsPath))
                    {
                        File.Delete(f);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Something went wrong! Error: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Failed to uninstall mods.");
                    return;
                }

                UpdateStatus("All mods uninstalled successfully!");
            }
        }
        void buttonModInfo_Click(object sender, EventArgs e) => OpenLinkFromRelease();
        
        private void OpenLinkFromRelease()
        {
            if (listViewMods.SelectedItems.Count > 0)
            {
                ReleaseInfo release = (ReleaseInfo)listViewMods.SelectedItems[0].Tag;
                UpdateStatus($"Opening GitHub page for {release.Name}");
                Process.Start(string.Format("https://github.com/{0}", release.GitPath));
            }
            
        }
        private void buttonToggleMods_Click(object sender, EventArgs e)
        {
            if (modsDisabled)
            {
                if (File.Exists(Path.Combine(InstallDirectory, "mods.disable")))
                {
                    File.Move(Path.Combine(InstallDirectory, "mods.disable"), Path.Combine(InstallDirectory, "winhttp.dll"));
                    buttonToggleMods.Text = "Disable Mods";
                    buttonToggleMods.BackColor = System.Drawing.Color.Transparent;
                    modsDisabled = false;
                    UpdateStatus("Enabled mods!");
                }
            }
            else
            {
                if (File.Exists(Path.Combine(InstallDirectory, "winhttp.dll")))
                {
                    File.Move(Path.Combine(InstallDirectory, "winhttp.dll"), Path.Combine(InstallDirectory, "mods.disable"));
                    buttonToggleMods.Text = "Enable Mods";
                    buttonToggleMods.BackColor = System.Drawing.Color.IndianRed;
                    modsDisabled = true;
                    UpdateStatus("Disabled mods!");
                }
            }
        }
        void Form1_Load(object sender, EventArgs e)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonkeUpdater");
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            File.WriteAllText(Path.Combine(path, "exepath.txt"), exePath);
            
            InstanceEnabled = Properties.Settings.Default.Instance;
            checkBox1.Checked = InstanceEnabled;
            AutoUpdateEnabled = Properties.Settings.Default.AutoUpdate;
            checkBox3.Checked = AutoUpdateEnabled;
            InstallDirectory = Properties.Settings.Default.InstallDirectory;
            checkBox1.Checked = InstanceEnabled;
            
            releases = [];
            releasesA = [];

            labelVersion.Text = $@"Monke Mod Manager v{VersionNumber}";
            
            if (InstallDirectory != @"" && Directory.Exists(InstallDirectory))
            {
                textBoxDirectory.Text = InstallDirectory;
            }
            else if(File.Exists(Path.Combine(DefaultSteamInstallDirectory, "Gorilla Tag.exe")))
            {
                InstallDirectory = DefaultSteamInstallDirectory;
                textBoxDirectory.Text = InstallDirectory;
                EditmmmConfig(InstallDirectory);
            }
            else if (File.Exists(Path.Combine(DefaultOculusInstallDirectory, "Gorilla Tag.exe")))
            {
                InstallDirectory = DefaultOculusInstallDirectory;
                textBoxDirectory.Text = InstallDirectory;
                EditmmmConfig(InstallDirectory);
            }
            else
            {
                ShowErrorFindingDirectoryMessage();
            }
            
            ConfigFix();
            
            AddInstancesToList();
            
            if (!File.Exists(Path.Combine(InstallDirectory, "winhttp.dll")))
            {
                if (File.Exists(Path.Combine(InstallDirectory, "mods.disable")))
                {
                    buttonToggleMods.Text = "Enable Mods";
                    modsDisabled = true;
                    buttonToggleMods.BackColor = System.Drawing.Color.IndianRed;
                    buttonToggleMods.Enabled = true;
                }
                else
                {
                    buttonToggleMods.Enabled = false;
                }
            }
            else
            {
                buttonToggleMods.Enabled = true;
            }
            new Thread(LoadRequiredPlugins).Start();
            
            if (AutoUpdateEnabled)
            {
                UpdateStatus("Checking for updates...");
                Int16 version = Convert.ToInt16(DownloadSite("https://raw.githubusercontent.com/NgbatzYT/MonkeModManager/master/update"));
                if (version > CurrentVersion)
                {
                    if(File.Exists(Path.Combine(path, @"MonkeUpdater\MonkeUpdater.exe")))
                    {
                        Process.Start(Path.Combine(path, "MonkeUpdater.exe"));
                    }
                    else
                    {
                        var ea = DownloadFile("https://github.com/NgbatzYT/MonkeUpdater/releases/latest/download/MonkeUpdater.exe");
                        File.WriteAllBytes(Path.Combine(path, "MonkeUpdater.exe"), ea);
                        Process.Start(Path.Combine(path, "MonkeUpdater.exe"));
                    }
                    Environment.Exit(0);
                }
            }
        }
        
        private void UpdateStatus(string status)
        {
            string formattedText = $"Status: {status}";
            this.Invoke((MethodInvoker)(() =>
            { //Invoke so we can call from any thread
                labelStatus.Text = formattedText;
            }));
        }
        
        private CookieContainer PermCookie;
        private string DownloadSite(string URL)
        {
            try
            {
                PermCookie ??= new CookieContainer();
                HttpWebRequest RQuest = (HttpWebRequest)HttpWebRequest.Create(URL);
                RQuest.Method = "GET";
                RQuest.KeepAlive = true;
                RQuest.CookieContainer = PermCookie;
                RQuest.ContentType = "application/x-www-form-urlencoded";
                RQuest.Referer = "";
                RQuest.UserAgent = "Monke-Mod-Manager";
                RQuest.Proxy = null;
                HttpWebResponse Response = (HttpWebResponse)RQuest.GetResponse();
                StreamReader Sr = new StreamReader(Response.GetResponseStream());
                string Code = Sr.ReadToEnd();
                Sr.Close();
                return Code;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message.Contains("403")
                        ? "Failed to fetch info, GitHub has most likely rate limited you, please check back in 15 - 30 minutes"
                        : "Failed to fetch info, please check your internet connection", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("You will still be able to use MMM but you won't be able to install mods.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus("Failed to fetch info");
                return null;
            }
        }

        #region LoadReleases
        
        private void LoadReleases()
        {
            var decodedGroups = JSON.Parse(DownloadSite("https://raw.githubusercontent.com/The-Graze/MonkeModInfo/master/groupinfo.json"));
            var decodedMods = JSON.Parse(DownloadSite("https://raw.githubusercontent.com/The-Graze/MonkeModInfo/master/modinfo.json"));

            
            var allMods = decodedMods.AsArray;
            var allGroups = decodedGroups.AsArray;

            for (int i = 0; i < allMods.Count; i++)
            {
                JSONNode current = allMods[i];
                ReleaseInfo release = new ReleaseInfo(current["name"], current["author"], current["version"], current["group"], current["download_url"], current["install_location"], current["git_path"], current["mod"] ,current["dependencies"].AsArray);
                //UpdateReleaseInfo(ref release);
                releases.Add(release);
            }


            IOrderedEnumerable<KeyValuePair<string, JSONNode>> keyValuePairs = allGroups.Linq.OrderBy(x => x.Value["rank"]);
            for (int i = 0; i < allGroups.Count; i++)
            {
                JSONNode current = allGroups[i];
                if (releases.Any(x => x.Group == current["name"]))
                {
                    groups.Add(current["name"], groups.Count());
                }
            }
            groups.Add("Uncategorized", groups.Count());

            foreach (ReleaseInfo release in releases)
            {
                foreach (string dep in release.Dependencies)
                {
                    releases.Where(x => x.Name == dep).FirstOrDefault()?.Dependents.Add(release.Name);
                }
            }
        }
        
        private void LoadReleasesAddon()
        {
            var adecodedMods = JSON.Parse(DownloadSite("https://raw.githubusercontent.com/NgbatzYT/MonkeModInfo/master/addoninfo.json"));
            var adecodedGroups = JSON.Parse(DownloadSite("https://raw.githubusercontent.com/NgbatzYT/MonkeModInfo/master/addonmodinfo.json"));

            
            var aallMods = adecodedMods.AsArray;
            var aallGroups = adecodedGroups.AsArray;

            for (int i = 0; i < aallMods.Count; i++)
            {
                JSONNode current = aallMods[i];
                ReleaseInfo release = new ReleaseInfo(current["name"], current["author"], current["version"], current["group"], current["download_url"], current["install_location"], current["git_path"], current["mod"] ,current["dependencies"].AsArray);
                //UpdateReleaseInfo(ref release);
                releasesA.Add(release);
            }

            IOrderedEnumerable<KeyValuePair<string, JSONNode>> keyValuePairs = aallGroups.Linq.OrderBy(x => x.Value["rank"]);
            for (int i = 0; i < aallGroups.Count; i++)
            {
                JSONNode current = aallGroups[i];
                if (releasesA.Any(x => x.Group == current["name"]))
                {
                    groupss.Add(current["name"], groupss.Count());
                }
            }
            groupss.Add("Uncategorized", groupss.Count);

            foreach (ReleaseInfo release in releasesA)
            {
                foreach (string dep in release.Dependencies)
                {
                    releasesA.Where(x => x.Name == dep).FirstOrDefault()?.Dependents.Add(release.Name);
                }
            }
        }
        
        #endregion
        public static void ConfigFix()
        {
            if (!File.Exists(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg")))
            {
                return;
            }

            string c = File.ReadAllText(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"));
            if (!c.Contains("HideManagerGameObject = false"))
            {
                return;
            }
               
            string e = c.Replace("HideManagerGameObject = false", "HideManagerGameObject = true");
            File.WriteAllText(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"), e);
        }        
        
        private static void DoorstopInstanceChange(string instancePath)
        {
            if (!File.Exists(Path.Combine(InstallDirectory, @"doorstop_config.ini")))
            {
                return;
            }

            string c = File.ReadAllText(Path.Combine(InstallDirectory, @"doorstop_config.ini"));
               
            string e = c.Replace(@"target_assembly=BepInEx\core\BepInEx.Preloader.dll", $@"target_assembly={instancePath}");
            File.WriteAllText(Path.Combine(InstallDirectory, @"doorstop_config.ini"), e);
        }
        
        private void AddInstancesToList()
        {
            string instancesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mmmInstances");
            if (!Directory.Exists(instancesPath))  
                Directory.CreateDirectory(instancesPath);
            
            checkedListBox1.Items.Clear();
            
            foreach (var directory in Directory.GetDirectories(instancesPath))
            {
                string folderName = Path.GetFileName(directory);
                checkedListBox1.Items.Add(folderName);
            }
        }

        #region LoadRequired
        
        private void LoadRequiredPlugins()
        {
            UpdateStatus("Getting latest version info...");
            LoadReleases();
            this.Invoke((MethodInvoker)(() =>
            {//Invoke so we can call from current thread
             //Update checkbox's text
                Dictionary<string, int> includedGroups = new Dictionary<string, int>();

                for (int i = 0; i < groups.Count(); i++)
                {
                    var key = groups.First(x => x.Value == i).Key;
                    var value = listViewMods.Groups.Add(new ListViewGroup(key, HorizontalAlignment.Left));
                    groups[key] = value;
                }

                foreach (ReleaseInfo release in releases)
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = release.Name;
                    if (!String.IsNullOrEmpty(release.Version)) item.Text = $"{release.Name} - {release.Version}";
                    if (!String.IsNullOrEmpty(release.Tag)) { item.Text = string.Format("{0} - ({1})",release.Name, release.Tag); };
                    item.SubItems.Add(release.Author);
                    item.Tag = release;
                    if (release.Install)
                    {
                        listViewMods.Items.Add(item);
                    }
                    CheckDefaultMod(release, item);

                    if (release.Group == null || !groups.ContainsKey(release.Group))
                    {
                        item.Group = listViewMods.Groups[groups["Uncategorized"]];
                    }
                    else if (groups.ContainsKey(release.Group))
                    {
                        int index = groups[release.Group];
                        item.Group = listViewMods.Groups[index];
                    }
                    else
                    {
                        //int index = listViewMods.Groups.Add(new ListViewGroup(release.Group, HorizontalAlignment.Left));
                        //item.Group = listViewMods.Groups[index];
                    }
                }

                tabControlMain.Enabled = true;
                buttonInstall.Enabled = true;

            }));
           
            UpdateStatus("Release info updated!");
            
            LoadRequiredPluginAddons();
        }
        
        private void LoadRequiredPluginAddons()
        {
            UpdateStatus("Getting latest addon info...");
            LoadReleasesAddon();
            this.Invoke((MethodInvoker)(() =>
            {
                var includedGroups = new Dictionary<string, int>();

                for (int i = 0; i < groupss.Count(); i++)
                {
                    var key = groupss.First(x => x.Value == i).Key;
                    var value = listViewed.Groups.Add(new ListViewGroup(key, HorizontalAlignment.Left));
                    groupss[key] = value;
                }

                foreach (ReleaseInfo release in releasesA)
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = release.Name;
                    if (!String.IsNullOrEmpty(release.Version)) item.Text = $"{release.Name} - {release.Version} - {release.Mod}";
                    if (!String.IsNullOrEmpty(release.Tag)) { item.Text = string.Format("{0} - ({1})",release.Name, release.Tag); };
                    item.SubItems.Add(release.Author);
                    item.SubItems.Add(release.Mod);
                    item.Tag = release;
                    if (release.Install)
                    {
                        listViewed.Items.Add(item);
                    }
                    
                    if (release.Group == null || !groupss.ContainsKey(release.Group))
                    {
                        item.Group = listViewed.Groups[groups["Uncategorized"]];
                    }
                    else if (groupss.ContainsKey(release.Group))
                    {
                        int index = groupss[release.Group];
                        item.Group = listViewed.Groups[index];
                    }
                    release.Install = false;
                }

                tabControlMain.Enabled = true;
                buttonInstall.Enabled = true;

            }));
           
            UpdateStatus("Release info updated!");
            
        }
                
        private void CheckDefaultMod(ReleaseInfo release, ListViewItem item)
        {
            if (release.Name.Contains("BepInEx"))
            {
                item.Checked = true;
                item.ForeColor = System.Drawing.Color.DimGray;
            }
            else
            {
                release.Install = false;
            }
        }
        
        
        #endregion
        #region location
        
        private void NotFoundHandler()
        {
            bool found = false;
            while (found == false)
            {
                using (var fileDialog = new OpenFileDialog())
                {
                    fileDialog.FileName = "Gorilla Tag.exe";
                    fileDialog.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*";
                    fileDialog.FilterIndex = 1;
                    if (fileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string path = fileDialog.FileName;
                        if (Path.GetFileName(path).Equals("Gorilla Tag.exe"))
                        {
                            InstallDirectory = Path.GetDirectoryName(path);
                            textBoxDirectory.Text = InstallDirectory;
                            found = true;
                            EditmmmConfig(InstallDirectory);
                        }
                        else
                            MessageBox.Show("That's not Gorilla Tag.exe! please try again!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                        Process.GetCurrentProcess().Kill();
                    
                }
            }
        }
        
        private void ShowErrorFindingDirectoryMessage()
        {
            MessageBox.Show("We couldn't seem to find your Gorilla Tag installation, please press \"OK\" and point us to it", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            NotFoundHandler();
            this.TopMost = true;
        }
        #endregion
        
        #region Random
        private void EditmmmConfig(string e)
        {
            Properties.Settings.Default.InstallDirectory = e;
            InstallDirectory = Properties.Settings.Default.InstallDirectory;
            Properties.Settings.Default.Save();
        }
        private void Instance_Click(object sender, EventArgs e)
        {
            string result = ShowInputDialog("Enter the name for your new Instance.", "Instance Creator");

            if (string.IsNullOrEmpty(result))
                MessageBox.Show("Instance name can't be null.", "Instance Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}")))
                MessageBox.Show("Instance name is already in use. Try a different name.", "Instance Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}")))
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"mmmInstances")))
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"mmmInstances"));
                
                
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}"));
                byte[] file = DownloadFile("https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip");
                UnzipFile(file, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}"));
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}\BepInEx\config")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}\BepInEx\config"));
                    File.Copy(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}\BepInEx\config\BepInEx.cfg"));
                }
                DoorstopInstanceChange(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{result}\BepInEx\core\BepInEx.Preloader.dll"));
                UpdateStatus("Instance Creation Success!");
            }
        }
        
        private void RefrshIns_Click(object sender, EventArgs e) => AddInstancesToList();
        
        
        static string ShowInputDialog(string prompt, string title)
        {
            Form form = new Form()
            {
                Width = 300,
                Height = 150,
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label label = new Label() { Left = 10, Top = 10, Text = prompt, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 10, Top = 30, Width = 260 };
            Button btnOK = new Button() { Text = "OK", Left = 110, Width = 80, Top = 60, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 200, Width = 80, Top = 60, DialogResult = DialogResult.Cancel };

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(btnOK);
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;

            btnOK.Click += (sender, e) => form.Close();
            btnCancel.Click += (sender, e) => { textBox.Text = ""; form.Close(); };

            return form.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            InstanceEnabled = checkBox1.Checked;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                string checkedItem = checkedListBox1.CheckedItems[0].ToString();
                Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"mmmInstances\{checkedItem}"), true);
                UpdateStatus($"Delete Instance: {checkedItem}");
            }
        }
        
        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (i != e.Index)
                {
                    checkedListBox1.SetItemChecked(i, false);
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            UpdateStatus("Checking for updates...");
            short version = Convert.ToInt16(DownloadSite("https://raw.githubusercontent.com/NgbatzYT/MonkeModManager/master/update"));
            if (version > CurrentVersion)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonkeUpdater");
                    byte[] mu = DownloadFile("https://github.com/ngbatzyt/MonkeUpdater/releases/latest/download/MonkeUpdater.exe");
                    File.WriteAllBytes(Path.Combine(path, "MonkeUpdater.exe"), mu);
                    Process.Start(Path.Combine(path, "MonkeUpdater.exe"));
                    Application.Exit();
                }));
            }
            else
            {
                UpdateStatus("You already have the latest MMM!");
            }
        }
        private void panel1_Click(object sender, EventArgs e)
        {
            ClickerMoney += ClickPower;
            label2.Text = $@"Money: ${ClickerMoney}";
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (ClickerMoney >= monkeAmount)
            {
                ClickerMoney -= monkeAmount;
                monkeAmount += monkeAmount;
                label2.Text = $@"Money: ${ClickerMoney}";
                button4.Text = $@"Buy Monke (+0.1 Click Power) - ${monkeAmount}";
                ClickPower += 0.1f;
                label3.Text = $@"Click Power: {ClickPower}";
            }
            else UpdateStatus("Not enough money!");
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoUpdate = checkBox3.Checked;
            AutoUpdateEnabled = Properties.Settings.Default.AutoUpdate;
            Properties.Settings.Default.Save();
        }
        
        
        #endregion

        private void button1_Click(object sender, EventArgs e) => Process.Start("https://github.com/the-graze/MonkeModManager/releases/latest");
        private void pictureBox1_Click(object sender, EventArgs e) => Process.Start("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
}