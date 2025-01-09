shader_type sky;
render_mode use_quarter_res_pass;

// Originaly based on https://godotshaders.com/shader/stylized-sky-shader-with-clouds/ but there's not much left

group_uniforms sky;
	uniform vec3 day_top_color : source_color = vec3( 0.1, 0.6, 1.0 );
	uniform vec3 day_bottom_color : source_color = vec3( 0.4, 0.8, 1.0 );
	uniform vec3 sunset_top_color : source_color = vec3( 0.7, 0.75, 1.0 );
	uniform vec3 sunset_bottom_color : source_color = vec3( 1.0, 0.5, 0.7 );
	uniform vec3 night_top_color : source_color = vec3( 0.02, 0.0, 0.04 );
	uniform vec3 night_bottom_color : source_color = vec3( 0.1, 0.0, 0.2 );

group_uniforms sun; // First DirectionalLight3D will be the sun
	uniform vec3 sun_color : source_color = vec3( 10.0, 8.0, 1.0 );
	uniform vec3 sun_sunset_color : source_color = vec3( 10.0, 0.0, 0.0 );
	uniform float sun_size : hint_range( 0.01, 1.0 ) = 0.2;
	uniform float sun_blur : hint_range( 0.01, 20.0 ) = 10.0;

group_uniforms moon; // Second DirectionalLight3D will be the moon
	uniform vec3 moon_color : source_color = vec3( 1.0, 0.95, 0.7 );
	uniform float moon_size : hint_range( 0.01, 1.0 ) = 0.06;
	uniform float moon_blur : hint_range( 0.01, 10.0 ) = 0.1;

group_uniforms settings;
	uniform float overwritten_time = 0.0;

void sky()
{
	float time = overwritten_time != 0.0 ? overwritten_time : TIME;

	//////////////////// SKY ///////////////////////////////////////////////////////////////////////
	float _eyedir_y = abs( sin( EYEDIR.y * PI * 0.5 ));

	// The day color will be our base color
	vec3 _sky_color = mix( day_bottom_color, day_top_color, _eyedir_y );
	_sky_color = mix( _sky_color, vec3( 0.0 ), clamp((0.7 - 1.0) * 0.0, 0.0, 1.0 ));

	float _sunset_amount = clamp( 0.5 - abs( LIGHT0_DIRECTION.y ), 0.0, 0.5 ) * 2.0;
	// The sky should be more red around the west, on the opposite side you don't see it as much
	float _sunset_distance = clamp( 1.0 - pow( distance( EYEDIR, LIGHT0_DIRECTION ), 2 ), 0.0, 1.0 );
	vec3 _sky_sunset_color = mix( sunset_bottom_color, sunset_top_color, _eyedir_y + 0.5 );
	_sky_sunset_color = mix( _sky_sunset_color, sunset_bottom_color, _sunset_amount * _sunset_distance );
	_sky_color = mix( _sky_color, _sky_sunset_color, _sunset_amount );

	float _night_amount = clamp( -LIGHT0_DIRECTION.y + 0.7, 0.0, 1.0 );
	vec3 _sky_night_color = mix( night_bottom_color, night_top_color, _eyedir_y );
	_sky_color = mix( _sky_color, _sky_night_color, _night_amount );

	// Final sky color
	COLOR = _sky_color;

	//////////////////// SUN ///////////////////////////////////////////////////////////////////////
	float _sun_distance = 0.0;
	if( LIGHT0_ENABLED )
	{
		_sun_distance = distance( EYEDIR, LIGHT0_DIRECTION );
		// Bigger sun near the horizon
		float _sun_size = sun_size + cos( LIGHT0_DIRECTION.y * PI ) * sun_size * 0.25;
		// Finding sun disc and edge blur
		float _sun_amount = clamp(( 1.0 - _sun_distance / _sun_size ) / sun_blur, 0.0, 1.0 );
		if( _sun_amount > 0.0 )
		{
			// Changing color of the sun during sunset
			float _sunset_amount = 1.0;
			if( LIGHT0_DIRECTION.y > 0.0 )
				_sunset_amount = clamp( cos( LIGHT0_DIRECTION.y * PI ), 0.0, 1.0 );
			vec3 _sun_color = mix( sun_color, sun_sunset_color, _sunset_amount );
			// Hiding the sun behind the moon
			_sun_amount = clamp( _sun_amount * ( 1.0 - 0.0 ), 0.0, 1.0 );
			// Hiding the sun behind the horizon
			_sun_amount *= 1.0 - 0.0;
			// Leveling the "glow" in color
			if( _sun_color.r > 1.0 || _sun_color.g > 1.0 || _sun_color.b > 1.0 )
				_sun_color *= _sun_amount;
			COLOR = mix( COLOR, _sun_color, _sun_amount );
		}
	}
}