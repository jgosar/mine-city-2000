using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;

namespace com.mc2k.AnvilFile
{
    public abstract class NBTConvertible
    {
        public abstract CompoundNBTTag NBTTag { get; set; }
    }
}
