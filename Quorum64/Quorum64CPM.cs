//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ZXMAK2.Engine.Devices.Disk;
//using ZXMAK2.Engine.Z80;
//using ZXMAK2.Engine.Interfaces;
//using ZXMAK2.Engine.Serializers;
//using ZXMAK2.Logging;
//using System.Xml;
//using ZXMAK2;

//namespace Quorum64
//{
//    public class Quorum64CPM : IConfigurable, IBetaDiskDevice, IBusDevice
//    {
//        private byte cmd;
//        private const byte CMD_DELAY = 4;
//        private const byte CMD_MULTIPLE = 0x10;
//        private const byte CMD_SEEK_DIR = 0x20;
//        private const byte CMD_SEEK_HEADLOAD = 8;
//        private const byte CMD_SEEK_RATE = 3;
//        private const byte CMD_SEEK_TRKUPD = 0x10;
//        private const byte CMD_SEEK_VERIFY = 4;
//        private const byte CMD_SIDE = 8;
//        private const byte CMD_SIDE_CMP_FLAG = 2;
//        private const byte CMD_SIDE_SHIFT = 3;
//        private const byte CMD_WRITE_DEL = 1;
//        private byte data;
//        private int drive = 0;
//        private long end_waiting_am;
//        private DiskImage[] fdd = new DiskImage[4];
//        public const int FDD_RPS = 5;
//        private int foundid;
//        private uint idx_cnt;
//        private WD_STATUS idx_status;
//        private long idx_tmo = 0x7fffffffffffffffL;
//        private int m_busOrder;
//        private Z80CPU m_cpu;
//        private bool m_ledDiskIO;
//        private bool m_logIO;
//        private IMemoryDevice m_memory;
//        private bool m_sandbox;
//        private bool m_selPorts;
//        private bool m_selTrdos;
//        private long next;
//        private BETA_STATUS rqs;
//        private int rwlen;
//        private int rwptr;
//        private byte sector;
//        private int side;
//        private int start_crc;
//        private WDSTATE state;
//        private WDSTATE state2;
//        private WD_STATUS status;
//        private int stepdirection = 1;
//        private byte system;
//        private long time;
//        private byte track;
//        private long tshift;
//        private bool wd93_nodelay;
//        public const int Z80FQ = 0x3567e0;

//        public Quorum64CPM()
//        {
//            for (int i = 0; i < this.fdd.Length; i++)
//            {
//                DiskImage image = new DiskImage();
//                image.Init(0xaae60);
//                this.fdd[i] = image;
//            }
//            this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//            this.wd93_nodelay = false;
//        }

//        public void BusConnect()
//        {
//            if (!this.m_sandbox)
//            {
//                foreach (DiskImage image in this.FDD)
//                {
//                    image.Connect();
//                }
//            }
//        }

//        public void BusDisconnect()
//        {
//            if (!this.m_sandbox)
//            {
//                foreach (DiskImage image in this.FDD)
//                {
//                    image.Disconnect();
//                }
//            }
//            if (this.m_memory != null)
//            {
//                this.m_memory.SEL_TRDOS = false;
//            }
//        }

//        public void BusInit(IBusManager bmgr)
//        {
//            this.m_cpu = bmgr.CPU;
//            this.m_sandbox = bmgr.IsSandbox;
//            this.m_memory = bmgr.FindDevice(typeof(IMemoryDevice)) as IMemoryDevice;
//            //bmgr.SubscribeRDMEM_M1(0xff00, 0x3d00, new BusReadProc(this.readMem3D00_M1));
//            //bmgr.SubscribeRDMEM_M1(0xc000, 0x4000, new BusReadProc(this.readMemRam));
//            //bmgr.SubscribeRDMEM_M1(0xc000, 0x8000, new BusReadProc(this.readMemRam));
//            //bmgr.SubscribeRDMEM_M1(0xc000, 0xc000, new BusReadProc(this.readMemRam));
//            bmgr.SubscribeWRIO(0x9f, 0x80, new BusWriteIoProc(this.writePortCMD));
//            bmgr.SubscribeWRIO(0x9f, 0x81, new BusWriteIoProc(this.writePortTRK));
//            bmgr.SubscribeWRIO(0x9f, 0x82, new BusWriteIoProc(this.writePortSEC));
//            bmgr.SubscribeWRIO(0x9f, 0x83, new BusWriteIoProc(this.writePortDATA));
//            bmgr.SubscribeWRIO(0x9f, 0x85, new BusWriteIoProc(this.writePortBETA));
//            bmgr.SubscribeRDIO(0x9f, 0x80, new BusReadIoProc(this.readPortCMD));
//            bmgr.SubscribeRDIO(0x9f, 0x81, new BusReadIoProc(this.readPortTRK));
//            bmgr.SubscribeRDIO(0x9f, 0x82, new BusReadIoProc(this.readPortSEC));
//            bmgr.SubscribeRDIO(0x9f, 0x83, new BusReadIoProc(this.readPortDATA));
//            bmgr.SubscribeRDIO(0x9f, 0x85, new BusReadIoProc(this.readPortBETA));
//            bmgr.SubscribeRESET(new BusSignalProc(this.busReset));
//            foreach (FormatSerializer serializer in this.FDD[0].SerializeManager.GetSerializers())
//            {
//                bmgr.AddSerializer(serializer);
//            }
//        }

//        private void busReset()
//        {
//            this.SEL_TRDOS = false;
//            this.SEL_PORTS = true;
//        }

