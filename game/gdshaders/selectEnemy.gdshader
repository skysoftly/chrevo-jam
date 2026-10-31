shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(0.3, 1.0, 0.4, 1.0);
uniform vec4 line_color_attack : source_color = vec4(1.0, 0.2, 0.2, 1.0);

uniform float line_thickness : hint_range(0, 10) = 1.0;

uniform bool enabled = false;
uniform bool attack = false;

const vec2 OFFSETS[8] = {
    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1),
    vec2(0, -1), vec2(0, 1),
    vec2(1, -1), vec2(1, 0), vec2(1, 1)
};

void fragment() {
    vec4 color = texture(TEXTURE, UV);

    if (!enabled && !attack) {
        COLOR = color;
    }
	else{
	    vec2 size = TEXTURE_PIXEL_SIZE * line_thickness;
	    float outline = 0.0;

		for (int i = 0; i < OFFSETS.length(); i++) {
	        outline += texture(TEXTURE, UV + size * OFFSETS[i]).a;
	    }

	    outline = min(outline, 1.0);

	    vec4 selected_color = attack ? line_color_attack : line_color;

	    COLOR = mix(color, selected_color, max(0.0, outline - color.a));
	}
}