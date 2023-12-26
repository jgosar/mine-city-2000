using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.mc2k.SimCityReader
{
    public class Building
    {
        byte _xPos;
        byte _zPos;
        BuildingType _type;

        public Building(byte xPos, byte zPos, byte code)
        {
            _xPos = xPos;
            _zPos = zPos;
            _type = BuildingType.getByCode(code);
        }

        public byte getXPos()
        {
            return _xPos;
        }

        public byte getZPos()
        {
            return _zPos;
        }

        public byte getCode()
        {
            return _type.getCode();
        }

        public byte getSize()
        {
            return _type.getSize();
        }
    }
}