//        public string DumpState()
//        {
//            string str = string.Format("CMD:    #{0:X2}\nSTATUS: #{1:X2} [{2}]\nTRK:    #{3:X2}\nSEC:    #{4:X2}\nDATA:   #{5:X2}", new object[] { this.cmd, (int)this.status, this.status, this.track, this.sector, this.data });
//            string str2 = string.Format("beta:   #{0:X2} [{1}]\nsystem: #{2:X2}\nstate:  {3}\nstate2: {4}\ndrive:  {5}\nside:   {6}\ntime:   {7}\nnext:   {8}\ntshift: {9}\nrwptr:  {10}\nrwlen:  {11}", new object[] { (int)this.rqs, this.rqs, this.system, this.state, this.state2, this.drive, this.side, this.time, this.next, this.tshift, this.rwptr, this.rwlen });
//            string str3 = string.Format("CYL COUNT: {0}\nHEAD POS:  {1}\nREADY:     {2}\nTR00:      {3}", new object[] { this.fdd[this.drive].CylynderCount, this.fdd[this.drive].HeadCylynder, this.fdd[this.drive].IsREADY, this.fdd[this.drive].IsTRK00 });
//            return string.Format("{0}\n--------------------------\n{1}\n--------------------------\n{2}", str, str2, str3);
//        }

//        private void find_marker(long toTact)
//        {
//            if (this.wd93_nodelay && (this.fdd[this.drive].HeadCylynder != this.track))
//            {
//                this.fdd[this.drive].HeadCylynder = this.track;
//            }
//            this.load();
//            this.foundid = -1;
//            if ((this.fdd[this.drive].motor > 0) && this.fdd[this.drive].IsREADY)
//            {
//                long num = this.fdd[this.drive].t.trklen * this.fdd[this.drive].t.ts_byte;
//                int num2 = (int)(((this.next + this.tshift) % num) / this.fdd[this.drive].t.ts_byte);
//                long num3 = 0x7fffffffffffffffL;
//                for (int i = 0; i < this.fdd[this.drive].t.HeaderList.Count; i++)
//                {
//                    int idOffset = this.fdd[this.drive].t.HeaderList[i].idOffset;
//                    int num6 = (idOffset > num2) ? (idOffset - num2) : ((this.fdd[this.drive].t.trklen + idOffset) - num2);
//                    if (num6 < num3)
//                    {
//                        num3 = num6;
//                        this.foundid = i;
//                    }
//                }
//                if (this.foundid != -1)
//                {
//                    num3 *= this.fdd[this.drive].t.ts_byte;
//                }
//                else
//                {
//                    num3 = 0x6acfc0;
//                }
//                if (this.wd93_nodelay && (this.foundid != -1))
//                {
//                    int num7 = this.fdd[this.drive].t.HeaderList[this.foundid].idOffset + 2;
//                    this.tshift = (((num7 * this.fdd[this.drive].t.ts_byte) - (this.next % num)) + num) % num;
//                    num3 = 100;
//                }
//                this.next += num3;
//            }
//            else
//            {
//                this.next = toTact + 1;
//            }
//            if (this.fdd[this.drive].IsREADY && (this.next > this.end_waiting_am))
//            {
//                this.next = this.end_waiting_am;
//                this.foundid = -1;
//            }
//            this.state = WDSTATE.S_WAIT;
//            this.state2 = WDSTATE.S_FOUND_NEXT_ID;
//        }

//        private void getindex()
//        {
//            long num = this.fdd[this.drive].t.trklen * this.fdd[this.drive].t.ts_byte;
//            long num2 = (this.next + this.tshift) % num;
//            if (!this.wd93_nodelay)
//            {
//                this.next += num - num2;
//            }
//            this.rwptr = 0;
//            this.rwlen = this.fdd[this.drive].t.trklen;
//            this.state = WDSTATE.S_WAIT;
//        }

//        public byte GetReg(RegWD1793 reg, long tact)
//        {
//            this.process(tact);
//            byte status = 0xff;
//            switch (reg)
//            {
//                case RegWD1793.COMMAND:
//                    this.rqs = (BETA_STATUS)((byte)(this.rqs & ((BETA_STATUS)0x7f)));
//                    if ((this.system & 8) == 0)
//                    {
//                        status = (byte)(this.status & ~WD_STATUS.WDS_RECORDT);
//                        if (!this.fdd[this.drive].Present)
//                        {
//                            status = (byte)(status & -3);
//                        }
//                        break;
//                    }
//                    status = (byte)this.status;
//                    if (!this.fdd[this.drive].Present)
//                    {
//                        status = (byte)(status & -3);
//                    }
//                    break;

//                case RegWD1793.TRACK:
//                    status = this.track;
//                    break;

//                case RegWD1793.SECTOR:
//                    status = this.sector;
//                    break;

//                case RegWD1793.DATA:
//                    this.status &= ~WD_STATUS.WDS_INDEX;
//                    this.rqs = (BETA_STATUS)((byte)(this.rqs & ((BETA_STATUS)0xbf)));
//                    status = this.data;
//                    break;

//                case RegWD1793.BETA128:
//                    status = (byte)(this.rqs | ((BETA_STATUS)0x3f));
//                    break;

//                default:
//                    throw new InvalidOperationException();
//            }
//            if (this.LogIO)
//            {
//                Logger.GetLogger().LogTrace(string.Format("WD93 {0} ==> #{1:X2} [PC=#{2:X4}, T={3}]", new object[] { reg, status, this.m_cpu.regs.PC, tact }));
//            }
//            return status;
//        }

//        private void load()
//        {
//            if (this.fdd[this.drive].t.sf)
//            {
//                this.fdd[this.drive].t.RefreshHeaders();
//            }
//            this.fdd[this.drive].t.sf = false;
//            this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//        }

