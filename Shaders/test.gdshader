shader_type spatial;
render_mode blend_mix,
    cull_disabled,
    depth_prepass_alpha,
    shadows_disabled,
    specular_disabled,
    vertex_lighting;

uniform bool use_snake_motion = false; // Now controls oscillating grid warp
uniform bool use_oscillating_grid_motion = false; // Now controls snake-like movement
uniform float warp_strength : hint_range(0.0, 1.0) = 0.5; // Shared warp intensity
uniform float warp_speed : hint_range(0.0, 10.0) = 1.0; // Shared speed of warping
uniform vec4 area_colour : source_color = vec4(1.0,1.0,1.0,1.0); // Glow or outline color
uniform sampler2D albedo : source_color, filter_nearest;
uniform float alpha_scissor : hint_range(0, 1) = 0.5;

void fragment()
{
    vec2 uv = UV;

    // Ensure only one effect can be active at a time
    if (use_oscillating_grid_motion && !use_snake_motion)
    {
        // Snake-like movement
        float phase = TIME * warp_speed;
        float snake_motion_x = sin(phase + uv.y * 15.0) * warp_strength;
        float snake_motion_y = cos(phase + uv.x * 15.0) * warp_strength;

        uv.x += snake_motion_x;
        uv.y += snake_motion_y;
    }
    else if (use_snake_motion && !use_oscillating_grid_motion)
    {
        // Smooth oscillating grid warp
        uv.x += sin(TIME * warp_speed + uv.y * 10.0) * warp_strength;
        uv.y += cos(TIME * warp_speed + uv.x * 10.0) * warp_strength;
    }

    // Sample the texture with adjusted UVs
    vec4 texture_color = texture(albedo, uv);

    // Apply the texture color to the fragment
    ALBEDO = texture_color.rgb;
    ALPHA = area_colour.a;
    //ALPHA_SCISSOR_THRESHOLD = alpha_scissor;
}
