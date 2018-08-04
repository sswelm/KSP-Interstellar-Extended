namespace FNPlugin
{
    public interface IPowerSupply
    {
        string DisplayName { get; set; }
        int PowerPriority { get; set; }
        double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime);
        double ConsumeMegajoulesPerSecond(double powerRequest);
        string getResourceManagerDisplayName();

        void SupplyMegajoulesPerSecondWithMax(double supply, double maxsupply);
    }
}