//        public void LoadConfig(XmlNode itemNode)
//        {
//            this.NoDelay = Utils.GetXmlAttributeAsBool(itemNode, "noDelay", false);
//            this.LogIO = Utils.GetXmlAttributeAsBool(itemNode, "logIO", false);
//            for (int i = 0; i < 4; i++)
//            {
//                this.FDD[i].FileName = string.Empty;
//                this.FDD[i].IsWP = true;
//                this.FDD[i].Present = false;
//            }
//            foreach (XmlNode node in itemNode.SelectNodes("Drive"))
//            {
//                int index = Utils.GetXmlAttributeAsInt32(node, "index", 0);
//                string str = Utils.GetXmlAttributeAsString(node, "fileName", string.Empty);
//                bool flag = Utils.GetXmlAttributeAsBool(node, "inserted", false);
//                bool flag2 = Utils.GetXmlAttributeAsBool(node, "readOnly", true);
//                if ((index >= 0) && (index <= 3))
//                {
//                    this.FDD[index].FileName = str;
//                    this.FDD[index].IsWP = flag2;
//                    this.FDD[index].Present = flag;
//                }
//            }
//        }

//        private bool notready()
//        {
//            if (!this.wd93_nodelay || (((byte)(this.rqs & BETA_STATUS.DRQ)) == 0))
//            {
//                return false;
//            }
//            if (this.next > this.end_waiting_am)
//            {
//                return false;
//            }
//            this.state2 = this.state;
//            this.state = WDSTATE.S_WAIT;
//            this.next += this.fdd[this.drive].t.ts_byte;
//            return true;
//        }

//        private void process(long toTact)
//        {
//            this.time = toTact;
//            if ((this.time > this.fdd[this.drive].motor) && ((this.system & 8) != 0))
//            {
//                this.fdd[this.drive].motor = 0;
//            }
//            if (this.fdd[this.drive].IsREADY)
//            {
//                this.status &= ~WD_STATUS.WDS_NOTRDY;
//            }
//            else
//            {
//                this.status |= WD_STATUS.WDS_NOTRDY;
//            }
//            if (((this.cmd & 0x80) == 0) || ((this.cmd & 240) == 0xd0))
//            {
//                this.idx_status &= ~WD_STATUS.WDS_INDEX;
//                this.status &= ~WD_STATUS.WDS_INDEX;
//                if (this.state != WDSTATE.S_IDLE)
//                {
//                    this.status &= ~(WD_STATUS.WDS_TRK00 | WD_STATUS.WDS_INDEX);
//                    if ((this.fdd[this.drive].motor > 0) && ((this.system & 8) != 0))
//                    {
//                        this.status |= WD_STATUS.WDS_RECORDT;
//                    }
//                    if (this.fdd[this.drive].IsTRK00)
//                    {
//                        this.status |= WD_STATUS.WDS_TRK00;
//                    }
//                }
//                if ((this.fdd[this.drive].IsREADY && (this.fdd[this.drive].motor > 0)) && (((this.time + this.tshift) % 0xaae60) < 0x36b0))
//                {
//                    if (this.state == WDSTATE.S_IDLE)
//                    {
//                        if (this.time < this.idx_tmo)
//                        {
//                            this.status |= WD_STATUS.WDS_INDEX;
//                        }
//                    }
//                    else
//                    {
//                        this.status |= WD_STATUS.WDS_INDEX;
//                    }
//                    this.idx_status |= WD_STATUS.WDS_INDEX;
//                }
//            }
//        Label_01AB:
//            switch (this.state)
//            {
//                case WDSTATE.S_IDLE:
//                    this.status &= ~WD_STATUS.WDS_BUSY;
//                    if ((this.idx_cnt >= 15) || (this.time > this.idx_tmo))
//                    {
//                        this.idx_cnt = 15;
//                        this.status &= WD_STATUS.WDS_NOTRDY;
//                        this.status |= WD_STATUS.WDS_NOTRDY;
//                        this.fdd[this.drive].motor = 0;
//                    }
//                    this.rqs = BETA_STATUS.INTRQ;
//                    return;

//                case WDSTATE.S_WAIT:
//                    if (this.time >= this.next)
//                    {
//                        this.state = this.state2;
//                        goto Label_01AB;
//                    }
//                    return;

//                case WDSTATE.S_DELAY_BEFORE_CMD:
//                    if (!this.wd93_nodelay && ((this.cmd & 4) != 0))
//                    {
//                        this.next += 0xcd14;
//                    }
//                    this.status = (this.status | WD_STATUS.WDS_BUSY) & ~(WD_STATUS.WDS_WRITEP | WD_STATUS.WDS_RECORDT | WD_STATUS.WDS_NOTFOUND | WD_STATUS.WDS_TRK00 | WD_STATUS.WDS_INDEX);
//                    this.state2 = WDSTATE.S_CMD_RW;
//                    this.state = WDSTATE.S_WAIT;
//                    goto Label_01AB;

