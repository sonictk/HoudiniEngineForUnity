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
using HAPI;

[ CustomEditor( typeof( HAPI_AssetOTL ) ) ]
public partial class HAPI_AssetGUIOTL : HAPI_AssetGUI 
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public
	
	public override void OnEnable() 
	{
		base.OnEnable();
		myAssetOTL = myAsset as HAPI_AssetOTL;
	}
	
	public override void OnInspectorGUI() 
	{
		myParmChanges = false;
		myDelayBuild = false;
		
		base.OnInspectorGUI();
		
		Event curr_event = Event.current;
		bool commitChanges = false;
		if ( curr_event.isKey && curr_event.type == EventType.KeyUp && curr_event.keyCode == KeyCode.Return )
			commitChanges = true;
		
		///////////////////////////////////////////////////////////////////////
		// Draw Game Object Controls
		
		myAssetOTL.prShowObjectControls = 
			EditorGUILayout.Foldout( myAssetOTL.prShowObjectControls, new GUIContent( "Object Controls" ) );
		
		if ( myAssetOTL.prShowObjectControls ) 
		{
			if ( GUILayout.Button( "Rebuild" ) ) 
			{
				myAssetOTL.prFullBuild = true;
				myAssetOTL.build();
			}
			if ( GUILayout.Button( "Recook" ) ) 
			{
				myAssetOTL.build();
			}
			
			EditorGUILayout.BeginHorizontal(); 
			{
				if ( GUILayout.Button( "Export To Hip File..." ) ) 
				{
					string hip_file_path = EditorUtility.SaveFilePanel( "Save HIP File", "", "hscene.hip", "hip" );
					if ( hip_file_path != "" && HAPI_Host.hasScene() )
						HAPI_Host.exportAssetToHIPFile( myAssetOTL.prAssetId, hip_file_path );
					else
						Debug.LogError( "Nothing to save." );
				}
				
				if ( GUILayout.Button( "Replace From Hip File..." ) ) 
				{
					string hip_file_path = EditorUtility.OpenFilePanel( "Import HIP File", "", "hip" );
					if ( hip_file_path != "" && HAPI_Host.hasScene() )
					{
						try
						{
							HAPI_Host.replaceAssetFromHIPFile ( myAssetOTL.prAssetId, hip_file_path );
						}
						catch ( HAPI_Error error )
						{
							Debug.LogError( error.ToString() );
						}
						
						myAssetOTL.prFullBuild = true;
						myAssetOTL.prReloadAssetInFullBuild = false;
						myAssetOTL.build();
					}
					else
						Debug.LogError( "Nothing to save." );
				}
				
			} 
			EditorGUILayout.EndHorizontal();
			
			string path = myAssetOTL.prAssetPath;
			myParmChanges |= HAPI_GUI.fileField( "otl_path", "OTL Path", ref myDelayBuild, ref path );
			if ( myParmChanges )
				myAssetOTL.prAssetPath = path;
			
			// These don't affect the asset directly so they don't trigger rebuilds.
			
			// Show Geometries
			{
				bool value = myAsset.prIsGeoVisible;
				bool changed = HAPI_GUI.toggle( "show_geometries", "Show Geometries", ref value );
				if ( changed )
				{
					myAsset.prIsGeoVisible = value;
					HAPI_PartControl[] controls = 
						myAsset.GetComponentsInChildren< HAPI_PartControl >();
					foreach ( HAPI_PartControl control in controls )
					{
						if ( control.prGeoType != HAPI_GeoType.HAPI_GEOTYPE_EXPOSED_EDIT
								&& control.gameObject.GetComponent< MeshRenderer >() != null )
							control.gameObject.GetComponent< MeshRenderer >().enabled = myAsset.prIsGeoVisible;
					}
				}
			}

			// Show Vertex Colours
			{
				bool value = myAsset.prShowVertexColours;
				bool changed = HAPI_GUI.toggle( "show_vertex_colours", "Show Vertex Colors", ref value );
				if ( changed )
				{
					myAsset.prShowVertexColours = value;
					foreach ( MeshRenderer renderer in myAsset.GetComponentsInChildren< MeshRenderer >() )
					{
						// Set material.
						if ( renderer.sharedMaterial == null )
							renderer.sharedMaterial = new Material( Shader.Find( "HAPI/SpecularVertexColor" ) );

						if ( myAsset.prShowVertexColours )
						{
							renderer.sharedMaterial.mainTexture = null;
							renderer.sharedMaterial.shader = Shader.Find( "HAPI/SpecularVertexColor" );
						}
						else
						{
							Transform parent = renderer.transform;
							HAPI_PartControl control = parent.GetComponent< HAPI_PartControl >();
							
							if ( control.prMaterialId >= 0 )
							{
								try
								{
									HAPI_MaterialInfo[] materials = new HAPI_MaterialInfo[ 1 ];
									HAPI_Host.getMaterials( myAsset.prAssetId, materials, control.prMaterialId, 1 );
									HAPI_MaterialInfo material = materials[ 0 ];

									if ( material.isTransparent() )
										renderer.sharedMaterial.shader = Shader.Find( "HAPI/AlphaSpecularVertexColor" );
									else if ( !material.isTransparent() )
										renderer.sharedMaterial.shader = Shader.Find( "HAPI/SpecularVertexColor" );

									Material mat = renderer.sharedMaterial;
									HAPI_AssetUtility.assignTexture( ref mat, material );
								}
								catch ( HAPI_Error error )
								{
									Debug.LogError( error.ToString() );
								}
							}
							else
							{
								renderer.sharedMaterial.shader = Shader.Find( "HAPI/SpecularVertexColor" );
							}
						}
					}
				}
			}

			HAPI_GUI.separator();

			// Enable Cooking Toggle
			{
				bool value = myAsset.prEnableCooking;
				HAPI_GUI.toggle( "enable_cooking", "Enable Cooking", ref value );
				myAsset.prEnableCooking = value;
			}

			// Auto Select Asset Node Toggle
			{
				bool value = myAsset.prAutoSelectAssetNode;
				HAPI_GUI.toggle( "auto_select_parent", "Auto Select Parent", ref value );
				myAsset.prAutoSelectAssetNode = value;
			}
			
			// Hide When Fed to Other Asset
			{
				bool value = myAsset.prHideWhenFedToOtherAsset;
				HAPI_GUI.toggle( "hide_when_fed_to_other_asset", "Hide When Fed to Other Asset", ref value );
				myAsset.prHideWhenFedToOtherAsset = value;
			}

			HAPI_GUI.separator();
			
			/* Hide for now since it's not used a lot.
			// Logging Toggle
			{
				bool value = myAsset.prEnableLogging;
				HAPI_GUI.toggle( "enable_logging", "Enable Logging", ref value );
				myAsset.prEnableLogging = value;
			}
			*/

			// Sync Asset Transform Toggle
			{
				bool value = myAsset.prSyncAssetTransform;
				HAPI_GUI.toggle( "sync_asset_transform", "Sync Asset Transform", ref value );
				myAsset.prSyncAssetTransform = value;
			}

			// Live Transform Propagation Toggle
			{
				bool value = myAsset.prLiveTransformPropagation;
				HAPI_GUI.toggle( "live_transform_propagation", "Live Transform Propagation", ref value );
				myAsset.prLiveTransformPropagation = value;
			}
		} // if
		
		///////////////////////////////////////////////////////////////////////
		// Draw Asset Controls
		
		EditorGUILayout.Separator();
		myAssetOTL.prShowAssetControls = 
			EditorGUILayout.Foldout( myAssetOTL.prShowAssetControls, new GUIContent( "Asset Controls" ) );
		
		if ( myAssetOTL.prShowAssetControls )
			myParmChanges |= generateAssetControls();
		
		if ( ( myParmChanges && !myDelayBuild ) || ( myUnbuiltChanges && commitChanges ) )
		{
			myAssetOTL.build();
			myUnbuiltChanges = false;
			myParmChanges = false;

			// To keep things consistent with Unity workflow, we should not save parameter changes
			// while in Play mode.
			if ( !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode )
				myAssetOTL.savePreset();
		}
		else if ( myParmChanges )
			myUnbuiltChanges = true;
	}
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private
	
	private bool generateAssetControl( int id, ref bool join_last, ref bool no_label_toggle_last )
	{
		if ( myAssetOTL.prParms == null )
			return false;
		
		if ( myAssetOTL.prParms[ id ].invisible )
			return false;
		
		bool changed 				= false;
		
		int asset_id				= myAssetOTL.prAssetId;
		
		HAPI_ParmInfo[] parms 		= myAssetOTL.prParms;
		HAPI_ParmInfo parm			= parms[ id ];
		
		int[] parm_int_values		= myAssetOTL.prParmIntValues;
		float[] parm_float_values	= myAssetOTL.prParmFloatValues;
		int[] parm_string_values	= myAssetOTL.prParmStringValues;
		
		HAPI_ParmType parm_type 	= (HAPI_ParmType) parm.type;
		int parm_size				= parm.size;
		
		HAPI_GUIParm gui_parm = new HAPI_GUIParm( parm );
		
		int values_index = -1;
		if ( parm.isInt() )
		{
			if ( parm.intValuesIndex < 0 || parm_int_values == null )
				return false;
			values_index = parm.intValuesIndex;
		}
		else if ( parm.isFloat() )
		{
			if ( parm.floatValuesIndex < 0 || parm_float_values == null )
				return false;
			values_index = parm.floatValuesIndex;
		}
		else if ( parms[ id ].isString() )
		{
			if ( parm.stringValuesIndex < 0 || parm_string_values == null )
				return false;
			values_index = parm.stringValuesIndex;
		}
		
		///////////////////////////////////////////////////////////////////////
		// Integer Parameter
		if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_INT )
		{
			if ( parm.choiceCount > 0 && parm.choiceIndex >= 0 )
			{
				// Draw popup (menu) field.
				List< string > 	labels = new List< string >();
				List< int>		values = new List< int >();
				
				// Go through our choices.
				for ( int i = 0; i < parm.choiceCount; ++i )
				{
					if ( myAssetOTL.prParmChoiceLists[ parm.choiceIndex + i ].parentParmId != id )
						Debug.LogError( "Parm choice parent parm id (" 
										+ myAssetOTL.prParmChoiceLists[ parm.choiceIndex + i ].parentParmId 
										+ ") not matching current parm id (" + id + ")!\n"
										+ "Choice index: " + ( parm.choiceIndex + i ) + ", "
										+ "Choice count: " + parm.choiceCount );
					
					labels.Add( myAssetOTL.prParmChoiceLists[ parm.choiceIndex + i ].label );
					values.Add( i );
				}
				
				changed = HAPI_GUI.dropdown( ref gui_parm, ref parm_int_values,
											 labels.ToArray(), values.ToArray(),
											 ref join_last, ref no_label_toggle_last );
			}
			else
			{
				changed = HAPI_GUI.intField( ref gui_parm, ref myDelayBuild, ref parm_int_values,
											 ref join_last, ref no_label_toggle_last );
			} // if parm.choiceCount
		} // if parm_type is INT
		///////////////////////////////////////////////////////////////////////
		// Float Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FLOAT )
		{
			changed = HAPI_GUI.floatField( ref gui_parm, ref myDelayBuild, ref parm_float_values, 
										   ref join_last, ref no_label_toggle_last );
		} // if parm_type is FLOAT
		///////////////////////////////////////////////////////////////////////
		// String Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_STRING )
		{
			string[] values = new string[ parm_size ];
			for ( int p = 0; p < parm_size; ++p )
				values[ p ] = HAPI_Host.getString( parm_string_values[ values_index + p ] );
			
			// The given string array is only for this parm so we need to set the values index to 0.
			gui_parm.valuesIndex = 0;
			
			changed = HAPI_GUI.stringField( ref gui_parm, ref myDelayBuild, ref values,
											ref join_last, ref no_label_toggle_last );
			
			if ( changed )
				for ( int p = 0; p < parm_size; ++p )
					HAPI_Host.setParmStringValue( asset_id, values[ p ], id, p );
		}
		///////////////////////////////////////////////////////////////////////
		// File Field
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FILE )
		{
			string path = HAPI_Host.getString( parm_string_values[ values_index ] );
			
			changed = HAPI_GUI.fileField( ref gui_parm, ref myDelayBuild, ref path,
										  ref join_last, ref no_label_toggle_last );
			
			if ( changed )
				HAPI_Host.setParmStringValue( asset_id, path, id, 0 );
		}
		///////////////////////////////////////////////////////////////////////
		// Toggle Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_TOGGLE )
		{
			changed = HAPI_GUI.toggle( ref gui_parm, ref parm_int_values,
									   ref join_last, ref no_label_toggle_last );
		}
		///////////////////////////////////////////////////////////////////////
		// Color Parameter
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_COLOUR )
		{
			changed = HAPI_GUI.colourField( ref gui_parm, ref myDelayBuild, ref parm_float_values,
											ref join_last, ref no_label_toggle_last );
		}
		///////////////////////////////////////////////////////////////////////
		// Separator
		else if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_SEPARATOR )
		{
			HAPI_GUI.separator();
		}
		
		if ( myAssetOTL.hasProgressBarBeenUsed() && id == myAssetOTL.prLastChangedParmId )
		{
			// TODO: Set the focus back to this control since the progress bar would have stolen it.	
		}
		
		if ( changed )
		{
			myAssetOTL.prLastChangedParmId = id;
		
			if ( parm.isInt() )
			{
				int[] temp_int_values = new int[ parm_size ];
				for ( int p = 0; p < parm_size; ++p )
					temp_int_values[ p ] = parm_int_values[ values_index + p ];
				HAPI_Host.setParmIntValues( asset_id, temp_int_values, values_index, parm_size );
			}
			else if ( parm.isFloat() )
			{
				float[] temp_float_values = new float[ parm_size ];
				for ( int p = 0; p < parm_size; ++p )
					temp_float_values[ p ] = parm_float_values[ values_index + p ];
				HAPI_Host.setParmFloatValues( asset_id, temp_float_values, values_index, parm_size );
			}
			
			// Note: String parameters update their values themselves so no need to do anything here.
		}
		
		return changed;
	}
	
	/// <summary>
	/// 	Draws all asset controls.
	/// </summary>
	/// <returns>
	/// 	<c>true</c> if any of the control values have changed from the corresponding cached parameter
	/// 	values, <c>false</c> otherwise.
	/// </returns>
	private bool generateAssetControls() 
	{
		if ( myAssetOTL.prParms == null )
			return false;
		
		bool changed 					= false;
		int current_index 				= 0;
		HAPI_ParmInfo[] parms 			= myAssetOTL.prParms;
				
		bool join_last 					= false;
		bool no_label_toggle_last 		= false;
		
		int folder_list_count 			= 0;
		
		// These stacks maintain the current folder depth, parent id, and how many more child 
		// parameters are still contained in the current folder.
		Stack< int > parent_id_stack 		= new Stack< int >();
		Stack< int > parent_count_stack 	= new Stack< int >();
		
		// Loop through all the parameters.
		while ( current_index < myAssetOTL.prParmCount )
		{
			int current_parent_id = -1; // The root has parent id -1.
			
			// If we're not at the root (empty parent stack), get the current parent id and parent 
			// count from the stack as well as decrement the parent count as we're about to parse 
			// another parameter.
			if ( parent_id_stack.Count != 0 )
		    {
				current_parent_id = parent_id_stack.Peek();
				
				if ( parent_count_stack.Count == 0 ) Debug.LogError( "" );
				
				// If the current parameter, whatever it may be, does not belong to the current active
				// parent then skip it. This check has to be done here because we do not want to
				// increment the top of the parent_count_stack as if we included a parameter in the
				// current folder.
				if ( parms[ current_index ].parentId != current_parent_id )
				{
					current_index++;
					continue;
				}				
				
				int current_parent_count = parent_count_stack.Peek();
				current_parent_count--;
				
				// If we've reached the last parameter in the current folder we need to pop the parent 
				// stacks (we're done with the current folder). Otherwise, update the top of the 
				// parent_count_stack.
				if ( current_parent_count <= 0 )
				{
					parent_id_stack.Pop();
					parent_count_stack.Pop();
				}
				else
				{
					parent_count_stack.Pop();
					parent_count_stack.Push( current_parent_count );
				}
		    }
			else if ( parms[ current_index ].parentId != current_parent_id )
			{
				// If the current parameter does not belong to the current active parent then skip it.
				current_index++;
				continue;
			}
			
			HAPI_ParmType parm_type = (HAPI_ParmType) parms[ current_index ].type;
			
			if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FOLDERLIST )
			{
				// The current parameter is a folder list which means the next parms[ current_index ].size
				// parameters will be folders belonging to this folder list. Push to the stack a new
				// folder depth, ready to eat the next few parameters belonging to the folder list's 
				// selected folder.
				
				bool folder_list_invisible	= parms[ current_index ].invisible;
				int folder_count 			= parms[ current_index ].size;
				int first_folder_index 		= current_index + 1;
				int last_folder_index 		= current_index + folder_count;
				
				// Generate the list of folders which will be passed to the GUILayout.Toolbar() method.
				List< int > 	tab_ids 	= new List< int >();
				List< string > 	tab_labels 	= new List< string >();
				List< int > 	tab_sizes 	= new List< int >();
				bool has_visible_folders	= false;
				for ( current_index = first_folder_index; current_index <= last_folder_index; ++current_index )
				{
					if ( parms[ current_index ].type != (int) HAPI_ParmType.HAPI_PARMTYPE_FOLDER )
					{
						Debug.LogError( "We should be iterating through folders only here!"
							+ "\nCurrent Index: " + current_index + ", folder_count: " + folder_count );
					}
					
					// Don't add this folder if it's invisible.
					if ( parms[ current_index ].invisible || folder_list_invisible )
						continue;
					else
						has_visible_folders = true;
					
					tab_ids.Add( 		parms[ current_index ].id );
					tab_labels.Add( 	parms[ current_index ].label );
					tab_sizes.Add( 		parms[ current_index ].size );
				}
				current_index--; // We decrement the current_index as we incremented one too many in the for loop.
				
				// If there are no folders visible in this folder list, don't even append the folder stacks.
				if ( has_visible_folders )
				{
					folder_list_count++;
					
					// If myObjectControl.myFolderListSelections is smaller than our current depth it means this
					// is the first GUI generation for this asset (no previous folder selection data) so
					// increase the size of the selection arrays to accomodate the new depth.
					if ( myAssetOTL.prFolderListSelections.Count <= folder_list_count )
					{
						myAssetOTL.prFolderListSelections.Add( 0 );
						myAssetOTL.prFolderListSelectionIds.Add( -1 );
					}
					
					int selected_folder 	= myAssetOTL.prFolderListSelections[ folder_list_count ];
					selected_folder 		= GUILayout.Toolbar( selected_folder, tab_labels.ToArray() );
					myAssetOTL.prFolderListSelections[ folder_list_count ] = selected_folder;
					
					// Push only the selected folder info to the parent stacks since for this depth and this folder
					// list only the parameters of the selected folder need to be generated.
					parent_id_stack.Push( 		tab_ids[ selected_folder ] );
					parent_count_stack.Push( 	tab_sizes[ selected_folder ] );
				}
			}
			else
			{
				// The current parameter is a simple parameter so just draw it.
				
				if ( parm_type == HAPI_ParmType.HAPI_PARMTYPE_FOLDER )
					Debug.LogError( "All folders should have been parsed in the folder list if clause!" );
				
				changed |= generateAssetControl( current_index, ref join_last, ref no_label_toggle_last );
			}
			
			current_index++;
		}
				
		return changed;
	}

	private HAPI_AssetOTL myAssetOTL;
}
