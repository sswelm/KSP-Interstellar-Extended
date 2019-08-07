using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    class FNModulePreecooler : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool functional;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Area")]
        public double area = 0.01;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Precooler status")]
        public string statusStr;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Intake")]
        public string attachedIntakeName;

        AtmosphericIntake attachedIntake;
        List<AtmosphericIntake> radialAttachedIntakes;

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

            Debug.Log("[KSPI]: FNModulePreecooler - Onstart start search for Air Intake module to cool");

            // first check if part itself has an air intake
            attachedIntake = part.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

            if (attachedIntake != null)
                Debug.Log("[KSPI]: FNModulePreecooler - Found Airintake on self");

            if (attachedIntake == null)
            {
                // then look to connect radial attached children
                radialAttachedIntakes = part.children
                    .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                    .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                Debug.Log(radialAttachedIntakes.Count > 0
                    ? "[KSPI]: FNModulePreecooler - Found Airintake in children"
                    : "[KSPI]: FNModulePreecooler - Did not find Airintake in children");
            }

            // third look for stack attachable air intake
            if (attachedIntake == null && (radialAttachedIntakes == null || radialAttachedIntakes.Count == 0))
            {
                Debug.Log("[KSPI]: FNModulePreecooler - looking at attached nodes");

                foreach (var attachNode in part.attachNodes.Where(a => a.attachedPart != null))
                {
                    var attachedPart = attachNode.attachedPart;

                    // skip any parts that contain a precooler
                    if (attachedPart.FindModulesImplementing<FNModulePreecooler>().Any())
                    {
                        Debug.Log("[KSPI]: FNModulePreecooler - skipping Module Implementing FNModulePreecooler");
                        continue;
                    }

                    attachedIntake = attachedPart.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

                    if (attachedIntake == null) continue;

                    Debug.Log("[KSPI]: FNModulePreecooler - found Airintake in attached part with name " + attachedIntake.name);
                    break;
                }

                if (attachedIntake == null)
                {
                    Debug.Log("[KSPI]: FNModulePreecooler - looking at deeper attached nodes");

                    // look for stack attacked parts one part further
                    foreach (var attachNode in part.attachNodes.Where(a => a.attachedPart != null))
                    {
                        // then look to connect radial attached children
                        radialAttachedIntakes = attachNode.attachedPart.children
                            .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                            .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                        if (radialAttachedIntakes.Count > 0)
                        {
                            Debug.Log("[KSPI]: FNModulePreecooler - Found " + radialAttachedIntakes.Count + " Airintake(s) in children in deeper node");
                            break;
                        }

                        if (attachNode.attachedPart.FindModulesImplementing<FNModulePreecooler>().Any()) continue;
                        
                        foreach (var subAttachNode in attachNode.attachedPart.attachNodes.Where(a => a.attachedPart != null))
                        {
                            Debug.Log("[KSPI]: FNModulePreecooler - look for Air intakes in part " + subAttachNode.attachedPart.name);

                            attachedIntake = subAttachNode.attachedPart.FindModulesImplementing<AtmosphericIntake>().FirstOrDefault();

                            if (attachedIntake != null)
                            {
                                Debug.Log("[KSPI]: FNModulePreecooler - found Airintake in deeper attached part with name " + attachedIntake.name);
                                break;
                            }

                            // then look to connect radial attached children
                            radialAttachedIntakes = subAttachNode.attachedPart.children
                                .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                                .SelectMany(p => p.FindModulesImplementing<AtmosphericIntake>()).ToList();

                            if (radialAttachedIntakes.Count <= 0) continue;

                            Debug.Log("[KSPI]: FNModulePreecooler - Found " + radialAttachedIntakes.Count + " Airintake(s) in children in even deeper node");
                            break;
                        }
                        if (attachedIntake != null)
                            break;
                    }
                }
            }

            if (attachedIntake != null)
                attachedIntakeName = attachedIntake.name;
            else
            {
                if (radialAttachedIntakes == null )
                    attachedIntakeName = "Null found";
                else if (radialAttachedIntakes.Count > 1)
                    attachedIntakeName = radialAttachedIntakes.Count + " radial intakes found";
                else if (radialAttachedIntakes.Count > 0)
                    attachedIntakeName = radialAttachedIntakes.First().name;
                else
                    attachedIntakeName = "Not found";
            }
        }

        public override void OnUpdate()
        {
            statusStr = functional ? "Active." : "Offline.";
        }

        public void FixedUpdate() // FixedUpdate is also called while not staged
        {
            functional = ((attachedIntake != null && attachedIntake.intakeOpen) || radialAttachedIntakes.Any(i => i.intakeOpen));
        }


    }
}