//                case WDSTATE.S_CMD_RW:
//                    if ((((this.cmd & 0xe0) != 160) && ((this.cmd & 240) != 240)) || !this.fdd[this.drive].IsWP)
//                    {
//                        if (((this.cmd & 0xc0) == 0x80) || ((this.cmd & 0xf8) == 0xc0))
//                        {
//                            this.end_waiting_am = this.next + 0x3567e0;
//                            this.find_marker(toTact);
//                        }
//                        else if ((this.cmd & 0xf8) == 240)
//                        {
//                            this.rqs = BETA_STATUS.DRQ;
//                            this.status |= WD_STATUS.WDS_INDEX;
//                            this.next += 3 * this.fdd[this.drive].t.ts_byte;
//                            this.state2 = WDSTATE.S_WRTRACK;
//                            this.state = WDSTATE.S_WAIT;
//                        }
//                        else if ((this.cmd & 0xf8) == 0xe0)
//                        {
//                            this.load();
//                            this.rwptr = 0;
//                            this.rwlen = this.fdd[this.drive].t.trklen;
//                            this.state2 = WDSTATE.S_READ;
//                            this.getindex();
//                        }
//                        else
//                        {
//                            this.state = WDSTATE.S_IDLE;
//                        }
//                    }
//                    else
//                    {
//                        this.status |= WD_STATUS.WDS_WRITEP;
//                        this.state = WDSTATE.S_IDLE;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_FOUND_NEXT_ID:
//                    if (this.fdd[this.drive].IsREADY)
//                    {
//                        if ((this.next >= this.end_waiting_am) || (this.foundid == -1))
//                        {
//                            this.status |= WD_STATUS.WDS_NOTFOUND;
//                            this.state = WDSTATE.S_IDLE;
//                        }
//                        else
//                        {
//                            this.status &= ~WD_STATUS.WDS_CRCERR;
//                            this.load();
//                            if ((this.cmd & 0x80) == 0)
//                            {
//                                if (this.fdd[this.drive].t.HeaderList[this.foundid].c != this.track)
//                                {
//                                    this.find_marker(toTact);
//                                }
//                                else if (!this.fdd[this.drive].t.HeaderList[this.foundid].c1)
//                                {
//                                    this.status |= WD_STATUS.WDS_CRCERR;
//                                    this.find_marker(toTact);
//                                }
//                                else
//                                {
//                                    this.state = WDSTATE.S_IDLE;
//                                }
//                            }
//                            else if ((this.cmd & 240) == 0xc0)
//                            {
//                                this.rwptr = this.fdd[this.drive].t.HeaderList[this.foundid].idOffset;
//                                this.rwlen = 6;
//                                this.data = this.fdd[this.drive].t.RawRead(this.rwptr++);
//                                this.rwlen--;
//                                this.rqs = BETA_STATUS.DRQ;
//                                this.status |= WD_STATUS.WDS_INDEX;
//                                this.next += this.fdd[this.drive].t.ts_byte;
//                                this.state = WDSTATE.S_WAIT;
//                                this.state2 = WDSTATE.S_READ;
//                            }
//                            else if ((this.fdd[this.drive].t.HeaderList[this.foundid].c != this.track) || (this.fdd[this.drive].t.HeaderList[this.foundid].n != this.sector))
//                            {
//                                this.find_marker(toTact);
//                            }
//                            else if (((this.cmd & 2) != 0) && ((((this.cmd >> 3) ^ this.fdd[this.drive].t.HeaderList[this.foundid].s) & 1) != 0))
//                            {
//                                this.find_marker(toTact);
//                            }
//                            else if (!this.fdd[this.drive].t.HeaderList[this.foundid].c1)
//                            {
//                                this.status |= WD_STATUS.WDS_CRCERR;
//                                this.find_marker(toTact);
//                            }
//                            else if ((this.cmd & 0x20) != 0)
//                            {
//                                this.rqs = BETA_STATUS.DRQ;
//                                this.status |= WD_STATUS.WDS_INDEX;
//                                this.next += this.fdd[this.drive].t.ts_byte * 9;
//                                this.state = WDSTATE.S_WAIT;
//                                this.state2 = WDSTATE.S_WRSEC;
//                            }
//                            else if (this.fdd[this.drive].t.HeaderList[this.foundid].dataOffset < 0)
//                            {
//                                this.find_marker(toTact);
//                            }
//                            else
//                            {
//                                if (!this.wd93_nodelay)
//                                {
//                                    this.next += this.fdd[this.drive].t.ts_byte * (this.fdd[this.drive].t.HeaderList[this.foundid].dataOffset - this.fdd[this.drive].t.HeaderList[this.foundid].idOffset);
//                                }
//                                this.state = WDSTATE.S_WAIT;
//                                this.state2 = WDSTATE.S_RDSEC;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        this.end_waiting_am = this.next + 0x3567e0;
//                        this.find_marker(toTact);
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_RDSEC:
//                    if (this.fdd[this.drive].t.RawRead(this.fdd[this.drive].t.HeaderList[this.foundid].dataOffset - 1) != 0xf8)
//                    {
//                        this.status &= ~WD_STATUS.WDS_RECORDT;
//                        break;
//                    }
//                    this.status |= WD_STATUS.WDS_RECORDT;
//                    break;

