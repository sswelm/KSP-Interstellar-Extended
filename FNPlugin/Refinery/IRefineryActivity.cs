namespace FNPlugin.Refinery
{
    interface IRefineryActivity
    {
        // 1 seperation
        // 2 desconstrution
        // 3 construction

        RefineryType RefineryType { get; }

        string ActivityName { get;}

        string Formula { get; }

        double CurrentPower { get; }

        bool HasActivityRequirements();

        double PowerRequirements { get; }

        double EnergyPerTon { get; }

        string Status { get; }

        void UpdateFrame(double rateMultiplier, double powerFraction,  double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false);

        void UpdateGUI();

        void PrintMissingResources();

        void Initialize(Part part);
    }
}