using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiscUtils;
using DiscUtils.Iso9660;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApplication2
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
		string ISONameFile = "", ExtPath = "";
        DiscDirectoryInfo currentFolder;
        public void AddtoISO(string directory, string isoFile, string sourceName)
        {
            try
            {
                DirectoryInfo rootDirToAdd = new DirectoryInfo(sourceName);
                DirectoryInfo currentDirToAdd = new DirectoryInfo(directory);
                int itemsAdded = 0;
                CDBuilder isoBuilder = new CDBuilder();
                foreach (FileInfo file in currentDirToAdd.GetFiles())
                {
                    string fileOnHdd = file.FullName;
                    string fileOnIso = fileOnHdd.Substring(fileOnHdd.IndexOf(rootDirToAdd.Name) + rootDirToAdd.Name.Length + 1);
                    MessageBox.Show(fileOnIso);
                    isoBuilder.AddFile(fileOnIso, fileOnHdd);
                    itemsAdded++;
                }
                foreach (DirectoryInfo subdir in currentDirToAdd.GetDirectories())
                {
                    itemsAdded++;
                    AddtoISO(subdir.FullName, isoFile, sourceName);
                }

                isoBuilder.Build(isoFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

        }
        public static void ReadIsoFile(string sIsoFile, string sDestinationRootPath)
        {
            Stream streamIsoFile = null;
            try
            {
                streamIsoFile = new FileStream(sIsoFile, FileMode.Open);
                DiscUtils.FileSystemInfo[] fsia = FileSystemManager.DetectDefaultFileSystems(streamIsoFile);
                if (fsia.Length < 1)
                {
                    MessageBox.Show("No valid disc file system detected.");
                }
                else
                {
                    DiscFileSystem dfs = fsia[0].Open(streamIsoFile);
                    ReadIsoFolder(dfs, @"", sDestinationRootPath);
                    return;
                }
            }
            finally
            {
                if (streamIsoFile != null)
                {
                    streamIsoFile.Close();
                }
            }
        }

        public static void ReadIsoFolder(DiscFileSystem cdReader, string sIsoPath, string sDestinationRootPath)
        {
            try
            {
                string[] saFiles = cdReader.GetFiles(sIsoPath);
                foreach (string sFile in saFiles)
                {
                    DiscFileInfo dfiIso = cdReader.GetFileInfo(sFile);
                    string sDestinationPath = Path.Combine(sDestinationRootPath, dfiIso.DirectoryName.Substring(0, dfiIso.DirectoryName.Length - 1));
                    if (!Directory.Exists(sDestinationPath))
                    {
                        Directory.CreateDirectory(sDestinationPath);
                    }
                    string sDestinationFile = Path.Combine(sDestinationPath, dfiIso.Name);
                    SparseStream streamIsoFile = cdReader.OpenFile(sFile, FileMode.Open);
                    FileStream fsDest = new FileStream(sDestinationFile, FileMode.Create);
                    byte[] baData = new byte[0x4000];
                    while (true)
                    {
                        int nReadCount = streamIsoFile.Read(baData, 0, baData.Length);
                        if (nReadCount < 1)
                        {
                            break;
                        }
                        else
                        {
                            fsDest.Write(baData, 0, nReadCount);
                        }
                    }
                    streamIsoFile.Close();
                    fsDest.Close();
                }
                string[] saDirectories = cdReader.GetDirectories(sIsoPath);
                foreach (string sDirectory in saDirectories)
                {
                    ReadIsoFolder(cdReader, sDirectory, sDestinationRootPath);
                }
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        void ExtractISO(string ISOName, string ExtractionPath)
        {
            using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
            {
                CDReader Reader = new CDReader(ISOStream, true, true);
                ExtractDirectory(Reader.Root, ExtractionPath + Path.GetFileNameWithoutExtension(ISOName) + "\\", "");
                Reader.Dispose();
            }
        }
        void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
        {
            if (!string.IsNullOrWhiteSpace(PathinISO))
            {
                PathinISO += "\\" + Dinfo.Name;
            }
            RootPath += "\\" + Dinfo.Name;
            AppendDirectory(RootPath);
            foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
            {
                ExtractDirectory(dinfo, RootPath, PathinISO);
            }
            foreach (DiscFileInfo finfo in Dinfo.GetFiles())
            {
                using (Stream FileStr = finfo.OpenRead())
                {
                    using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name)) // Here you can Set the BufferSize Also e.g. File.Create(RootPath + "\\" + finfo.Name, 4 * 1024)
                    {
                        FileStr.CopyTo(Fs, 4 * 1024); // Buffer Size is 4 * 1024 but you can modify it in your code as per your need
                    }
                }
            }
        }
        static void AppendDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Exx)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
        }
        void BrowseDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO, DiscDirectoryInfo ComparingNode)
        {
            if (Dinfo.FullName == ComparingNode.FullName)
            {
                metroListView1.Items.Clear();
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    ListViewItem.ListViewSubItem[] subItems = null;
                    ListViewItem item = null;
                    item = new ListViewItem(dinfo.Name, 0);
                    subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, "Directory"),
                   new ListViewItem.ListViewSubItem(item,
                dinfo.LastAccessTime.ToShortDateString())};
                    item.SubItems.AddRange(subItems);
                    metroListView1.Items.Add(item);
                    currentFolder = Dinfo;
                    textBox1.Text = Dinfo.FullName;
                    //DiscDirectoryInfo nodeDirInfo;
                    //for (int i = 0; i<treeView1.Nodes.Count; i++)
                    //{
                    //    nodeDirInfo = (DiscDirectoryInfo)treeView1.Nodes[i].Tag;
                    //    if (nodeDirInfo.FullName == Dinfo.FullName)
                    //        treeView1.Nodes[i].Expand();
                    //    else treeView1.Nodes[0].NextVisibleNode.Expand();
                    //}
                       
                }
                foreach (DiscFileInfo finfo in Dinfo.GetFiles())
                {
                    ListViewItem.ListViewSubItem[] subItems = null;
                    ListViewItem item = null;
                    item = new ListViewItem(finfo.Name, 1);
                    subItems = new ListViewItem.ListViewSubItem[]
                              { new ListViewItem.ListViewSubItem(item, "File"),
                   new ListViewItem.ListViewSubItem(item,
                finfo.LastAccessTime.ToShortDateString())};
                    item.SubItems.AddRange(subItems);
                    metroListView1.Items.Add(item);
                }
            } else
            {
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    if (dinfo.FullName == ComparingNode.FullName)
                    {
                        metroListView1.Items.Clear();
                        BrwsDirectory(dinfo, RootPath, PathinISO, ComparingNode);
                    }
                    BrowseDirectory(dinfo, RootPath, PathinISO, ComparingNode);
                }
            }
            
        }
        private void BrwsDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO, DiscDirectoryInfo ComparingNode)
        {
            foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
            {
                ListViewItem.ListViewSubItem[] subItems = null;
                ListViewItem item = null;
                item = new ListViewItem(dinfo.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                          {new ListViewItem.ListViewSubItem(item, "Directory"),
                   new ListViewItem.ListViewSubItem(item,
                dinfo.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                metroListView1.Items.Add(item);
                currentFolder = Dinfo;
                textBox1.Text = Dinfo.FullName;
                //DiscDirectoryInfo nodeDirInfo;
                
                //for (int i = 0; i < treeView1.Nodes.Count; i++)
                //{
                //    //nodeDirInfo = (DiscDirectoryInfo)treeView1.Nodes[i].Tag;
                //    //if (nodeDirInfo.FullName == Dinfo.FullName)
                //    //    treeView1.Nodes[i].Expand();
                //    //else treeView1.Nodes[0].NextVisibleNode.Expand();

                //}
            }
            foreach (DiscFileInfo finfo in Dinfo.GetFiles())
            {
                ListViewItem.ListViewSubItem[] subItems = null;
                ListViewItem item = null;
                item = new ListViewItem(finfo.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                          { new ListViewItem.ListViewSubItem(item, "File"),
                   new ListViewItem.ListViewSubItem(item,
                finfo.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                metroListView1.Items.Add(item);

            }
        }
        private void GoToDirectory(DiscDirectoryInfo Dinfo, string path, string directoryName, DiscDirectoryInfo ComparingNode)
        {
            if (Dinfo.FullName == textBox1.Text)
            {
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    if (dinfo.Name == directoryName)
                    {
                        BrwsDirectory(dinfo, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", dinfo);
                        textBox1.Text = dinfo.FullName;
                        currentFolder = dinfo;
                    }
                }
            }
            else
            {
                foreach (DiscDirectoryInfo disc in Dinfo.GetDirectories())
                {
                    GoToDirectory(disc, path, directoryName, ComparingNode);
                }
            }

        }
        private void GoToDirectory(DiscDirectoryInfo Dinfo, string path)
        {
            if (Dinfo.FullName == textBox1.Text)
            {
                BrwsDirectory(Dinfo, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", Dinfo);
                textBox1.Text = Dinfo.FullName;
                currentFolder = Dinfo;
            }
            else
            {
                foreach (DiscDirectoryInfo disc in Dinfo.GetDirectories())
                {
                    GoToDirectory(disc, path);
                }
            }

        }
        private void GoBackDirectory(DiscDirectoryInfo Dinfo, string path, DiscDirectoryInfo ComparingNode)
        {
            if (Dinfo.FullName == textBox1.Text)
            {
                BrwsDirectory(Dinfo.Parent, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", Dinfo.Parent);
                textBox1.Text = Dinfo.Parent.FullName;
            }
            else
            {
                foreach (DiscDirectoryInfo disc in Dinfo.GetDirectories())
                {
                    GoBackDirectory(disc, path, ComparingNode);
                }
            }

        }
        public Form1()
        {
            InitializeComponent();
            metroListView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                PopulateTreeView(openFileDialog1.SafeFileName, openFileDialog1.FileName.Replace(openFileDialog1.SafeFileName, ""));
                using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                {
                    CDReader Reader = new CDReader(ISOStream, true, true);
                    metroListView1.Items.Clear();
                    BrowseDirectory(Reader.Root, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", Reader.Root);
                    metroListView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    DiscDirectoryInfo nodeDirInfo = (DiscDirectoryInfo)treeView1.Nodes[0].Tag;
                    textBox1.Text = nodeDirInfo.FullName;
                }
                }
        }
        private void PopulateTreeView(string ISOName, string ExtractionPath)
        {
            TreeNode rootNode; ISONameFile = ISOName; ExtPath = ExtractionPath;
            using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
			{
				CDReader Reader = new CDReader(ISOStream, true, true);
				DiscDirectoryInfo info = Reader.Root;
				if (info.Exists)
				{
					rootNode = new TreeNode(ISOName);
					rootNode.Tag = info;
					GetDirectories(info.GetDirectories(), rootNode);
					treeView1.Nodes.Add(rootNode);
				}
				Reader.Dispose();
			}
        }

        private void GetDirectories(DiscDirectoryInfo[] subDirs,
   TreeNode nodeToAddTo)
        {
            TreeNode aNode;
			DiscDirectoryInfo[] subSubDirs;
            foreach (DiscDirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }
        void treeView1_NodeMouseClick(object sender,
    TreeNodeMouseClickEventArgs e)
        {
			using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
			{
				CDReader Reader = new CDReader(ISOStream, true, true);
                metroListView1.Items.Clear();        
				TreeNode newSelected = e.Node;
				DiscDirectoryInfo nodeDirInfo = (DiscDirectoryInfo)newSelected.Tag;
                BrowseDirectory(Reader.Root, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", nodeDirInfo);
                textBox1.Text = nodeDirInfo.FullName;
                currentFolder = nodeDirInfo;
                Reader.Dispose();
			}
			metroListView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
        private void button1_Click(object sender, EventArgs e)
        {

        }
        private void metroTile1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                PopulateTreeView(openFileDialog1.SafeFileName, openFileDialog1.FileName.Replace(openFileDialog1.SafeFileName, ""));
                using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                {
                    CDReader Reader = new CDReader(ISOStream, true, true);
                    metroListView1.Items.Clear();
                    BrowseDirectory(Reader.Root, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", Reader.Root);
                    metroListView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    DiscDirectoryInfo nodeDirInfo = (DiscDirectoryInfo)treeView1.Nodes[0].Tag;
                    textBox1.Text = nodeDirInfo.FullName;
                }
            }
        }

        private void metroTile2_Click(object sender, EventArgs e)
        {
            try
            {
                using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                {
                    CDReader Reader = new CDReader(ISOStream, true, true);
                    metroListView1.Items.Clear();
                    BrowseDirectory(Reader.Root, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", currentFolder.Parent);
                }
            }
            catch { }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if((e.KeyCode == Keys.Enter) &&  textBox1.Text != "")
            using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
            {
                CDReader Reader = new CDReader(ISOStream, true, true);
                metroListView1.Items.Clear();
                GoToDirectory(Reader.Root, textBox1.Text);
            }
        }

        private void metroListView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if ((e.KeyCode == Keys.Back) && (textBox1.Text != ""))

                    using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                    {
                        CDReader Reader = new CDReader(ISOStream, true, true);
                        metroListView1.Items.Clear();
                        BrowseDirectory(Reader.Root, ExtPath + Path.GetFileNameWithoutExtension(ISONameFile) + "\\", "", currentFolder.Parent);
                    }

                if ((e.KeyCode == Keys.Enter) && (textBox1.Text != ""))
                    if (metroListView1.Items.Count > 0)
                    {
                        if (metroListView1.SelectedItems[0].SubItems[1].Text == "Directory")
                        {
                            string item = metroListView1.SelectedItems[0].Text;

                            using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                            {
                                CDReader Reader = new CDReader(ISOStream, true, true);
                                metroListView1.Items.Clear();
                                GoToDirectory(Reader.Root, textBox1.Text, item, currentFolder);
                            }
                        }
                    }
            }
            catch { }
        }

        private void metroListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (metroListView1.Items.Count > 0)
            {
                if (metroListView1.SelectedItems[0].SubItems[1].Text == "Directory")
                {
                    string item = metroListView1.SelectedItems[0].Text;
                    using (FileStream ISOStream = File.Open(openFileDialog1.FileName, FileMode.Open))
                    {
                        CDReader Reader = new CDReader(ISOStream, true, true);
                        metroListView1.Items.Clear();
                        GoToDirectory(Reader.Root, textBox1.Text, item, currentFolder);
                    }
                }
                else if(metroListView1.SelectedItems[0].SubItems[1].Text == "File")
                {
                    string runfile = openFileDialog1.FileName.Replace(".iso", "") + "\\" + textBox1.Text + metroListView1.SelectedItems[0].Text;
                    if (!Directory.Exists(runfile))
                    ExtractISO(openFileDialog1.SafeFileName, openFileDialog1.FileName.Replace(openFileDialog1.SafeFileName, ""));
                    Process.Start(runfile);
                }
            }
        }
    }
}
