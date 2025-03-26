using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StarshipLaunchExpansion.Modules
{
    public class ModuleSLEController : PartModule
    {
        // Constants
        public const string MODULENAME = "ModuleSLEController";

        private float CurrentExtensionSpeed;

        private float CurrentExtensionLimit;

        [KSPField(guiActive = false, isPersistant = true)]
        private float CurrentOpenCloseLimit;

        [KSPField(guiActive = false, isPersistant = true)]
        public bool OpenClose = false;

        private bool CurrentOpenClose = false;

        private ModuleSLEAnimate Anim1;

        private ModuleSLEAnimate Anim2;

        private List<ModuleSLEAnimate> AnimList;

        private float MaxExtension = 0f;

        // Settable Variables

        [KSPField]
        public string moduleID = "ModuleSLEController";

        [KSPField]
        public string Anim1ID = "default";

        [KSPField]
        public string Anim2ID = "default";

        [KSPField]
        public bool ConvertUnits = false;

        [KSPField]
        public Vector3 OpenCloseRange = new Vector3(0, 0, 0); //min, max, default

        [KSPField]
        public bool ShowGUI = true;

        [KSPField]
        public bool ShowExtensionSlider = true;

        [KSPField]
        public string AnimStopName = "AnimStopName";

        [KSPField]
        public string ExtensionLimitName = "ExtensionLimitName";

        [KSPField]
        public string ExtensionSpeedName = "ExtensionSpeedName";

        [KSPField]
        public string OpenCloseLimitName = "OpenCloseLimitName";

        [KSPField]
        public string ExtendedCurrentName = "ExtendedCurrentName";

        [KSPField]
        public string OpenName = "OpenName";

        [KSPField]
        public string CloseName = "CloseName";

        [KSPField]
        public string ToggleName = "ToggleName";

        [KSPField]
        public string UIUnit = "%";

        [KSPField]
        public string UIDecimals = "0.0";

        [KSPField]
        public bool OpenCloseExternal = false;

        [KSPField]
        public bool ReverseOpenLimit = false;

        //Ingame UI stuffz

        [KSPField(guiActive = true, guiActiveEditor = false, guiFormat = "F2", guiName = "ExtendedCurrentName", guiUnits = "%")]
        public float ExtendedCurrent;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = false, guiFormat = "0", guiName = "ExtensionLimitName", guiUnits = "%", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 100f, minValue = 0f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float ExtensionLimit = 0f;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = false, guiFormat = "0", guiName = "ExtensionSpeedName", guiUnits = "%", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 100f, minValue = 5f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float ExtensionSpeed = 100f;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = false, guiFormat = "0", guiName = "OpenCloseLimitName", guiUnits = "%", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 100f, minValue = 0f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float OpenCloseLimit = 0f;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "AnimStopName", unfocusedRange = 5f)]
        public void Button1()
        {
            StopAnimations();
        }

        [KSPAction(guiName = "AnimStopName")]
        public void Action1(KSPActionParam param)
        {
            StopAnimations();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, externalToEVAOnly = false, guiActiveUnfocused = false, guiName = "OpenName", unfocusedRange = 500f)]
        public void Button2()
        {
            OpenClose = !OpenClose;
            
        }

        [KSPAction(guiName = "ToggleName")]
        public void Action2(KSPActionParam param)
        {
            OpenClose = !OpenClose;
        }

        //Voids

        public void Start()
        {
            GetAnims();
            DoCheckGUI();
        }

        public void FixedUpdate()
        {
            UpdateAnimPositions();
            UpdateGUI();
        }

        public override void OnLoad(ConfigNode TestNode)
        {

            Debug.Log($"[{MODULENAME}] Module Loaded, Previous Open Close Limit: {CurrentOpenCloseLimit}");
            OpenCloseLimit = CurrentOpenCloseLimit;

        }

        private void StopAnimations()
        {
            if (ConvertUnits)
            {
                Anim1.Events.Send("Button1");
                Anim2.Events.Send("Button1");
                float Anim1Pos = Anim1.AnimatePosition / Anim1.AnimationLength * MaxExtension;
                float Anim2Pos = MaxExtension - Anim2.AnimatePosition / Anim2.AnimationLength * MaxExtension;
                float AveragePos = (Anim1Pos + Anim2Pos) / 2 - MaxExtension / 2;
                float CurrentOpenAngle = ((float)Math.Round(Math.Abs(Anim1Pos - Anim2Pos) * 1, 2)) / 1;
                ExtensionLimit = ((float)Math.Round(AveragePos * 1,2)) / 1;
                if (OpenClose)
                {
                    
                    OpenCloseLimit = CurrentOpenAngle;
                    if (Fields.TryGetFieldUIControl("OpenCloseLimit", out UI_FloatRange OpenCloseLimitVar))
                    {
                        if (OpenCloseLimit > OpenCloseLimitVar.maxValue)
                        {
                            OpenCloseLimit = OpenCloseLimitVar.maxValue;
                        }
                        else if (OpenCloseLimit < OpenCloseLimitVar.minValue)
                        {
                            OpenCloseLimit = OpenCloseLimitVar.minValue;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                    if (ReverseOpenLimit)
                    {
                        OpenClose = false;
                    }
                    CurrentOpenClose = !OpenClose;
                }
                else if (!OpenClose)
                {
                    
                    OpenCloseLimit = CurrentOpenAngle;
                    if (Fields.TryGetFieldUIControl("OpenCloseLimit", out UI_FloatRange OpenCloseLimitVar))
                    {
                        if (OpenCloseLimit > OpenCloseLimitVar.maxValue)
                        {
                            OpenCloseLimit = OpenCloseLimitVar.maxValue;
                        }
                        else if (OpenCloseLimit < OpenCloseLimitVar.minValue)
                        {
                            OpenCloseLimit = OpenCloseLimitVar.minValue;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                    if (CurrentOpenAngle >= OpenCloseRange.x && !ReverseOpenLimit)
                    {
                        OpenClose = true;
                    }
                    else
                    {
                        CurrentOpenClose = !OpenClose;
                    }
                }

                //Debug.Log($"[{MODULENAME}] The position of Anim 1 is {Anim1Pos} And Anim 2 is {Anim2Pos} The average is {AveragePos}");
            }
            else
            {
                //maybe someday huh
            }
        }
        private void UpdateAnimPositions()
        {
            if (OpenClose != CurrentOpenClose || ExtensionLimit != CurrentExtensionLimit || OpenCloseLimit != CurrentOpenCloseLimit)
            {
                if (!OpenClose && ReverseOpenLimit == false)
                {

                    Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2, MaxExtension));
                    Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2), MaxExtension));
                }
                else if (OpenClose && ReverseOpenLimit == false)
                {
                    Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2 - OpenCloseLimit /2, MaxExtension - OpenCloseLimit));
                    Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2 + OpenCloseLimit / 2), MaxExtension - OpenCloseLimit));
                }
                else if (!OpenClose && ReverseOpenLimit)
                {
                    Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2 - OpenCloseLimit / 2, MaxExtension - OpenCloseLimit));
                    Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2 + OpenCloseLimit / 2), MaxExtension - OpenCloseLimit));
                    //Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2 - OpenCloseLimit / 2, MaxExtension - OpenCloseLimit));
                    //Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2 + OpenCloseLimit / 2), MaxExtension - OpenCloseLimit));
                }
                else if (OpenClose && ReverseOpenLimit)
                {
                    Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2 - OpenCloseRange.y / 2, MaxExtension - OpenCloseRange.y));
                    Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2 + OpenCloseRange.y / 2), MaxExtension - OpenCloseRange.y));
                    //Anim1.ExtensionLimit = Math.Max(0, Math.Min(ExtensionLimit + MaxExtension / 2 - OpenCloseLimit / 2, MaxExtension - OpenCloseLimit));
                    //Anim2.ExtensionLimit = Math.Max(0, Math.Min(MaxExtension - (ExtensionLimit + MaxExtension / 2 + OpenCloseLimit / 2), MaxExtension - OpenCloseLimit));
                }
                CurrentExtensionLimit = ExtensionLimit;
                CurrentOpenClose = OpenClose;
                CurrentOpenCloseLimit = OpenCloseLimit;
            }
            if (ExtensionSpeed != CurrentExtensionSpeed)
            {
                Anim1.ExtensionSpeed = ExtensionSpeed;
                Anim2.ExtensionSpeed = ExtensionSpeed;

                CurrentExtensionSpeed = ExtensionSpeed;
            }
            


            
        }

        private void UpdateGUI() 
        {
            if (OpenClose && !ReverseOpenLimit)
            {
                if (ConvertUnits)
                {
                    if (Fields.TryGetFieldUIControl("ExtensionLimit", out UI_FloatRange ExtensionLimitVar))
                    {
                        ExtensionLimitVar.maxValue = MaxExtension / 2 - OpenCloseLimit / 2;
                        ExtensionLimitVar.minValue = -(MaxExtension / 2 - OpenCloseLimit / 2);
                        if (ExtensionLimit > ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.maxValue;
                        }
                        else if (ExtensionLimit < ExtensionLimitVar.minValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.minValue;
                        }
                        if (ExtensionLimitVar.minValue == ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimitVar.minValue = ExtensionLimitVar.minValue - 0.1f;
                            ExtensionLimitVar.maxValue = ExtensionLimitVar.maxValue + 0.1f;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                }
                else
                {
                    //Aint got no time to code this lmao
                }

            }
            else if (!OpenClose && !ReverseOpenLimit)
            {
                if (ConvertUnits)
                {
                    if (Fields.TryGetFieldUIControl("ExtensionLimit", out UI_FloatRange ExtensionLimitVar))
                    {
                        ExtensionLimitVar.maxValue = MaxExtension / 2;
                        ExtensionLimitVar.minValue = -(MaxExtension / 2);
                        if (ExtensionLimit > ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.maxValue;
                        }
                        else if (ExtensionLimit < ExtensionLimitVar.minValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.minValue;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                }
                else
                {
                    //Aint got no time to code this lmao
                }
            }
            else if (OpenClose && ReverseOpenLimit)
            {
                if (ConvertUnits)
                {
                    if (Fields.TryGetFieldUIControl("ExtensionLimit", out UI_FloatRange ExtensionLimitVar))
                    {
                        ExtensionLimitVar.maxValue = MaxExtension / 2 - OpenCloseRange.y / 2;
                        ExtensionLimitVar.minValue = -(MaxExtension / 2 - OpenCloseRange.y / 2);
                        if (ExtensionLimit > ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.maxValue;
                        }
                        else if (ExtensionLimit < ExtensionLimitVar.minValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.minValue;
                        }
                        if (ExtensionLimitVar.minValue == ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimitVar.minValue = ExtensionLimitVar.minValue - 0.1f;
                            ExtensionLimitVar.maxValue = ExtensionLimitVar.maxValue + 0.1f;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                }
                else
                {
                    //Aint got no time to code this lmao
                }

            }
            else if (!OpenClose && ReverseOpenLimit)
            {
                if (ConvertUnits)
                {
                    if (Fields.TryGetFieldUIControl("ExtensionLimit", out UI_FloatRange ExtensionLimitVar))
                    {
                        ExtensionLimitVar.maxValue = MaxExtension / 2 - OpenCloseLimit / 2;
                        ExtensionLimitVar.minValue = -(MaxExtension / 2 - OpenCloseLimit / 2);
                        if (ExtensionLimit > ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.maxValue;
                        }
                        else if (ExtensionLimit < ExtensionLimitVar.minValue)
                        {
                            ExtensionLimit = ExtensionLimitVar.minValue;
                        }
                        if (ExtensionLimitVar.minValue == ExtensionLimitVar.maxValue)
                        {
                            ExtensionLimitVar.minValue = ExtensionLimitVar.minValue - 0.1f;
                            ExtensionLimitVar.maxValue = ExtensionLimitVar.maxValue + 0.1f;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                    }
                }
                else
                {
                    //Aint got no time to code this lmao
                }

            }

            if (OpenClose)
            {
                Events["Button2"].guiName = CloseName;
            }
            else
            {
                Events["Button2"].guiName = OpenName;
            }

            if (Anim1.AnimateAnim.isPlaying || Anim2.AnimateAnim.isPlaying)
            {
                Events["Button1"].guiActive = true;
                Events["Button1"].guiActiveEditor = true;
            }
            else
            {
                Events["Button1"].guiActive = false;
                Events["Button1"].guiActiveEditor = false;
            }

            float Anim1Pos = Anim1.ExtendedCurrent / Anim1.MaxExtension;
            float Anim2Pos = 1 - Anim2.ExtendedCurrent / Anim2.MaxExtension;
            float AveragePos = (Anim1Pos * MaxExtension + Anim2Pos * MaxExtension) / 2 - MaxExtension / 2;
            ExtendedCurrent = ((float)Math.Round(AveragePos * 1, 2)) / 1;

        }
        private void GetAnims()
        {
            AnimList = part.Modules.GetModules<ModuleSLEAnimate>();
            Debug.Log($"[{MODULENAME}] Looking for {Anim1ID} and {Anim2ID}");

            for (int i = 0; i < AnimList.Count; i++)
            {
                if (AnimList[i].moduleID == Anim1ID)
                {
                    Anim1 = AnimList[i];
                    Debug.Log($"[{MODULENAME}] Anim1 Selected name: {AnimList[i].moduleID}");
                }
                else if (AnimList[i].moduleID == Anim2ID)
                {
                    Anim2 = AnimList[i];
                    Debug.Log($"[{MODULENAME}] Anim2 Selected name: {AnimList[i].moduleID}");
                } 
            }
            MaxExtension = Anim1.MaxExtension;
        }

        private void DoCheckGUI()
        {
            if (!ShowGUI)
            {
                Fields["ExtensionLimit"].guiActive = false;
                Fields["ExtensionSpeed"].guiActive = false;
                Fields["ExtendedCurrent"].guiActive = false;
                Fields["OpenCloseLimit"].guiActive = false;
                Events["Button1"].guiActive = false;
                Actions["Action1"].active = false;
                Events["Button2"].guiActive = false;
                Actions["Action2"].active = false;
                Fields["ExtensionLimit"].guiActiveEditor = false;
                Fields["ExtensionSpeed"].guiActiveEditor = false;
                Fields["OpenCloseLimit"].guiActiveEditor = false;
                Fields["ExtendedCurrent"].guiActiveEditor = false;
                Events["Button1"].guiActiveEditor = false;
                Actions["Action1"].activeEditor = false;
                Events["Button2"].guiActiveEditor = false;
                Actions["Action2"].activeEditor = false;
            }
            else if (!ShowExtensionSlider)
            {
                Fields["ExtensionLimit"].guiActive = false;
                Fields["ExtendedCurrent"].guiActive = false;
                ExtensionLimit = 0;
            }
            Actions["Action1"].guiName = AnimStopName;
            Events["Button1"].guiName = AnimStopName;
            Actions["Action2"].guiName = ToggleName;
            Events["Button2"].guiName = OpenName;
            Fields["ExtensionLimit"].guiName = ExtensionLimitName;
            Fields["ExtensionSpeed"].guiName = ExtensionSpeedName;
            Fields["OpenCloseLimit"].guiName = OpenCloseLimitName;
            Events["Button2"].guiActiveUnfocused = OpenCloseExternal;
            Fields["ExtendedCurrent"].guiName = ExtendedCurrentName;
            Fields["ExtendedCurrent"].guiUnits = UIUnit;

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
                    Fields["ExtensionLimit"].guiFormat = UIDecimals;
                    ExtensionLimitVar.maxValue = MaxExtension / 2;
                    ExtensionLimitVar.minValue = -(MaxExtension / 2);
                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                }
                if (Fields.TryGetFieldUIControl("ExtensionSpeed", out UI_FloatRange ExtensionSpeedVar))
                {
                    Anim1.Fields.TryGetFieldUIControl("ExtensionSpeed", out UI_FloatRange AnimSpeedVar);

                    Fields["ExtensionSpeed"].guiUnits = UIUnit + "/s";
                    Fields["ExtensionSpeed"].guiFormat = "0.00";
                    ExtensionSpeedVar.maxValue = AnimSpeedVar.maxValue;
                    ExtensionSpeedVar.minValue = AnimSpeedVar.minValue;
                    ExtensionSpeedVar.stepIncrement = AnimSpeedVar.stepIncrement;
                    if (ExtensionSpeed > AnimSpeedVar.maxValue)
                    {
                        ExtensionSpeed = AnimSpeedVar.maxValue;
                    }
                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                }
                if (Fields.TryGetFieldUIControl("OpenCloseLimit", out UI_FloatRange OpenCloseVar))
                {
                    if (OpenCloseRange.z < OpenCloseRange.x)
                    {
                        OpenCloseRange.z = OpenCloseRange.x;
                    }
                    Fields["OpenCloseLimit"].guiUnits = UIUnit;
                    Fields["OpenCloseLimit"].guiFormat = UIDecimals;
                    OpenCloseVar.maxValue = OpenCloseRange.y;
                    OpenCloseVar.minValue = OpenCloseRange.x;
                    if (CurrentOpenCloseLimit == 0)
                    {
                        OpenCloseLimit = OpenCloseRange.z;
                    }

                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Error, Cannot find UI Field");
                }
            }


        }
    }
}
