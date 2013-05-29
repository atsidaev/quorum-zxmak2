using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Devices.Disk;
using ZXMAK2.Engine.Z80;
using ZXMAK2.Logging;

namespace Quorum64
{
	public class QuorumPorts : IBusDevice
	{
		WD1793 m_betaDisk;
		Z80CPU m_cpu;

		public void BusConnect()
		{

		}

		public void BusDisconnect()
		{

		}

		public void BusInit(IBusManager bmgr)
		{
			m_betaDisk = (WD1793)bmgr.FindDevice(typeof(WD1793));
			m_cpu = bmgr.CPU;

			bmgr.SubscribeWRIO(0x9f, 0x80, new BusWriteIoProc(this.busWritePortCMD));
			bmgr.SubscribeWRIO(0x9f, 0x81, new BusWriteIoProc(this.busWritePortTRK));
			bmgr.SubscribeWRIO(0x9f, 0x82, new BusWriteIoProc(this.busWritePortSEC));
			bmgr.SubscribeWRIO(0x9f, 0x83, new BusWriteIoProc(this.busWritePortDAT));
			bmgr.SubscribeWRIO(0x9f, 0x85, new BusWriteIoProc(this.busWritePortSYS));
			bmgr.SubscribeRDIO(0x9f, 0x80, new BusReadIoProc(this.busReadPortCMD));
			bmgr.SubscribeRDIO(0x9f, 0x81, new BusReadIoProc(this.busReadPortTRK));
			bmgr.SubscribeRDIO(0x9f, 0x82, new BusReadIoProc(this.busReadPortSEC));
			bmgr.SubscribeRDIO(0x9f, 0x83, new BusReadIoProc(this.busReadPortDAT));
			bmgr.SubscribeRDIO(0x9f, 0x85, new BusReadIoProc(this.busReadPortSYS));

		}

		public int BusOrder
		{
			get;
			set;
		}

		public BusCategory Category
		{
			get { return BusCategory.Other; }
		}

		public string Description
		{
			get { return "QQ"; }
		}

		public string Name
		{
			get { return "QQ"; }
		}

		private void busReadPortCMD(ushort addr, ref byte value, ref bool iorqge)
		{
			//	if (iorqge && ((((addr & 0xe3) == 3) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0x83)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.CMD);
			}
		}

		private void busReadPortDAT(ushort addr, ref byte value, ref bool iorqge)
		{
			//	if (iorqge && ((((addr & 0xe3) == 0x63) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xe3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.DAT);
			}
		}

		private void busReadPortSEC(ushort addr, ref byte value, ref bool iorqge)
		{
			//	if (iorqge && ((((addr & 0xe3) == 0x43) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xc3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.SEC);
			}
		}

		private void busReadPortSYS(ushort addr, ref byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0x83) == 0x83) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0x3f)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.SYS);
			}
		}

		private void busReadPortTRK(ushort addr, ref byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0xe3) == 0x23) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xa3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				value = this.m_betaDisk.GetReg(WD93REG.TRK);
			}
		}
		private void busWritePortCMD(ushort addr, byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0xe3) == 3) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0x83)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.CMD, value);
			}
			if ((value & 0x80) != 0)
				Logger.GetLogger().LogTrace(String.Format("Reading sector. Track: {0}, sector: {1}, address {2:X4}", this.m_betaDisk.GetReg(WD93REG.TRK), this.m_betaDisk.GetReg(WD93REG.SEC), m_cpu.regs.PC));
		}

		private void busWritePortDAT(ushort addr, byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0xe3) == 0x63) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xe3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.DAT, value);
			}
		}

		private void busWritePortSEC(ushort addr, byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0xe3) == 0x43) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xc3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.SEC, value);
				//Logger.GetLogger().LogTrace(String.Format("Track: {0}, sector: {1}", this.m_betaDisk.GetReg(WD93REG.TRACK), this.m_betaDisk.GetReg(WD93REG.SECTOR)));
			}
		}

		private void busWritePortSYS(ushort addr, byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0x83) == 0x83) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0x3f)) && ((this.CMR0 & 0x10) != 0))))
			ushort[] decode = new ushort[] { 3, 0, 1, 3 };
			ushort drv = decode[value & 3];


			//this.SetReg(WD93REG.BETA128, 0);
			//this.status = WD_STATUS.WDS_TRK00;

			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.SYS, (byte)(((value & ~3) ^ 0x10) | drv));
			}
		}

		private void busWritePortTRK(ushort addr, byte value, ref bool iorqge)
		{
			//if (iorqge && ((((addr & 0xe3) == 0x23) && ((this.m_cpm && ((this.CMR0 & 0x10) == 0)) || (!this.m_cpm && this.SEL_SHADOW))) || ((this.m_cpm && ((addr & 0xff) == 0xa3)) && ((this.CMR0 & 0x10) != 0))))
			{
				iorqge = false;
				this.m_betaDisk.SetReg(WD93REG.TRK, value);
				//Logger.GetLogger().LogTrace(String.Format("Track: {0}, sector: {1}", this.m_betaDisk.GetReg(WD93REG.TRACK), this.m_betaDisk.GetReg(WD93REG.SECTOR)));
			}
		}

	}
}
