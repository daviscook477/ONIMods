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

using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using SideScreenRef = DetailsScreen.SideScreenRef;

namespace PeterHan.PLib.UI {
	/// <summary>
	/// Utility functions for dealing with Unity UIs.
	/// </summary>
	public static class PUIUtils {
		/// <summary>
		/// Adds text describing a particular component if available.
		/// </summary>
		/// <param name="result">The location to append the text.</param>
		/// <param name="component">The component to describe.</param>
		private static void AddComponentText(StringBuilder result, Component component) {
			// Include all fields
			var fields = component.GetType().GetFields(BindingFlags.DeclaredOnly |
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			// Class specific
			if (component is TMPro.TMP_Text lt)
				result.AppendFormat(", Text={0}, Color={1}, Font={2}", lt.text, lt.color,
					lt.font);
			else if (component is Image im) {
				result.AppendFormat(", Color={0}", im.color);
				if (im is KImage ki)
					result.AppendFormat(", Sprite={0}", ki.sprite);
			} else if (component is HorizontalOrVerticalLayoutGroup lg)
				result.AppendFormat(", Child Align={0}, Control W={1}, Control H={2}",
					lg.childAlignment, lg.childControlWidth, lg.childControlHeight);
			foreach (var field in fields) {
				object value = field.GetValue(component) ?? "null";
				// Value type specific
				if (value is LayerMask lm)
					value = "Layer #" + lm.value;
				else if (value is System.Collections.ICollection ic)
					value = "[" + ic.Join() + "]";
				else if (value is Array ar)
					value = "[" + ar.Join() + "]";
				result.AppendFormat(", {0}={1}", field.Name, value);
			}
		}

		/// <summary>
		/// Adds the specified side screen content to the side screen list. The side screen
		/// behavior should be defined in a class inherited from SideScreenContent.
		/// </summary>
		/// <typeparam name="T">The type of the controller that will determine how the side
		/// screen works. A new instance will be created and added as a component to the new
		/// side screen.</typeparam>
		/// <param name="uiPrefab">The UI prefab to use. If null is passed, the UI should
		/// be created and added to the GameObject hosting the controller object in its
		/// OnPrefabInit function.</param>
		/// <param name="inOrder">If the insertion should be performed in some order. The default of
		/// false will simply insert at the end of the list which will make this screen appear
		/// at the top of every side screen it applies to. Set to true to perform insertion
		/// elsewhere in the list.</param>
		/// <param name="insertBefore">If the insertion should be done before "insertionName" or
		/// after "insertionName". True means before, false means after.</param>
		/// <param name="insertionName">The name of the side screen that you want to choose
		/// the ordering of this one around. Should be from this list:
		/// Telepad Side Screen
		/// Assignable Side Screen
		/// Valve Side Screen
		/// Tree Filterable Side Screen
		/// Single Entity Receptacle Screen
		/// Planter Side Screen
		/// SingleSliderScreen
		/// DualSliderScreen
		/// Timed Switch Side Screen
		/// Threshold Switch Side Screen
		/// Research Side Screen
		/// Filter Side Screen
		/// Access Control Side Screen
		/// ActivationRangeSideScreen
		/// Gene Shuffler Side Screen
		/// Sealed Door Side Screen
		/// Capacity Control Side Screen
		/// Door Toggle Side Screen
		/// Suit Locker Side Screen
		/// Lure Side Screen
		/// Role Station Side Screen
		/// Automatable Side Screen
		/// SingleButtonSideScreen
		/// IncubatorSideScreen
		/// IntSliderSideScreen
		/// SingleCheckboxSideScreen
		/// CommandModuleSideScreen
		/// CometDetectorSideScreen
		/// TelescopeSideScreen
		/// ComplexFabricatorSideScreen
		/// MinionSideScreen
		/// MonumentSideScreen
		/// Logic Filter Side Screen</param>
		public static void AddSideScreenContent<T>(GameObject uiPrefab = null, bool inOrder = false, bool insertBefore = true, string insertionName = null)
				where T : SideScreenContent {
			var inst = DetailsScreen.Instance;
			if (inst == null) {
				LogUIWarning("DetailsScreen is not yet initialized, try a postfix on " +
					"DetailsScreen.OnPrefabInit");
				return;
			}
			if (inOrder && insertionName == null) {
				LogUIWarning("When specifying \"inOrder: true\" the \"insertionName\" must be non-null");
				return;
			}
			var trInst = Traverse.Create(inst);
			// These are private fields
			var ss = trInst.GetField<List<SideScreenRef>>("sideScreens");
			var body = trInst.GetField<GameObject>("sideScreenContentBody");
			string name = typeof(T).Name;
			if (ss != null && body != null) {
				// The ref normally contains a prefab which is instantiated
				var newScreen = new SideScreenRef();
				// Mimic the basic screens
				var rootObject = new GameObject(name);
				PUIElements.SetParent(rootObject, body);
				rootObject.AddComponent<LayoutElement>();
				rootObject.AddComponent<VerticalLayoutGroup>();
				rootObject.AddComponent<CanvasRenderer>();
				var controller = rootObject.AddComponent<T>();
				if (uiPrefab != null) {
					// Add prefab if supplied
					controller.ContentContainer = uiPrefab;
					uiPrefab.transform.parent = rootObject.transform;
				}
				newScreen.name = name;
				// Never used
				newScreen.offset = Vector2.zero;
				newScreen.screenPrefab = controller;
				newScreen.screenInstance = controller;
				if (!inOrder)
					ss.Add(newScreen);
				else {
					for (var i = 0; i < ss.Count; i++) {
						if (ss[i].name.Equals(insertionName)) {
							if (insertBefore) {
								ss.Insert(i, newScreen);
							} else {
								if (i + 1 >= ss.Count) {
									ss.Add(newScreen);
								} else {
									ss.Insert(i + 1, newScreen);
								}
							}
							return;
						}
					}
				}
			}
		}

		/// <summary>
		/// Builds a PLib UI object and adds it to an existing UI object.
		/// </summary>
		/// <param name="component">The UI object to add.</param>
		/// <param name="parent">The parent of the new object.</param>
		/// <param name="index">The sibling index to insert the element at, if provided.</param>
		/// <returns>The built version of the UI object.</returns>
		public static GameObject AddTo(this IUIComponent component, GameObject parent,
				int index = -2) {
			if (component == null)
				throw new ArgumentNullException("component");
			if (parent == null)
				throw new ArgumentNullException("parent");
			var child = component.Build();
			PUIElements.SetParent(child, parent);
			if (index == -1)
				child.transform.SetAsLastSibling();
			else if (index >= 0)
				child.transform.SetSiblingIndex(index);
			return child;
		}

		/// <summary>
		/// Dumps information about the parent tree of the specified GameObject to the debug
		/// log.
		/// </summary>
		/// <param name="item">The item to determine hierarchy.</param>
		public static void DebugObjectHierarchy(this GameObject item) {
			string info = "null";
			if (item != null) {
				var result = new StringBuilder(256);
				do {
					result.Append("- ");
					result.Append(item.name ?? "Unnamed");
					item = item.transform?.parent?.gameObject;
					if (item != null)
						result.AppendLine();
				} while (item != null);
				info = result.ToString();
			}
			LogUIDebug("Object Tree:" + Environment.NewLine + info);
		}

		/// <summary>
		/// Dumps information about the specified GameObject to the debug log.
		/// </summary>
		/// <param name="root">The root hierarchy to dump.</param>
		public static void DebugObjectTree(this GameObject root) {
			string info = "null";
			if (root != null)
				info = GetObjectTree(root, 0);
			LogUIDebug("Object Dump:" + Environment.NewLine + info);
		}

		/// <summary>
		/// A debug function used to forcefully re-layout a UI.
		/// </summary>
		/// <param name="uiElement">The UI to layout</param>
		/// <returns>The UI element, for call chaining.</returns>
		public static GameObject ForceLayoutRebuild(GameObject uiElement) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var rt = uiElement.rectTransform();
			if (rt != null)
				LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
			return uiElement;
		}