//                case WDSTATE.S_READ:
//                    if (!this.notready())
//                    {
//                        this.load();
//                        if (this.fdd[this.drive].Present)
//                        {
//                            if (this.rwlen > 0)
//                            {
//                                if (((byte)(this.rqs & BETA_STATUS.DRQ)) != 0)
//                                {
//                                    this.status |= WD_STATUS.WDS_TRK00;
//                                }
//                                this.data = this.fdd[this.drive].t.RawRead(this.rwptr++);
//                                this.rwlen--;
//                                this.rqs = BETA_STATUS.DRQ;
//                                this.status |= WD_STATUS.WDS_INDEX;
//                                if (!this.wd93_nodelay)
//                                {
//                                    this.next += this.fdd[this.drive].t.ts_byte;
//                                }
//                                else
//                                {
//                                    this.next = this.time + 1;
//                                }
//                                this.state = WDSTATE.S_WAIT;
//                                this.state2 = WDSTATE.S_READ;
//                            }
//                            else
//                            {
//                                if ((this.cmd & 0xe0) == 0x80)
//                                {
//                                    if (!this.fdd[this.drive].t.HeaderList[this.foundid].c2)
//                                    {
//                                        this.status |= WD_STATUS.WDS_CRCERR;
//                                    }
//                                    if ((this.cmd & 0x10) != 0)
//                                    {
//                                        this.sector = (byte)(this.sector + 1);
//                                        this.state = WDSTATE.S_CMD_RW;
//                                        goto Label_01AB;
//                                    }
//                                }
//                                if (((this.cmd & 240) == 0xc0) && !this.fdd[this.drive].t.HeaderList[this.foundid].c1)
//                                {
//                                    this.status |= WD_STATUS.WDS_CRCERR;
//                                }
//                                this.state = WDSTATE.S_IDLE;
//                            }
//                        }
//                        else
//                        {
//                            this.status |= WD_STATUS.WDS_NOTFOUND;
//                            this.state = WDSTATE.S_IDLE;
//                        }
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_WRSEC:
//                    this.load();
//                    if (((byte)(this.rqs & BETA_STATUS.DRQ)) == 0)
//                    {
//                        DiskImage image1 = this.fdd[this.drive];
//                        image1.ModifyFlag |= ModifyFlag.SectorLevel;
//                        this.rwptr = ((this.fdd[this.drive].t.HeaderList[this.foundid].idOffset + 6) + 11) + 11;
//                        this.rwlen = 0;
//                        while (this.rwlen < 12)
//                        {
//                            this.fdd[this.drive].t.RawWrite(this.rwptr++, 0, false);
//                            this.rwlen++;
//                        }
//                        this.rwlen = 0;
//                        while (this.rwlen < 3)
//                        {
//                            this.fdd[this.drive].t.RawWrite(this.rwptr++, 0xa1, true);
//                            this.rwlen++;
//                        }
//                        this.fdd[this.drive].t.RawWrite(this.rwptr++, ((this.cmd & 1) != 0) ? ((byte)0xf8) : ((byte)0xfb), false);
//                        this.rwlen = ((int)0x80) << (this.fdd[this.drive].t.HeaderList[this.foundid].l & 3);
//                        this.state = WDSTATE.S_WRITE;
//                    }
//                    else
//                    {
//                        this.status |= WD_STATUS.WDS_TRK00;
//                        this.state = WDSTATE.S_IDLE;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_WRITE:
//                    if (!this.notready())
//                    {
//                        if (((byte)(this.rqs & BETA_STATUS.DRQ)) != 0)
//                        {
//                            this.status |= WD_STATUS.WDS_TRK00;
//                            this.data = 0;
//                        }
//                        this.fdd[this.drive].t.RawWrite(this.rwptr++, this.data, false);
//                        this.rwlen--;
//                        if (this.rwptr == this.fdd[this.drive].t.trklen)
//                        {
//                            this.rwptr = 0;
//                        }
//                        this.fdd[this.drive].t.sf = true;
//                        if (this.rwlen <= 0)
//                        {
//                            int size = (((int)0x80) << (this.fdd[this.drive].t.HeaderList[this.foundid].l & 3)) + 1;
//                            byte[] data = new byte[0x808];
//                            if (this.rwptr < size)
//                            {
//                                for (int i = 0; i < this.rwptr; i++)
//                                {
//                                    data[i] = this.fdd[this.drive].t.RawRead((this.fdd[this.drive].t.trklen - this.rwptr) + i);
//                                }
//                                for (int j = 0; j < (size - this.rwptr); j++)
//                                {
//                                    data[this.rwptr + j] = this.fdd[this.drive].t.RawRead(j);
//                                }
//                            }
//                            else
//                            {
//                                for (int k = 0; k < size; k++)
//                                {
//                                    data[k] = this.fdd[this.drive].t.RawRead((this.rwptr - size) + k);
//                                }
//                            }
//                            uint num5 = wd93_crc(data, 0, size);
//                            this.fdd[this.drive].t.RawWrite(this.rwptr++, (byte)num5, false);
//                            this.fdd[this.drive].t.RawWrite(this.rwptr++, (byte)(num5 >> 8), false);
//                            this.fdd[this.drive].t.RawWrite(this.rwptr, 0xff, false);
//                            if ((this.cmd & 0x10) != 0)
//                            {
//                                this.sector = (byte)(this.sector + 1);
//                                this.state = WDSTATE.S_CMD_RW;
//                            }
//                            else
//                            {
//                                this.state = WDSTATE.S_IDLE;
//                            }
//                        }
//                        else
//                        {
//                            if (!this.wd93_nodelay)
//                            {
//                                this.next += this.fdd[this.drive].t.ts_byte;
//                            }
//                            this.state = WDSTATE.S_WAIT;
//                            this.state2 = WDSTATE.S_WRITE;
//                            this.rqs = BETA_STATUS.DRQ;
//                            this.status |= WD_STATUS.WDS_INDEX;
//                        }
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_WRTRACK:
//                    if (((byte)(this.rqs & BETA_STATUS.DRQ)) == 0)
//                    {
//                        DiskImage image2 = this.fdd[this.drive];
//                        image2.ModifyFlag |= ModifyFlag.TrackLevel;
//                        this.state2 = WDSTATE.S_WR_TRACK_DATA;
//                        this.start_crc = 0;
//                        this.getindex();
//                        this.end_waiting_am = this.next + 0x3567e0;
//                    }
//                    else
//                    {
//                        this.status |= WD_STATUS.WDS_TRK00;
//                        this.state = WDSTATE.S_IDLE;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_WR_TRACK_DATA:
//                    if (!this.notready())
//                    {
//                        if (((byte)(this.rqs & BETA_STATUS.DRQ)) != 0)
//                        {
//                            this.status |= WD_STATUS.WDS_TRK00;
//                            this.data = 0;
//                        }
//                        this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//                        this.fdd[this.drive].t.sf = true;
//                        bool clock = false;
//                        byte num6 = this.data;
//                        uint num7 = 0;
//                        if (this.data == 0xf5)
//                        {
//                            num6 = 0xa1;
//                            clock = true;
//                            this.start_crc = this.rwptr + 1;
//                        }
//                        if (this.data == 0xf6)
//                        {
//                            num6 = 0xc2;
//                            clock = true;
//                        }
//                        if (this.data == 0xf7)
//                        {
//                            num7 = this.fdd[this.drive].t.WD1793_CRC(this.start_crc, this.rwptr - this.start_crc);
//                            num6 = (byte)(num7 & 0xff);
//                        }
//                        this.fdd[this.drive].t.RawWrite(this.rwptr++, num6, clock);
//                        this.rwlen--;
//                        if (this.data == 0xf7)
//                        {
//                            this.fdd[this.drive].t.RawWrite(this.rwptr++, (byte)(num7 >> 8), clock);
//                            this.rwlen--;
//                        }
//                        if (this.rwlen <= 0)
//                        {
//                            this.state = WDSTATE.S_IDLE;
//                        }
//                        else
//                        {
//                            if (!this.wd93_nodelay)
//                            {
//                                this.next += this.fdd[this.drive].t.ts_byte;
//                            }
//                            this.state2 = WDSTATE.S_WR_TRACK_DATA;
//                            this.state = WDSTATE.S_WAIT;
//                            this.rqs = BETA_STATUS.DRQ;
//                            this.status |= WD_STATUS.WDS_INDEX;
//                        }
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_TYPE1_CMD:
//                    this.status = (this.status | WD_STATUS.WDS_BUSY) & ~(WD_STATUS.WDS_WRITEP | WD_STATUS.WDS_NOTFOUND | WD_STATUS.WDS_CRCERR | WD_STATUS.WDS_INDEX);
//                    this.rqs = BETA_STATUS.NONE;
//                    if (this.fdd[this.drive].IsWP)
//                    {
//                        this.status |= WD_STATUS.WDS_WRITEP;
//                    }
//                    this.fdd[this.drive].motor = this.next + 0x6acfc0;
//                    this.state2 = WDSTATE.S_SEEKSTART;
//                    if ((this.cmd & 0xe0) != 0)
//                    {
//                        if ((this.cmd & 0x40) != 0)
//                        {
//                            this.stepdirection = ((this.cmd & 0x20) != 0) ? ((sbyte)(-1)) : ((sbyte)1);
//                        }
//                        this.state2 = WDSTATE.S_STEP;
//                    }
//                    if (!this.wd93_nodelay)
//                    {
//                        this.next += 0x20;
//                    }
//                    this.state = WDSTATE.S_WAIT;
//                    goto Label_01AB;

