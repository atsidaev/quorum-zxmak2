//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

//using ZXMAK2.Engine.Z80;
//using ZXMAK2.Engine.Interfaces;
//using ZXMAK2.Engine.Devices.Ula;
//using System.IO;

//public class UlaQuorum64 : UlaQuorum
//{
//    Quorum64Memory memory;
//    Z80CPU cpu;

//    public UlaQuorum64()
//    {
		
//    }

//    public override string Name { get { return "Quorum-64"; } }

//    public override string Description
//    {
//        get
//        {
//            return "Quorum-64 ULA";
//        }
//    }

//    public override void BusInit(IBusManager bmgr)
//    {
//        base.BusInit(bmgr);
//        cpu = bmgr.CPU;
//        memory = (Quorum64Memory)bmgr.FindDevice(typeof(Quorum64Memory));
//    }

//    public void EnableShadowRAM(bool F_RAM)
//    {
//        this.m_videoPage = F_RAM ? 3 : 1;
//    }
//}
