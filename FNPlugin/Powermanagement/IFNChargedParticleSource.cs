using FNPlugin.Redist;

namespace FNPlugin.Powermanagement
{
    public interface IFNChargedParticleSource : IChargedParticleSource
    {
        bool MayExhaustInAtmosphereHomeworld { get; }
        bool MayExhaustInLowSpaceHomeworld { get; }
    }
}
