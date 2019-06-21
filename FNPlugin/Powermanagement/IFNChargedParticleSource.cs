using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Redist;

namespace FNPlugin.Redist
{
    public interface IFNChargedParticleSource : IChargedParticleSource
    {
        bool MayExhaustInAtmosphereHomeworld { get; }
        bool MayExhaustInLowSpaceHomeworld { get; }
    }
}
