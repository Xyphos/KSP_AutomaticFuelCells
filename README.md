# KSP_AutomaticFuelCells
KSP plugin, Requires **Advanced Tweakables** game setting to be enabled.
The default behavior for Fuel Cells is to run continuously, constantly draining fuel to generate ElectricCharge.
This plugin will attempt to conserve fuel consumption by automatically toggling Fuel Cells on or off 
in accordance with a minimum and maximum threshold setting on the Fuel Cell's context menu.
If the Fuel Cell's stored ElectricCharge falls below the minimum threshold, the Fuel Cell will turn on
and if the Fuel Cell's stored ElectricCharge rises above the maximum threshold, the Fuel Cell will turn off.
This modified behavior is best used as a back-up system when Solar Power isn't available.