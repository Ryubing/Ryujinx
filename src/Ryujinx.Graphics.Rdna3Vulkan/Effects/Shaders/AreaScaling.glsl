// Scaling

#version 430 core
layout (local_size_x = 16, local_size_y = 16) in;
layout( rgba8, binding = 0, set = 3) uniform image2D imgOutput;
layout( binding = 1, set = 2) uniform sampler2D Source;
layout( binding = 2 ) uniform dimensions{
 float srcX0;
 float srcX1;
 float srcY0;
 float srcY1;
 float dstX0;
 float dstX1;
 float dstY0;
 float dstY1;
};

/***** Area Sampling *****/

// By Sam Belliveau and Filippo Tarpini. Public Domain license.
// Effectively a more accurate sharp bilinear filter when upscaling,
// that also works as a mathematically perfect downscale filter.
// https://entropymine.com/imageworsener/pixelmixing/
// https://github.com/obsproject/obs-studio/pull/1715
// https://legacy.imagemagick.org/Usage/filter/
vec4 AreaSampling(vec2 xy)
{
    // Determine the sizes of the source and target images.
    vec2 source_size = vec2(abs(srcX1 - srcX0), abs(srcY1 - srcY0));
    vec2 target_size = vec2(abs(dstX1 - dstX0), abs(dstY1 - dstY0));
    vec2 inverted_target_size = vec2(1.0) / target_size;

    // Compute the top-left and bottom-right corners of the target pixel box.
    vec2 t_beg = floor(xy - vec2(dstX0 < dstX1 ? dstX0 : dstX1, dstY0 < dstY1 ? dstY0 : dstY1));
    vec2 t_end = t_beg + vec2(1.0, 1.0);

    // Convert the target pixel box to source pixel box.
    vec2 beg = t_beg * inverted_target_size * source_size;
    vec2 end = t_end * inverted_target_size * source_size;

    // Compute the top-left and bottom-right corners of the pixel box.
    ivec2 f_beg = ivec2(beg);
    ivec2 f_end = ivec2(end);

    // Compute how much of the start and end pixels are covered horizontally & vertically.
    float area_w = 1.0 - fract(beg.x);
    float area_n = 1.0 - fract(beg.y);
    float area_e = fract(end.x);
    float area_s = fract(end.y);

    // Compute the areas of the corner pixels in the pixel box.
    float area_nw = area_n * area_w;
    float area_ne = area_n * area_e;
    float area_sw = area_s * area_w;
    float area_se = area_s * area_e;

    // Initialize the color accumulator.
    vec4 avg_color = vec4(0.0, 0.0, 0.0, 0.0);

    // Accumulate corner pixels.
    avg_color += area_nw * texelFetch(Source, ivec2(f_beg.x, f_beg.y), 0);
    avg_color += area_ne * texelFetch(Source, ivec2(f_end.x, f_beg.y), 0);
    avg_color += area_sw * texelFetch(Source, ivec2(f_beg.x, f_end.y), 0);
    avg_color += area_se * texelFetch(Source, ivec2(f_end.x, f_end.y), 0);

    // Determine the size of the pixel box.
    int x_range = int(f_end.x - f_beg.x - 0.5);
    int y_range = int(f_end.y - f_beg.y - 0.5);

    // Accumulate top and bottom edge pixels.
    for (int x = f_beg.x + 1; x <= f_beg.x + x_range; ++x)
    {
        avg_color += area_n * texelFetch(Source, ivec2(x, f_beg.y), 0);
        avg_color += area_s * texelFetch(Source, ivec2(x, f_end.y), 0);
    }

    // Accumulate left and right edge pixels and all the pixels in between.
    for (int y = f_beg.y + 1; y <= f_beg.y + y_range; ++y)
    {
        avg_color += area_w * texelFetch(Source, ivec2(f_beg.x, y), 0);
        avg_color += area_e * texelFetch(Source, ivec2(f_end.x, y), 0);

        for (int x = f_beg.x + 1; x <= f_beg.x + x_range; ++x)
        {
            avg_color += texelFetch(Source, ivec2(x, y), 0);
        }
    }

    // Compute the area of the pixel box that was sampled.
    float area_corners = area_nw + area_ne + area_sw + area_se;
    float area_edges = float(x_range) * (area_n + area_s) + float(y_range) * (area_w + area_e);
    float area_center = float(x_range) * float(y_range);

    // Return the normalized average color.
    return avg_color / (area_corners + area_edges + area_center);
}

float insideBox(vec2 v, vec2 bLeft, vec2 tRight) {
    vec2 s = step(bLeft, v) - step(tRight, v);
    return s.x * s.y;
}

vec2 translateDest(vec2 pos) {
    vec2 translatedPos = vec2(pos.x, pos.y);
    translatedPos.x = dstX1 < dstX0 ? dstX1 - translatedPos.x : translatedPos.x;
    translatedPos.y = dstY0 < dstY1 ? dstY1 + dstY0 - translatedPos.y - 1 : translatedPos.y;
    return translatedPos;
}

void main()
{
    vec2 bLeft = vec2(dstX0 < dstX1 ? dstX0 : dstX1, dstY0 < dstY1 ? dstY0 : dstY1);
    vec2 tRight = vec2(dstX1 > dstX0 ? dstX1 : dstX0, dstY1 > dstY0 ? dstY1 : dstY0);
    ivec2 loc = ivec2(gl_GlobalInvocationID.x, gl_GlobalInvocationID.y);
    if (insideBox(loc, bLeft, tRight) == 0) {
        imageStore(imgOutput, loc, vec4(0, 0, 0, 1));
        return;
    }

    vec4 outColor = AreaSampling(loc);
    imageStore(imgOutput, ivec2(translateDest(loc)), vec4(outColor.rgb, 1));
}
