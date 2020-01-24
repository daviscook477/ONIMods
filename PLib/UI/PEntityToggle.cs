﻿/*
 * Copyright 2020 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace PeterHan.PLib.UI {
	/// <summary>
	/// A custom UI button check box factory class.
	/// </summary>
	public class PEntityToggle : PTextComponent {
		/// <summary>
		/// The border size between the checkbox border and icon.
		/// </summary>
		private const float CHECKBOX_MARGIN = 2.0f;

		/// <summary>
		/// The unchecked state.
		/// </summary>
		public const int STATE_UNCHECKED = 0;

		/// <summary>
		/// The checked state.
		/// </summary>
		public const int STATE_CHECKED = 1;

		/// <summary>
		/// The partially checked state.
		/// </summary>
		public const int STATE_PARTIAL = 2;

		/// <summary>
		/// Generates the checkbox image states.
		/// </summary>
		/// <param name="imageColor">The color style for the checked icon.</param>
		/// <returns>The states for this checkbox.</returns>
		private static ToggleState[] GenerateStates(ColorStyleSetting imageColor) {
			var sps = new StatePresentationSetting() {
				color = imageColor.activeColor, use_color_on_hover = true,
				color_on_hover = imageColor.hoverColor, image_target = null,
				name = "Partial"
			};
			return new ToggleState[] {
				new ToggleState() {
					// Unchecked
					color = PUITuning.Colors.Transparent, color_on_hover = PUITuning.Colors.
					Transparent, sprite = null, use_color_on_hover = false,
					additional_display_settings = new StatePresentationSetting[] {
						new StatePresentationSetting() {
							color = imageColor.activeColor, use_color_on_hover = false,
							image_target = null, name = "Unchecked"
						}
					}
				},
				new ToggleState() {
					// Checked
					color = imageColor.activeColor, color_on_hover = imageColor.hoverColor,
					sprite = PUITuning.Images.Checked, use_color_on_hover = true,
					additional_display_settings = new StatePresentationSetting[] { sps }
				},
				new ToggleState() {
					// Partial
					color = imageColor.activeColor, color_on_hover = imageColor.hoverColor,
					sprite = PUITuning.Images.Partial, use_color_on_hover = true,
					additional_display_settings = new StatePresentationSetting[] { sps }
				}
			};
		}

		/// <summary>
		/// Gets a realized check box's state.
		/// </summary>
		/// <param name="realized">The realized check box.</param>
		/// <returns>The check box state.</returns>
		public static int GetCheckState(GameObject realized) {
			int state = STATE_UNCHECKED;
			if (realized == null)
				throw new ArgumentNullException("realized");
			var checkButton = realized.GetComponentInChildren<MultiToggle>();
			if (checkButton != null)
				state = checkButton.CurrentState;
			return state;
		}

		/// <summary>
		/// Sets a realized check box's state.
		/// </summary>
		/// <param name="realized">The realized check box.</param>
		/// <param name="state">The new state to set.</param>
		public static void SetCheckState(GameObject realized, int state) {
			if (realized == null)
				throw new ArgumentNullException("realized");
			var checkButton = realized.GetComponentInChildren<MultiToggle>();
			if (checkButton != null && checkButton.CurrentState != state)
				checkButton.ChangeState(state);
		}

		/// <summary>
		/// The check box color.
		/// </summary>
		public ColorStyleSetting CheckColor { get; set; }

		/// <summary>
		/// The check box's background color.
		/// </summary>
		public Color BackColor { get; set; }

		/// <summary>
		/// The size to scale the check box. If 0x0, it will not be scaled.
		/// </summary>
		public Vector2 CheckSize { get; set; }

		/// <summary>
		/// The initial check box state.
		/// </summary>
		public int InitialState { get; set; }

		/// <summary>
		/// The margin around the component.
		/// </summary>
		public RectOffset Margin { get; set; }

		/// <summary>
		/// The action to trigger on click. It is passed the realized source object.
		/// </summary>
		public PUIDelegates.OnChecked OnChecked { get; set; }

		public PEntityToggle() : this(null) { }

		public PEntityToggle(string name) : base(name ?? "CheckBox") {
			BackColor = PUITuning.Colors.BackgroundLight;
			CheckColor = null;
			CheckSize = new Vector2(16.0f, 16.0f);
			IconSpacing = 3;
			InitialState = STATE_UNCHECKED;
			Margin = new RectOffset();
			Sprite = null;
			Text = null;
			ToolTip = "";
		}

		public override GameObject Build() {
			var checkbox = PUIElements.CreateUI(null, Name);
			// Background
			var trueColor = CheckColor ?? PUITuning.Colors.ComponentLightStyle;
			// Add text
			if (!string.IsNullOrEmpty(Text))
				TextChildHelper(checkbox, TextStyle ?? PUITuning.Fonts.UILightStyle, Text);
			// Add foreground image since the background already has one
			if (Sprite != null)
				ImageChildHelper(checkbox, this);
			// Checkbox background
			var checkBack = PUIElements.CreateUI(checkbox, "CheckBox");
			checkBack.AddComponent<Image>().color = BackColor;
			// Checkbox border
			var checkBorder = PUIElements.CreateUI(checkBack, "CheckBorder");
			var borderImg = checkBorder.AddComponent<Image>();
			borderImg.sprite = PUITuning.Images.CheckBorder;
			borderImg.color = trueColor.activeColor;
			borderImg.type = Image.Type.Sliced;
			// Checkbox foreground
			var imageChild = PUIElements.CreateUI(checkBorder, "CheckMark", true, PUIAnchoring.
				Center, PUIAnchoring.Center);
			var img = imageChild.AddComponent<Image>();
			img.sprite = PUITuning.Images.Checked;
			img.preserveAspect = true;
			// Determine the checkbox size
			var actualSize = CheckSize;
			if (actualSize.x <= 0.0f || actualSize.y <= 0.0f) {
				var rt = imageChild.rectTransform();
				actualSize.x = LayoutUtility.GetPreferredWidth(rt);
				actualSize.y = LayoutUtility.GetPreferredHeight(rt);
			}
			imageChild.SetUISize(CheckSize, false);
			// Add tooltip
			if (!string.IsNullOrEmpty(ToolTip))
				checkbox.AddComponent<ToolTip>().toolTip = ToolTip;
			// Toggle
			var mToggle = checkbox.AddComponent<MultiToggle>();
			var evt = OnChecked;
			if (evt != null)
				mToggle.onClick += () => {
					evt?.Invoke(checkbox, mToggle.CurrentState);
				};
			mToggle.play_sound_on_click = true;
			mToggle.play_sound_on_release = false;
			mToggle.states = GenerateStates(trueColor);
			mToggle.toggle_image = img;
			mToggle.ChangeState(InitialState);
			checkbox.SetActive(true);
			// Lay out the checkbox using anchors only
			checkBack.SetUISize(new Vector2(CheckSize.x + 2.0f * CHECKBOX_MARGIN, CheckSize.y +
				2.0f * CHECKBOX_MARGIN), true);
			imageChild.SetUISize(CheckSize);
			// Icon and text are side by side
			var lp = new BoxLayoutParams() {
				Margin = Margin, Spacing = Math.Max(IconSpacing, 0), Alignment = TextAnchor.
				UpperCenter, Direction = PanelDirection.Vertical
			};
			if (DynamicSize)
				checkbox.AddComponent<BoxLayoutGroup>().Params = lp;
			else
				BoxLayoutGroup.LayoutNow(checkbox, lp);
			checkbox.SetFlexUISize(FlexSize);
			InvokeRealize(checkbox);
			return checkbox;
		}

		/// <summary>
		/// Sets the default Klei pink button style as this button's color and text style.
		/// </summary>
		/// <returns>This button for call chaining.</returns>
		public PEntityToggle SetKleiPinkStyle() {
			TextStyle = PUITuning.Fonts.UILightStyle;
			BackColor = PUITuning.Colors.ButtonPinkStyle.inactiveColor;
			CheckColor = PUITuning.Colors.ComponentDarkStyle;
			return this;
		}

		/// <summary>
		/// Sets the default Klei blue button style as this button's color and text style.
		/// </summary>
		/// <returns>This button for call chaining.</returns>
		public PEntityToggle SetKleiBlueStyle() {
			TextStyle = PUITuning.Fonts.UILightStyle;
			BackColor = PUITuning.Colors.ButtonBlueStyle.inactiveColor;
			CheckColor = PUITuning.Colors.ComponentDarkStyle;
			return this;
		}
	}
}
