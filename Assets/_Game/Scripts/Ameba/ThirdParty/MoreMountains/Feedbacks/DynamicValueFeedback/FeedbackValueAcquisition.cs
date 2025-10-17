using System;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace Ameba.MoreMountains.Feedbacks
{
	/// <summary>
	/// A class collecting value acquisition settings for feedbacks
	/// </summary>
	[System.Serializable]
	public class FeedbackValueAcquisition
	{
		public enum Modes
		{
			None,
			FirstValueHolder,
			PreviousValueHolder,
			ClosestValueHolder,
			NextValueHolder,
			LastValueHolder,
			ValueHolderByLabel
		}
		
		/// the selected mode for value acquisition
		[Tooltip("the selected mode for value acquisition")]
		public Modes Mode = Modes.None;
		
		/// the label of the value holder to look for when using ValueHolderByLabel mode
		[Tooltip("The label of the value holder to look for when using ValueHolderByLabel mode")]
		[MMFEnumCondition("Mode", (int)Modes.ValueHolderByLabel)]
		public string ValueHolderLabel = "";
		
		private static ValueHolderFeedback _valueHolder;
		
		/// <summary>
		/// Gets the appropriate value holder based on the acquisition settings
		/// </summary>
		public static ValueHolderFeedback GetValueHolder(FeedbackValueAcquisition settings, MMF_Player owner, int currentFeedbackIndex)
		{
			if (settings.Mode == Modes.None || owner?.FeedbacksList == null)
			{
				return null;
			}
			
			switch (settings.Mode)
			{
				case Modes.FirstValueHolder:
					return owner.GetFeedbackOfType<ValueHolderFeedback>(MMF_Player.AccessMethods.First, currentFeedbackIndex);
				case Modes.PreviousValueHolder:
					return owner.GetFeedbackOfType<ValueHolderFeedback>(MMF_Player.AccessMethods.Previous, currentFeedbackIndex);
				case Modes.ClosestValueHolder:
					return owner.GetFeedbackOfType<ValueHolderFeedback>(MMF_Player.AccessMethods.Closest, currentFeedbackIndex);
				case Modes.NextValueHolder:
					return owner.GetFeedbackOfType<ValueHolderFeedback>(MMF_Player.AccessMethods.Next, currentFeedbackIndex);
				case Modes.LastValueHolder:
					return owner.GetFeedbackOfType<ValueHolderFeedback>(MMF_Player.AccessMethods.Last, currentFeedbackIndex);
				case Modes.ValueHolderByLabel:
					if (!string.IsNullOrEmpty(settings.ValueHolderLabel))
					{
						for (int i = 0; i < owner.FeedbacksList.Count; i++)
						{
							if (owner.FeedbacksList[i] is ValueHolderFeedback valueHolder && 
							    valueHolder.Label.Equals(settings.ValueHolderLabel, StringComparison.OrdinalIgnoreCase))
							{
								return valueHolder;
							}
						}
					}
					break;
			}
			
			return null;
		}
	}
}