//                case WDSTATE.S_STEP:
//                    if (!this.fdd[this.drive].IsTRK00 || ((this.cmd & 240) != 0))
//                    {
//                        if (((this.cmd & 0xe0) == 0) || ((this.cmd & 0x10) != 0))
//                        {
//                            this.track = (byte)(this.track + this.stepdirection);
//                        }
//                        DiskImage image3 = this.fdd[this.drive];
//                        image3.HeadCylynder += this.stepdirection;
//                        if (this.fdd[this.drive].HeadCylynder >= (this.fdd[this.drive].CylynderCount - 1))
//                        {
//                            this.fdd[this.drive].HeadCylynder = this.fdd[this.drive].CylynderCount - 1;
//                        }
//                        this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//                        uint[] numArray = new uint[] { 6, 12, 20, 30 };
//                        if (!this.wd93_nodelay)
//                        {
//                            this.next += (numArray[this.cmd & 3] * 0x3567e0) / 0x3e8;
//                        }
//                        this.state2 = ((this.cmd & 0xe0) != 0) ? WDSTATE.S_VERIFY : WDSTATE.S_SEEK;
//                        this.state = WDSTATE.S_WAIT;
//                    }
//                    else
//                    {
//                        this.track = 0;
//                        this.state = WDSTATE.S_VERIFY;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_SEEKSTART:
//                    if ((this.cmd & 0x10) == 0)
//                    {
//                        this.track = 0xff;
//                        this.data = 0;
//                    }
//                    if (this.data == this.track)
//                    {
//                        this.state = WDSTATE.S_VERIFY;
//                    }
//                    else
//                    {
//                        this.stepdirection = (this.data < this.track) ? -1 : 1;
//                        this.state = WDSTATE.S_STEP;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_SEEK:
//                    if (this.data != this.track)
//                    {
//                        this.stepdirection = (this.data < this.track) ? -1 : 1;
//                        this.state = WDSTATE.S_STEP;
//                    }
//                    else
//                    {
//                        this.state = WDSTATE.S_VERIFY;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_VERIFY:
//                    if ((this.cmd & 4) != 0)
//                    {
//                        this.end_waiting_am = this.next + 0x401640;
//                        this.load();
//                        this.find_marker(toTact);
//                    }
//                    else
//                    {
//                        this.status |= WD_STATUS.WDS_BUSY;
//                        this.state2 = WDSTATE.S_IDLE;
//                        this.state = WDSTATE.S_WAIT;
//                        this.idx_tmo = this.next + 0xa037a0;
//                    }
//                    goto Label_01AB;

//                case WDSTATE.S_RESET:
//                    if (!this.fdd[this.drive].IsTRK00)
//                    {
//                        DiskImage image4 = this.fdd[this.drive];
//                        image4.HeadCylynder--;
//                        this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//                    }
//                    else
//                    {
//                        this.state = WDSTATE.S_IDLE;
//                    }
//                    this.next += 0x5208;
//                    goto Label_01AB;

