﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toolbox.Library;
using Toolbox.Library.IO;
using Toolbox.Library.Forms;
using BarsLib;
using VGAudio.Formats;
using VGAudio;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;
using NAudio.Wave;

namespace FirstPlugin
{
    public class BARS : TreeNodeFile, IFileFormat, IContextMenuNode
    {
        public FileType FileType { get; set; } = FileType.Audio;

        public bool CanSave { get; set; }
        public string[] Description { get; set; } = new string[] { "Sound Archive" };
        public string[] Extension { get; set; } = new string[] { "*.bars" };
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public IFileInfo IFileInfo { get; set; }

        public bool Identify(Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "BARS");
            }
        }

        public Type[] Types
        {
            get
            {
                List<Type> types = new List<Type>();
                return types.ToArray();
            }
        }

        public override void OnClick(TreeView treeview)
        {
            STPropertyGrid editor = (STPropertyGrid)LibraryGUI.GetActiveContent(typeof(STPropertyGrid));
            if (editor == null)
            {
                editor = new STPropertyGrid();
                LibraryGUI.LoadEditor(editor);
            }

            var prop = new BarsProperty(bars);

            editor.Text = Text;
            editor.Dock = DockStyle.Fill;
            editor.LoadProperty(prop, null);
        }

        public class BarsProperty
        {
            public int AudioCount { get; private set; }

            public BarsProperty(BarsLib.BARS bars)
            {
                AudioCount = bars.AudioEntries.Count;
            }
        }

        public class AudioEntry : TreeNodeCustom, IContextMenuNode
        {
            public BARSAudioFile audioFile;

            public string Magic;
            public byte[] Data
            {
                get
                {
                    return audioFile.data;
                }
                set
                {
                    audioFile.data = value;
                }
            }

            public AudioEntry()
            {
            }

            public ToolStripItem[] GetContextMenuItems()
            {
                List<ToolStripItem> Items = new List<ToolStripItem>();
                Items.Add(new ToolStripMenuItem("Export", null, ExportAction, Keys.Control | Keys.E));
                Items.Add(new ToolStripMenuItem("Replace", null, ReplaceAction, Keys.Control | Keys.R));
                return Items.ToArray();
            }

            public void SetupMusic()
            {
                if (Magic == "FWAV" || Magic == "BWAV")
                {
                    ImageKey = "bfwav";
                    SelectedImageKey = "bfwav";
                }
                else if (Magic == "FSTP")
                {
                    ImageKey = "bfstp";
                    SelectedImageKey = "bfstp";
                }
                else
                {
                    ImageKey = "fileBlank";
                    SelectedImageKey = "fileBlank";
                }
            }
            public AudioData GetAudioData()
            {
                BCFstmReader reader = new BCFstmReader();
                return reader.Read(Data);
            }
            public byte[] BfwavToWav()
            {
                MemoryStream mem = new MemoryStream();
                WaveWriter writer = new WaveWriter();

                AudioData audioData = GetAudioData();
                writer.WriteToStream(audioData, mem);

                return mem.ToArray();
            }

            private void ExportAction(object sender, EventArgs args)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = Text;
                sfd.DefaultExt = Path.GetExtension(Text);
                sfd.Filter = "All files(*.*)|*.*";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(sfd.FileName, Data);
                }
            }

            private void ReplaceAction(object sender, EventArgs args)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.FileName = Text;
                ofd.DefaultExt = Path.GetExtension(Text);
                ofd.Filter = "All files(*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Data = File.ReadAllBytes(ofd.FileName);
                    UpdateEditor();
                }
            }
            public void UpdateEditor()
            {
                switch (Magic)
                {
                    case "FWAV":
                       // ShowHexView();
                        ShowBfwavPlayer();
                        break;
                    default:
                        ShowHexView();
                        break;
                }
            }

            private void ShowBfwavPlayer()
            {
                var audioFile = new VGAdudioFile();
                audioFile.LoadAudio(new MemoryStream(Data), new BFWAV());

                AudioPlayerPanel editor = (AudioPlayerPanel)LibraryGUI.GetActiveContent(typeof(AudioPlayerPanel));
                if (editor == null)
                {
                    editor = new AudioPlayerPanel();
                    LibraryGUI.LoadEditor(editor);
                }
                editor.Text = Text;
                editor.Dock = DockStyle.Fill;
                editor.LoadFile(audioFile.audioData, new BFWAV(), true);
            }

            private void ShowHexView()
            {
                HexEditor editor = (HexEditor)LibraryGUI.GetActiveContent(typeof(HexEditor));
                if (editor == null)
                {
                    editor = new HexEditor();
                    LibraryGUI.LoadEditor(editor);
                }
                editor.Text = Text;
                editor.Dock = DockStyle.Fill;
                editor.LoadData(Data);
            }

            public override void OnClick(TreeView treeview)
            {
                UpdateEditor();
            }
        }

        private class MetaDataNodeWrapper : TreeNodeCustom
        {
            public MetaDataNodeWrapper(AMTA amta) { MetaFile = amta; }

            public AMTA MetaFile { get; set; }

            public override void OnClick(TreeView treeview)
            {
                STPropertyGrid editor = (STPropertyGrid)LibraryGUI.GetActiveContent(typeof(STPropertyGrid));
                if (editor == null)
                {
                    editor = new STPropertyGrid();
                    LibraryGUI.LoadEditor(editor);
                }
                editor.Text = Text;
                editor.Dock = DockStyle.Fill;
                editor.LoadProperty(MetaFile.Data, OnPropertyChanged);
            }

            private void OnPropertyChanged() { }
        }

        public BarsLib.BARS bars;
        public void Load(Stream stream)
        {
            CanSave = true;

            Text = FileName;

            bars = new BarsLib.BARS(stream);

            if (bars.HasMetaData)
                Nodes.Add("Meta Data");

            if (bars.HasAudioFiles)
                Nodes.Add(new AudioFolder("Audio"));

            for (int i = 0; i < bars.AudioEntries.Count; i++)
            {
                var amtaWrapper = new MetaDataNodeWrapper(bars.AudioEntries[i].MetaData);
                amtaWrapper.ImageKey = "MetaInfo";
                amtaWrapper.SelectedImageKey = amtaWrapper.ImageKey;

                string audioName = bars.AudioEntries[i].MetaData.Name;

                amtaWrapper.Text = $"{audioName}.amta";
                Nodes[0].Nodes.Add(amtaWrapper);

                if (bars.AudioEntries[i].AudioFile != null)
                {
                    BARSAudioFile audio = bars.AudioEntries[i].AudioFile;

                    AudioEntry node = new AudioEntry();
                    node.audioFile = audio;
                    node.Magic = audio.Magic;
                    node.SetupMusic();

                    if (audio.Magic == "FWAV")
                        node.Text = audioName + ".bfwav";
                    else if (audio.Magic == "FSTP")
                        node.Text = audioName + ".bfstp";
                    else if (audio.Magic == "BWAV")
                        node.Text = audioName + ".bwav";
                    else
                        node.Text = $"{audioName}.{audio.Magic}";

                    Nodes[1].Nodes.Add(node);
                }
            }
        }

        public void Unload()
        {

        }

        public ToolStripItem[] GetContextMenuItems()
        {
            List<ToolStripItem> Items = new List<ToolStripItem>();
            Items.Add(new ToolStripMenuItem("Save", null, SaveAction, Keys.Control | Keys.S));
            return Items.ToArray();
        }

        private void SaveAction(object sender, EventArgs args)
        {
            List<IFileFormat> formats = new List<IFileFormat>();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = Utils.GetAllFilters(formats);
            sfd.FileName = FileName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                STFileSaver.SaveFileFormat(this, sfd.FileName);
            }
        }

        public void Save(System.IO.Stream stream)
        {
            bars.Save(stream);
        }

        public class AudioFolder : TreeNode, IContextMenuNode
        {
            public AudioFolder(string text) : base(text)
            {

            }

            public ToolStripItem[] GetContextMenuItems()
            {
                List<ToolStripItem> Items = new List<ToolStripItem>();
                Items.Add(new ToolStripMenuItem("Export All", null, ExportAllAction, Keys.Control | Keys.E));
                return Items.ToArray();
            }

            private void ExportAllAction(object sender, EventArgs e)
            {
                FolderSelectDialog ofd = new FolderSelectDialog();
                if (ofd.ShowDialog() == DialogResult.OK) {
                    foreach (AudioEntry audio in Nodes) {
                        File.WriteAllBytes($"{ofd.SelectedPath}/{audio.Text}", audio.Data);
                    }
                }
            }
        }
    }
}
