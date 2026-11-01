shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(0.3, 1.0, 0.4, 1.0);
uniform vec4 line_color_attack : source_color = vec4(1.0, 0.2, 0.2, 1.0);

uniform float line_thickness : hint_range(0, 10) = 1.0;

uniform bool enabled = false;
uniform bool attack = false;
uniform bool hit = false;

const vec2 OFFSETS[8] = {
    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1),
    vec2(0, -1), vec2(0, 1),
    vec2(1, -1), vec2(1, 0), vec2(1, 1)
};

void fragment() {
    vec4 tex = COLOR;

    if (enabled || attack) {
        vec2 size = TEXTURE_PIXEL_SIZE * line_thickness;

        float outline = 0.0;

        for (int i = 0; i < OFFSETS.length(); i++) {
            outline += texture(TEXTURE, UV + size * OFFSETS[i]).a;
        }

        outline = min(outline, 1.0);

        float tex_alpha = texture(TEXTURE, UV).a;
        float mask = max(0.0, outline - tex_alpha);

        vec4 selected_color = attack ? line_color_attack : line_color;

        COLOR = mix(tex, selected_color, mask);
    }

    // Hit накладывается поверх результата существующей логики
    if (hit) {
        COLOR.rgb = vec3(1.0);
    }
}