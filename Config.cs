using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using Nautilus.Handlers;
using Nautilus.Utility;

namespace ValdyrSubnauticaMods
{
    [Menu("Time Speed Mod", LoadOn = MenuAttribute.LoadEvents.MenuOpened | MenuAttribute.LoadEvents.MenuRegistered,
    SaveOn = MenuAttribute.SaveEvents.ChangeValue | MenuAttribute.SaveEvents.SaveGame | MenuAttribute.SaveEvents.QuitGame)]
    public class Config : ConfigFile
	{
        [Slider("Day time speed", 0.0f, 10.0f, DefaultValue = 1.0f, Format ="{0:F2}", Step = 0.01f)]
        public float DayTimeSpeed = 1.0f;

        [Slider("Night time speed", 0.0f, 10.0f, DefaultValue = 1.0f, Format = "{0:F2}", Step = 0.01f)]
        public float NightTimeSpeed = 1.0f;

        [Slider("Eease in/out", 0.0f, 2.0f, DefaultValue = 1.0f, Format = "{0:F2}", Step = 0.01f)]
        public float EaseInOutLerp = 1.0f;
    }
}
