using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
public class ConditionalHideAttribute : PropertyAttribute
{
	//The name of the bool field that will be in control
	public string ConditionalSourceField = "";

	//TRUE = Hide in inspector / FALSE = Disable in inspector 
	public bool HideInInspector = false;

	public float MinValue = 0f;
	public float MaxValue = 0f;
	public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector = false, float minValue = 0, float maxValue = 0)
	{
		this.ConditionalSourceField = conditionalSourceField;
		this.HideInInspector = hideInInspector;
		this.MinValue = minValue;
		this.MaxValue = maxValue;
	}
}