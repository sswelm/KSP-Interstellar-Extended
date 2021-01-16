namespace InterstellarFuelSwitch
{
    class IFSModuleDecouple : ModuleDecouple
    {
        public override void OnAwake()
        {
            fx = part.findFxGroup(fxGroupName);

            if (menuName == string.Empty)
                return;

            Events[nameof(Decouple)].guiName = menuName;
            Actions[nameof(DecoupleAction)].guiName = menuName;
        }
    }
}
