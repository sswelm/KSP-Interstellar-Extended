namespace FNPlugin.Reactors
{
    interface IFNNuclearFuelReprocessable
    {
        double WasteToReprocess { get; }

        double ReprocessFuel(double rate, double deltaTime, double productionModifier, Part processor);
    }
}