		/// <summary>
		/// Creates a string recursively describing the specified GameObject.
		/// </summary>
		/// <param name="root">The root GameObject hierarchy.</param>
		/// <param name="indent">The indentation to use.</param>
		/// <returns>A string describing this game object.</returns>
		private static string GetObjectTree(GameObject root, int indent) {
			var result = new StringBuilder(1024);
			// Calculate indent to make nested reading easier
			var solBuilder = new StringBuilder(indent);
			for (int i = 0; i < indent; i++)
				solBuilder.Append(' ');
			string sol = solBuilder.ToString();
			var transform = root.transform;
			int n = transform.childCount;
			// Basic information
			result.Append(sol).AppendFormat("GameObject[{0}, {1:D} child(ren), Layer {2:D}, " +
				"Active={3}]", root.name, n, root.layer, root.activeInHierarchy).AppendLine();
			// Transformation
			result.Append(sol).AppendFormat(" Translation={0} [{3}] Rotation={1} [{4}] " +
				"Scale={2}", transform.position, transform.rotation, transform.
				localScale, transform.localPosition, transform.localRotation).AppendLine();
			// Components
			foreach (var component in root.GetComponents<Component>()) {
				if (component is RectTransform rt) {
					// UI rectangle
					Vector2 size = rt.sizeDelta;
					result.Append(sol).AppendFormat(" Rect[Size=({0:F2},{1:F2}) Min=" +
						"({2:F2},{3:F2}) ", size.x, size.y, LayoutUtility.GetMinWidth(rt),
						LayoutUtility.GetMinHeight(rt));
					result.AppendFormat("Preferred=({0:F2},{1:F2}) Flexible=({2:F2}," +
						"{3:F2})]", LayoutUtility.GetPreferredWidth(rt), LayoutUtility.
						GetPreferredHeight(rt), LayoutUtility.GetFlexibleWidth(rt),
						LayoutUtility.GetFlexibleHeight(rt)).AppendLine();
				} else if (component != null && !(component is Transform)) {
					// Exclude destroyed components and Transform objects
					result.Append(sol).Append(" Component[").Append(component.GetType().
						FullName);
					AddComponentText(result, component);
					result.AppendLine("]");
				}
			}
			// Children
			if (n > 0)
				result.Append(sol).AppendLine(" Children:");
			for (int i = 0; i < n; i++) {
				var child = transform.GetChild(i).gameObject;
				if (child != null)
					// Exclude destroyed objects
					result.AppendLine(GetObjectTree(child, indent + 2));
			}
			return result.ToString().TrimEnd();
		}

