using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomContainerColors
{
    internal static class ColorRendering
    {
        internal static void Apply(UnityEngine.Component world)
        {
            if (!ColorData.Supports(FoundationApi.WorkspaceAccess.FromView(world)))
                return;
            var tracker = world.GetComponent<ColorBackground>();
            if (!ColorData.TryGet(FoundationApi.WorkspaceAccess.FromView(world), out var color))
            {
                if (tracker && tracker.Applied)
                {
                    tracker.Image.color = tracker.NativeTint;
                    tracker.Applied = false;
                }

                return;
            }

            var image = (Image)FoundationApi.Decorations.Background(world);
            if (!image)
                return;
            EnsurePalette(world);
            var neutral = NeutralFor(world);
            if (!neutral)
                return;
            if (!tracker)
            {
                tracker = world.gameObject.AddComponent<ColorBackground>();
                tracker.Image = image;
                tracker.NativeTint = image.color;
            }

            image.sprite = neutral;
            image.color = color;
            tracker.Applied = true;
        }

        internal static Sprite Neutral { get; private set; }
        internal static Sprite LabelNeutral { get; private set; }

        static readonly Texture2D[] textures = new Texture2D[2];
        static Sprite NeutralFor(UnityEngine.Component world) => FoundationApi.WorkspaceAccess.FromView(world).Label != null ? LabelNeutral : Neutral;
        internal static void EnsurePalette(UnityEngine.Component world)
        {
            var item = FoundationApi.WorkspaceAccess.FromView(world);
            if (!ColorData.Supports(item) || NeutralFor(world))
                return;
            int family = item.Label != null ? 1 : 0;
            var palette = FoundationApi.Decorations.BackgroundPalette(world);
            if (palette == null || palette.Length == 0)
                return;
            var sampled = new Color[palette.Length];
            int neutralIndex = palette.Length > 1 ? 1 : 0;
            for (int i = 0; i < palette.Length; i++)
            {
                var copy = ReadSprite(palette[i]);
                sampled[i] = copy.GetPixel(copy.width / 2, copy.height / 2);
                if (i == neutralIndex)
                {
                    var pixels = copy.GetPixels32();
                    var center = sampled[i];
                    float level = Mathf.Max(.01f, center.grayscale);
                    for (int j = 0; j < pixels.Length; j++)
                    {
                        byte gray = (byte)Mathf.RoundToInt(Mathf.Clamp01(((Color)pixels[j]).grayscale / level) * 255);
                        pixels[j] = new Color32(gray, gray, gray, pixels[j].a);
                    }

                    copy.name = "ContainerColorsNeutralTexture";
                    copy.SetPixels32(pixels);
                    copy.Apply(false, true);
                    textures[family] = copy;
                    var neutral = Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(.5f, .5f), palette[i].pixelsPerUnit, 0, SpriteMeshType.FullRect, palette[i].border);
                    neutral.name = family == 1 ? "ContainerColorsLabelNeutral" : "ContainerColorsNeutral";
                    if (family == 1)
                        LabelNeutral = neutral;
                    else
                        Neutral = neutral;
                }
                else
                    global::UnityEngine.Object.Destroy(copy);
            }
            ColorData.SetPalette(item, sampled);
        }

        static Texture2D ReadSprite(Sprite sprite)
        {
            var source = sprite.texture;
            var temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                var rect = sprite.rect;
                var result = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                result.ReadPixels(rect, 0, 0);
                result.Apply();
                result.filterMode = FilterMode.Point;
                result.wrapMode = TextureWrapMode.Clamp;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        internal static void Release()
        {
            foreach (var tracker in global::UnityEngine.Object.FindObjectsByType<ColorBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tracker.Image)
                    tracker.Image.color = tracker.NativeTint;
                tracker.Applied = false;
                var world = FoundationApi.Decorations.View(tracker.gameObject);
                if (world)
                    FoundationApi.Decorations.RefreshStyle(world);
                global::UnityEngine.Object.Destroy(tracker);
            }

            if (Neutral)
                global::UnityEngine.Object.Destroy(Neutral);
            if (LabelNeutral)
                global::UnityEngine.Object.Destroy(LabelNeutral);
            Neutral = null;
            LabelNeutral = null;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i])
                    global::UnityEngine.Object.Destroy(textures[i]);
                textures[i] = null;
            }
        }
    }
}
