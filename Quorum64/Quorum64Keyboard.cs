using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Z80;

namespace Quorum64
{
	public class Quorum64Keyboard : IKeyboardDevice, IBusDevice
	{
		private int m_busOrder;
		private long m_keyboardState;
		private Z80CPU m_cpu;

		public void BusConnect()
		{
		}

		public void BusDisconnect()
		{
		}

		public void BusInit(IBusManager bmgr)
		{
			bmgr.SubscribeRDIO(1, 0, new BusReadIoProc(this.readPortFE));
			bmgr.SubscribeRDIO(0x99, 0x18, new BusReadIoProc(this.readPort7E));

			m_cpu = bmgr.CPU;
		}

		private void readPortFE(ushort addr, ref byte value, ref bool iorqge)
		{
			// Quorum ROM contains modificated keyboard procedures so 0x7E should not be yet another FE port
			// Otherwise ROM Basic will be unasable

			if ((addr & 0xFF) != 0x7E)
			{
				value = (byte)(value & 0xe0);
				value = (byte)(value | ((byte)(this.scanKbdPort(addr) & 0x1f)));
			}
		}

		private void readPort7E(ushort addr, ref byte value, ref bool iorqge)
		{
			// Additional Quorum keyboard port
			value = 0xFF;
		}

		private int scanKbdPort(ushort port)
		{
			byte num = 0x1f;
			int num2 = 0x100;
			int num3 = 0;
			while (num3 < 8)
			{
				if ((port & num2) == 0)
				{
					num = (byte)(num & ((byte)(((this.m_keyboardState >> (num3 * 5)) ^ 0x1f) & 0x1f)));
				}
				num3++;
				num2 = num2 << 1;
			}
			return num;
		}

		public int BusOrder
		{
			get
			{
				return this.m_busOrder;
			}
			set
			{
				this.m_busOrder = value;
			}
		}

		public BusCategory Category
		{
			get
			{
				return BusCategory.Keyboard;
			}
		}

		public string Description
		{
			get
			{
				return "Quorum extended keyboard";
			}
		}

		public long KeyboardState
		{
			get
			{
				return this.m_keyboardState;
			}
			set
			{
				this.m_keyboardState = value;
			}
		}

		public string Name
		{
			get
			{
				return "Quorum64";
			}
		}

	}
}
