using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using System.Windows.Forms;
using ZXMAK2.Engine.Devices.Memory;
using System.IO;
using ZXMAK2.Engine.Z80;
using ZXMAK2.Logging;

public class Quorum64Memory : MemoryBase
{
	public Quorum64Memory()
		: base()
	{
		for (int i = 0; i < this.m_ramPages.Length; i++)
		{
			this.m_ramPages[i] = new byte[0x4000];
		}
	}

	public override string Description
	{
		get { return "Quorum 64 Memory"; }
	}

	public override string Name
	{
		get { return "Quorum 64"; }
	}

	public override bool IsMap48
	{
		get
		{
			return true;
		}
	}
	
	private IBetaDiskDevice m_betaDisk;
	private Z80CPU m_cpu;
	private bool m_lock = true;
	private byte[][] m_ramPages = new byte[0x10][];
	private byte[] m_trashPage = new byte[0x4000];
	private const int Q_B_ROM = 0x20;
	private const int Q_BLK_WR = 0x40;
	private const int Q_F_RAM = 1;
	private const int Q_RAM_8 = 8;
	private const int Q_TR_DOS = 0x80;

	public override void BusInit(IBusManager bmgr)
	{
		base.BusInit(bmgr);
		this.m_cpu = bmgr.CPU;
		this.m_betaDisk = bmgr.FindDevice(typeof(IBetaDiskDevice)) as IBetaDiskDevice;
		bmgr.SubscribeWRIO(0x801a, 0x18, new BusWriteIoProc(this.busWritePort7FFD));
		bmgr.SubscribeWRIO(0x91, 0, new BusWriteIoProc(this.busWritePort0000));
		bmgr.SubscribeWRIO(0xA00D, 0x800D, new BusWriteIoProc(this.busWritePort80FD)); // 80FD
		bmgr.SubscribeRDIO(0xA00D, 0x800D, new BusReadIoProc(this.busReadPort80FD)); // 80FD
		bmgr.SubscribeRESET(new BusSignalProc(this.busReset));
	}

	private void busReset()
	{
		this.CMR0 = 0;
		this.CMR1 = 0;
	}

	private void busWritePort0000(ushort addr, byte value, ref bool iorqge)
	{
		this.CMR1 = value;
		Logger.GetLogger().LogTrace(String.Format("out 0, #{0:X2} at address #{1:X4}", value, m_cpu.regs.PC));
		UpdateMapping();
	}

	private void busWritePort7FFD(ushort addr, byte value, ref bool iorqge)
	{
		Logger.GetLogger().LogTrace(String.Format("out 7FFD, #{0:X2} at address #{1:X4}", value, m_cpu.regs.PC));
		if (!this.m_lock)
		{
			this.CMR0 = value;
		}
	}

	private void busWritePort80FD(ushort addr, byte value, ref bool iorqge)
	{
		Logger.GetLogger().LogTrace(String.Format("out 80FD, #{0:X2} at address #{1:X4}", value, m_cpu.regs.PC));
	}

	private void busReadPort80FD(ushort addr, ref byte value, ref bool iorqge)
	{
		Logger.GetLogger().LogTrace(String.Format("in 80FD at address #{0:X4}", m_cpu.regs.PC));
	}

	protected override void LoadRom()
	{
		base.LoadRom();
		base.LoadRomPack("Quorum64");
	}

	protected override void UpdateMapping()
	{
		bool shadowRam = (this.CMR1 & 1) != 0;
		int videoPage = ((this.CMR1 & 2) == 0) ? 1 : 3;
		base.m_ula.SetPageMapping(videoPage, (shadowRam) ? 0 : -1, 1, 2, 3);
		base.MapRead0000 = shadowRam ? this.RamPages[0] : this.RomPages[0];
		base.MapRead4000 = this.RamPages[1];
		base.MapRead8000 = this.RamPages[2];
		base.MapReadC000 = this.RamPages[3];
		base.MapWrite0000 = this.RamPages[0];
		base.MapWrite4000 = base.MapRead4000;
		base.MapWrite8000 = base.MapRead8000;
		base.MapWriteC000 = base.MapReadC000;
	}

	public override byte[][] RamPages
	{
		get
		{
			return this.m_ramPages;
		}
	}

	//public override bool SEL_SHADOW
	//{
	//    get
	//    {
	//        return ((this.CMR1 & 0x20) == 0);
	//    }
	//    set
	//    {
	//        if (value)
	//        {
	//            this.CMR1 = (byte) (this.CMR1 | 0x20);
	//        }
	//        else
	//        {
	//            this.CMR1 = (byte) (this.CMR1 & 0xdf);
	//        }
	//        this.UpdateMapping();
	//    }
	//}


}
