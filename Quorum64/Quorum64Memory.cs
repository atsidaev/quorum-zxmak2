using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using System.Windows.Forms;
using ZXMAK2.Engine.Devices.Memory;
using System.IO;

public class Quorum64Memory : MemoryPentagon128
{
	public Quorum64Memory() : base()
	{
	}

	public override string Description
	{
		get { return "Quorum 64 Memory"; }
	}

	public override string Name
	{
		get { return "Quorum 64"; }
	}

	public override void BusInit(IBusManager bmgr)
	{
		base.BusInit(bmgr);
	}

	protected override void LoadRom()
	{
		base.LoadRom();
//		base.LoadRomPack("Pentagon");
		RomPages[0] = File.ReadAllBytes(@"e:\Distr\quorum\QU4I1993.ROM");
	}

	public override bool IsMap48
	{
		get
		{
			return true;
		}
	}

}
