/*
 * PROPRIETARY INFORMATION.  This software is proprietary to
 * Side Effects Software Inc., and is not to be reproduced,
 * transmitted, or disclosed in any way without written permission.
 *
 * Produced by:
 *      Side Effects Software Inc
 *		123 Front Street West, Suite 1401
 *		Toronto, Ontario
 *		Canada   M5J 2M2
 *		416-504-9876
 *
 * COMMENTS:
 * 
 */


using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[ CustomEditor( typeof( HoudiniAssetOTL ) ) ]
public partial class HoudiniAssetGUIOTL : HoudiniAssetGUI 
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public
	
	public override void OnEnable() 
	{
		base.OnEnable();
		myAssetOTL = myAsset as HoudiniAssetOTL;
	}

	public override void OnDisable()
	{
		base.OnDisable();

		if ( myGeoAttributeManager && myAssetOTL.prActiveEditPaintGeo )
		{
			myGeoAttributeManagerGUI = null;
		}
	}
	
	public override void OnInspectorGUI() 
	{
		base.OnInspectorGUI();

		bool gui_enable = GUI.enabled;
		
		// We can only build or do anything if we can link to our libraries.
#if !( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )
		GUI.enabled = false;
#else
		if ( !HoudiniHost.isInstallationOk() )
			GUI.enabled = false;
#endif // !( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )

		if ( myAssetOTL.isPrefabInstance() )
			myParentPrefabAsset = myAsset.getParentPrefabAsset();

		///////////////////////////////////////////////////////////////////////
		// Draw Game Object Controls
		

		myAsset.prShowHoudiniControls
			= HoudiniGUI.foldout( "Houdini Controls", myAsset.prShowHoudiniControls, true );
		if ( myAsset.prShowHoudiniControls )
		{
			if ( !myAsset.isPrefab() )
			{
				if ( GUILayout.Button( "Rebuild" ) )
					myAsset.buildAll();
	
				if ( GUILayout.Button( "Recook" ) )
					myAsset.buildClientSide();

				if ( GUILayout.Button( "Bake" ) )
					myAsset.bakeAsset();
			}
		}

		// Draw Help Pane
		myAsset.prShowHelp = HoudiniGUI.foldout( "Asset Help", myAsset.prShowHelp, true );
		if ( myAsset.prShowHelp )
			drawHelpBox( myAsset.prAssetHelp );
		
		///////////////////////////////////////////////////////////////////////
		// Draw Asset Settings
		// These don't affect the asset directly so they don't trigger rebuilds.

		myAsset.prShowAssetSettings = HoudiniGUI.foldout( "Asset Settings", myAsset.prShowAssetSettings, true );
		if ( myAsset.prShowAssetSettings )
			generateAssetSettings();

		///////////////////////////////////////////////////////////////////////
		// Draw Baking Controls
		
		if( !myAsset.isPrefab() )
		{
			myAsset.prShowBakeOptions = HoudiniGUI.foldout( "Bake Animations", myAssetOTL.prShowBakeOptions, true );
			if ( myAsset.prShowBakeOptions )
				generateAssetBakeControls();
		}

		///////////////////////////////////////////////////////////////////////
		// Draw Paint Tools
#if !HAPI_PAINT_SUPPORT
		if( !myAsset.isPrefab() )
		{
			myAsset.prShowPaintTools = HoudiniGUI.foldout( "Paint Tools", myAssetOTL.prShowPaintTools, true );
			if ( myAsset.prShowPaintTools )
				generatePaintToolGUI();
		}
#endif // !HAPI_PAINT_SUPPORT

		GUI.enabled = gui_enable;
	}

	public override void OnSceneGUI()
	{
		base.OnSceneGUI();

#if !HAPI_PAINT_SUPPORT
		if ( myGeoAttributeManagerGUI != null )
		{
			myGeoAttributeManagerGUI.OnSceneGUI();
			if ( myGeoAttributeManager != null && myGeoAttributeManager.prHasChanged && myGeoAttributeManager.prLiveUpdates )
			{
				myGeoAttributeManager.prHasChanged = false;

				HoudiniPartControl part_control =
					myAssetOTL.prActiveEditPaintGeo.prParts[ 0 ].GetComponent< HoudiniPartControl >();
				Mesh mesh = part_control.GetComponent< MeshFilter >().sharedMesh;
				HoudiniAssetUtility.setMesh(
					part_control.prAssetId, part_control.prObjectId, part_control.prGeoId,
					ref mesh, part_control, myGeoAttributeManager );
				myAssetOTL.buildClientSide();
			}
		}
#endif // !HAPI_PAINT_SUPPORT

		handlesOnSceneGUI();
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private
	
	private void generateAssetBakeControls()
	{
		// Start Time
		{
			float value = myAsset.prBakeStartTime;
			bool changed = HoudiniGUI.floatField(
				"bake_start_time", "Start Time", ref value, 
				myUndoInfo, ref myUndoInfo.bakeStartTime );

			if ( changed )
				myAsset.prBakeStartTime = value;
		}
		
		// End Time
		{
			float value = myAsset.prBakeEndTime;
			bool changed = HoudiniGUI.floatField(
				"bake_end_time", "End Time", ref value, 
				myUndoInfo, ref myUndoInfo.bakeEndTime );
			if ( changed )
				myAsset.prBakeEndTime = value;
		}
		
		// Samples per second
		{
			int value = myAsset.prBakeSamplesPerSecond;
			bool changed = HoudiniGUI.intField(
				"bake_samples_per_second", "Samples Per Second", ref value,
				1, 120, myUndoInfo, ref myUndoInfo.bakeSamplesPerSecond );

			if ( changed )
				myAsset.prBakeSamplesPerSecond = value;
		}
		
		if ( GUILayout.Button( "Bake" ) ) 
		{
			HoudiniProgressBar progress_bar = new HoudiniProgressBar();
			myAsset.bakeAnimations(
				myAsset.prBakeStartTime, 
				myAsset.prBakeEndTime, 
				myAsset.prBakeSamplesPerSecond, 
				myAsset.gameObject,
				progress_bar );
			progress_bar.clearProgressBar();
		}
	}

#if !HAPI_PAINT_SUPPORT
	private void generatePaintToolGUI()
	{
		foreach ( HoudiniGeoControl geo_control in myAssetOTL.prEditPaintGeos )
		{
			if ( HoudiniGUI.button( geo_control.prGeoName, geo_control.prGeoName ) )
			{
				myAssetOTL.prActiveEditPaintGeo = geo_control;
				myGeoAttributeManager = geo_control.prGeoAttributeManager;
				myGeoAttributeManagerGUI = new HoudiniGeoAttributeManagerGUI( myGeoAttributeManager );
			}
		}
	}
#endif // !HAPI_PAINT_SUPPORT

	private void generateViewSettings()
	{
		// Show Geometries
		createToggleForProperty(
			"show_geometries", "Show Geometries", "prIsGeoVisible", 
			ref myUndoInfo.isGeoVisible, () => myAsset.applyGeoVisibilityToParts() );
		
		// Show Pinned Instances
		createToggleForProperty(
			"show_pinned_instances", "Show Pinned Instances", "prShowPinnedInstances", 
			ref myUndoInfo.showPinnedInstances, null );

		// Auto Select Asset Root Node Toggle
		createToggleForProperty(
			"auto_select_asset_root_node", "Auto Select Asset Root Node", 
			"prAutoSelectAssetRootNode", ref myUndoInfo.autoSelectAssetRootNode,
			null, !HoudiniHost.isAutoSelectAssetRootNodeDefault() );
	}
	
	private void generateMaterialSettings()
	{	
		if ( GUILayout.Button( "Re-Render" ) ) 
		{
			HoudiniAssetUtility.reApplyMaterials( myAsset );
		}

		// Material Shader Type
		{
			HAPI_ShaderType value = myAsset.prMaterialShaderType;
			bool is_bold = myParentPrefabAsset && myParentPrefabAsset.prMaterialShaderType != value;
			string[] labels = { "OpenGL", "Houdini Mantra Renderer" };
			HAPI_ShaderType[] values = { HAPI_ShaderType.HAPI_SHADER_OPENGL, HAPI_ShaderType.HAPI_SHADER_MANTRA };
			bool changed = HoudiniGUI.dropdown(
				"material_renderer", "Material Renderer", ref value, 
				is_bold, labels, values, myUndoInfo, 
				ref myUndoInfo.materialShaderType );
			if ( changed )
			{
				myAsset.prMaterialShaderType = (HAPI_ShaderType) value;
				HoudiniAssetUtility.reApplyMaterials( myAsset );
			}
		}

		// Render Resolution
		{
			bool delay_build = false;
			int[] values 			= new int[ 2 ];
			values[ 0 ] 			= (int) myAsset.prRenderResolution[ 0 ];
			values[ 1 ] 			= (int) myAsset.prRenderResolution[ 1 ];
			int[] undo_values 		= new int[ 2 ];
			undo_values[ 0 ] 		= (int) myUndoInfo.renderResolution[ 0 ];
			undo_values[ 1 ] 		= (int) myUndoInfo.renderResolution[ 1 ];
			HoudiniGUIParm gui_parm 	= new HoudiniGUIParm( "render_resolution", "Render Resolution", 2 );

			gui_parm.isBold =
				myParentPrefabAsset && 
				(int) myParentPrefabAsset.prRenderResolution[ 0 ] != values[ 0 ] &&
				(int) myParentPrefabAsset.prRenderResolution[ 1 ] != values[ 1 ];

			bool changed = HoudiniGUI.intField(
				ref gui_parm, ref delay_build, ref values, 
				myUndoInfo, ref undo_values );
			if ( changed )
			{
				Vector2 new_resolution = new Vector2( (float) values[ 0 ], (float) values[ 1 ] );
				myAsset.prRenderResolution = new_resolution;
				myUndoInfo.renderResolution = new_resolution;
			}
		}

		// Show Vertex Colours
		createToggleForProperty(
			"show_only_vertex_colours", "Show Only Vertex Colors", 
			"prShowOnlyVertexColours", ref myUndoInfo.showOnlyVertexColours,
			() => HoudiniAssetUtility.reApplyMaterials( myAsset ) );

		// Generate Tangents
		createToggleForProperty(
			"generate_tangents", "Generate Tangents", "prGenerateTangents",
			ref myUndoInfo.generateTangents,
			() => myAssetOTL.build(
				true,	// reload_asset
				false,	// unload_asset_first
				false,	// serialization_recovery_only
				true,	// force_reconnect
				false,	// is_duplication
				myAsset.prCookingTriggersDownCooks,
				true	// use_delay_for_progress_bar
			), !HoudiniHost.isGenerateTangentsDefault() );
	}

	private void generateCookingSettings()
	{
		// Enable Cooking Toggle
		createToggleForProperty(
			"enable_cooking", "Enable Cooking", "prEnableCooking",
			ref myUndoInfo.enableCooking, null, !HoudiniHost.isEnableCookingDefault() );

		HoudiniGUI.separator();

		// Cooking Triggers Downstream Cooks Toggle
		createToggleForProperty(
			"cooking_triggers_downstream_cooks", "Cooking Triggers Downstream Cooks", 
			"prCookingTriggersDownCooks", ref myUndoInfo.cookingTriggersDownCooks,
			null, !HoudiniHost.isCookingTriggersDownCooksDefault(),
			!myAsset.prEnableCooking, " (all cooking is disabled)" );

		// Playmode Per-Frame Cooking Toggle
		createToggleForProperty(
			"playmode_per_frame_cooking", "Playmode Per-Frame Cooking", 
			"prPlaymodePerFrameCooking", ref myUndoInfo.playmodePerFrameCooking,
			null, !HoudiniHost.isPlaymodePerFrameCookingDefault(),
			!myAsset.prEnableCooking, " (all cooking is disabled)" );

		HoudiniGUI.separator();

		// Push Unity Transform To Houdini Engine Toggle
		createToggleForProperty(
			"push_unity_transform_to_houdini_engine", "Push Unity Transform To Houdini Engine", 
			"prPushUnityTransformToHoudini", ref myUndoInfo.pushUnityTransformToHoudini,
			null, !HoudiniHost.isPushUnityTransformToHoudiniDefault() );

		// Transform Change Triggers Cooks Toggle
		createToggleForProperty(
			"transform_change_triggers_cooks", "Transform Change Triggers Cooks", 
			"prTransformChangeTriggersCooks", ref myUndoInfo.transformChangeTriggersCooks,
			null, !HoudiniHost.isTransformChangeTriggersCooksDefault(),
			!myAsset.prEnableCooking, " (all cooking is disabled)" );

		HoudiniGUI.separator();

		// Import Templated Geos Toggle
		createToggleForProperty(
			"import_templated_geos", "Import Templated Geos", "prImportTemplatedGeos",
			ref myUndoInfo.importTemplatedGeos, null, !HoudiniHost.isImportTemplatedGeosDefault(),
			!myAsset.prEnableCooking, " (all cooking is disabled)" );

		HoudiniGUI.separator();

		// Split Geos by Group Toggle
		{
			createToggleForProperty(
				"split_geos_by_group_override", "Override Split Geos by Group", "prSplitGeosByGroupOverride",
				ref myUndoInfo.splitGeosByGroupOverride, null );
			createToggleForProperty(
				"split_geos_by_group", "Split Geos by Group", "prSplitGeosByGroup",
				ref myUndoInfo.splitGeosByGroup, () => EditorUtility.DisplayDialog(
					"Rebuild Required",
					"This change will take affect only after a full asset rebuild.",
					"Ok" ), false,
				!myAsset.prSplitGeosByGroupOverride, " (check the override checkbox to enable)" );
		}
	}

	private void generateAssetSettings()
	{
		GUIContent[] modes = new GUIContent[ 3 ];
		modes[ 0 ] = new GUIContent( "View" );
		modes[ 1 ] = new GUIContent( "Materials" );
		modes[ 2 ] = new GUIContent( "Cooking" );
		myAsset.prAssetSettingsCategory = GUILayout.Toolbar( myAsset.prAssetSettingsCategory, modes );

		switch ( myAsset.prAssetSettingsCategory )
		{
			case 0: generateViewSettings(); break;
			case 1: generateMaterialSettings(); break;
			case 2: generateCookingSettings(); break;
			default: Debug.LogError( "Invalid Asset Settings Tab." ); break;
		}
	}

	private HoudiniAssetOTL myAssetOTL;
	private HoudiniGeoAttributeManager myGeoAttributeManager;
	private HoudiniGeoAttributeManagerGUI myGeoAttributeManagerGUI;
}
