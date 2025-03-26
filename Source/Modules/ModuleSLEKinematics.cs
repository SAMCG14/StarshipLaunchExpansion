using System;

namespace StarshipLaunchExpansion.Modules
{
    public class ModuleSLEKinematics : PartModule
    {
        // Constants
        public const string MODULENAME = "ModuleSLEKinematics";

        public void FixedUpdate()
        {
            // Simple plugin to enable kinematics on the chopsticks permanently
            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    part.Rigidbody.isKinematic = true;
                }
                catch (Exception e)
                {
                    // Do nothing, simply don't print errors if rigidbody isn't initialized before called because eventually it will
                } 
            }
            
        }
    }
}