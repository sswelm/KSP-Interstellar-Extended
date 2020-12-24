using FNPlugin.Redist;

namespace FNPlugin.Powermanagement.Interfaces
{
    public interface IFNChargedParticleSource : IChargedParticleSource
    {
        bool MayExhaustInAtmosphereHomeworld { get; }
        bool MayExhaustInLowSpaceHomeworld { get; }
    }
}
