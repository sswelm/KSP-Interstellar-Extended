using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Beamedpower
{
    public class MirrorRelaysTransmitterTag : BeamedPowerTransmitterTag { };

    public class PhasedArrayTransmitterTag : BeamedPowerTransmitterTag {};

    public class MicrowavePowerTransmitterTag : BeamedPowerTransmitterTag {};

    public class BeamedPowerLaserTransmitterTag : BeamedPowerTransmitterTag { };

    public class BeamedPowerTransmitterTag : PartModule {}
}