//                default:
//                    throw new Exception("WD1793.process - WD1793 in wrong state");
//            }
//            this.rwptr = this.fdd[this.drive].t.HeaderList[this.foundid].dataOffset;
//            this.rwlen = ((int)0x80) << (this.fdd[this.drive].t.HeaderList[this.foundid].l & 3);
//            this.data = this.fdd[this.drive].t.RawRead(this.rwptr++);
//            this.rwlen--;
//            this.rqs = BETA_STATUS.DRQ;
//            this.status |= WD_STATUS.WDS_INDEX;
//            this.next += this.fdd[this.drive].t.ts_byte;
//            this.state = WDSTATE.S_WAIT;
//            this.state2 = WDSTATE.S_READ;
//            goto Label_01AB;
//        }

//        private void readMem3D00_M1(ushort addr, ref byte value)
//        {
//            if (!this.SEL_TRDOS && this.m_memory.IsRom48)
//            {
//                this.SEL_TRDOS = true;
//            }
//        }

//        private void readMemRam(ushort addr, ref byte value)
//        {
//            if (this.SEL_TRDOS)
//            {
//                this.SEL_TRDOS = false;
//            }
//        }

//        private void readPortBETA(ushort addr, ref byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                value = this.GetReg(RegWD1793.BETA128, this.m_cpu.Tact);
//                this.LedDiskIO = true;
//            }
//        }

//        private void readPortCMD(ushort addr, ref byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                value = this.GetReg(RegWD1793.COMMAND, this.m_cpu.Tact);
//                this.LedDiskIO = true;
//            }
//        }

//        private void readPortDATA(ushort addr, ref byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                value = this.GetReg(RegWD1793.DATA, this.m_cpu.Tact);
//                this.LedDiskIO = true;
//            }
//        }

//        private void readPortSEC(ushort addr, ref byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                value = this.GetReg(RegWD1793.SECTOR, this.m_cpu.Tact);
//            }
//        }

//        private void readPortTRK(ushort addr, ref byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                value = this.GetReg(RegWD1793.TRACK, this.m_cpu.Tact);
//            }
//        }

//        public void SaveConfig(XmlNode itemNode)
//        {
//            Utils.SetXmlAttribute(itemNode, "noDelay", this.NoDelay);
//            Utils.SetXmlAttribute(itemNode, "logIO", this.LogIO);
//            for (int i = 0; i < 4; i++)
//            {
//                XmlNode node = itemNode.AppendChild(itemNode.OwnerDocument.CreateElement("Drive"));
//                Utils.SetXmlAttribute(node, "index", i);
//                if (!string.IsNullOrEmpty(this.FDD[i].FileName))
//                {
//                    Utils.SetXmlAttribute(node, "fileName", this.FDD[i].FileName);
//                }
//                Utils.SetXmlAttribute(node, "readOnly", this.FDD[i].IsWP.ToString());
//                Utils.SetXmlAttribute(node, "inserted", this.FDD[i].Present.ToString());
//            }
//        }

//        public void SetReg(RegWD1793 reg, byte value, long tact)
//        {
//            if (this.LogIO)
//            {
//                Logger.GetLogger().LogTrace(string.Format("WD93 {0} <== #{1:X2} [PC=#{2:X4}, T={3}]", new object[] { reg, value, this.m_cpu.regs.PC, tact }));
//            }
//            this.process(tact);
//            switch (reg)
//            {
//                case RegWD1793.COMMAND:
//                    if ((value & 240) != 0xd0)
//                    {
//                        if ((this.status & WD_STATUS.WDS_BUSY) == WD_STATUS.WDS_NONE)
//                        {
//                            this.cmd = value;
//                            this.next = tact;
//                            this.status |= WD_STATUS.WDS_BUSY;
//                            this.rqs = BETA_STATUS.NONE;
//                            this.idx_cnt = 0;
//                            this.idx_tmo = 0x7fffffffffffffffL;
//                            if ((this.cmd & 0x80) != 0)
//                            {
//                                if ((this.status & WD_STATUS.WDS_NOTRDY) != WD_STATUS.WDS_NONE)
//                                {
//                                    this.state2 = WDSTATE.S_IDLE;
//                                    this.state = WDSTATE.S_WAIT;
//                                    this.next = tact + 0xaae60;
//                                    this.rqs = BETA_STATUS.INTRQ;
//                                }
//                                else
//                                {
//                                    if ((this.fdd[this.drive].motor > 0) || this.wd93_nodelay)
//                                    {
//                                        this.fdd[this.drive].motor = this.next + 0x6acfc0;
//                                    }
//                                    this.state = WDSTATE.S_DELAY_BEFORE_CMD;
//                                }
//                            }
//                            else
//                            {
//                                this.state = WDSTATE.S_TYPE1_CMD;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        int num = value & 15;
//                        this.next = tact;
//                        this.idx_cnt = 0;
//                        this.idx_tmo = this.next + 0xa037a0;
//                        this.cmd = value;
//                        if (num != 0)
//                        {
//                            if ((num & 8) != 0)
//                            {
//                                this.state = WDSTATE.S_IDLE;
//                                this.rqs = BETA_STATUS.INTRQ;
//                                this.status &= ~WD_STATUS.WDS_BUSY;
//                            }
//                            else if ((num & 4) != 0)
//                            {
//                                this.state = WDSTATE.S_IDLE;
//                                this.rqs = BETA_STATUS.INTRQ;
//                                this.status &= ~WD_STATUS.WDS_BUSY;
//                            }
//                            else if ((num & 2) != 0)
//                            {
//                                this.state = WDSTATE.S_IDLE;
//                                this.rqs = BETA_STATUS.INTRQ;
//                                this.status &= ~WD_STATUS.WDS_BUSY;
//                            }
//                            else if ((num & 1) != 0)
//                            {
//                                this.state = WDSTATE.S_IDLE;
//                                this.rqs = BETA_STATUS.INTRQ;
//                                this.status &= ~WD_STATUS.WDS_BUSY;
//                            }
//                        }
//                        else
//                        {
//                            this.state = WDSTATE.S_IDLE;
//                            this.rqs = BETA_STATUS.NONE;
//                            this.status &= ~WD_STATUS.WDS_BUSY;
//                        }
//                    }
//                    goto Label_038D;

