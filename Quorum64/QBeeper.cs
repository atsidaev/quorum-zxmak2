using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Z80;
using System.Xml;
using ZXMAK2;

namespace Quorum64
{
	public class QBeeperDevice : ISoundRenderer, IBusDevice, IConfigurable
	{
		private int _beeperSamplePos;
		private uint[] _beeperSamples = new uint[0x372];
		private int _frameTactCount;
		private int _portFE;
		private int m_busOrder;
		private Z80CPU m_cpu;
		private uint m_dacValue0;
		private uint m_dacValue1 = 0x1fff;
		private int m_volume = 100;

		public QBeeperDevice()
		{
			this.Volume = 30;
		}

		protected virtual void BeginFrame()
		{
			this._beeperSamplePos = 0;
		}

		public void BusConnect()
		{
		}

		public void BusDisconnect()
		{
		}

		public void BusInit(IBusManager bmgr)
		{
			this.m_cpu = bmgr.CPU;
			IUlaDevice device = (IUlaDevice)bmgr.FindDevice(typeof(IUlaDevice));
			this.FrameTactCount = device.FrameTactCount;
			bmgr.SubscribeWRIO(0x99, 0x98, new BusWriteIoProc(this.writePortFE));
			bmgr.SubscribeBeginFrame(new BusFrameEventHandler(this.BeginFrame));
			bmgr.SubscribeEndFrame(new BusFrameEventHandler(this.EndFrame));
		}

		protected virtual void EndFrame()
		{
			this.UpdateState(this._frameTactCount);
		}

		public void LoadConfig(XmlNode itemNode)
		{
			this.Volume = Utils.GetXmlAttributeAsInt32(itemNode, "volume", this.Volume);
		}

		public void SaveConfig(XmlNode itemNode)
		{
			Utils.SetXmlAttribute(itemNode, "volume", this.Volume);
		}

		public void UpdateState(int frameTact)
		{
			int length = (this._beeperSamples.Length * frameTact) / this._frameTactCount;
			if (length > this._beeperSamples.Length)
			{
				length = this._beeperSamples.Length;
			}
			if (length > this._beeperSamplePos)
			{
				uint num2 = this.m_dacValue0;
				if ((this._portFE & 0x10) != 0)
				{
					num2 += this.m_dacValue1;
				}
				num2 |= num2 << 0x10;
				while (this._beeperSamplePos < length)
				{
					this._beeperSamples[this._beeperSamplePos] = num2;
					this._beeperSamplePos++;
				}
			}
		}

		private void writePortFE(ushort addr, byte value, ref bool iorqge)
		{
			this.PortFE = value;
		}

		public uint[] AudioBuffer
		{
			get
			{
				return this._beeperSamples;
			}
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
				return BusCategory.Sound;
			}
		}

		public string Description
		{
			get
			{
				return "Simple Beeper & Tape Sound";
			}
		}

		protected int FrameTactCount
		{
			get
			{
				return this._frameTactCount;
			}
			set
			{
				this._frameTactCount = value;
			}
		}

		public string Name
		{
			get
			{
				return "QBeeper";
			}
		}

		public int PortFE
		{
			get
			{
				return this._portFE;
			}
			set
			{
				if (value != this._portFE)
				{
					int frameTact = (int)((this.m_cpu.Tact + 1) % ((long)this.FrameTactCount));
					this.UpdateState(frameTact);
					this._portFE = value;
				}
			}
		}

		public int Volume
		{
			get
			{
				return this.m_volume;
			}
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				if (value > 100)
				{
					value = 100;
				}
				this.m_volume = value;
				this.m_dacValue0 = 0;
				this.m_dacValue1 = (uint)((0x7fff * this.m_volume) / 100);
			}
		}
	}

}