		/// <summary>
		/// Calculates the size of a single game object.
		/// </summary>
		/// <param name="obj">The object to calculate.</param>
		/// <param name="direction">The direction to calculate.</param>
		/// <param name="elements">The layout eligible components of this game object.</param>
		/// <returns>The object's minimum and preferred size.</returns>
		internal static LayoutSizes GetSize(GameObject obj, PanelDirection direction,
				IEnumerable<ILayoutElement> elements) {
			float min = 0.0f, preferred = 0.0f, flexible = 0.0f, scaleFactor;
			int minPri = int.MinValue, prefPri = int.MinValue, flexPri = int.MinValue;
			var scale = obj.transform.localScale;
			// Find the correct scale direction
			if (direction == PanelDirection.Horizontal)
				scaleFactor = Math.Abs(scale.x);
			else
				scaleFactor = Math.Abs(scale.y);
			foreach (var component in elements)
				if (((component as Behaviour)?.isActiveAndEnabled ?? false) && !IgnoreLayout(
						component)) {
					// Calculate must come first
					if (direction == PanelDirection.Horizontal)
						component.CalculateLayoutInputHorizontal();
					else // if (direction == PanelDirection.Vertical)
						component.CalculateLayoutInputVertical();
					int lp = component.layoutPriority;
					// Larger values win
					if (direction == PanelDirection.Horizontal) {
						PriValue(ref min, component.minWidth, lp, ref minPri);
						PriValue(ref preferred, component.preferredWidth, lp, ref prefPri);
						PriValue(ref flexible, component.flexibleWidth, lp, ref flexPri);
					} else {
						PriValue(ref min, component.minHeight, lp, ref minPri);
						PriValue(ref preferred, component.preferredHeight, lp, ref prefPri);
						PriValue(ref flexible, component.flexibleHeight, lp, ref flexPri);
					}
				}
			return new LayoutSizes(obj, min * scaleFactor, Math.Max(min, preferred) *
				scaleFactor, flexible);
		}

