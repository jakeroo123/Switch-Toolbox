﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using Syroot.Maths;

namespace LayoutBXLYT.Revolution
{
    public class PAN1 : BasePane
    {
        public override string Signature { get; } = "pan1";

        private byte _flags1;

        public override bool Visible
        {
            get { return (_flags1 & 0x1) == 0x1; }
            set
            {
                if (value)
                    _flags1 |= 0x1;
                else
                    _flags1 &= 0xFE;
            }
        }

        public override bool InfluenceAlpha
        {
            get { return (_flags1 & 0x2) == 0x2; }
            set
            {
                if (value)
                    _flags1 |= 0x2;
                else
                    _flags1 &= 0xFD;
            }
        }

        public bool IsWideScreen
        {
            get { return (_flags1 & 0x4) == 0x4; }
        }

        public byte PartsScale { get; set; }

        public PAN1() { }

        public PAN1(BxlytHeader header, string name) : base()
        {
            LoadDefaults();
            Name = name;
        }

        public override void LoadDefaults()
        {
            base.LoadDefaults();
            UserDataInfo = "";
        }

        enum OriginXRev : byte
        {
            Left = 0,
            Center = 1,
            Right = 2
        };

        enum OriginYRev : byte
        {
            Top = 0,
            Center = 1,
            Bottom = 2
        };

        public PAN1(FileReader reader) : base()
        {
            _flags1 = reader.ReadByte();
            byte origin = reader.ReadByte();
            Alpha = reader.ReadByte();
            PaneMagFlags = reader.ReadByte();
            Name = reader.ReadString(0x10, true);
            UserDataInfo = reader.ReadString(0x8, true);
            Translate = reader.ReadVec3SY();
            Rotate = reader.ReadVec3SY();
            Scale = reader.ReadVec2SY();
            Width = reader.ReadSingle();
            Height = reader.ReadSingle();

            originX = OriginXMap[(OriginXRev)(origin % 3)];
            originY = OriginYMap[(OriginYRev)(origin / 3)];
        }

        public override void Write(FileWriter writer, LayoutHeader header)
        {
            byte originL = (byte)OriginXMap.FirstOrDefault(x => x.Value == originX).Key;
            byte originH = (byte)OriginYMap.FirstOrDefault(x => x.Value == originY).Key;

            writer.Write(_flags1);
            writer.Write((byte)(((int)originL) + ((int)originH * 3)));
            writer.Write(Alpha);
            writer.Write(PaneMagFlags);
            writer.WriteString(Name, 0x10);
            writer.WriteString(UserDataInfo, 0x8);
            writer.Write(Translate);
            writer.Write(Rotate);
            writer.Write(Scale);
            writer.Write(Width);
            writer.Write(Height);
        }

        private Dictionary<OriginYRev, OriginY> OriginYMap = new Dictionary<OriginYRev, OriginY>()
        {
            { OriginYRev.Center, OriginY.Center },
            { OriginYRev.Top,    OriginY.Top },
            { OriginYRev.Bottom, OriginY.Bottom },
        };

        private Dictionary<OriginXRev, OriginX> OriginXMap = new Dictionary<OriginXRev, OriginX>()
        {
            { OriginXRev.Center, OriginX.Center },
            { OriginXRev.Left,    OriginX.Left },
            { OriginXRev.Right, OriginX.Right },
        };

        public bool ParentVisibility
        {
            get
            {
                if (Scale.X == 0 || Scale.Y == 0)
                    return false;
                if (!Visible)
                    return false;
                if (Parent != null && Parent is PAN1)
                {
                    return ((PAN1)Parent).ParentVisibility && Visible;
                }
                return true;
            }
        }
    }
}
