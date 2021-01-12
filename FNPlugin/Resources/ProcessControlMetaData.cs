using System.Collections.Generic;
using System.Reflection;

namespace FNPlugin.Resources
{
    class ProcessControlMetaData
    {
        public ProcessControlMetaData(PartModule partModule)
        {
            var type = partModule.GetType();
            _partModule = partModule;
            var titleField = partModule.Fields["title"];
            Title = (string)titleField.GetValue(partModule);
            _capacityBaseField = partModule.Fields["capacity"];
            _runningBaseField = partModule.Fields["running"];
            _reliablityEventMethodInfo = type.GetMethod("ReliablityEvent");
        }

        public string Title { get; }
        private readonly PartModule _partModule;
        private readonly MethodInfo _reliablityEventMethodInfo;
        private readonly BaseField _capacityBaseField;
        private readonly BaseField _runningBaseField;

        public bool Running
        {
            get => (bool) _runningBaseField.GetValue(_partModule);
            set => _runningBaseField.SetValue(value, _partModule);
        }

        public double Capacity
        {
            get => (double)_capacityBaseField.GetValue(_partModule);
            set => _capacityBaseField.SetValue(value, _partModule);
        }

        public void ReliablityEvent()
        {
            _reliablityEventMethodInfo?.Invoke(_partModule, new object[] { false });
        }

        public void ReliablityEvent(bool running, double capacity)
        {
            Capacity = capacity;
            Running = running;
            ReliablityEvent();
        }
    }

    class ProcessControlManager
    {
        public Dictionary<string, ProcessControlMetaData> Collection { get; } = new Dictionary<string, ProcessControlMetaData>();

        public  ProcessControlManager(Part part)
        {
            foreach (var partModule in part.Modules)
            {
                if (partModule.ClassName != "ProcessController") continue;
                var processControl = new ProcessControlMetaData(partModule);
                Collection.Add(processControl.Title, processControl);
            }
        }
    }
}
