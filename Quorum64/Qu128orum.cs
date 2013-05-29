using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Z80;
using ZXMAK2.Engine.Devices.Memory;
using ZXMAK2.Logging;

namespace Quorum64
{
	public class MemoryQuorum128 : MemoryBase
	{
		private IBetaDiskDevice m_betaDisk;
		private Z80CPU m_cpu;
		private bool m_dosPort;
		private bool m_lock;
		private byte[][] m_ramPages = new byte[0x10][];
		private byte[] m_trashPage = new byte[0x4000];
		private const int Q_B_ROM = 0x20;
		private const int Q_BLK_WR = 0x40;
		private const int Q_F_RAM = 1;
		private const int Q_RAM_8 = 8;
		private const int Q_TR_DOS = 0x80;
		private static readonly int[] s_drvDecode = new int[] { 3, 0, 1, 3 };

		public MemoryQuorum128()
		{
			for (int i = 0; i < this.m_ramPages.Length; i++)
			{
				this.m_ramPages[i] = new byte[0x4000];
			}
		}

		public override void BusInit(IBusManager bmgr)
		{
			base.BusInit(bmgr);
			this.m_cpu = bmgr.CPU;
			this.m_betaDisk = bmgr.FindDevice(typeof(IBetaDiskDevice)) as IBetaDiskDevice;
			bmgr.SubscribeWRIO(0x801a, 0x18, new BusWriteIoProc(this.busWritePort7FFD));
			bmgr.SubscribeWRIO(0xA01A, 0x8018, new BusWriteIoProc(this.busWritePort80FD));
			bmgr.SubscribeWRIO(0x91, 0, new BusWriteIoProc(this.busWritePort0000));
			bmgr.SubscribeRESET(new BusSignalProc(this.busReset));
			bmgr.SubscribeNMIACK(new BusSignalProc(this.busNmi));
			if (this.m_betaDisk != null)
			{
				bmgr.SubscribeWRIO(0x9f, 0x80, new BusWriteIoProc(this.busWritePortCMD));
				bmgr.SubscribeWRIO(0x9f, 0x81, new BusWriteIoProc(this.busWritePortTRK));
				bmgr.SubscribeWRIO(0x9f, 130, new BusWriteIoProc(this.busWritePortSEC));
				bmgr.SubscribeWRIO(0x9f, 0x83, new BusWriteIoProc(this.busWritePortDAT));
				bmgr.SubscribeWRIO(0x9f, 0x85, new BusWriteIoProc(this.busWritePortSYS));
				bmgr.SubscribeRDIO(0x9f, 0x80, new BusReadIoProc(this.busReadPortCMD));
				bmgr.SubscribeRDIO(0x9f, 0x81, new BusReadIoProc(this.busReadPortTRK));
				bmgr.SubscribeRDIO(0x9f, 130, new BusReadIoProc(this.busReadPortSEC));
				bmgr.SubscribeRDIO(0x9f, 0x83, new BusReadIoProc(this.busReadPortDAT));
				bmgr.SubscribeRDIO(0x9f, 0x85, new BusReadIoProc(this.busReadPortSYS));
			}
		}

		private void busNmi()
		{
			this.CMR1 = 0;
		}

		private void busReadPortCMD(ushort addr, ref byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.CMD);
			}
		}

		private void busReadPortDAT(ushort addr, ref byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.DAT);
			}
		}

		private void busReadPortSEC(ushort addr, ref byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.SEC);
			}
		}

		private void busReadPortSYS(ushort addr, ref byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.SYS);
			}
		}

		private void busReadPortTRK(ushort addr, ref byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.TRK);
			}
		}

		private void busReset()
		{
			this.CMR0 = 0;
			this.CMR1 = 0;
		}

		private void busWritePort0000(ushort addr, byte value, ref bool iorqge)
		{
			this.CMR1 = value;
		}

		private void busWritePort80FD(ushort addr, byte value, ref bool iorqge)
		{
			Logger.GetLogger().LogTrace(String.Format("Writing {0:X2} to 80fd", value));
		}

		private void busWritePort7FFD(ushort addr, byte value, ref bool iorqge)
		{
			Logger.GetLogger().LogTrace(String.Format("Writing {0:X2} to 7ffd at {1:X4}...", value, m_cpu.regs.PC));
			if (!this.m_lock)
			{
				Logger.GetLogger().LogTrace("Success");
				this.CMR0 = value;
			}
		}

		private void busWritePortCMD(ushort addr, byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.CMD, value);
			}
		}

		private void busWritePortDAT(ushort addr, byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.DAT, value);
			}
		}

		private void busWritePortSEC(ushort addr, byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.SEC, value);
			}
		}

		private void busWritePortSYS(ushort addr, byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				int num = s_drvDecode[value & 3];
				num = ((value & -4) ^ 0x10) | num;
				this.m_betaDisk.SetReg(WD93REG.SYS, (byte)num);
			}
		}

		private void busWritePortTRK(ushort addr, byte value, ref bool iorqge)
		{
			if (iorqge && this.m_dosPort)
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.TRK, value);
			}
		}

		protected override void LoadRom()
		{
			base.LoadRom();
			base.LoadRomPack("Quorum");
		}

		protected override void UpdateMapping()
		{
			this.m_lock = false;
			int num = this.CMR0 & 7;
			num |= (this.CMR0 & 0xc0) >> 3;
			num &= 15;
			int index = (this.CMR0 & 0x10) >> 4;
			int videoPage = ((this.CMR0 & 8) == 0) ? 5 : 7;
			bool flag = (this.CMR1 & 0x40) != 0;
			int num4 = ((this.CMR1 & 8) != 0) ? 8 : 0;
			bool flag2 = (this.CMR1 & 1) != 0;
			this.m_dosPort = true;
			if (this.SEL_TRDOS)
			{
				index = 2;
			}
			if (this.SEL_SHADOW)
			{
				index = 3;
			}
			base.m_ula.SetPageMapping(videoPage, (flag2 && !flag) ? num4 : -1, 5, 2, num);
			base.MapRead0000 = flag2 ? this.RamPages[num4] : this.RomPages[index];
			base.MapRead4000 = this.RamPages[5];
			base.MapRead8000 = this.RamPages[2];
			base.MapReadC000 = this.RamPages[num];
			base.MapWrite0000 = (flag2 && !flag) ? this.RamPages[num4] : this.m_trashPage;
			base.MapWrite4000 = base.MapRead4000;
			base.MapWrite8000 = base.MapRead8000;
			base.MapWriteC000 = base.MapReadC000;

			Logger.GetLogger().LogTrace("Current pages: " + (flag2 ? "ram " + num4.ToString() : "rom " + index.ToString())+
				String.Format(" ram {0} {1} {2}", 5, 2, num) + " screen " + videoPage.ToString());
		}

		public override string Description
		{
			get
			{
				return "Quorum 128 Memory Module";
			}
		}

		public override bool IsMap48
		{
			get
			{
				return false;
			}
		}

		public override string Name
		{
			get
			{
				return "QUORUM 128";
			}
		}

		public override byte[][] RamPages
		{
			get
			{
				return this.m_ramPages;
			}
		}

		public override bool SEL_SHADOW
		{
			get
			{
				return ((this.CMR1 & 0x20) == 0);
			}
			set
			{
				if (value)
				{
					this.CMR1 = (byte)(this.CMR1 | 0x20);
				}
				else
				{
					this.CMR1 = (byte)(this.CMR1 & 0xdf);
				}
				this.UpdateMapping();
			}
		}
	}
}