//                case RegWD1793.TRACK:
//                    this.track = value;
//                    goto Label_038D;

//                case RegWD1793.SECTOR:
//                    this.sector = value;
//                    goto Label_038D;

//                case RegWD1793.DATA:
//                    this.data = value;
//                    this.rqs = (BETA_STATUS)((byte)(this.rqs & ((BETA_STATUS)0xbf)));
//                    this.status &= ~WD_STATUS.WDS_INDEX;
//                    goto Label_038D;

//                case RegWD1793.BETA128:
//                    this.drive = value & 3;
//                    this.side = 1 & ~(value >> 4);
//                    this.fdd[this.drive].HeadSide = this.side;
//                    this.fdd[this.drive].t = this.fdd[this.drive].CurrentTrack;
//                    if ((value & 4) != 0)
//                    {
//                        if ((((this.system ^ value) & 8) != 0) && ((this.status & WD_STATUS.WDS_BUSY) == WD_STATUS.WDS_NONE))
//                        {
//                            this.idx_cnt += 1;
//                        }
//                        break;
//                    }
//                    this.status = WD_STATUS.WDS_NOTRDY;
//                    this.rqs = BETA_STATUS.INTRQ;
//                    this.fdd[this.drive].motor = 0;
//                    this.state = WDSTATE.S_IDLE;
//                    this.idx_cnt = 0;
//                    this.idx_status = WD_STATUS.WDS_NONE;
//                    break;

//                default:
//                    throw new Exception("WD1793.SetReg: Invalid register");
//            }
//            this.system = value;
//        Label_038D:
//            this.process(tact);
//        }

//        public static ushort wd93_crc(byte[] data, int startIndex, int size)
//        {
//            uint num2 = 0xcdb4;
//            while (size-- > 0)
//            {
//                num2 ^= (uint)(data[startIndex++] << 8);
//                for (int i = 8; i != 0; i--)
//                {
//                    if (((num2 *= 2) & 0x10000) != 0)
//                    {
//                        num2 ^= 0x1021;
//                    }
//                }
//            }
//            return (ushort)(((num2 & 0xff00) >> 8) | ((num2 & 0xff) << 8));
//        }

//        private void writePortBETA(ushort addr, byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                ushort[] decode = new ushort[] { 3, 0, 1, 3 };
//                ushort drv = decode[value & 3];

//                iorqge = false;
//                SetReg(RegWD1793.BETA128, (byte)(((value & ~3) ^ 0x10) | drv), this.m_cpu.Tact);
//                //this.SetReg(RegWD1793.BETA128, 0, this.m_cpu.Tact);
//                //this.status = WD_STATUS.WDS_TRK00;
//                this.LedDiskIO = true;
//            }
//        }

//        private void writePortCMD(ushort addr, byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                if (value == 0x80)
//                {
//                    Console.WriteLine();
//                }
//                this.SetReg(RegWD1793.COMMAND, value, this.m_cpu.Tact);

//                this.LedDiskIO = true;
//            }
//        }

//        private void writePortDATA(ushort addr, byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                this.SetReg(RegWD1793.DATA, value, this.m_cpu.Tact);
//                this.LedDiskIO = true;
//            }
//        }

//        private void writePortSEC(ushort addr, byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                this.SetReg(RegWD1793.SECTOR, value, this.m_cpu.Tact);
//            }
//        }

//        private void writePortTRK(ushort addr, byte value, ref bool iorqge)
//        {
//            if (iorqge && (this.SEL_TRDOS | this.SEL_PORTS))
//            {
//                iorqge = false;
//                this.SetReg(RegWD1793.TRACK, value, this.m_cpu.Tact);
//            }
//        }

//        public int BusOrder
//        {
//            get
//            {
//                return this.m_busOrder;
//            }
//            set
//            {
//                this.m_busOrder = value;
//            }
//        }

//        public BusCategory Category
//        {
//            get
//            {
//                return BusCategory.Disk;
//            }
//        }

//        public string Description
//        {
//            get
//            {
//                return "WD1793 Generic";
//            }
//        }

//        public DiskImage[] FDD
//        {
//            get
//            {
//                return this.fdd;
//            }
//        }

//        public bool LedDiskIO
//        {
//            get
//            {
//                return this.m_ledDiskIO;
//            }
//            set
//            {
//                this.m_ledDiskIO = value;
//            }
//        }

//        public bool LogIO
//        {
//            get
//            {
//                return this.m_logIO;
//            }
//            set
//            {
//                this.m_logIO = value;
//            }
//        }

//        public string Name
//        {
//            get
//            {
//                return "Quorum CPM";
//            }
//        }

//        public bool NoDelay
//        {
//            get
//            {
//                return this.wd93_nodelay;
//            }
//            set
//            {
//                this.wd93_nodelay = value;
//            }
//        }

//        public bool SEL_PORTS
//        {
//            get
//            {
//                return this.m_selPorts;
//            }
//            set
//            {
//                this.m_selPorts = value;
//            }
//        }

//        public bool SEL_TRDOS
//        {
//            get
//            {
//                return this.m_selTrdos;
//            }
//            set
//            {
//                this.m_selTrdos = value;
//                this.m_memory.SEL_TRDOS = value;
//            }
//        }

//    }
//}
