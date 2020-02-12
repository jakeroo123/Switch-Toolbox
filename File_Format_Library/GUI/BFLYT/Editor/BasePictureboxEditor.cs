﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toolbox.Library.Forms;

namespace LayoutBXLYT
{
    public partial class BasePictureboxEditor : EditorPanelBase
    {
        public BasePictureboxEditor()
        {
            InitializeComponent();

            vertexColorBox1.OnColorChanged += OnColorChanged;

            topLeftXUD.ValueChanged += texCoordValue_Changed;
            topLeftYUD.ValueChanged += texCoordValue_Changed;
            topRightXUD.ValueChanged += texCoordValue_Changed;
            topRightYUD.ValueChanged += texCoordValue_Changed;

            bottomLeftXUD.ValueChanged += texCoordValue_Changed;
            bottomLeftYUD.ValueChanged += texCoordValue_Changed;
            bottomRightXUD.ValueChanged += texCoordValue_Changed;
            bottomRightYUD.ValueChanged += texCoordValue_Changed;

            stDropDownPanel1.ResetColors();
            stDropDownPanel3.ResetColors();
        }

        private IPicturePane ActivePane;
        private PaneEditor parentEditor;

        private bool Loaded = false;

        public void LoadPane(IPicturePane pane, PaneEditor paneEditor)
        {
            parentEditor = paneEditor;
            Loaded = false;

            ActivePane = pane;
            vertexColorBox1.TopLeftColor = pane.ColorTopLeft.Color;
            vertexColorBox1.TopRightColor = pane.ColorTopRight.Color;
            vertexColorBox1.BottomLeftColor = pane.ColorBottomLeft.Color;
            vertexColorBox1.BottomRightColor = pane.ColorBottomRight.Color;
            vertexColorBox1.Refresh();

            texCoordIndexCB.Items.Clear();
            for (int i = 0; i < pane.TexCoords?.Length; i++)
                texCoordIndexCB.Items.Add($"TexCoord [{i}]");

            if (pane.TexCoords?.Length > 0)
                texCoordIndexCB.SelectedIndex = 0;

            Loaded = true;
        }

        private void OnColorChanged(object sender, EventArgs e)
        {
            if (!Loaded) return;

            ActivePane.ColorTopLeft.Color = vertexColorBox1.TopLeftColor;
            ActivePane.ColorTopRight.Color = vertexColorBox1.TopRightColor;
            ActivePane.ColorBottomLeft.Color = vertexColorBox1.BottomLeftColor;
            ActivePane.ColorBottomRight.Color = vertexColorBox1.BottomRightColor;

            //Apply to all selected panes
            foreach (BasePane pane in parentEditor.SelectedPanes)
            {
                if (pane is  IPicturePane) {
                    ((IPicturePane)pane).ColorTopLeft.Color = vertexColorBox1.TopLeftColor;
                    ((IPicturePane)pane).ColorTopRight.Color = vertexColorBox1.TopRightColor;
                    ((IPicturePane)pane).ColorBottomLeft.Color = vertexColorBox1.BottomLeftColor;
                    ((IPicturePane)pane).ColorBottomRight.Color = vertexColorBox1.BottomRightColor;
                }
            }

            parentEditor.PropertyChanged?.Invoke(sender, e);
        }

        private void texCoordValue_Changed(object sender, EventArgs e)
        {
            int index = texCoordIndexCB.SelectedIndex;
            if (index < 0 || !Loaded) return;

            ActivePane.TexCoords[index].TopLeft = 
                new Syroot.Maths.Vector2F(topLeftXUD.Value, topLeftYUD.Value);

            ActivePane.TexCoords[index].TopRight =
             new Syroot.Maths.Vector2F(topRightXUD.Value, topRightYUD.Value);

            ActivePane.TexCoords[index].BottomLeft =
          new Syroot.Maths.Vector2F(bottomLeftXUD.Value, bottomLeftYUD.Value);

            ActivePane.TexCoords[index].BottomRight =
             new Syroot.Maths.Vector2F(bottomRightXUD.Value, bottomRightYUD.Value);

            parentEditor.PropertyChanged?.Invoke(sender, e);
        }

        private void texCoordIndexCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = texCoordIndexCB.SelectedIndex;
            if (index < 0) return;

            topLeftXUD.Value = ActivePane.TexCoords[index].TopLeft.X;
            topLeftYUD.Value = ActivePane.TexCoords[index].TopLeft.Y;
            topRightXUD.Value = ActivePane.TexCoords[index].TopRight.X;
            topRightYUD.Value = ActivePane.TexCoords[index].TopRight.Y;

            bottomLeftXUD.Value = ActivePane.TexCoords[index].BottomLeft.X;
            bottomLeftYUD.Value = ActivePane.TexCoords[index].BottomLeft.Y;
            bottomRightXUD.Value = ActivePane.TexCoords[index].BottomRight.X;
            bottomRightYUD.Value = ActivePane.TexCoords[index].BottomRight.Y;
        }

        private void btnResetColors_Click(object sender, EventArgs e)
        {
            vertexColorBox1.TopLeftColor = Color.White;
            vertexColorBox1.TopRightColor = Color.White;
            vertexColorBox1.BottomLeftColor = Color.White;
            vertexColorBox1.BottomRightColor = Color.White;
            vertexColorBox1.Refresh();
        }

        public override void OnControlClosing() {
            vertexColorBox1.DisposeControl();
        }
    }
}
