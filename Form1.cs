using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH
        private string DefaultOculusInstallDirectory = @"C:\Program Files\Oculus\Software\Software\another-axiom-gorilla-tag";
        private string DefaultSteamInstallDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag";
        public static string InstallDirectory = @""; // If you see this you should give me 500 dollars!!!!!
        Dictionary<string, int> groups = new Dictionary<string, int>();
        Dictionary<string, string> installed = new Dictionary<string, string>();
        Dictionary<string, bool> installedr = new Dictionary<string, bool>();
        private List<ReleaseInfo> releases;
        private bool modsDisabled;
        private int CurrentVersion = 14; // actual version is just below // (big changes update).(Feature update).(minor update).(hotfix) // i forget to forget fun fact
        public readonly string VersionNumber = "2.7.0.0"; // Fun fact of the update: abcdefghijklmnopqrstuvwxyz // Fun fact of the year: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAHHHHHHHHHHHHHHHHHHHHHHH
        private string currentMod;
        
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
                        EditConfig(InstallDirectory);
                    }
                    else
                        MessageBox.Show(@"That's not Gorilla Tag, please try again.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonInstall_Click(object sender, EventArgs e) => new Thread(Install).Start(); // im very tired help aeuhgn( Frna8eyhify7sveur

        private void Install()
        {
            try
            {
                ChangeInstallButtonState(false);
                UpdateStatus("Starting install sequence...");
                foreach (ReleaseInfo release in releases)
                {
                    currentMod = release.Name;
                    if (release.Install)
                    {
                        UpdateStatus(string.Format("Downloading...{0}", release.Name)); // its a lie it never downloaded
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
                        UpdateStatus($"Installed {release.Name}!");
                    }
                }
                UpdateStatus("Install complete!");
                ChangeInstallButtonState(true);
                GetInstalledMods();
            }
            catch (Exception e)
            {
                MessageBox.Show(@"Hey, an error occurred. Please go to discord.gg/monkemod and tell 'Ngbatz' or 'Graze' to fix this mod: " + currentMod + @". Error:" + e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (release.Name.Contains("BepInEx")) { e.Item.Checked = true; }
            
            release.Install = e.Item.Checked;
        }

        private void listViewMods_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            buttonModInfo.Enabled = listViewMods.SelectedItems.Count > 0; // idk what this is for but i think its important
        }
        void listViewMods_DoubleClick(object sender, EventArgs e) => OpenLinkFromRelease();
        void viewInfoToolStripMenuItem_Click(object sender, EventArgs e) => OpenLinkFromRelease(); // why is there 2 of these methods like what
        void buttonDiscordLink_Click(object sender, EventArgs e) => Process.Start("https://discord.gg/monkemod");

        private void buttonOpenGameFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(InstallDirectory))
                Process.Start(InstallDirectory); // hmm very useful // agreed just like whats below us
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
                UpdateStatus("Removing all mods");

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
                    MessageBox.Show($@"Something went wrong! Error: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Failed to remove mods.");
                    return;
                }

                UpdateStatus("All mods removed successfully!");
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

        void Form1_Load(object sender, EventArgs e)
        {
            CheckForUpdates();
            
            
            InstallDirectory = Properties.Settings.Default.InstallDirectory;

            releases = [];

            labelVersion.Text = $@"Monke Mod Manager v{VersionNumber}";

            if (InstallDirectory != @"" && File.Exists(Path.Combine(InstallDirectory, "Gorilla Tag.exe")))
            {
                textBoxDirectory.Text = InstallDirectory;
            }
            else if (File.Exists(Path.Combine(DefaultSteamInstallDirectory, "Gorilla Tag.exe")))
            {
                InstallDirectory = DefaultSteamInstallDirectory;
                textBoxDirectory.Text = InstallDirectory;
                EditConfig(InstallDirectory); 
            }
            else if (File.Exists(Path.Combine(DefaultOculusInstallDirectory, "Gorilla Tag.exe")))
            {
                InstallDirectory = DefaultOculusInstallDirectory;
                textBoxDirectory.Text = InstallDirectory;
                EditConfig(InstallDirectory);
            }
            else
            {
                ShowErrorFindingDirectoryMessage();
            }

            ConfigFix(); // i forgot about this // lol same // haha i can't find that method funny right
            new Thread(LoadRequiredPlugins).Start();
            new Thread(GetInstalledMods).Start();
        }

        private void UpdateStatus(string status)
        {
            string formattedText = $"Status: {status}";
            this.Invoke((MethodInvoker)(() =>
            {
                //Invoke so we can call from any thread
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
                        : "Failed to fetch info, please check your internet connection. If this persists contact \"ngbatz\" on discord via GTMG (https://discord.gg/monkemod)", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(@"You will still be able to use MMM but you won't be able to install mods.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus("Failed to fetch info");
                return null;
            }
        }

        private void LoadReleases()
        {
            const bool debugging = false;
            JSONNode decodedGroups = JSON.Parse(DownloadSite("https://raw.githubusercontent.com/The-Graze/MonkeModInfo/master/groupinfo.json"));
            JSONNode decodedMods = null;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            decodedMods = JSON.Parse(!debugging ? DownloadSite("https://raw.githubusercontent.com/The-Graze/MonkeModInfo/master/modinfo.json") : DownloadSite("https://raw.githubusercontent.com/ngbatzyt/MonkeModInfo/master/modinfo.json"));


            var allMods = decodedMods.AsArray;
            var allGroups = decodedGroups.AsArray;

            for (int i = 0; i < allMods.Count; i++)
            {
                JSONNode current = allMods[i];
                ReleaseInfo release = new ReleaseInfo(current["name"], current["author"], current["version"], current["group"], current["download_url"], current["install_location"], current["git_path"],
                    current["mod"], current["dependencies"].AsArray);
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

        public void ConfigFix()
        {
            if (!File.Exists(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"))) {
                var eggs = DownloadSite("https://github.com/The-Graze/MonkeModInfo/raw/refs/heads/master/BepInEx.cfg");
                File.WriteAllText(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"), eggs);
            }

            string c = File.ReadAllText(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"));
            if (!c.Contains("HideManagerGameObject = false")) {
                return;
            }

            string e = c.Replace("HideManagerGameObject = false", "HideManagerGameObject = true");
            File.WriteAllText(Path.Combine(InstallDirectory, @"BepInEx\config\BepInEx.cfg"), e);
        }

        private void LoadRequiredPlugins()
        {
            try
            {
                UpdateStatus("Getting latest version info...");
                LoadReleases();
                this.Invoke((MethodInvoker)(() =>
                {
                    //Invoke so we can call from current thread
                    //Update checkbox's text
                    //Dictionary<string, int> includedGroups = new Dictionary<string, int>(); // is this even used // lol its not!

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
                        if (!String.IsNullOrEmpty(release.Tag)) { item.Text = string.Format("{0} - ({1})", release.Name, release.Tag); }
                        ;
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
                    }

                    tabControlMain.Enabled = true;
                    buttonInstall.Enabled = true;

                }));

                UpdateStatus("Release info updated!");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error!");
                return;
            }
        }

        private void CheckDefaultMod(ReleaseInfo release, ListViewItem item)
        {
            if (release.Name.Contains("BepInEx"))
            {
                item.Checked = true;
                item.ForeColor = System.Drawing.Color.DimGray;
            }
            else
                release.Install = false;
            
        }

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
                            EditConfig(InstallDirectory);
                        }
                        else
                            MessageBox.Show(@"That's not Gorilla Tag, please try again.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                        Environment.Exit(0);

                }
            }
        }

        private void ShowErrorFindingDirectoryMessage()
        {
            MessageBox.Show("""We couldn't find your Gorilla Tag installation, please press "OK" and point us to it""", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            NotFoundHandler();
            this.TopMost = true;
        }

        private void EditConfig(string e)
        {
            Properties.Settings.Default.InstallDirectory = e;
            Properties.Settings.Default.Save();
            InstallDirectory = Properties.Settings.Default.InstallDirectory;
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("https://gorillatagmodding.ngbatzstudios.com/#/");
        }
        private void modsButton_Click(object sender, EventArgs e)
        {
            if (modsDisabled)
            {
                if (!File.Exists(Path.Combine(InstallDirectory, "mods.disable")))
                    return;
                File.Move(Path.Combine(InstallDirectory, "mods.disable"), Path.Combine(InstallDirectory, "winhttp.dll"));
                modsButton.Text = "Disable Mods";
                modsButton.BackColor = System.Drawing.Color.Transparent;
                modsDisabled = false;
                UpdateStatus("Enabled mods!");
            }
            else
            {
                if (!File.Exists(Path.Combine(InstallDirectory, "winhttp.dll")))
                    return;
                File.Move(Path.Combine(InstallDirectory, "winhttp.dll"), Path.Combine(InstallDirectory, "mods.disable"));
                modsButton.Text = "Enable Mods";
                modsButton.BackColor = System.Drawing.Color.IndianRed;
                modsDisabled = true;
                UpdateStatus("Disabled mods!");
            }
        }


        private void GetInstalledMods()
        {
            listView1.Items.Clear();
            installedr.Clear();
            installed.Clear();
            
            var modsPath = Path.Combine(InstallDirectory, "BepInEx/plugins");
            
            var modDlls = Directory.GetFiles(modsPath, "*.dll", SearchOption.AllDirectories);

            foreach (var d in modDlls)
            {
                ListViewItem item = new ListViewItem();


                if (installed.ContainsKey(Path.GetFileNameWithoutExtension(d)))
                {
                    int a = 0;
                    foreach (var i in installed)
                    {
                        if (i.Key.Contains(Path.GetFileNameWithoutExtension(d)))
                        {
                            a++;
                        }
                    }
                    a++;
                    item.Text = Path.GetFileNameWithoutExtension(d) + @" " + a;
                }
                else
                {
                    item.Text = Path.GetFileNameWithoutExtension(d);
                }
                
                installed.Add(item.Text, d);
                
                listView1.Items.Add(item);
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var d in  installed)
            {
                if (installedr.ContainsKey(d.Key) && installedr[d.Key])
                    if (File.Exists(d.Value))
                    {
                        File.Delete(d.Value);
                        UpdateStatus($"Uninstalled {d.Key}...");
                    }
            }
            GetInstalledMods();
        }
        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                installedr.Remove(item.Text);
                installedr.Add(item.Text, item.Checked);
            }
        }
        
        private void CheckForUpdates() // i somehow deleted this when cleaning up unrequired code AAAAAAAAAAAAAAAAAAa
        {
            try
            {
                UpdateStatus("Checking for updates...");
                short version = Convert.ToInt16(DownloadSite("https://raw.githubusercontent.com/NgbatzYT/MonkeModManager/master/update"));
                if (version > CurrentVersion)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show(@"You have an old version of MMM.", @"Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Process.Start("https://github.com/NgbatzYT/MonkeModManager/releases/latest");
                        Environment.Exit(0);
                    }));
                }
            }
            catch (Exception e)
            {
                // HAHAHA I WILL NEVER CATCH!!!
            }
        }
    }
}
