namespace FNPlugin
{
    public interface IPowerSupply
    {
        string DisplayName { get; set; }

        double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime);
        double ConsumeMegajoulesPerSecond(double powerRequest);
        string getResourceManagerDisplayName();

        void SupplyMegajoulesPerSecondWithMax(double supply, double maxsupply);
        void SupplyMegajoulesFixedWithMax(double supply, double maxsupply);

    }
}