		/// <summary>
		/// Reports whether the component should be ignored for layout.
		/// </summary>
		/// <param name="component">The component to check.</param>
		/// <returns>true if it specifies to ignore layout, or false otherwise.</returns>
		internal static bool IgnoreLayout(object component) {
			return (component as ILayoutIgnorer)?.ignoreLayout ?? false;
		}

		/// <summary>
		/// Loads a sprite embedded in the current assembly as a 9-slice sprite.
		/// 
		/// It may be encoded using PNG, DXT5, or JPG format.
		/// </summary>
		/// <param name="path">The fully qualified path to the image to load.</param>
		/// <param name="border">The sprite border.</param>
		/// <param name="log">true to log the load, or false otherwise.</param>
		/// <returns>The sprite thus loaded.</returns>
		/// <exception cref="ArgumentException">If the image could not be loaded.</exception>
		internal static Sprite LoadSprite(string path, Vector4 border = default, bool log = false) {
			// Open a stream to the image
			try {
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
						path)) {
					if (stream == null)
						throw new ArgumentException("Could not load image: " + path);
					// If len > int.MaxValue we will not go to space today
					int len = (int)stream.Length;
					byte[] buffer = new byte[len];
					var texture = new Texture2D(2, 2);
					// Load the texture from the stream
					stream.Read(buffer, 0, len);
					ImageConversion.LoadImage(texture, buffer, false);
					// Create a sprite centered on the texture
					int width = texture.width, height = texture.height;
#if DEBUG
					log = true;
#endif
					if (log)
						PUtil.LogDebug("Loaded sprite: {0} ({1:D}x{2:D}, {3:D} bytes)".F(path,
							width, height, len));
					// pivot is in RELATIVE coordinates!
					return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(
						0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, border);
				}
			} catch (System.IO.IOException e) {
				throw new ArgumentException("Could not load image: " + path, e);
			}
		}

		/// <summary>
		/// Loads a DDS sprite embedded in the current assembly as a 9-slice sprite.
		/// 
		/// It must be encoded using the DXT5 format.
		/// </summary>
		/// <param name="path">The fully qualified path to the DDS image to load.</param>
		/// <param name="width">The desired width.</param>
		/// <param name="height">The desired height.</param>
		/// <param name="border">The sprite border.</param>
		/// <param name="log">true to log the load, or false otherwise.</param>
		/// <returns>The sprite thus loaded.</returns>
		/// <exception cref="ArgumentException">If the image could not be loaded.</exception>
		internal static Sprite LoadSpriteLegacy(string path, int width, int height,
				Vector4 border = default) {
			// Open a stream to the image
			try {
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
						path)) {
					const int SKIP = 128;
					if (stream == null)
						throw new ArgumentException("Could not load image: " + path);
					// If len > int.MaxValue we will not go to space today, skip first 128
					// bytes of stream
					int len = (int)stream.Length - SKIP;
					if (len < 0)
						throw new ArgumentException("Image is too small: " + path);
					byte[] buffer = new byte[len];
					stream.Seek(SKIP, System.IO.SeekOrigin.Begin);
					stream.Read(buffer, 0, len);
					// Load the texture from the stream
					var texture = new Texture2D(width, height, TextureFormat.DXT5, false);
					texture.LoadRawTextureData(buffer);
					texture.Apply(true, true);
					// Create a sprite centered on the texture
					LogUIDebug("Loaded sprite: {0} ({1:D}x{2:D}, {3:D} bytes)".F(path,
						width, height, len));
					// pivot is in RELATIVE coordinates!
					return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(
						0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, border);
				}
			} catch (System.IO.IOException e) {
				throw new ArgumentException("Could not load image: " + path, e);
			}
		}

		/// <summary>
		/// Logs a debug message encountered in PLib UI functions.
		/// </summary>
		/// <param name="message">The debug message.</param>
		internal static void LogUIDebug(string message) {
			Debug.LogFormat("[PLib/UI/{0}] {1}", Assembly.GetCallingAssembly()?.GetName()?.
				Name ?? "?", message);
		}

		/// <summary>
		/// Logs a warning encountered in PLib UI functions.
		/// </summary>
		/// <param name="message">The warning message.</param>
		internal static void LogUIWarning(string message) {
			Debug.LogWarningFormat("[PLib/UI/{0}] {1}", Assembly.GetCallingAssembly()?.
				GetName()?.Name ?? "?", message);
		}

		/// <summary>
		/// Aggregates layout values, replacing the value if a higher priority value is given
		/// and otherwise taking the largest value.
		/// </summary>
		/// <param name="value">The current value.</param>
		/// <param name="newValue">The candidate new value. No operation if this is less than zero.</param>
		/// <param name="newPri">The new value's layout priority.</param>
		/// <param name="pri">The current value's priority</param>
		private static void PriValue(ref float value, float newValue, int newPri, ref int pri)
		{
			int thisPri = pri;
			if (newValue >= 0.0f) {
				if (newPri > thisPri) {
					// Priority override?
					pri = newPri;
					value = newValue;
				} else if (newValue > value && newPri == thisPri)
					// Same priority and higher value?
					value = newValue;
			}
		}
		
		/// <summary>
		/// Sets a UI element's flexible size.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="flexSize">The flexible size as a ratio.</param>
		/// <returns>The UI element, for call chaining.</returns>
		public static GameObject SetFlexUISize(this GameObject uiElement, Vector2 flexSize) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var fs = uiElement.GetComponent<ISettableFlexSize>();
			if (fs == null) {
				var le = uiElement.AddOrGet<LayoutElement>();
				le.flexibleWidth = flexSize.x;
				le.flexibleHeight = flexSize.y;
			} else {
				// Avoid duplicate LayoutElement on layouts
				fs.flexibleWidth = flexSize.x;
				fs.flexibleHeight = flexSize.y;
			}
			return uiElement;
		}

		/// <summary>
		/// Sets a UI element's minimum size.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="minSize">The minimum size in units.</param>
		/// <returns>The UI element, for call chaining.</returns>
		public static GameObject SetMinUISize(this GameObject uiElement, Vector2 minSize) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var le = uiElement.AddOrGet<LayoutElement>();
			float minX = minSize.x, minY = minSize.y;
			if (minX > 0.0f)
				le.minWidth = minX;
			if (minY > 0.0f)
				le.minHeight = minY;
			return uiElement;
		}

		/// <summary>
		/// Immediately resizes a UI element.
		/// </summary>
		/// <param name="uiElement">The UI element to modify.</param>
		/// <param name="size">The new element size.</param>
		/// <param name="addLayout">true to add a layout element with that size, or false
		/// otherwise.</param>
		/// <returns>The UI element, for call chaining.</returns>
		public static GameObject SetUISize(this GameObject uiElement, Vector2 size,
				bool addLayout = false) {
			if (uiElement == null)
				throw new ArgumentNullException("uiElement");
			var transform = uiElement.AddOrGet<RectTransform>();
			float width = size.x, height = size.y;
			if (width >= 0.0f)
				transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
			if (height >= 0.0f)
				transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			if (addLayout) {
				var le = uiElement.AddOrGet<LayoutElement>();
				// Set minimum and preferred size
				le.minWidth = width;
				le.minHeight = height;
				le.preferredWidth = width;
				le.preferredHeight = height;
				le.flexibleHeight = 0.0f;
				le.flexibleWidth = 0.0f;
			}
			return uiElement;
		}
	}
}
