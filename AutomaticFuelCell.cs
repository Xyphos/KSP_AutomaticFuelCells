// 
//  MIT License
// 
//  Copyright (c) 2019 William "Xyphos" Scott (TheGreatXyphos@gmail.com)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XyphosAerospace
{
  public class AutomaticFuelCell : PartModule
  {
    private const string GroupName = "XyphosAerospace.AutomaticFuelCell";

    private const float MinThresholdPercent = 15.0f;
    private const float MaxThresholdPercent = 85.0f;


    private PartResource            _electricCharge;
    private ModuleResourceConverter _resourceConverter;


    /// <summary>
    ///   Automatic/Manual toggle
    /// </summary>
    [UI_Toggle(
        affectSymCounterparts = UI_Scene.All,
        scene                 = UI_Scene.All,
        controlEnabled        = false,
        invertButton          = false,
        requireFullControl    = false,
        disabledText          = "Manual",
        enabledText           = "Automatic"
      )]
    [KSPField(
        advancedTweakable   = true,
        isPersistant        = true,
        guiActive           = true,
        guiActiveEditor     = true,
        guiName             = "Operation",
        groupDisplayName    = "Automatic Fuel Cell",
        groupName           = GroupName,
        groupStartCollapsed = true
      )]
    public bool Automatic = GameSettings.ADVANCED_TWEAKABLES;


    // for display purposes only    
    /// <summary>
    ///   The current ElectricCharge %
    /// </summary>
    //[UI_Label(scene = UI_Scene.All)]
    [KSPField(
        advancedTweakable = true,
        isPersistant      = false,
        guiActive         = true,
        guiActiveEditor   = true,
        guiName           = "ElectricCharge",
        guiFormat         = "F2",
        guiUnits          = "%",
        groupName         = GroupName
      )]
    public double CurrentCharge;


    /// <summary>
    ///   The maximum threshold axis
    /// </summary>
    [KSPAxisField(
        axisMode              = KSPAxisMode.Incremental,
        advancedTweakable     = true,
        isPersistant          = true,
        ignoreIncrementByZero = true,
        guiName               = "Maximum Threshold %",
        minValue              = MinThresholdPercent,
        maxValue              = MaxThresholdPercent,
        incrementalSpeed      = 10.0f,
        unfocusedRange        = MaxThresholdPercent
      )]
    public float MaxThresholdAxis = MaxThresholdPercent;


    /// <summary>
    ///   The minimum threshold axis
    /// </summary>
    [KSPAxisField(
        axisMode              = KSPAxisMode.Incremental,
        advancedTweakable     = true,
        ignoreIncrementByZero = true,
        isPersistant          = true,
        guiName               = "Minimum Threshold %",
        minValue              = MinThresholdPercent,
        maxValue              = MaxThresholdPercent,
        incrementalSpeed      = 10.0f,
        unfocusedRange        = MinThresholdPercent
      )]
    public float MinThresholdAxis = MinThresholdPercent;


    /// <summary>
    ///   The min/max thresholds
    /// </summary>
    [UI_MinMaxRange(
        affectSymCounterparts = UI_Scene.All,
        maxValueX             = MaxThresholdPercent, // fuel cells have hard-coded 95% capacity ratings, any higher and this mod won't work.
        maxValueY             = MaxThresholdPercent,
        minValueX             = MinThresholdPercent,        // 1% bare-minimum, in an attempt to avoid "dead probe" syndrome :p
        minValueY             = MinThresholdPercent + 1.0f, // must be one higher than minValueX
        stepIncrement         = 1.0f
      )]
    [KSPField(
        advancedTweakable = true,
        isPersistant      = true,
        guiActive         = true,
        guiActiveEditor   = true,
        guiName           = "Automatic Threshold %",
        guiFormat         = "F0",
        guiUnits          = "%",
        groupName         = GroupName
      )]
    public Vector2 Thresholds = new Vector2(
        x: MinThresholdPercent,
        y: MaxThresholdPercent
      );
    

    /// <summary>
    ///   For debugging use only.
    /// </summary>
    /// <param name="m">The m.</param>
    [Conditional(conditionString: "DEBUG")]
    private static void DebugLog(object m) => Debug.Log(message: $"[AutomaticFuelCell]: {m}");


    /// <summary>
    ///   Toggles the operation modes.
    /// </summary>
    /// <param name="param">The parameter.</param>
    [KSPAction(
        guiName           = "Toggle Auto/Manual Operation",
        advancedTweakable = true
      )]
    public void ToggleOperationModes(KSPActionParam param)
    {
      if (GameSettings.ADVANCED_TWEAKABLES) Automatic = !Automatic;
    }


    /// <summary>
    ///   Sets the operation mode to Automatic.
    /// </summary>
    /// <param name="param">The parameter.</param>
    [KSPAction(
        guiName           = "Set Auto Operation",
        advancedTweakable = true
      )]
    public void AutomaticOperationMode(KSPActionParam param)
    {
      if (GameSettings.ADVANCED_TWEAKABLES) Automatic = true;
    }


    /// <summary>
    ///   Sets the operation mode to Manual.
    /// </summary>
    /// <param name="param">The parameter.</param>
    [KSPAction(
        guiName           = "Set Manual Operation",
        advancedTweakable = true
      )]
    public void ManualOperationMode(KSPActionParam param)
    {
      if (GameSettings.ADVANCED_TWEAKABLES) Automatic = false;
    }


    /// <summary>
    ///   Called by unity API on game start.
    /// </summary>
    /// <param name="state">The state.</param>
    public override void OnStart(StartState state)
    {
      // ReSharper disable once InconsistentNaming
      const string EC = "ElectricCharge";

      try
      {
        base.OnStart(state: state);

        Fields[fieldName: "MinThresholdAxis"].OnValueModified += o => MinThresholdAxis = Thresholds.x = (float) UtilMath.Clamp(
                                                                                             value: MinThresholdAxis,
                                                                                             min: MinThresholdPercent,
                                                                                             max: MaxThresholdAxis
                                                                                           );

        Fields[fieldName: "MaxThresholdAxis"].OnValueModified += o => MaxThresholdAxis = Thresholds.y = (float) UtilMath.Clamp(
                                                                                             value: MaxThresholdAxis,
                                                                                             min: MinThresholdAxis + 1.0f,
                                                                                             max: MaxThresholdPercent
                                                                                           );


        // obtain the EC resource
        _electricCharge = part.Resources?.Get(name: EC);
        if (_electricCharge == null) DebugLog(m: "Error: failed to obtain EC resource"); // warn if not found, this module cannot function without.


        // find the first converter module that outputs EC
        _resourceConverter = part.Modules.GetModules<ModuleResourceConverter>()
                                 .First(
                                      predicate: converter => converter.outputList.Any(
                                          predicate: resource => resource.ResourceName.Equals(
                                              value: EC,
                                              comparisonType:
                                              StringComparison
                                               .InvariantCultureIgnoreCase
                                            )
                                        )
                                    );

        if (_resourceConverter == null) DebugLog(m: "Error: failed to obtain Resource Converter"); // warn if not found, this module cannot function without.
      }
      catch (Exception e) { DebugLog(m: e); }
    }


    /// <summary>
    ///   Called by Unity API for every physics update.
    /// </summary>
    public void FixedUpdate()
    {
      try
      {
        base.OnFixedUpdate();

        if (_electricCharge    == null
         || _resourceConverter == null)
        {
          DebugLog(m: "Something is null");
          return; // already checked for this in OnStart()
        }

        CurrentCharge = _electricCharge.amount / _electricCharge.maxAmount * 100; // compute current EC, in percent
        DebugLog(m: $"Current Charge = {CurrentCharge:F2}");

        if (!GameSettings.ADVANCED_TWEAKABLES
         || !Automatic)
        {
          DebugLog(m: "Automatic Disabled");
          return; // don't do anything in manual operation
        }

        if (CurrentCharge <= Thresholds.x
         && !_resourceConverter.IsActivated)
        {
          DebugLog(m: "Starting Fuel Cell");
          _resourceConverter.StartResourceConverterAction(param: null); // turn on if below minimum setting
        }
        else if (CurrentCharge >= Thresholds.y
              && _resourceConverter.IsActivated)
        {
          DebugLog(m: "Stopping Fuel Cell");
          _resourceConverter.StopResourceConverterAction(param: null); // automatically turn off if above maximum setting
        }
      }
      catch (Exception e) { DebugLog(m: e); }
    }

    /// <summary>
    ///   Gets the information.
    /// </summary>
    /// <returns></returns>
    public override string GetInfo() => "Able to automatically toggle itself on and off as needed.";
  }
}
