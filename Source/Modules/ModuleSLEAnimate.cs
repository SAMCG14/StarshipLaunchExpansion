using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StarshipLaunchExpansion.Modules
{
    //[KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ModuleSLEAnimate : PartModule
    {
        // Constants
        public const string MODULENAME = "ModuleSLEAnimate";

        [KSPField(guiActive = false, isPersistant = true)]
        public float AnimatePosition = 0;

        [KSPField(guiActive = false, isPersistant = true)]
        public float AnimationLength = 0;

        private bool StartupValue = true;
        private float CalcValue;

        public string AnimationState
        {
            get;
            private set;
        }

        private float CurrentAnimateSpeed;

        private float CurrentExtensionLimit;

        private float StartMoveExtensionLimit;

        public Animation AnimateAnim;

        // Settable Variables

        [KSPField]
        public string moduleID = "ModuleSLEAnimate";

        [KSPField]
        public float AnimationSpeed = 1f;

        [KSPField]
        public float AnimationMaxVelocity = 1f;

        [KSPField]
        public float AnimationMinVelocity = 0f;

        [KSPField]
        public string AnimationName = "default";

        [KSPField]
        public bool ConvertUnits = false;

        [KSPField]
        public float MaxExtension = 0f;

        [KSPField]
        public float AllowedErrorDelta = 0.005f;

        [KSPField]
        public bool ShowGUI = true;

        [KSPField]
        public bool ShowButtons = false;

        [KSPField]
        public bool ButtonsUnfocussed = false;

        [KSPField]
        public bool ShowSpeedSlider = true;

        [KSPField]
        public bool ShowExtensionSlider = true;

        [KSPField]
        public string AnimStopName = "AnimStopName";

        [KSPField]
        public string AnimRetractName = "AnimRetractName";

        [KSPField]
        public string AnimExtendName = "AnimExtendName";

        [KSPField]
        public string ExtensionLimitName = "ExtensionLimitName";

        [KSPField]
        public string ExtensionSpeedName = "ExtensionSpeedName";

        [KSPField]
        public string ExtendedCurrentName = "ExtendedCurrentName";

        [KSPField]
        public string UIUnit = "%";

        [KSPField]
        public float ErrorToleranceMultiplier = 8;

        //Ingame UI stuffz

        [KSPField(guiActive = true, guiActiveEditor = true, guiFormat = "F2", guiName = "ExtendedCurrentName", guiUnits = "%")]
        public float ExtendedCurrent;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = false, guiFormat = "0", guiName = "ExtensionLimitName", guiUnits = "%", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 100f, minValue = 0f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float ExtensionLimit = 0f;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = false, guiFormat = "0", guiName = "ExtensionSpeedName", guiUnits = "%", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 100f, minValue = 5f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float ExtensionSpeed = 100f;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "AnimStopName", unfocusedRange = 5f)]
        public void Button1()
        {
            if (ConvertUnits && AnimationState != "Stopped")
            {
                AnimateStop();
                ExtensionLimit = ((float)Math.Round(AnimatePosition / AnimateAnim[AnimationName].length * MaxExtension * 100)) / 100;
            }
            else if (!ConvertUnits && AnimationState != "Stopped") {
                AnimateStop();
                ExtensionLimit = ((float)Math.Round(AnimatePosition / AnimateAnim[AnimationName].length * 100 * 100)) / 100;
            }
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, externalToEVAOnly = false, guiActiveUnfocused = false, guiName = "AnimRetractName", unfocusedRange = 500f)]
        public void Button2()
        {
            ExtensionLimit = 0;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, externalToEVAOnly = false, guiActiveUnfocused = false, guiName = "AnimExtendName", unfocusedRange = 500f)]
        public void Button3()
        {
            ExtensionLimit = MaxExtension;
        }

        [KSPAction(guiName = "AnimStopName")]
        public void Action1(KSPActionParam param)
        {
            if (ConvertUnits && AnimationState != "Stopped")
            {
                AnimateStop();
                ExtensionLimit = AnimatePosition / AnimateAnim[AnimationName].length * MaxExtension;
            }
            else if (!ConvertUnits && AnimationState != "Stopped")
            {
                AnimateStop();
                ExtensionLimit = AnimatePosition / AnimateAnim[AnimationName].length * 100;
            }
        }

        [KSPAction(activeEditor = false, guiName = "AnimRetractName")]
        public void Action2(KSPActionParam param)
        {
            ExtensionLimit = 0;
        }

        [KSPAction(activeEditor = false, guiName = "AnimExtendName")]
        public void Action3(KSPActionParam param)
        {
            ExtensionLimit = MaxExtension;
        }

        //Voids
        public void Start()
        {
            AnimateAnim = part.FindModelAnimator(AnimationName);
            if (AnimateAnim == null)
            {
                Debug.LogError($"[{MODULENAME}] Error, No Animation with name {AnimationName} found on part {part.name}");
            }
            DoCheckGUI();
            CheckUseMeters();
            CurrentExtensionLimit = ExtensionLimit;
            AnimationLength = AnimateAnim[AnimationName].length;

        }
        
        public void FixedUpdate()
        {      
            CheckAnimationNeed();
            ButtonHandler();
            UpdateGUIPosition();
            if (StartupValue)
            {
                AnimateAnim[AnimationName].speed = 0f;
                AnimateAnim[AnimationName].time = AnimatePosition;
                AnimateAnim[AnimationName].enabled = true;
                AnimateAnim.Play(AnimationName);
                AnimationState = "Stopped";
                StartupValue = false;
            }
        }
        public override void OnLoad(ConfigNode TestNode)
        {

            Debug.Log($"[{MODULENAME}] Module Loaded, Previous Extension: {AnimatePosition}");
            StartupValue = true;

        }

        private void CheckUseMeters()
        {
            if (ConvertUnits && MaxExtension == 0)
            {
                Debug.LogError($"[{MODULENAME}] Error, Convert Units Active but no Max Extension set!");
                ConvertUnits = false;
            }
            else if (ConvertUnits)
            {
                if (Fields.TryGetFieldUIControl("ExtensionLimit", out UI_FloatRange ExtensionLimitVar))
                {
                    Fields["ExtensionLimit"].guiUnits = UIUnit;
                    Fields["ExtensionLimit"].guiFormat = "0.0";
                    ExtensionLimitVar.maxValue = MaxExtension;
                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                }
                if (Fields.TryGetFieldUIControl("ExtensionSpeed", out UI_FloatRange ExtensionSpeedVar))
                {
                    Fields["ExtensionSpeed"].guiUnits = UIUnit + "/s";
                    Fields["ExtensionSpeed"].guiFormat = "0.00";
                    ExtensionSpeedVar.maxValue = AnimationMaxVelocity;
                    ExtensionSpeedVar.minValue = AnimationMinVelocity;
                    ExtensionSpeedVar.stepIncrement = 0.01f;
                    if (ExtensionSpeed > AnimationMaxVelocity)
                    {
                        ExtensionSpeed = AnimationMaxVelocity;
                    }
                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                }
            }
        }

        private void UpdateGUIPosition()
        {
            float CurrentPos = AnimateAnim[AnimationName].time / AnimateAnim[AnimationName].length;
            if (ConvertUnits)
            {
                if (AnimateAnim.IsPlaying(AnimationName))
                {
                    ExtendedCurrent = CurrentPos * MaxExtension;
                }
                else
                {
                    ExtendedCurrent = ExtensionLimit;
                }    
            }
            else
            {
                if (AnimateAnim.IsPlaying(AnimationName))
                {
                    ExtendedCurrent = CurrentPos * 100;
                }
                else
                {
                    ExtendedCurrent = ExtensionLimit;
                }
            }
            
        }
        
        private void CheckAnimationNeed()
        {
            CalcValue = AnimateAnim[AnimationName].time;
            if (AnimateAnim.IsPlaying(AnimationName))
            {
                AnimatePosition = CalcValue;
            }
            
            if (ConvertUnits)
            {
                if ((AnimatePosition + AllowedErrorDelta) * MaxExtension < ExtensionLimit * AnimateAnim[AnimationName].length && AnimationState == "Stopped")
                {
                    AnimateExtend();
                    StartMoveExtensionLimit = ExtensionLimit;
                }
                else if ((AnimatePosition - AllowedErrorDelta) * MaxExtension > ExtensionLimit * AnimateAnim[AnimationName].length && AnimationState == "Stopped")
                {
                    AnimateRetract();
                    StartMoveExtensionLimit = ExtensionLimit;
                }
                else if (AnimatePosition * MaxExtension >= ExtensionLimit * AnimateAnim[AnimationName].length - AllowedErrorDelta * MaxExtension && AnimationState == "Extending")
                {
                    if (ExtensionLimit >= StartMoveExtensionLimit)
                    {
                        AnimatePosition = ExtensionLimit / MaxExtension * AnimateAnim[AnimationName].length;
                    }
                    AnimateStop();
                }
                else if (AnimatePosition * MaxExtension <= ExtensionLimit * AnimateAnim[AnimationName].length + AllowedErrorDelta * MaxExtension && AnimationState == "Retracting")
                {
                    if (ExtensionLimit <= StartMoveExtensionLimit)
                    {
                        AnimatePosition = ExtensionLimit / MaxExtension * AnimateAnim[AnimationName].length;
                    }
                    AnimateStop();
                }
                

            }
            else
            {
                if ((AnimatePosition + AllowedErrorDelta) * 100 < ExtensionLimit * AnimateAnim[AnimationName].length && AnimationState == "Stopped")
                {
                    AnimateExtend();
                }
                else if ((AnimatePosition - AllowedErrorDelta) * 100 > ExtensionLimit * AnimateAnim[AnimationName].length && AnimationState == "Stopped")
                {
                    AnimateRetract();
                }
                else if (AnimatePosition * 100 >= ExtensionLimit * AnimateAnim[AnimationName].length - AllowedErrorDelta * 100 && AnimationState == "Extending")
                {
                    if (ExtensionLimit >= StartMoveExtensionLimit)
                    {
                        AnimatePosition = ExtensionLimit / 100 * AnimateAnim[AnimationName].length;
                    }
                    AnimateStop();
                }
                else if (AnimatePosition * 100 <= ExtensionLimit * AnimateAnim[AnimationName].length + AllowedErrorDelta * 100 && AnimationState == "Retracting")
                {
                    if (ExtensionLimit <= StartMoveExtensionLimit)
                    {
                        AnimatePosition = ExtensionLimit / 100 * AnimateAnim[AnimationName].length;
                    }
                    AnimateStop();
                }
            }

            if (AnimationState != "Stopped" && (ExtensionSpeed != CurrentAnimateSpeed || ExtensionLimit != CurrentExtensionLimit))
            {
                AnimateStop();
                CheckAnimationNeed();
            }
        }
        private void AnimateExtend()
        {
            if (ConvertUnits)
            {
                AnimateAnim[AnimationName].speed = AnimationSpeed * (ExtensionSpeed / AnimationMaxVelocity);
            }
            else
            {
                AnimateAnim[AnimationName].speed = AnimationSpeed * (ExtensionSpeed / 100);
            }
            AnimateAnim[AnimationName].time = AnimatePosition;
            AnimateAnim[AnimationName].enabled = true;
            AnimateAnim.Play(AnimationName);
            CurrentAnimateSpeed = ExtensionSpeed;
            CurrentExtensionLimit = ExtensionLimit;
            AnimationState = "Extending";
        }
        private void AnimateRetract()
        {
            if (ConvertUnits)
            {
                AnimateAnim[AnimationName].speed = -(AnimationSpeed * (ExtensionSpeed / AnimationMaxVelocity));
            }
            else
            {
                AnimateAnim[AnimationName].speed = -(AnimationSpeed * (ExtensionSpeed / 100));
            }
            AnimateAnim[AnimationName].time = AnimatePosition;
            AnimateAnim[AnimationName].enabled = true;
            AnimateAnim.Play(AnimationName);
            CurrentAnimateSpeed = ExtensionSpeed;
            CurrentExtensionLimit = ExtensionLimit;
            AnimationState = "Retracting";
        }
        private void AnimateStop()
        {
            AnimateAnim.Stop();
            AnimationState = "Stopped";
        }
       
        private void ButtonHandler()
        {
            if (AnimationState == "Stopped" && Events["Button1"].guiActive)
            {
                Events["Button1"].guiActive = false;
                Events["Button1"].guiActiveEditor = false;
            }
            else if (AnimationState != "Stopped" && Events["Button1"].guiActive == false && ShowGUI)
            {
                Events["Button1"].guiActive = true;
                Events["Button1"].guiActiveEditor = true;
            }

            if (!Events["Button2"].guiActive && ExtensionLimit != 0 && ShowGUI && ShowButtons)
            {
                Events["Button2"].guiActive = true;
                Events["Button2"].guiActiveEditor = true;
                Events["Button2"].guiActiveUnfocused = ButtonsUnfocussed;
            }
            else if (Events["Button2"].guiActive && ExtensionLimit == 0)
            {
                Events["Button2"].guiActive = false;
                Events["Button2"].guiActiveEditor = false;
                Events["Button2"].guiActiveUnfocused = false;
            }

            if (!Events["Button3"].guiActive && ExtensionLimit != MaxExtension && ShowGUI && ShowButtons)
            {
                Events["Button3"].guiActive = true;
                Events["Button3"].guiActiveEditor = true;
                Events["Button3"].guiActiveUnfocused = ButtonsUnfocussed;
            }
            else if (Events["Button3"].guiActive && ExtensionLimit == MaxExtension)
            {
                Events["Button3"].guiActive = false;
                Events["Button3"].guiActiveEditor = false;
                Events["Button3"].guiActiveUnfocused = false;
            }
        }

        private void DoCheckGUI()
        {
            if (!ShowGUI)
            {
                Fields["ExtensionLimit"].guiActive = false;
                Fields["ExtensionSpeed"].guiActive = false;
                Fields["ExtendedCurrent"].guiActive = false;
                Events["Button1"].guiActive = false;
                Events["Button2"].guiActive = false;
                Events["Button3"].guiActive = false;
                Actions["Action1"].active = false;
                Actions["Action2"].active = false;
                Actions["Action3"].active = false;
                Fields["ExtensionLimit"].guiActiveEditor = false;
                Fields["ExtensionSpeed"].guiActiveEditor = false;
                Fields["ExtendedCurrent"].guiActiveEditor = false;
                Events["Button1"].guiActiveEditor = false;
                Events["Button2"].guiActiveEditor = false;
                Events["Button2"].guiActiveEditor = false;
                Actions["Action1"].activeEditor = false;
                Actions["Action2"].activeEditor = false;
                Actions["Action3"].activeEditor = false;
            }
            if (!ShowSpeedSlider)
            {
                Fields["ExtensionSpeed"].guiActive = false;
                Fields["ExtensionSpeed"].guiActiveEditor = false;
            }
            if (!ShowExtensionSlider)
            {
                Fields["ExtensionLimit"].guiActive = false;
                Fields["ExtensionLimit"].guiActiveEditor = false;
            }

            Actions["Action1"].guiName = AnimStopName;
            Actions["Action2"].guiName = AnimRetractName;
            Actions["Action3"].guiName = AnimExtendName;
            Events["Button1"].guiName = AnimStopName;
            Events["Button2"].guiName = AnimRetractName;
            Events["Button3"].guiName = AnimExtendName;
            Fields["ExtensionLimit"].guiName = ExtensionLimitName;
            Fields["ExtensionSpeed"].guiName = ExtensionSpeedName;
            Fields["ExtendedCurrent"].guiName = ExtendedCurrentName;
            Fields["ExtendedCurrent"].guiUnits = UIUnit;
        }
    }
}
