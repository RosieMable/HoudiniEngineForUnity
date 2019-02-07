/*
* Copyright (c) <2019> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace HoudiniEngineUnity
{

	[CustomEditor(typeof(HEU_PDGAssetLink))]
	public class HEU_PDGAssetLinkUI : Editor
	{
		private void OnEnable()
		{
			_assetLink = target as HEU_PDGAssetLink;
		}

		public override void OnInspectorGUI()
		{
			if (_assetLink == null)
			{
				DrawNoAssetLink();
				return;
			}

			// Always hook into asset UI callback. This could have got reset on code refresh.
			_assetLink._repaintUIDelegate = RefreshUI;

			serializedObject.Update();

			SetupUI();

			DrawPDGStatus();

			DrawAssetLink();
		}

		private void DrawNoAssetLink()
		{
			HEU_EditorUI.DrawSeparator();

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.yellow : Color.red;
			EditorGUILayout.LabelField("Houdini Engine Asset - no HEU_PDGAssetLink found!", labelStyle);

			HEU_EditorUI.DrawSeparator();
		}

		private void DrawAssetLink()
		{
			HEU_PDGAssetLink.LinkState validState = _assetLink.AssetLinkState;

			using (new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				EditorGUILayout.Space();

				SerializedProperty assetGOProp = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_assetGO");
				if (assetGOProp != null)
				{
					EditorGUILayout.PropertyField(assetGOProp, _assetGOLabel, false);
				}

				EditorGUILayout.Space();

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button(_refreshContent))
					{
						_assetLink.Refresh();
					}

					if (GUILayout.Button(_resetContent))
					{
						_assetLink.Reset();
					}
				}

				EditorGUILayout.Space();

				using (new EditorGUILayout.VerticalScope(HEU_EditorUI.GetSectionStyle()))
				{
					EditorGUILayout.LabelField("Asset is " + validState);

					if (validState == HEU_PDGAssetLink.LinkState.ERROR_NOT_LINKED)
					{
						EditorGUILayout.LabelField("Failed to link with HDA. Unable to proceed. Try rebuilding asset.");
					}
					else if (validState == HEU_PDGAssetLink.LinkState.LINKED)
					{
						EditorGUILayout.Space();

						EditorGUILayout.LabelField(_assetStatusLabel);

						DrawWorkItemTally(_assetLink._workItemTally);

						EditorGUILayout.Space();
					}
				}
			}

			if (validState == HEU_PDGAssetLink.LinkState.INACTIVE)
			{
				_assetLink.Refresh();
			}
			else if (validState == HEU_PDGAssetLink.LinkState.LINKED)
			{
				using (new EditorGUILayout.VerticalScope(_backgroundStyle))
				{
					EditorGUILayout.Space();

					// Dropdown list of TOP network names
					DrawSelectedTOPNetwork();

					EditorGUILayout.Space();

					// Dropdown list of TOP nodes
					DrawSelectedTOPNode();
				}
			}
		}

		private void DrawSelectedTOPNetwork()
		{
			HEU_EditorUI.DrawHeadingLabel("Internal TOP Networks");

			int numTopNodes = _assetLink._topNetworkNames.Length;
			if (numTopNodes > 0)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.PrefixLabel(_topNetworkChooseLabel);

					int numTOPs = _assetLink._topNetworkNames.Length;

					int selectedIndex = Mathf.Clamp(_assetLink.SelectedTOPNetwork, 0, numTopNodes - 1);
					int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, _assetLink._topNetworkNames);
					if (newSelectedIndex != selectedIndex)
					{
						_assetLink.SelectTOPNetwork(newSelectedIndex);
					}
				}
			}
			else
			{
				EditorGUILayout.PrefixLabel(_topNetworkNoneLabel);
			}
		}

		private void DrawSelectedTOPNode()
		{
			HEU_TOPNetworkData topNetworkData = _assetLink.GetSelectedTOPNetwork();
			if (topNetworkData == null)
			{
				return;
			}

			using(new EditorGUILayout.VerticalScope())
			{
				//HEU_EditorUI.DrawHeadingLabel("Internal TOP Nodes");

				int numTopNodes = topNetworkData._topNodeNames.Length;
				if (numTopNodes > 0)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.PrefixLabel(_topNodeChooseLabel);

						int selectedIndex = Mathf.Clamp(topNetworkData._selectedTOPIndex, 0, numTopNodes);
						int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, topNetworkData._topNodeNames);
						if (newSelectedIndex != selectedIndex)
						{
							_assetLink.SelectTOPNode(topNetworkData, newSelectedIndex);
						}

						//EditorGUILayout.Space();
					}
				}
				else
				{
					EditorGUILayout.PrefixLabel(_topNodeNoneLabel);
				}

				EditorGUILayout.Space();

				HEU_TOPNodeData topNode = _assetLink.GetSelectedTOPNode();
				if (topNode != null)
				{
					bool autoLoad = topNode._autoLoad;
					autoLoad = EditorGUILayout.Toggle("AutoLoad Results", autoLoad);
					if (autoLoad != topNode._autoLoad)
					{
						topNode._autoLoad = autoLoad;
					}

					EditorGUILayout.Space();

					using (new EditorGUILayout.HorizontalScope())
					{
						//GUILayout.Space(15);

						if (GUILayout.Button(_buttonDirtyContent))
						{
							_assetLink.DirtyTOPNode(topNode);
						}

						if (GUILayout.Button(_buttonCookContent))
						{
							_assetLink.CookTOPNode(topNode);
						}

						//GUILayout.Space(15);
					}

					EditorGUILayout.Space();

					using (new EditorGUILayout.VerticalScope(HEU_EditorUI.GetSectionStyle()))
					{
						EditorGUILayout.LabelField("Node State: " + topNode._pdgState);

						EditorGUILayout.Space();

						EditorGUILayout.LabelField(_topNodeStatusLabel);
						DrawWorkItemTally(topNode._workItemTally);
					}
				}
			}
		}

		private void DrawPDGStatus()
		{
			string pdgState = "PDG is NOT READY";
			Color stateColor = Color.red;

			HEU_PDGSession pdgSession = HEU_PDGSession.GetPDGSession();
			if (pdgSession != null)
			{
				if (pdgSession._pdgState == HAPI_PDG_State.HAPI_PDG_STATE_COOKING)
				{
					pdgState = "PDG is COOKING";
					stateColor = Color.yellow;

					if (_assetLink != null)
					{
						pdgState = string.Format("{0} ({1})", pdgState, _assetLink._workItemTally.ProgressRatio());
					}
				}
				else if (pdgSession._pdgState == HAPI_PDG_State.HAPI_PDG_STATE_READY)
				{
					pdgState = "PDG is READY";
					stateColor = Color.green;
				}
			}

			EditorGUILayout.Space();

			_boxStyleStatus.normal.textColor = stateColor;
			GUILayout.Box(pdgState, _boxStyleStatus);
		}

		private void DrawWorkItemTally(HEU_WorkItemTally tally)
		{
			float totalWidth = EditorGUIUtility.currentViewWidth;
			float cellWidth = totalWidth / 6f;

			float titleCellHeight = 26;
			float cellHeight = 24;

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				_boxStyleTitle.normal.textColor = Color.black;
				DrawGridBoxTitle("TOTAL", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = (tally._waitingWorkItems > 0) ? Color.cyan : Color.black;
				DrawGridBoxTitle("WAITING", cellWidth, titleCellHeight);

				//_boxStyleTitle.normal.textColor = (tally._scheduledWorkItems > 0) ? Color.yellow : Color.black;
				//DrawGridBoxTitle("SCHEDULED", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = ((tally._scheduledWorkItems + tally._cookingWorkItems) > 0) ? Color.yellow : Color.black;
				DrawGridBoxTitle("COOKING", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = (tally._cookedWorkItems > 0) ? _cookedColor : Color.black;
				DrawGridBoxTitle("COOKED", cellWidth, titleCellHeight);
				
				_boxStyleTitle.normal.textColor = (tally._erroredWorkItems > 0) ? Color.red : Color.black;
				DrawGridBoxTitle("FAILED", cellWidth, titleCellHeight);

				GUILayout.FlexibleSpace();
			}

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				DrawGridBoxValue(string.Format("{0}", tally._totalWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._waitingWorkItems), cellWidth, cellHeight);
				//DrawGridBoxValue(string.Format("{0}", tally._scheduledWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", (tally._scheduledWorkItems + tally._cookingWorkItems)), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._cookedWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._erroredWorkItems), cellWidth, cellHeight);

				GUILayout.FlexibleSpace();
			}
		}

		private void DrawGridBoxTitle(string text, float width, float height)
		{
			GUILayout.Box(text, _boxStyleTitle, GUILayout.Width(width), GUILayout.Height(height));
		}

		private void DrawGridBoxValue(string text, float width, float height)
		{
			GUILayout.Box(text, _boxStyleValue, GUILayout.Width(width), GUILayout.Height(height));
		}

        private void SetupUI()
		{
			_cookedColor = new Color(0.1f, 0.9f, 0.0f, 1f);

			_assetGOLabel = new GUIContent("TOP Asset To Link", "The HDA containing TOP networks to link with.");
			_assetStatusLabel = new GUIContent("Asset Work Items Status:");

			_resetContent = new GUIContent("Reset", "Reset the state and generated items. Updates from linked HDA.");
			_refreshContent = new GUIContent("Refresh", "Refresh the state and UI.");

			_topNetworkChooseLabel = new GUIContent("TOP Network");
			_topNetworkNoneLabel = new GUIContent("TOP Network: None");

			_topNodeChooseLabel = new GUIContent("TOP Node");
			_topNodeNoneLabel = new GUIContent("TOP Node: None");
			_topNodeStatusLabel = new GUIContent("TOP Node Work Items Status:");

			_buttonDirtyContent = new GUIContent("Dirty Node", "Removes all work items.");
			_buttonCookContent = new GUIContent("Cook Node", "Generates and cooks all work items.");

			_backgroundStyle = new GUIStyle(GUI.skin.box);
			RectOffset br = _backgroundStyle.margin;
			br.top = 10;
			br.bottom = 6;
			br.left = 4;
			br.right = 4;
			_backgroundStyle.margin = br;

			br = _backgroundStyle.padding;
			br.top = 8;
			br.bottom = 8;
			br.left = 8;
			br.right = 8;
			_backgroundStyle.padding = br;

			_boxStyleTitle = new GUIStyle(GUI.skin.box);
			float c = 0.35f;
			_boxStyleTitle.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleTitle.normal.textColor = Color.black;
			_boxStyleTitle.fontStyle = FontStyle.Bold;
			_boxStyleTitle.alignment = TextAnchor.MiddleCenter;
			_boxStyleTitle.fontSize = 10;

			_boxStyleValue = new GUIStyle(GUI.skin.box);
			c = 0.7f;
			_boxStyleValue.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleValue.normal.textColor = Color.black;
			_boxStyleValue.fontStyle = FontStyle.Bold;
			_boxStyleValue.fontSize = 14;

			_boxStyleStatus = new GUIStyle(GUI.skin.box);
			c = 0.3f;
			_boxStyleStatus.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleStatus.normal.textColor = Color.black;
			_boxStyleStatus.fontStyle = FontStyle.Bold;
			_boxStyleStatus.alignment = TextAnchor.MiddleCenter;
			_boxStyleStatus.fontSize = 14;
			_boxStyleStatus.stretchWidth = true;
		}

		public void RefreshUI()
		{
			if (_assetLink != null)
			{
				_assetLink.UpdateWorkItemTally();
			}

			Repaint();
		}

		//	DATA ------------------------------------------------------------------------------------------------------

		public HEU_PDGAssetLink _assetLink;

		private GUIStyle _backgroundStyle;

		private GUIContent _assetGOLabel;
		private GUIContent _assetStatusLabel;

		private GUIContent _resetContent;
		private GUIContent _refreshContent;

		private GUIContent _topNetworkChooseLabel;
		private GUIContent _topNetworkNoneLabel;

		private GUIContent _topNodeChooseLabel;
		private GUIContent _topNodeNoneLabel;
		private GUIContent _topNodeStatusLabel;

		private GUIContent _buttonDirtyContent;
		private GUIContent _buttonCookContent;

		private GUIStyle _boxStyleTitle;
		private GUIStyle _boxStyleValue;
		private GUIStyle _boxStyleStatus;

		private Texture2D _boxTitleTexture;

		private Color _cookedColor;
	}

}   // HoudiniEngineUnity