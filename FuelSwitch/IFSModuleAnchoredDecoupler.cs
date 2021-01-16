namespace InterstellarFuelSwitch
{
    class IFSModuleAnchoredDecoupler : ModuleAnchoredDecoupler
    {
        public override void OnAwake()
        {
            fx = part.findFxGroup(fxGroupName);
        }
    }
}
