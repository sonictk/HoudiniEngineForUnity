﻿using UnityEngine;

public class HoudiniPresetMap : HoudiniMap< string, HoudiniPreset >
{
	public HoudiniPresetMap() : base( "PresetMap" ) {}

	public void set( string key, byte[] value )
	{
		if ( base.contains( key ) )
		{
			HoudiniPreset preset = base.get( key );
			preset.myPreset = new byte[ value.Length ];
			value.CopyTo( preset.myPreset, 0 );
		}
		else
		{
			HoudiniPreset preset = ScriptableObject.CreateInstance< HoudiniPreset >();
			preset.myPreset = new byte[ value.Length ];
			value.CopyTo( preset.myPreset, 0 );
			base.add( key, preset );
		}
	}

	public void add( string key, byte[] value )
	{
		this.set( key, value );
	}

	public new byte[] get( string key )
	{
		HoudiniPreset preset = base.get( key );
		return preset.myPreset;
	}
}
