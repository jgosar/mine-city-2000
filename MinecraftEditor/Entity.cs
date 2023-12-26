using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.mc2k.AnvilFile.Tags;
using com.mc2k.AnvilFile;

namespace com.mc2k.MinecraftEditor
{
    class Entity : NBTConvertible
    {
        public override CompoundNBTTag NBTTag { get; set; }

        public Entity()
        {
        }
    }
}
