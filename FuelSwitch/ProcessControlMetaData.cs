using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InterstellarFuelSwitch
{
    class ProcessControlMetaData
    {
        public ProcessControlMetaData(PartModule partModule, string title)
        {
            if (partModule == null)
                throw new InvalidOperationException("partModule cannot be null");

            Title = title;
            _partModule = partModule;
            var resourceField = partModule.Fields["resource"];
            Resource = (string)resourceField.GetValue(partModule);
            _capacityBaseField = partModule.Fields["capacity"];
            _runningBaseField = partModule.Fields["running"];
            _reliablityEventMethodInfo = partModule.GetType().GetMethod("ReliablityEvent");
            Capacity = 0;
        }

        public string Title { get; }
        public string Resource { get; }

        private readonly PartModule _partModule;
        private readonly MethodInfo _reliablityEventMethodInfo;
        private readonly BaseField _capacityBaseField;
        private readonly BaseField _runningBaseField;

        private double _capacity;
        private bool _running;

        public bool Running
        {
            get => (bool)_runningBaseField.GetValue(_partModule);
            set
            {
                if (_running == value) return;

                _running = value;
                _runningBaseField.SetValue(value, _partModule);
            }
        }

        public double Capacity
        {
            get => (double)_capacityBaseField.GetValue(_partModule);
            set
            {
                if (!(Math.Abs(_capacity - value) > float.Epsilon)) return;

                _capacity = value;
                _capacityBaseField.SetValue(value, _partModule);
            }
        }

        public void ReliablityEvent()
        {
            _reliablityEventMethodInfo?.Invoke(_partModule, new object[] { false });
        }

        public void ReliablityEvent(double capacity)
        {
            if (!(Math.Abs(_capacity - capacity) > float.Epsilon)) return;

            Capacity = capacity;
            ReliablityEvent();
        }

        public void ReliablityEvent(double capacity, bool running)
        {
            if (_running == running && !(Math.Abs(_capacity - capacity) > float.Epsilon)) return;

            Capacity = capacity;
            Running = running;
            ReliablityEvent();
        }
    }

    class ProcessControlManager
    {
        public Dictionary<string, ProcessControlMetaData> Collection { get; } = new Dictionary<string, ProcessControlMetaData>();

        public ProcessControlManager(Part part)
        {
            foreach (var partModule in part.Modules)
            {
                if (partModule.ClassName != "ProcessController") continue;

                var titleField = partModule.Fields["title"];
                var title = (string)titleField.GetValue(partModule);

                var processControl = new ProcessControlMetaData(partModule, title);
                Collection.Add(processControl.Title, processControl);
            }
        }

        public static ProcessControlMetaData FindProcessControl(Part part, string searchTitle)
        {
            foreach (var partModule in part.Modules)
            {
                if (partModule.ClassName != "ProcessController") continue;

                var titleField = partModule.Fields["title"];
                var title = (string)titleField.GetValue(partModule);

                if (title == searchTitle)
                    return new ProcessControlMetaData(partModule, title);
            }

            return null;
        }
    }
}
