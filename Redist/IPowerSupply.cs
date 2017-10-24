namespace FNPlugin
{
	public interface IPowerSupply
	{
		string DisplayName { get; set; }

		double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime);
		double ConsumeMegajoulesPerSecond(double powerRequest);
		string getResourceManagerDisplayName();
	}
}